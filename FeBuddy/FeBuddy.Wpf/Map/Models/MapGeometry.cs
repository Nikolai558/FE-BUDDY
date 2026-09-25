namespace FeBuddy.Wpf.Map.Models;

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
