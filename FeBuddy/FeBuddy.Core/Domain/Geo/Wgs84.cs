using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Domain.Geo;

/// <summary>
/// The one geometry factory FE-Buddy builds everything with: WGS84 (SRID 4326), the coordinate
/// system GeoJSON requires. Sharing it means clipping, splitting and buffering always combine
/// geometry that agrees on precision and SRID.
/// </summary>
/// <remarks>
/// NTS stores a <see cref="Coordinate"/> as X = longitude, Y = latitude - the reverse of how
/// aviation data is usually written. <see cref="Point"/> takes latitude first so callers never
/// have to remember that.
/// </remarks>
public static class Wgs84
{
	/// <summary>The shared WGS84 geometry factory.</summary>
	public static readonly GeometryFactory Factory =
		NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

	/// <summary>Creates a Point from a latitude and longitude, in that order.</summary>
	/// <param name="latitude">Latitude in decimal degrees.</param>
	/// <param name="longitude">Longitude in decimal degrees.</param>
	/// <returns>The Point.</returns>
	public static Point Point(double latitude, double longitude) =>
		Factory.CreatePoint(new Coordinate(longitude, latitude));
}
