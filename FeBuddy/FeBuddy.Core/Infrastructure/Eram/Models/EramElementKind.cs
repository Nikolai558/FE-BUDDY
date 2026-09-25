namespace FeBuddy.Core.Infrastructure.Eram.Models;

/// <summary>What an ERAM GeoMap element draws.</summary>
public enum EramElementKind
{
	/// <summary>A line segment: a <c>GeoMapLine</c>, or a <c>GeoMapSaaLine</c> of an SAA boundary.</summary>
	Line,

	/// <summary>A symbol at a point: a <c>GeoMapSymbol</c>.</summary>
	Symbol,

	/// <summary>Text at a point: a <c>GeoMapText</c>, a symbol's own label, or an SAA's label.</summary>
	Text,
}
