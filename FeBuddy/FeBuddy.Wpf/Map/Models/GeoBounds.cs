namespace FeBuddy.Wpf.Map.Models;

/// <summary>A lat/lon axis-aligned box (a region of interest).</summary>
/// <param name="SouthWest">The south-west corner.</param>
/// <param name="NorthEast">The north-east corner.</param>
public readonly record struct GeoBounds(GeoPoint SouthWest, GeoPoint NorthEast)
{
	/// <summary>The western edge's longitude.</summary>
	public double West => SouthWest.Lon;

	/// <summary>The eastern edge's longitude.</summary>
	public double East => NorthEast.Lon;

	/// <summary>The southern edge's latitude.</summary>
	public double South => SouthWest.Lat;

	/// <summary>The northern edge's latitude.</summary>
	public double North => NorthEast.Lat;

	/// <summary>The smallest box holding both <paramref name="a"/> and <paramref name="b"/>.</summary>
	/// <param name="a">One box.</param>
	/// <param name="b">The other box.</param>
	/// <returns>Their union.</returns>
	public static GeoBounds Union(GeoBounds a, GeoBounds b) => new(
		new GeoPoint(Math.Min(a.South, b.South), Math.Min(a.West, b.West)),
		new GeoPoint(Math.Max(a.North, b.North), Math.Max(a.East, b.East)));
}
