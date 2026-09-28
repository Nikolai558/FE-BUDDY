namespace FeBuddy.Wpf.Map.Models;

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
