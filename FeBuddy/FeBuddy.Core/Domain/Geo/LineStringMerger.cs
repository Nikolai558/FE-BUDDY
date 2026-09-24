using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Domain.Geo;

/// <summary>
/// "Efficient Linestring Handling": turns a set of overlapping paths into the fewest, longest
/// LineStrings that still draw every segment exactly once.
/// </summary>
/// <remarks>
/// <para>
/// Many NASR structures are several paths that share segments - every body of a departure runs
/// through the same trunk, every transition leaves from the same fix. Writing each path as its
/// own LineString draws the shared segments on top of each other, which bloats the file and
/// makes a dashed style render as a solid line.
/// </para>
/// <para>
/// The approach is greedy. Paths are taken longest first (ties keep their original order), so
/// the longest run becomes one unbroken base LineString. Every later path contributes only the
/// stretches not already drawn, each as its own LineString that starts on the point where it
/// leaves what is already there - so the branches visibly connect to the base. A segment is the
/// same segment in either direction.
/// </para>
/// <para>
/// Points are matched on their key, not their coordinates, so two paths that pass through the
/// same named fix always join there.
/// </para>
/// </remarks>
internal static class LineStringMerger
{
	/// <summary>
	/// Builds the LineStrings for a set of paths.
	/// </summary>
	/// <param name="paths">Each path as an ordered list of keyed coordinates.</param>
	/// <returns>
	/// The LineStrings, base first. Empty when no path has two different consecutive points.
	/// </returns>
	internal static IReadOnlyList<LineString> Merge(
		IEnumerable<IReadOnlyList<(string Key, Coordinate Coordinate)>> paths)
	{
		ArgumentNullException.ThrowIfNull(paths);

		List<IReadOnlyList<(string Key, Coordinate Coordinate)>> ordered = paths
			.Select((path, index) => (Path: CollapseRepeats(path), Index: index))
			.OrderByDescending(entry => entry.Path.Count)
			.ThenBy(entry => entry.Index)
			.Select(entry => entry.Path)
			.ToList();

		HashSet<(string, string)> drawn = new();
		List<LineString> lines = new();

		foreach (IReadOnlyList<(string Key, Coordinate Coordinate)> path in ordered)
		{
			List<Coordinate> run = new();

			for (int i = 1; i < path.Count; i++)
			{
				(string Key, Coordinate Coordinate) from = path[i - 1];
				(string Key, Coordinate Coordinate) to = path[i];

				if (!drawn.Add(SegmentKey(from.Key, to.Key)))
				{
					Flush(run, lines);
					continue;
				}

				if (run.Count == 0)
				{
					run.Add(from.Coordinate.Copy());
				}

				run.Add(to.Coordinate.Copy());
			}

			Flush(run, lines);
		}

		return lines;
	}

	/// <summary>
	/// Drops a point that repeats the one before it, so a path never contains a zero-length
	/// segment and its length reflects the segments it actually draws.
	/// </summary>
	private static IReadOnlyList<(string Key, Coordinate Coordinate)> CollapseRepeats(
		IReadOnlyList<(string Key, Coordinate Coordinate)> path)
	{
		List<(string Key, Coordinate Coordinate)> collapsed = new(path.Count);

		foreach ((string Key, Coordinate Coordinate) point in path)
		{
			if (collapsed.Count > 0 && string.Equals(collapsed[^1].Key, point.Key, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			collapsed.Add(point);
		}

		return collapsed;
	}

	private static (string, string) SegmentKey(string a, string b)
	{
		string first = a.ToUpperInvariant();
		string second = b.ToUpperInvariant();

		return string.CompareOrdinal(first, second) <= 0 ? (first, second) : (second, first);
	}

	private static void Flush(List<Coordinate> run, List<LineString> lines)
	{
		if (run.Count >= 2)
		{
			lines.Add(Wgs84.Factory.CreateLineString(run.ToArray()));
		}

		run.Clear();
	}
}
