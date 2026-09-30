using FeBuddy.Core.Application.Conversions.EramToGeojson.Models;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// The FE-Buddy custom properties the ERAM to GeoJSON tab offers - the original ERAM_2_GEOJSON
/// tool's <c>E2G_*</c> properties - in display order, with their tooltip text.
/// </summary>
/// <remarks>
/// Each property's name comes from Core (<see cref="Core.Application.Airac.FebProperties.Name{TProperty}"/>),
/// the same name <c>EramToGeojsonSettingsParser</c> accepts, so only the order and the wording live here.
/// </remarks>
public static class EramFebPropertyOptions
{
	/// <summary>Every property, in the order the tab lists them.</summary>
	public static IReadOnlyList<(EramFebProperty Property, string Description)> All { get; } =
	[
		(EramFebProperty.MapObjectType, "The element's MapObjectType in Geomaps.xml, e.g. AIRWAY."),
		(EramFebProperty.MapGroupId, "The element's MapGroupId in Geomaps.xml."),
		(EramFebProperty.LineObjectId, "A line's LineObjectId. Lines only."),
		(EramFebProperty.SymbolId, "A symbol's SymbolId, which its labels carry too. Symbols and Text."),
		(EramFebProperty.SaaId, "An SAA's SaaID, on its boundary and its label."),
	];
}
