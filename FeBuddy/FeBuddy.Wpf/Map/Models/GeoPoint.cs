namespace FeBuddy.Wpf.Map.Models;

/// <summary>A WGS84 point. GeoJSON stores <c>[lon, lat]</c>; this keeps them named.</summary>
/// <param name="Lat">Latitude in decimal degrees.</param>
/// <param name="Lon">Longitude in decimal degrees.</param>
public readonly record struct GeoPoint(double Lat, double Lon);
