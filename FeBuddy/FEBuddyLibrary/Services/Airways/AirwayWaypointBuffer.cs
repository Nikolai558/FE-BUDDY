using FEBuddyLibrary.Handlers;
using FEBuddyLibrary.Models.Services.Airways;

using NetTopologySuite.Geometries;

using Location = FEBuddyLibrary.Models.Location.Location;

namespace FEBuddyLibrary.Services.Airways;

/// <summary>
/// Shortens each leg of an airway's geometry so it stops short of the waypoint symbols at
/// both ends, making waypoint markers legible instead of buried under the line.
/// </summary>
/// <remarks>
/// Every leg becomes its own disjoint two-point LineString (see the build plan's
/// "Consequence" note under §7.5): a buffered airway is a <see cref="MultiLineString"/> even
/// when the un-buffered airway was a single continuous <see cref="LineString"/>.
/// </remarks>
public static class AirwayWaypointBuffer
{
	/// <summary>Buffer radius, in nautical miles, around a 5-character fix identifier.</summary>
	public const double FixRadiusNm = 2.5;

	/// <summary>Buffer radius, in nautical miles, around any other identifier (NAVAID, airport).</summary>
	public const double OtherRadiusNm = 5.0;

	/// <summary>Coordinate rounding precision (decimal places) used to match a leg endpoint back to a named waypoint.</summary>
	private const int CoordinateMatchPrecision = 6;

	/// <summary>
	/// Buffers every leg of every LineString in <paramref name="lineStrings"/>.
	/// </summary>
	/// <param name="lineStrings">The airway's current LineStrings (after antimeridian split / ROI clip, if any).</param>
	/// <param name="airwayPoints">
	/// The airway's resolved waypoints, used to look up each leg endpoint's identifier (and
	/// therefore its buffer radius) by coordinate. A leg endpoint that does not match any
	/// known waypoint (e.g. a synthetic antimeridian-split or ROI-clip boundary point) falls
	/// back to <see cref="OtherRadiusNm"/>.
	/// </param>
	/// <param name="geometryFactory">The geometry factory used to build the resulting LineStrings.</param>
	/// <param name="awyId">The airway identifier being processed (used only in warning text).</param>
	/// <returns>The buffered legs, plus a warning for each leg dropped as too short to buffer.</returns>
	public static AirwayBufferResult Buffer(
		IReadOnlyList<LineString> lineStrings,
		IReadOnlyList<AirwayPoint> airwayPoints,
		GeometryFactory geometryFactory,
		string awyId)
	{
		ArgumentNullException.ThrowIfNull(lineStrings);
		ArgumentNullException.ThrowIfNull(airwayPoints);
		ArgumentNullException.ThrowIfNull(geometryFactory);

		Dictionary<(double Lon, double Lat), AirwayPoint> pointsByCoordinate = BuildCoordinateIndex(airwayPoints);

		List<LineString> legs = new();
		List<string> warnings = new();

		foreach (LineString lineString in lineStrings)
		{
			Coordinate[] coordinates = lineString.Coordinates;

			for (int i = 0; i < coordinates.Length - 1; i++)
			{
				Coordinate start = coordinates[i];
				Coordinate end = coordinates[i + 1];

				double startRadius = RadiusFor(start, pointsByCoordinate);
				double endRadius = RadiusFor(end, pointsByCoordinate);

				Location startLocation = new(start.Y, start.X);
				Location endLocation = new(end.Y, end.X);

				double legDistanceNm = CoordinateHandler.Distance(startLocation, endLocation, Round: false);

				if (legDistanceNm <= startRadius + endRadius)
				{
					warnings.Add(
						$"Airway '{awyId}': a leg between ({start.Y:F5}, {start.X:F5}) and " +
						$"({end.Y:F5}, {end.X:F5}) is {legDistanceNm:F2} NM long, shorter than " +
						$"its combined waypoint buffer radius of {startRadius + endRadius:F1} NM. " +
						"This leg was dropped.");
					continue;
				}

				double startToEndBearing = CoordinateHandler.Bearing(startLocation, endLocation);
				double endToStartBearing = CoordinateHandler.Bearing(endLocation, startLocation);

				Location bufferedStart =
					CoordinateHandler.PointAtDistanceAndBearing(startLocation, startToEndBearing, startRadius);

				Location bufferedEnd =
					CoordinateHandler.PointAtDistanceAndBearing(endLocation, endToStartBearing, endRadius);

				legs.Add(geometryFactory.CreateLineString(new[]
				{
					new Coordinate(bufferedStart.DecLon, bufferedStart.DecLat),
					new Coordinate(bufferedEnd.DecLon, bufferedEnd.DecLat)
				}));
			}
		}

		return new AirwayBufferResult(legs, warnings);
	}

	/// <summary>
	/// Determines the buffer radius for a coordinate based on the matching waypoint's
	/// identifier length (5 characters -&gt; a fix), falling back to
	/// <see cref="OtherRadiusNm"/> when the coordinate does not match any known waypoint.
	/// </summary>
	private static double RadiusFor(
		Coordinate coordinate,
		Dictionary<(double Lon, double Lat), AirwayPoint> pointsByCoordinate)
	{
		var key = (Math.Round(coordinate.X, CoordinateMatchPrecision), Math.Round(coordinate.Y, CoordinateMatchPrecision));

		if (pointsByCoordinate.TryGetValue(key, out AirwayPoint? point) && point.PointId.Length == 5)
		{
			return FixRadiusNm;
		}

		return OtherRadiusNm;
	}

	/// <summary>
	/// Builds a coordinate -&gt; AirwayPoint index for radius lookups. When multiple waypoints
	/// round to the same coordinate, the first one encountered wins.
	/// </summary>
	private static Dictionary<(double Lon, double Lat), AirwayPoint> BuildCoordinateIndex(
		IReadOnlyList<AirwayPoint> airwayPoints)
	{
		Dictionary<(double Lon, double Lat), AirwayPoint> index = new();

		foreach (AirwayPoint point in airwayPoints)
		{
			var key = (Math.Round(point.Longitude, CoordinateMatchPrecision), Math.Round(point.Latitude, CoordinateMatchPrecision));
			index.TryAdd(key, point);
		}

		return index;
	}
}
