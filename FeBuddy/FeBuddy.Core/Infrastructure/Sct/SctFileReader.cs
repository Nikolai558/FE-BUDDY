using System.Globalization;
using System.Text.RegularExpressions;

using FeBuddy.Core.Infrastructure.Sct.Models;

using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Infrastructure.Sct;

/// <summary>
/// Reads a VRC sector file (<c>.sct2</c> / <c>.sct</c>): its boundary, airway and GEO segments,
/// SID and STAR diagrams, labels and regions.
/// </summary>
/// <remarks>
/// <para>
/// A sector file is a set of <c>[SECTION]</c>s. A coordinate is written as DMS -
/// <c>N040.38.23.000 W073.46.42.000</c> - or as the name of a point defined in <c>[VOR]</c>,
/// <c>[NDB]</c>, <c>[AIRPORT]</c> or <c>[FIXES]</c>; names are resolved against those sections
/// wherever they appear in the file. Text after <c>;</c> or <c>//</c> is a comment.
/// </para>
/// <para>
/// Layouts are read the way VRC reads them, with a fallback for the variations real files have:
/// a boundary or airway record is its name then four coordinates (or a multi-word name followed
/// by four coordinates); a SID/STAR diagram starts with a name in its first 26 columns (or before
/// its last four coordinates and colour), and a record that starts with white space continues the
/// diagram above it. <c>[REGIONS]</c> entries start with a colour name and a point, and continue
/// with one point per line.
/// </para>
/// <para>
/// A record that cannot be read is reported in <see cref="SctFile.Problems"/> and skipped, so one
/// bad record never loses the rest of the file. Colours and the <c>[INFO]</c> section are not
/// read: CRC styles lines through its own properties, not VRC colours.
/// </para>
/// </remarks>
public static partial class SctFileReader
{
	private static readonly IReadOnlyDictionary<string, SctLineSection> LineSections =
		new Dictionary<string, SctLineSection>(StringComparer.OrdinalIgnoreCase)
		{
			["ARTCC"] = SctLineSection.Artcc,
			["ARTCC HIGH"] = SctLineSection.ArtccHigh,
			["ARTCC LOW"] = SctLineSection.ArtccLow,
			["LOW AIRWAY"] = SctLineSection.LowAirway,
			["HIGH AIRWAY"] = SctLineSection.HighAirway,
			["GEO"] = SctLineSection.Geo,
		};

	/// <summary>How wide the name column of a SID/STAR diagram header is, in VRC's layout.</summary>
	private const int DiagramNameWidth = 26;

	/// <summary>Reads a sector file from disk.</summary>
	/// <param name="path">The file to read.</param>
	/// <returns>The file's drawable content and any records that could not be used.</returns>
	/// <exception cref="IOException">Thrown when the file cannot be read.</exception>
	public static SctFile Read(string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		return Parse(File.ReadLines(path), path);
	}

	/// <summary>Reads sector-file content that is already in memory.</summary>
	/// <param name="records">The file's lines, in order.</param>
	/// <param name="sourcePath">What to call the source in the result, usually its path.</param>
	/// <returns>The content's drawable parts and any records that could not be used.</returns>
	public static SctFile Parse(IEnumerable<string> records, string sourcePath)
	{
		ArgumentNullException.ThrowIfNull(records);

		List<Record> all = Sectioned(records);
		List<string> problems = [];

		// Names can be used before the section defining them, so every point is known up front.
		Dictionary<string, Coordinate> points = ReadNamedPoints(all);

		Dictionary<SctLineSection, List<SctSegment>> lines = [];
		List<SctSegment> sids = [];
		List<SctSegment> stars = [];
		List<SctLabel> labels = [];
		List<SctRegion> regions = [];
		string? diagram = null;
		RegionBuilder? region = null;

		foreach (Record record in all)
		{
			if (LineSections.TryGetValue(record.Section, out SctLineSection lineSection))
			{
				if (!lines.TryGetValue(lineSection, out List<SctSegment>? list))
				{
					list = lines[lineSection] = [];
				}

				ReadLine(record, lineSection, points, list, problems);
			}
			else if (record.Section is "SID" or "STAR")
			{
				diagram = ReadDiagram(record, diagram, points, record.Section == "SID" ? sids : stars, problems);
			}
			else if (record.Section == "LABELS")
			{
				ReadLabel(record, points, labels, problems);
			}
			else if (record.Section == "REGIONS")
			{
				region = ReadRegion(record, region, points, regions, problems);
			}
		}

		region?.AddTo(regions, problems);

		return new SctFile(
			sourcePath,
			lines.Where(pair => pair.Value.Count > 0).ToDictionary(pair => pair.Key, pair => (IReadOnlyList<SctSegment>)pair.Value),
			sids,
			stars,
			labels,
			regions,
			problems);
	}

