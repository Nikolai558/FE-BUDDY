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
/// The file is a header followed by <c>LINE</c> blocks. In the header, the record containing
/// <c>9900</c> holds the point of tangency. After the first <c>LINE</c> marker, every record is
/// either another <c>LINE</c> marker, which starts a new line, or one point on the current line.
/// </para>
/// <para>
/// A point is written as degrees, minutes and seconds of latitude, then of longitude, e.g.
/// <c>40 38 23.00 N 073 46 42.00 W</c>. The hemisphere letters are optional: without them a
/// point is north and west, as every FAA facility map outside the Pacific is. A record may carry
/// one leading label token before the latitude, which is ignored.
/// </para>
/// <para>
/// A record that cannot be read is reported in <see cref="DatFile.Problems"/> and skipped, so one
/// bad record never loses the rest of the map.
/// </para>
/// </remarks>
public static class DatFileReader
{
	private const string LineMarker = "LINE";
	private const string PointOfTangencyMarker = "9900";

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
		int currentStartsAt = 0;
		int lineNumber = 0;

		foreach (string record in records)
		{
			lineNumber++;

			if (record.Contains(LineMarker, StringComparison.OrdinalIgnoreCase))
			{
				FinishLine(current, currentStartsAt, lines, problems);
				current = [];
				currentStartsAt = lineNumber;
				continue;
			}

			if (string.IsNullOrWhiteSpace(record))
			{
				continue;
			}

			// Still in the header: only the point of tangency matters here.
			if (current is null)
			{
				if (pointOfTangency is null && record.Contains(PointOfTangencyMarker, StringComparison.Ordinal))
				{
					string coordinates = record[(record.IndexOf(PointOfTangencyMarker, StringComparison.Ordinal) + PointOfTangencyMarker.Length)..];
					pointOfTangency = TryParsePoint(coordinates, out double latitude, out double longitude)
						? new Location(latitude, longitude)
						: null;

					if (pointOfTangency is null)
					{
						problems.Add($"Line {lineNumber}: the point of tangency '{record.Trim()}' could not be read.");
					}
				}

				continue;
			}

			if (TryParsePoint(record, out double lat, out double lon))
			{
				current.Add(new Coordinate(lon, lat));
			}
			else
			{
				problems.Add($"Line {lineNumber}: '{record.Trim()}' is not a coordinate and was skipped.");
			}
		}

		FinishLine(current, currentStartsAt, lines, problems);

		return new DatFile(sourcePath, pointOfTangency, lines, problems);
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
			|| seconds >= 60
			|| minutes >= 60)
		{
			return false;
		}

		degrees = wholeDegrees + minutes / 60d + seconds / 3600;
		return degrees <= maxDegrees;
	}

	private static void FinishLine(List<Coordinate>? points, int startsAt, List<LineString> lines, List<string> problems)
	{
		if (points is null)
		{
			return;
		}

		if (points.Count < 2)
		{
			problems.Add($"Line {startsAt}: the LINE block has {points.Count} point(s), so it draws nothing and was skipped.");
			return;
		}

		lines.Add(Wgs84.Factory.CreateLineString([.. points]));
	}
}
