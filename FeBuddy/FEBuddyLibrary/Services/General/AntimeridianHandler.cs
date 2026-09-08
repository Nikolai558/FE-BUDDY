using FEBuddyLibrary.Handlers;

using NetTopologySuite.Geometries;

using Location = FEBuddyLibrary.Models.Location.Location;

namespace FEBuddyLibrary.Services.General;

/// <summary>
/// Splits LineString geometry at the antimeridian (±180° longitude) so a line that crosses it
/// is represented as two separate LineStrings instead of one that visually wraps across the
/// entire map.
/// </summary>
/// <remarks>
/// This is a thin wrapper around the existing <see cref="CoordinateHandler.CrossesAntimeridian"/>
/// and <see cref="CoordinateHandler.SplitLineSegmentAtAntimeridian"/> logic, applied
/// segment-by-segment across a full LineString.
/// </remarks>
public static class AntimeridianHandler
{
	/// <summary>
	/// Splits a single LineString into one or more LineStrings, breaking it wherever a
	/// consecutive coordinate pair crosses the antimeridian.
	/// </summary>
	/// <param name="lineString">The LineString to split.</param>
	/// <param name="geometryFactory">The geometry factory used to build the resulting LineStrings.</param>
	/// <returns>
	/// A list containing <paramref name="lineString"/> unchanged (as a single-element list)
	/// when it never crosses the antimeridian, otherwise the resulting split LineStrings in
	/// order.
	/// </returns>
	public static IReadOnlyList<LineString> Split(LineString lineString, GeometryFactory geometryFactory)
	{
		ArgumentNullException.ThrowIfNull(lineString);
		ArgumentNullException.ThrowIfNull(geometryFactory);

		Coordinate[] coordinates = lineString.Coordinates;

		if (coordinates.Length < 2)
		{
			return new[] { lineString };
		}

		List<LineString> result = new();
		List<Coordinate> current = new() { coordinates[0] };
		bool crossedAtLeastOnce = false;

		for (int i = 0; i < coordinates.Length - 1; i++)
		{
			Coordinate start = coordinates[i];
			Coordinate end = coordinates[i + 1];

			// NTS Coordinate.X = longitude, .Y = latitude; Location's constructor takes (lat, lon).
			Location startLocation = new(start.Y, start.X);
			Location endLocation = new(end.Y, end.X);

			if (!CoordinateHandler.CrossesAntimeridian(startLocation, endLocation))
			{
				AppendIfDistinct(current, end);
				continue;
			}

			crossedAtLeastOnce = true;

			// [pointA(start), midPointStart, midPointEnd, pointB(end)] - midPointStart shares
			// the starting side of the antimeridian, midPointEnd shares the ending side.
			List<Location> split =
				CoordinateHandler.SplitLineSegmentAtAntimeridian(startLocation, endLocation);

			Coordinate antimeridianStart = new(split[1].DecLon, split[1].DecLat);
			Coordinate antimeridianEnd = new(split[2].DecLon, split[2].DecLat);

			// Finish the current fragment at the antimeridian on the starting side. When the
			// crossing point is identical to the segment's own endpoint (a real waypoint that
			// sits exactly on +/-180), this adds nothing rather than a duplicate coordinate.
			AppendIfDistinct(current, antimeridianStart);
			EmitFragment(result, current, geometryFactory);

			// Begin the next fragment on the ending side. If the ending-side crossing point is
			// the segment's endpoint itself, carry the endpoint alone (no degenerate two-point
			// fragment made of the same coordinate twice).
			current = CoordinatesEqual(antimeridianEnd, end)
				? new List<Coordinate> { end }
				: new List<Coordinate> { antimeridianEnd, end };
		}

		// Never crossed: return the input untouched (as a single-element list).
		if (!crossedAtLeastOnce)
		{
			return new[] { lineString };
		}

		EmitFragment(result, current, geometryFactory);

		return result;
	}

	/// <summary>Appends <paramref name="coordinate"/> unless it is identical to the last coordinate already in the list.</summary>
	private static void AppendIfDistinct(List<Coordinate> coordinates, Coordinate coordinate)
	{
		if (coordinates.Count == 0 || !CoordinatesEqual(coordinates[^1], coordinate))
		{
			coordinates.Add(coordinate);
		}
	}

	/// <summary>Emits <paramref name="coordinates"/> as a LineString only if it has at least two <b>distinct</b> coordinates.</summary>
	private static void EmitFragment(List<LineString> result, List<Coordinate> coordinates, GeometryFactory geometryFactory)
	{
		if (HasAtLeastTwoDistinctCoordinates(coordinates))
		{
			result.Add(geometryFactory.CreateLineString(coordinates.ToArray()));
		}
	}

	/// <summary>Whether a coordinate list contains at least two positions that are not all the same point.</summary>
	private static bool HasAtLeastTwoDistinctCoordinates(IReadOnlyList<Coordinate> coordinates)
	{
		if (coordinates.Count < 2)
		{
			return false;
		}

		Coordinate first = coordinates[0];
		for (int i = 1; i < coordinates.Count; i++)
		{
			if (!CoordinatesEqual(first, coordinates[i]))
			{
				return true;
			}
		}

		return false;
	}

	private static bool CoordinatesEqual(Coordinate a, Coordinate b) =>
		a.X == b.X && a.Y == b.Y;

	/// <summary>
	/// Splits every LineString in <paramref name="lineStrings"/> at the antimeridian and
	/// flattens the results into a single list.
	/// </summary>
	/// <param name="lineStrings">The LineStrings to split.</param>
	/// <param name="geometryFactory">The geometry factory used to build the resulting LineStrings.</param>
	public static IReadOnlyList<LineString> Split(
		IEnumerable<LineString> lineStrings,
		GeometryFactory geometryFactory)
	{
		ArgumentNullException.ThrowIfNull(lineStrings);
		ArgumentNullException.ThrowIfNull(geometryFactory);

		List<LineString> result = new();

		foreach (LineString lineString in lineStrings)
		{
			result.AddRange(Split(lineString, geometryFactory));
		}

		return result;
	}
}
