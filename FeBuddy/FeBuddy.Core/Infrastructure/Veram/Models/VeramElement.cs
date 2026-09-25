using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Infrastructure.Veram.Models;

/// <summary>One drawable <c>Element</c> of a vERAM GeoMap object.</summary>
/// <param name="Kind">What it draws.</param>
/// <param name="Start">
/// Where a line starts, or where a symbol or text is drawn (NTS order: X = longitude,
/// Y = latitude). Longitudes are wrapped into -180 to 180.
/// </param>
/// <param name="End">Where a line ends; <see langword="null"/> for a symbol or text.</param>
/// <param name="Text">The text a <see cref="VeramElementKind.Text"/> element shows; otherwise <see langword="null"/>.</param>
/// <param name="Overrides">The element's own display properties, which override its object's defaults.</param>
public sealed record VeramElement(
	VeramElementKind Kind,
	Coordinate Start,
	Coordinate? End,
	string? Text,
	VeramProperties Overrides);
