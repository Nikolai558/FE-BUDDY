using System.Globalization;

using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Domain.Geo;

/// <summary>
/// Joins a list of separate two-point segments - the way sector files and vERAM GeoMaps store
/// lines - back into lines.
/// </summary>
/// <remarks>
/// <para>
/// Consecutive segments that meet become one path, including a segment written backwards (its end
/// is where the path already is). The paths then go through <see cref="LineStringMerger"/>, so a
/// segment drawn twice is drawn once, and <see cref="AntimeridianSplitter"/>, so nothing wraps
/// round the map. The result is smaller, and a dashed style stays dashed instead of restarting
/// its pattern at every segment.
/// </para>
/// <para>
/// Points are matched to 7 decimal places (about a centimetre).
/// </para>
/// </remarks>
public static class SegmentJoiner
{
	/// <summary>Joins segments into lines.</summary>
	/// <param name="segments">The segments, in the order they were written.</param>
	/// <returns>The lines; empty when no segment has any length.</returns>
	public static IReadOnlyList<LineString> Join(IEnumerable<(Coordinate Start, Coordinate End)> segments)
	{
		ArgumentNullException.ThrowIfNull(segments);

		List<List<(string Key, Coordinate Coordinate)>> paths = [];
		List<(string Key, Coordinate Coordinate)>? path = null;

		foreach ((Coordinate start, Coordinate end) in segments)
		{
			(string Key, Coordinate Coordinate) from = Keyed(start);
			(string Key, Coordinate Coordinate) to = Keyed(end);

			if (path is not null && path[^1].Key == from.Key)
			{
				path.Add(to);
			}
			else if (path is not null && path[^1].Key == to.Key)
			{
				// Written backwards, but it still continues the same line.
				path.Add(from);
			}
			else
			{
				path = [from, to];
				paths.Add(path);
			}
		}

		return AntimeridianSplitter.Split(LineStringMerger.Merge(paths));
	}

	private static (string Key, Coordinate Coordinate) Keyed(Coordinate coordinate) =>
		(string.Create(CultureInfo.InvariantCulture, $"{coordinate.Y:F7},{coordinate.X:F7}"), coordinate);
}
