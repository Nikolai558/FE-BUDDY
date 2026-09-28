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
}
