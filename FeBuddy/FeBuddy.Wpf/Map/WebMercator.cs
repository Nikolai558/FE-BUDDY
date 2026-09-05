namespace FeBuddy.Wpf.Map;

/// <summary>
/// Spherical Web-Mercator, expressed in "world units" where the whole globe maps
/// to the unit square (0..1 on each axis, y increasing southward). The map
/// control then applies a simple scale + translate to reach screen pixels.
/// </summary>
internal static class WebMercator
{
    /// <summary>Latitude beyond which Mercator y runs to infinity.</summary>
    public const double MaxLatitude = 85.051129;

    public static double LonToWorldX(double lon) => (lon + 180.0) / 360.0;

    public static double LatToWorldY(double lat)
    {
        var clamped = Math.Clamp(lat, -MaxLatitude, MaxLatitude);
        var s = Math.Sin(clamped * Math.PI / 180.0);
        return 0.5 - Math.Log((1.0 + s) / (1.0 - s)) / (4.0 * Math.PI);
    }

    public static double WorldXToLon(double x) => (x * 360.0) - 180.0;

    public static double WorldYToLat(double y)
    {
        var n = Math.PI * (1.0 - (2.0 * y));
        return Math.Atan(Math.Sinh(n)) * 180.0 / Math.PI;
    }
}
