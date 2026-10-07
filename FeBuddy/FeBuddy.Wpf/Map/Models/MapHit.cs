namespace FeBuddy.Wpf.Map.Models;

/// <summary>One shape a Ctrl+click on the map landed on.</summary>
/// <param name="Layer">The layer it is drawn in.</param>
/// <param name="Geometry">The shape.</param>
/// <param name="Point">For a point shape, the point clicked (a MultiPoint has several); <see langword="null"/> for a line or ring.</param>
public sealed record MapHit(MapLayer Layer, MapGeometry Geometry, GeoPoint? Point)
{
	/// <summary>The shape's index in <see cref="MapLayer.Geometries"/>, to draw it highlighted.</summary>
	internal int Index { get; init; }
}

/// <summary>What a Ctrl+click on the map landed on.</summary>
/// <param name="At">Where on the map it was.</param>
/// <param name="Hits">The shapes under the pointer, the one drawn on top first, at most <see cref="MaxHits"/>; empty when there are none.</param>
public sealed record MapInspection(GeoPoint At, IReadOnlyList<MapHit> Hits)
{
	/// <summary>The most shapes one Ctrl+click reports.</summary>
	public const int MaxHits = 100;
}
