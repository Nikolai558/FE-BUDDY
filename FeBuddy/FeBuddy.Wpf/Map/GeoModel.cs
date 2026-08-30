namespace FeBuddy.Wpf.Map;

/// <summary>A WGS84 point. GeoJSON stores <c>[lon, lat]</c>; we keep them named.</summary>
public readonly record struct GeoPoint(double Lat, double Lon);

/// <summary>A lat/lon axis-aligned box (a region of interest).</summary>
public readonly record struct GeoBounds(GeoPoint SouthWest, GeoPoint NorthEast)
{
    public double West => SouthWest.Lon;
    public double East => NorthEast.Lon;
    public double South => SouthWest.Lat;
    public double North => NorthEast.Lat;
}

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
/// A projected-geometry-agnostic shape: a kind plus a flat list of coordinate
/// runs. For <see cref="MapGeometryKind.Point"/> each run holds a single point.
/// </summary>
public sealed class MapGeometry
{
    public MapGeometry(MapGeometryKind kind, IReadOnlyList<IReadOnlyList<GeoPoint>> parts)
    {
        Kind = kind;
        Parts = parts;
    }

    public MapGeometryKind Kind { get; }

    /// <summary>Line/ring runs, or (for points) one run per point.</summary>
    public IReadOnlyList<IReadOnlyList<GeoPoint>> Parts { get; }
}
