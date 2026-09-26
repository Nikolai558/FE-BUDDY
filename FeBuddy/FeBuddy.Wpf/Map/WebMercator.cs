namespace FeBuddy.Wpf.Map;

/// <summary>
/// Spherical Web-Mercator, expressed in "world units" where the whole globe maps
/// to the unit square (0..1 on each axis, y increasing southward). The map
/// control then applies a simple scale + translate to reach screen pixels.
/// <para>
/// World x is not limited to 0..1: the map repeats side by side forever, so x = 1.25 is the
/// same place as x = 0.25 one world to the east. <see cref="NormalizeLon"/> folds a longitude
/// read from such an x back into -180..180.
/// </para>
/// </summary>
internal static class WebMercator
{
	/// <summary>Latitude beyond which Mercator y runs to infinity.</summary>
	public const double MaxLatitude = 85.051129;

	/// <summary>The pixels-per-world-unit at zoom level 0 (one 256px tile shows the world).</summary>
	public const double TileSize = 256.0;

	/// <summary>Earth's circumference at the equator, in nautical miles.</summary>
	public const double EquatorNauticalMiles = 21_638.8;

	/// <summary>Longitude to world x (0 at 180W, 1 at 180E).</summary>
	public static double LonToWorldX(double lon) => (lon + 180.0) / 360.0;

	/// <summary>Latitude to world y (0 at the top), clamped to <see cref="MaxLatitude"/>.</summary>
	public static double LatToWorldY(double lat)
	{
		var clamped = Math.Clamp(lat, -MaxLatitude, MaxLatitude);
		var s = Math.Sin(clamped * Math.PI / 180.0);
		return 0.5 - Math.Log((1.0 + s) / (1.0 - s)) / (4.0 * Math.PI);
	}

	/// <summary>World x back to longitude. Not folded: x outside 0..1 gives a longitude past ±180.</summary>
	public static double WorldXToLon(double x) => (x * 360.0) - 180.0;

	/// <summary>World y back to latitude.</summary>
	public static double WorldYToLat(double y)
	{
		var n = Math.PI * (1.0 - (2.0 * y));
		return Math.Atan(Math.Sinh(n)) * 180.0 / Math.PI;
	}

	/// <summary>Folds any longitude into -180 (inclusive) to 180 (exclusive).</summary>
	public static double NormalizeLon(double lon) => ((((lon + 180.0) % 360.0) + 360.0) % 360.0) - 180.0;

	/// <summary>The slippy-map zoom level for a scale, e.g. 256 px per world is zoom 0.</summary>
	public static double ScaleToZoom(double scale) => Math.Log2(scale / TileSize);

	/// <summary>The scale for a slippy-map zoom level.</summary>
	public static double ZoomToScale(double zoom) => TileSize * Math.Pow(2.0, zoom);
}
