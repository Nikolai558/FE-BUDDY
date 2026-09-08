using FEBuddyLibrary.Handlers;
using FEBuddyLibrary.Models.Services.Airac.Airways;
using FEBuddyLibrary.Models.Services.General;
using FEBuddyLibrary.Services.General;

using NetTopologySuite.Geometries;

using Location = FEBuddyLibrary.Models.Location.Location;

namespace FEBuddyLibrary.Services.Airac.Airways;

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
	/// The airway's real resolved waypoints - and only those. A leg endpoint that matches one
	/// of them is buffered (2.5 NM for a 5-character fix, 5 NM otherwise, including a real fix
	/// sitting exactly on +/-180). A leg endpoint that matches none of them is by definition
	/// synthetic - a vertex the antimeridian split or ROI clip invented - and is <b>not</b>
	/// buffered (radius 0), so an ROI-clipped airway reaches the ROI boundary instead of
	/// stopping 5 NM inside it (remediation plan 3.10).
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
		List<ServiceMessage> messages = new();

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
					// Info, not Warning: this is the buffer doing exactly what it was asked to
					// do (remediation plan 3.8).
					messages.Add(new ServiceMessage(LogLevel.Info, "AirwayWaypointBuffer",
						$"Airway '{awyId}': a leg between ({start.Y:F5}, {start.X:F5}) and " +
						$"({end.Y:F5}, {end.X:F5}) is {legDistanceNm:F2} NM long, shorter than " +
						$"its combined waypoint buffer radius of {startRadius + endRadius:F1} NM. " +
						"This leg was dropped."));
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

		return new AirwayBufferResult(legs, messages);
	}

	/// <summary>
	/// Determines the buffer radius for a leg endpoint: <see cref="FixRadiusNm"/> for a
	/// matched 5-character fix, <see cref="OtherRadiusNm"/> for any other matched waypoint,
	/// and <b>0</b> (no buffering) for an endpoint that matches no real waypoint - which is
	/// exactly the definition of a synthetic antimeridian-split or ROI-clip vertex
	/// (remediation plan 3.10).
	/// </summary>
	private static double RadiusFor(
		Coordinate coordinate,
		Dictionary<(double Lon, double Lat), AirwayPoint> pointsByCoordinate)
	{
		var key = (
			Math.Round(NormalizeLongitude(coordinate.X), CoordinateMatchPrecision),
			Math.Round(coordinate.Y, CoordinateMatchPrecision));

		if (!pointsByCoordinate.TryGetValue(key, out AirwayPoint? point))
		{
			// Not a real waypoint -> a vertex the AM split or ROI clip invented. Do not buffer.
			return 0.0;
		}

		return point.PointId.Length == 5 ? FixRadiusNm : OtherRadiusNm;
	}

	/// <summary>
	/// Builds a coordinate -&gt; AirwayPoint index for radius lookups. Longitude is normalized
	/// so a real waypoint stored at <c>-180</c> still matches a fragment endpoint the
	/// antimeridian split expressed as <c>+180</c> (and vice versa). When multiple waypoints
	/// round to the same coordinate, the first one encountered wins.
	/// </summary>
	private static Dictionary<(double Lon, double Lat), AirwayPoint> BuildCoordinateIndex(
		IReadOnlyList<AirwayPoint> airwayPoints)
	{
		Dictionary<(double Lon, double Lat), AirwayPoint> index = new();

		foreach (AirwayPoint point in airwayPoints)
		{
			var key = (
				Math.Round(NormalizeLongitude(point.Longitude), CoordinateMatchPrecision),
				Math.Round(point.Latitude, CoordinateMatchPrecision));
			index.TryAdd(key, point);
		}

		return index;
	}

	/// <summary>Treats <c>+180</c> and <c>-180</c> as the same meridian by mapping <c>+180</c> to <c>-180</c>.</summary>
	private static double NormalizeLongitude(double longitude) =>
		longitude == 180.0 ? -180.0 : longitude;
}
