namespace FeBuddy.Wpf.Map.Models;

/// <summary>
/// One shape to draw: a kind plus a flat list of coordinate runs. For
/// <see cref="MapGeometryKind.Point"/> each run holds a single point.
/// </summary>
/// <param name="kind">What the shape is drawn as.</param>
/// <param name="parts">The coordinate runs.</param>
/// <param name="label">
/// Text to draw at a point (a vNAS text feature's <c>text</c> lines, joined), or
/// <see langword="null"/> for none. A labelled point is drawn as its text rather than a dot.
/// </param>
public sealed class MapGeometry(MapGeometryKind kind, IReadOnlyList<IReadOnlyList<GeoPoint>> parts, string? label = null)
{
	/// <summary>What the shape is drawn as.</summary>
	public MapGeometryKind Kind { get; } = kind;

	/// <summary>Line/ring runs, or (for points) one run per point.</summary>
	public IReadOnlyList<IReadOnlyList<GeoPoint>> Parts { get; } = parts;

	/// <summary>The text drawn at a point, or <see langword="null"/>.</summary>
	public string? Label { get; } = label;
}
