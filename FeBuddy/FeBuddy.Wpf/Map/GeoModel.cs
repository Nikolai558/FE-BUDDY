namespace FeBuddy.Wpf.Map;

/// <summary>A WGS84 point. GeoJSON stores <c>[lon, lat]</c>; this keeps them named.</summary>
/// <param name="Lat">Latitude in decimal degrees.</param>
/// <param name="Lon">Longitude in decimal degrees.</param>
public readonly record struct GeoPoint(double Lat, double Lon);

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

/// <summary>What a <see cref="MapGeometry"/> is drawn as.</summary>
public enum MapGeometryKind
{
	/// <summary>One or more poly-lines (LineString / MultiLineString).</summary>
	Line,

	/// <summary>One or more closed rings (Polygon / MultiPolygon), drawn as outlines.</summary>
	Polygon,

	/// <summary>One or more standalone points (Point / MultiPoint).</summary>
	Point,
}

/// <summary>
/// One shape to draw: a kind plus a flat list of coordinate runs. For
/// <see cref="MapGeometryKind.Point"/> each run holds a single point.
/// </summary>
/// <param name="kind">What the shape is drawn as.</param>
/// <param name="parts">The coordinate runs.</param>
public sealed class MapGeometry(MapGeometryKind kind, IReadOnlyList<IReadOnlyList<GeoPoint>> parts)
{
	/// <summary>What the shape is drawn as.</summary>
	public MapGeometryKind Kind { get; } = kind;

	/// <summary>Line/ring runs, or (for points) one run per point.</summary>
	public IReadOnlyList<IReadOnlyList<GeoPoint>> Parts { get; } = parts;
}