	/// <summary>
	/// Reads one DMS latitude or longitude, e.g. <c>N040.38.23.000</c> or <c>W73.46.42.5</c>.
	/// </summary>
	/// <param name="text">The value.</param>
	/// <param name="isLatitude">Whether it must be a latitude (N/S) rather than a longitude (E/W).</param>
	/// <param name="degrees">The value in decimal degrees, negative for S and W.</param>
	/// <returns><see langword="true"/> when the text is a valid value of the right kind.</returns>
	internal static bool TryParseDms(string text, bool isLatitude, out double degrees)
	{
		degrees = 0;
		Match match = DmsPattern().Match(text);

		if (!match.Success)
		{
			return false;
		}

		char hemisphere = char.ToUpperInvariant(match.Groups["h"].Value[0]);

		if (isLatitude != (hemisphere is 'N' or 'S'))
		{
			return false;
		}

		int whole = int.Parse(match.Groups["d"].Value, CultureInfo.InvariantCulture);
		int minutes = int.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture);
		double seconds = double.Parse($"{match.Groups["s"].Value}.{match.Groups["f"].Value}0", CultureInfo.InvariantCulture);

		if (minutes >= 60 || seconds >= 60)
		{
			return false;
		}

		degrees = whole + minutes / 60d + seconds / 3600;

		if (degrees > (isLatitude ? 90 : 180))
		{
			return false;
		}

		if (hemisphere is 'S' or 'W')
		{
			degrees = -degrees;
		}

