using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Infrastructure.Eram.Models;

/// <summary>
/// One drawable element of an ERAM GeoMap object: a <c>GeoMapLine</c>, <c>GeoMapSymbol</c> or
/// <c>GeoMapText</c>, or one piece of a <c>GeoMapSaa</c> (a boundary segment or its label).
/// </summary>
/// <param name="Kind">What it draws.</param>
/// <param name="Start">
/// Where a line starts, or where a symbol or text is drawn (NTS order: X = longitude,
/// Y = latitude).
/// </param>
/// <param name="End">Where a line ends; <see langword="null"/> for a symbol or text.</param>
/// <param name="TextLines">The lines a <see cref="EramElementKind.Text"/> element shows, top to bottom; otherwise <see langword="null"/>.</param>
/// <param name="Overrides">The element's own display properties, which override its object's defaults.</param>
public sealed record EramElement(
	EramElementKind Kind,
	Coordinate Start,
	Coordinate? End,
	IReadOnlyList<string>? TextLines,
	EramProperties Overrides);
