namespace FeBuddy.Core.Infrastructure.Veram.Models;

/// <summary>One <c>GeoMapObject</c>: a described group of elements sharing default display properties.</summary>
/// <param name="Description">The object's description, e.g. <c>ZOB BOUNDARY</c>.</param>
/// <param name="TdmOnly">Whether the object is shown only in TDM.</param>
/// <param name="LineDefaults">Its <c>LineDefaults</c>, or <see langword="null"/> when it has none.</param>
/// <param name="SymbolDefaults">Its <c>SymbolDefaults</c>, or <see langword="null"/> when it has none.</param>
/// <param name="TextDefaults">Its <c>TextDefaults</c>, or <see langword="null"/> when it has none.</param>
/// <param name="Elements">Its elements, in file order.</param>
public sealed record VeramGeoMapObject(
	string Description,
	bool TdmOnly,
	VeramProperties? LineDefaults,
	VeramProperties? SymbolDefaults,
	VeramProperties? TextDefaults,
	IReadOnlyList<VeramElement> Elements);
