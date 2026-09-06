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

		for (int i = 0; i < coordinates.Length - 1; i++)
		{
			Coordinate start = coordinates[i];
			Coordinate end = coordinates[i + 1];

			// NTS Coordinate.X = longitude, .Y = latitude; Location's constructor takes (lat, lon).
			Location startLocation = new(start.Y, start.X);
			Location endLocation = new(end.Y, end.X);

			if (!CoordinateHandler.CrossesAntimeridian(startLocation, endLocation))
			{
				current.Add(end);
				continue;
			}

			// [pointA(start), midPointStart, midPointEnd, pointB(end)] - midPointStart shares
			// the starting side of the antimeridian, midPointEnd shares the ending side.
			List<Location> split =
				CoordinateHandler.SplitLineSegmentAtAntimeridian(startLocation, endLocation);

			Location antimeridianStart = split[1];
			Location antimeridianEnd = split[2];

			// Finish the current LineString at the antimeridian on the starting side.
			current.Add(new Coordinate(antimeridianStart.DecLon, antimeridianStart.DecLat));
			result.Add(geometryFactory.CreateLineString(current.ToArray()));

			// Begin a new LineString at the antimeridian on the ending side.
			current = new List<Coordinate>
			{
				new Coordinate(antimeridianEnd.DecLon, antimeridianEnd.DecLat),
				end
			};
		}

		if (current.Count >= 2)
		{
			result.Add(geometryFactory.CreateLineString(current.ToArray()));
		}

		return result;
	}

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
