using System.Globalization;

using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Infrastructure.Dat.Models;

using NetTopologySuite.Geometries;

using Location = FeBuddy.Core.Domain.Geo.Models.Location;

namespace FeBuddy.Core.Infrastructure.Dat;

/// <summary>
/// Reads an FAA <c>.dat</c> RADAR Video Map (RVM) file: its point of tangency and its lines.
/// </summary>
/// <remarks>
/// <para>
/// The file is a header followed by <c>LINE</c> blocks, and <c>!</c> starts a comment:
/// </para>
/// <code>
/// !   Filename:    example.dgn
/// !   9900    40 00 00.0000  100 00 00.0000
/// LINE !
/// GP 40 10 00.0000  100 10 00.0000  !
/// GP 40 12 30.5000  100 05 00.0000  !
/// </code>
/// <para>
/// In the header, the record holding <c>9900</c> - itself a comment - is the point of tangency;
/// the other 99xx reference records are not used. After the first <c>LINE</c> marker, every
/// record is either another <c>LINE</c>, which starts a new line, or one point on the current
/// line.
/// </para>
/// <para>
/// A point is degrees, minutes and seconds of latitude, then of longitude, after one leading
/// label (<c>GP</c>). Hemisphere letters are accepted after each value but FAA files do not
/// write them: a point without them is north and west, as every FAA facility map outside the
/// Pacific is. Seconds may be written as <c>60</c>, which the FAA does where a value rounds up.
/// </para>
/// <para>
/// A record that cannot be read is reported in <see cref="DatFile.Problems"/> and skipped, so one
/// bad record never loses the rest of the map. A <c>LINE</c> block of fewer than two points - FAA
/// files routinely start with a one-point block at the point of tangency - draws nothing; it is
/// counted in <see cref="DatFile.ShortLineBlocks"/> rather than reported.
/// </para>
/// </remarks>
public static class DatFileReader
{
	private const string LineMarker = "LINE";
	private const string PointOfTangencyMarker = "9900";
	private const char CommentMarker = '!';

	/// <summary>Reads a <c>.dat</c> file from disk.</summary>
	/// <param name="path">The file to read.</param>
	/// <returns>The file's point of tangency, lines and any records that could not be used.</returns>
	/// <exception cref="IOException">Thrown when the file cannot be read.</exception>
	public static DatFile Read(string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		return Parse(File.ReadLines(path), path);
	}

	/// <summary>Reads <c>.dat</c> content that is already in memory.</summary>
	/// <param name="records">The file's lines, in order.</param>
	/// <param name="sourcePath">What to call the source in the result, usually its path.</param>
	/// <returns>The content's point of tangency, lines and any records that could not be used.</returns>
	public static DatFile Parse(IEnumerable<string> records, string sourcePath)
	{
		ArgumentNullException.ThrowIfNull(records);

		Location? pointOfTangency = null;
		List<LineString> lines = [];
		List<string> problems = [];
		List<Coordinate>? current = null;
		int shortBlocks = 0;
		int lineNumber = 0;

		foreach (string record in records)
		{
			lineNumber++;

			// Still in the header: only the point of tangency matters, and it is written in a comment.
			if (current is null && pointOfTangency is null && record.Contains(PointOfTangencyMarker, StringComparison.Ordinal))
			{
				string coordinates = StripComment(record[(record.IndexOf(PointOfTangencyMarker, StringComparison.Ordinal) + PointOfTangencyMarker.Length)..]);
				pointOfTangency = TryParsePoint(coordinates, out double latitude, out double longitude)
					? new Location(latitude, longitude)
					: null;

				if (pointOfTangency is null)
				{
					problems.Add($"Line {lineNumber}: the point of tangency '{record.Trim()}' could not be read.");
				}

				continue;
			}

			string text = StripComment(record);

			if (text.Contains(LineMarker, StringComparison.OrdinalIgnoreCase))
			{
				shortBlocks += FinishLine(current, lines);
				current = [];
				continue;
			}

			if (current is null || string.IsNullOrWhiteSpace(text))
			{
				continue;
			}

			if (TryParsePoint(text, out double lat, out double lon))
			{
				current.Add(new Coordinate(lon, lat));
			}
			else
			{
				problems.Add($"Line {lineNumber}: '{record.Trim()}' is not a coordinate and was skipped.");
			}
		}

		shortBlocks += FinishLine(current, lines);

		return new DatFile(sourcePath, pointOfTangency, lines, shortBlocks, problems);
	}

	/// <summary>
	/// Reads one point: latitude degrees, minutes, seconds and optional <c>N</c>/<c>S</c>, then
	/// longitude degrees, minutes, seconds and optional <c>E</c>/<c>W</c>, with at most one
	/// leading label token.
	/// </summary>
	/// <param name="text">The record.</param>
	/// <param name="latitude">The latitude in decimal degrees.</param>
	/// <param name="longitude">The longitude in decimal degrees.</param>
	/// <returns><see langword="true"/> when the record is a valid point.</returns>
	internal static bool TryParsePoint(string text, out double latitude, out double longitude)
	{
		latitude = 0;
		longitude = 0;

		// Read from the right: the longitude always ends the record, and whatever is left of the
		// latitude is the optional label.
		Stack<string> tokens = new(text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

		char longitudeHemisphere = TakeHemisphere(tokens, 'E', 'W') ?? 'W';

		if (!TryTakeDms(tokens, maxDegrees: 180, out double lonDegrees))
		{
			return false;
		}

		char latitudeHemisphere = TakeHemisphere(tokens, 'N', 'S') ?? 'N';

		if (!TryTakeDms(tokens, maxDegrees: 90, out double latDegrees) || tokens.Count > 1)
		{
			return false;
		}

		latitude = latitudeHemisphere == 'S' ? -latDegrees : latDegrees;
		longitude = longitudeHemisphere == 'W' ? -lonDegrees : lonDegrees;
		return true;
	}

	private static char? TakeHemisphere(Stack<string> tokens, char positive, char negative)
	{
		if (!tokens.TryPeek(out string? token) || token.Length != 1)
		{
			return null;
		}

		char letter = char.ToUpperInvariant(token[0]);

		if (letter != positive && letter != negative)
		{
			return null;
		}

		tokens.Pop();
		return letter;
	}

	/// <summary>Takes seconds, minutes and degrees off the top of the stack (they were pushed in reading order).</summary>
	private static bool TryTakeDms(Stack<string> tokens, int maxDegrees, out double degrees)
	{
		degrees = 0;

		if (tokens.Count < 3
			|| !double.TryParse(tokens.Pop(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double seconds)
			|| !int.TryParse(tokens.Pop(), NumberStyles.None, CultureInfo.InvariantCulture, out int minutes)
			|| !int.TryParse(tokens.Pop(), NumberStyles.None, CultureInfo.InvariantCulture, out int wholeDegrees)
			|| seconds > 60
			|| minutes >= 60)
		{
			return false;
		}

		degrees = wholeDegrees + minutes / 60d + seconds / 3600;
		return degrees <= maxDegrees;
	}

	private static string StripComment(string record)
	{
		int comment = record.IndexOf(CommentMarker);
		return comment < 0 ? record : record[..comment];
	}

	/// <summary>Adds the block's line; returns 1 when the block is too short to draw, otherwise 0.</summary>
	private static int FinishLine(List<Coordinate>? points, List<LineString> lines)
	{
		if (points is null)
		{
			return 0;
		}

		if (points.Count < 2)
		{
			return 1;
		}

		lines.Add(Wgs84.Factory.CreateLineString([.. points]));
		return 0;
	}
}
