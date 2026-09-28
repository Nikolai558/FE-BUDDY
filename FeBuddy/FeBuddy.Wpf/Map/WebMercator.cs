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
	/// <summary>
	/// The latitude of the square world map's top and bottom edges (85.0511287798°): beyond it,
	/// Mercator y runs to infinity. Worked out exactly, so it lands on y = 0 and 1 and not a hair past.
	/// </summary>
	public static readonly double MaxLatitude = Math.Atan(Math.Sinh(Math.PI)) * 180.0 / Math.PI;

	/// <summary>The pixels-per-world-unit at zoom level 0 (one 256px tile shows the world).</summary>
	public const double TileSize = 256.0;

	/// <summary>Earth's circumference at the equator, in nautical miles.</summary>
	public const double EquatorNauticalMiles = 21_638.8;

	/// <summary>Longitude to world x (0 at 180W, 1 at 180E).</summary>
	public static double LonToWorldX(double lon) => (lon + 180.0) / 360.0;

	/// <summary>
	/// Latitude to world y (0 at the top), clamped to <see cref="MaxLatitude"/> and so to 0..1 - a box
	/// from pole to pole fills the world exactly, which a move of it relies on.
	/// </summary>
	public static double LatToWorldY(double lat)
	{
		var clamped = Math.Clamp(lat, -MaxLatitude, MaxLatitude);
		var s = Math.Sin(clamped * Math.PI / 180.0);
		return Math.Clamp(0.5 - Math.Log((1.0 + s) / (1.0 - s)) / (4.0 * Math.PI), 0.0, 1.0);
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

	/// <summary>
	/// <paramref name="lon"/> moved whole turns of the globe to lie within 180 degrees of
	/// <paramref name="previous"/>, so a line from one to the other runs the short way across the
	/// 180th meridian. One step, so however far off a longitude is (a damaged file's 1e20), it never loops.
	/// </summary>
	public static double UnwrapLon(double lon, double previous) => previous + NormalizeLon(lon - previous);

	/// <summary>The slippy-map zoom level for a scale, e.g. 256 px per world is zoom 0.</summary>
	public static double ScaleToZoom(double scale) => Math.Log2(scale / TileSize);

	/// <summary>The scale for a slippy-map zoom level.</summary>
	public static double ZoomToScale(double zoom) => TileSize * Math.Pow(2.0, zoom);
}
