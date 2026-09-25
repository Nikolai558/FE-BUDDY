namespace FeBuddy.Core.Infrastructure.Eram.Models;

/// <summary>
/// One <c>GeoMapObjectType</c>: a group of elements of one map object type and map group, sharing
/// default display properties.
/// </summary>
/// <param name="ObjectType">The object's <c>MapObjectType</c>, e.g. <c>AIRWAY</c> or <c>SupplementalLine</c>.</param>
/// <param name="MapGroupId">The object's <c>MapGroupId</c>, or <see langword="null"/> when the file gives none.</param>
/// <param name="LineDefaults">Its <c>DefaultLineProperties</c>, or <see langword="null"/> when it has none.</param>
/// <param name="SymbolDefaults">Its <c>DefaultSymbolProperties</c>, or <see langword="null"/> when it has none.</param>
/// <param name="TextDefaults">Its <c>TextDefaultProperties</c>, or <see langword="null"/> when it has none.</param>
/// <param name="Elements">Its elements, in file order.</param>
public sealed record EramGeoMapObject(
	string ObjectType,
	int? MapGroupId,
	EramProperties? LineDefaults,
	EramProperties? SymbolDefaults,
	EramProperties? TextDefaults,
	IReadOnlyList<EramElement> Elements)
{
	/// <summary>
	/// What the object is called in file names and messages: <c>&lt;MapObjectType&gt;_&lt;MapGroupId&gt;</c>,
	/// e.g. <c>AIRWAY_3</c>, or just the type when it has no map group.
	/// </summary>
	public string Name => MapGroupId is { } group ? $"{ObjectType}_{group}" : ObjectType;
}