		return true;
	}

	// ================= sections =================

	/// <summary>One usable record: its section, line number, text with comments removed, and whether it was indented.</summary>
	private readonly record struct Record(string Section, int LineNumber, string Text, bool IsIndented, string Raw);

	/// <summary>Splits the file into records tagged with their section, dropping blanks, comments and section headers.</summary>
	private static List<Record> Sectioned(IEnumerable<string> records)
	{
		List<Record> result = [];
		string section = string.Empty;
		int lineNumber = 0;

		foreach (string raw in records)
		{
			lineNumber++;
			Match header = SectionHeaderPattern().Match(raw);

			if (header.Success)
			{
				section = header.Groups["name"].Value.Trim().ToUpperInvariant();
				continue;
			}

			string text = StripComment(raw).Trim();

			if (text.Length == 0 || section.Length == 0)
			{
				continue;
			}

			result.Add(new Record(section, lineNumber, text, char.IsWhiteSpace(raw[0]), raw));
		}

		return result;
	}

	private static string StripComment(string raw)
	{
		int semicolon = raw.IndexOf(';');
		int slashes = raw.IndexOf("//", StringComparison.Ordinal);
		int cut = semicolon < 0 ? slashes : slashes < 0 ? semicolon : Math.Min(semicolon, slashes);
		return cut < 0 ? raw : raw[..cut];
	}

	private static string[] Tokens(string text) =>
		text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

	/// <summary>Every point named in <c>[VOR]</c>, <c>[NDB]</c>, <c>[AIRPORT]</c> and <c>[FIXES]</c>; the first definition of a name wins.</summary>
	private static Dictionary<string, Coordinate> ReadNamedPoints(List<Record> records)
	{
		Dictionary<string, Coordinate> points = new(StringComparer.OrdinalIgnoreCase);

		foreach (Record record in records)
		{
			// VOR, NDB and AIRPORT: id, frequency, lat, lon. FIXES: name, lat, lon.
			int latAt = record.Section switch
			{
				"VOR" or "NDB" or "AIRPORT" => 2,
				"FIXES" => 1,
				_ => -1,
			};

			string[] tokens = Tokens(record.Text);

			if (latAt < 0 || tokens.Length < latAt + 2)
			{
				continue;
			}

			if (TryParseDms(tokens[latAt], isLatitude: true, out double lat)
				&& TryParseDms(tokens[latAt + 1], isLatitude: false, out double lon))
			{
				points.TryAdd(tokens[0], new Coordinate(lon, lat));
			}
		}

		return points;
	}

	/// <summary>Resolves a latitude and longitude, each either DMS or a named point.</summary>
	private static bool TryResolve(string latText, string lonText, Dictionary<string, Coordinate> points, out Coordinate coordinate)
	{
		coordinate = null!;

		double? lat = TryParseDms(latText, isLatitude: true, out double dmsLat) ? dmsLat
			: points.TryGetValue(latText, out Coordinate? latPoint) ? latPoint.Y
			: null;

		double? lon = TryParseDms(lonText, isLatitude: false, out double dmsLon) ? dmsLon
			: points.TryGetValue(lonText, out Coordinate? lonPoint) ? lonPoint.X
			: null;

		if (lat is null || lon is null)
		{
			return false;
		}

		coordinate = new Coordinate(lon.Value, lat.Value);
		return true;
	}

	/// <summary>Reads the segment whose four coordinates start at <paramref name="at"/>.</summary>
	private static bool TryReadSegment(
		string[] tokens,
		int at,
		Dictionary<string, Coordinate> points,
		out Coordinate start,
		out Coordinate end)
	{
		end = null!;

		return TryResolve(tokens[at], tokens[at + 1], points, out start)
			&& TryResolve(tokens[at + 2], tokens[at + 3], points, out end);
	}

	// ================= records =================

	private static void ReadLine(
		Record record,
		SctLineSection section,
		Dictionary<string, Coordinate> points,
		List<SctSegment> segments,
		List<string> problems)
	{
		string[] tokens = Tokens(record.Text);

		// GEO is four coordinates and an optional colour; the others are a name then four
		// coordinates. Failing the usual layout, a name of several words before the last four.
		(int At, Func<string> Name)[] layouts = section == SctLineSection.Geo
			? [(0, () => string.Empty), (1, () => string.Empty)]
			: [(1, () => tokens[0]), (tokens.Length - 4, () => string.Join(' ', tokens[..^4]))];

		foreach ((int at, Func<string> name) in layouts)
		{
			if (at >= 0 && tokens.Length >= at + 4
				&& TryReadSegment(tokens, at, points, out Coordinate start, out Coordinate end))
			{
				segments.Add(new SctSegment(name(), start, end));
				return;
			}
		}

		problems.Add(Unreadable(record));
	}

	/// <summary>Reads one SID/STAR record and returns the diagram the next record continues.</summary>
	private static string? ReadDiagram(
		Record record,
		string? diagram,
		Dictionary<string, Coordinate> points,
		List<SctSegment> segments,
		List<string> problems)
	{
		string[] tokens = Tokens(record.Text);

		if (record.IsIndented)
		{
			if (diagram is not null && tokens.Length >= 4
				&& TryReadSegment(tokens, 0, points, out Coordinate start, out Coordinate end))
			{
				segments.Add(new SctSegment(diagram, start, end));
			}
			else
			{
				problems.Add(diagram is null
					? $"Line {record.LineNumber}: [{record.Section}] '{record.Text}' continues a diagram, but no diagram has started."
					: Unreadable(record));
			}

			return diagram;
		}

		// VRC's layout: the name fills the first 26 columns, then four coordinates.
		string raw = StripComment(record.Raw).TrimEnd();

		if (raw.Length > DiagramNameWidth)
		{
			string name = raw[..DiagramNameWidth].Trim();
			string[] rest = Tokens(raw[DiagramNameWidth..]);

			if (rest.Length >= 4 && TryReadSegment(rest, 0, points, out Coordinate start, out Coordinate end))
			{
				segments.Add(new SctSegment(name, start, end));
				return name;
			}
		}

		// Tab-separated or short names: the name is whatever comes before the last four
		// coordinates, or before the last four and a colour.
		foreach (int trailing in (int[])[4, 5])
		{
			int at = tokens.Length - trailing;

			if (at >= 1 && TryReadSegment(tokens, at, points, out Coordinate start, out Coordinate end))
			{
				string name = string.Join(' ', tokens[..at]);
				segments.Add(new SctSegment(name, start, end));
				return name;
			}
		}

		problems.Add(Unreadable(record));

		// The records below this one cannot be told which diagram they belong to.
		return null;
	}

	private static void ReadLabel(
		Record record,
		Dictionary<string, Coordinate> points,
		List<SctLabel> labels,
		List<string> problems)
	{
		// The quoted text is taken from the raw record, so a ';' inside the quotes is not a
		// comment; only what follows the closing quote has its comment removed.
		Match match = LabelPattern().Match(record.Raw);
		string[] rest = match.Success ? Tokens(StripComment(match.Groups["rest"].Value)) : [];

		if (rest.Length >= 2 && TryResolve(rest[0], rest[1], points, out Coordinate position))
		{
			labels.Add(new SctLabel(match.Groups["text"].Value, position));
			return;
		}

		problems.Add(Unreadable(record));
	}

	/// <summary>Reads one REGIONS record and returns the region the next record continues.</summary>
	private static RegionBuilder? ReadRegion(
		Record record,
		RegionBuilder? region,
		Dictionary<string, Coordinate> points,
		List<SctRegion> regions,
		List<string> problems)
	{
		string[] tokens = Tokens(record.Text);

		// A point on its own continues the region, indented or not.
		if (tokens.Length == 2 && TryResolve(tokens[0], tokens[1], points, out Coordinate next))
		{
			if (region is null)
			{
				problems.Add($"Line {record.LineNumber}: [REGIONS] '{record.Text}' continues a region, but no region has started.");
			}

			region?.Points.Add(next);
			return region;
		}

		if (!record.IsIndented)
		{
			region?.AddTo(regions, problems);

			// A colour name and the first point; or just a name, with the points to follow.
			Coordinate? first = tokens.Length >= 3 && TryResolve(tokens[^2], tokens[^1], points, out Coordinate point)
				? point
				: null;

			RegionBuilder started = new(string.Join(' ', first is null ? tokens : tokens[..^2]), record.LineNumber);

			if (first is not null)
			{
				started.Points.Add(first);
			}

			return started;
		}

		problems.Add(Unreadable(record));
		return region;
	}

	private static string Unreadable(Record record) =>
		$"Line {record.LineNumber}: [{record.Section}] '{record.Text}' could not be read and was skipped.";

	/// <summary>A region being read, until the next one starts.</summary>
	private sealed class RegionBuilder(string name, int lineNumber)
	{
		public List<Coordinate> Points { get; } = [];

		/// <summary>Finishes the region: kept when it has an area, reported when it cannot.</summary>
		public void AddTo(List<SctRegion> regions, List<string> problems)
		{
			if (Points.Distinct().Count() < 3)
			{
				problems.Add($"Line {lineNumber}: [REGIONS] '{name}' has fewer than three points, so it has no area and was skipped.");
				return;
			}

			regions.Add(new SctRegion(name, Points));
		}
	}

	[GeneratedRegex(@"^\s*\[(?<name>[^\]]+)\]\s*(;.*)?$")]
	private static partial Regex SectionHeaderPattern();

	[GeneratedRegex(@"^(?<h>[NSEWnsew])(?<d>\d{1,3})\.(?<m>\d{1,2})\.(?<s>\d{1,2})(?:\.(?<f>\d+))?$")]
	private static partial Regex DmsPattern();

	[GeneratedRegex(@"^\s*""(?<text>[^""]*)""(?<rest>.*)$")]
	private static partial Regex LabelPattern();
}
