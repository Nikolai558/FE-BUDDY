namespace FeBuddy.Core.Application.Conversions.EramToGeojson.Models;

/// <summary>
/// The <c>feb.*</c> properties the ERAM to GeoJSON conversion can add to each Feature, naming
/// where it came from in <c>Geomaps.xml</c> (the original tool's <c>E2G_*</c> properties).
/// </summary>
public enum EramFebProperty
{
	/// <summary>The element's object's <c>MapObjectType</c>, e.g. <c>AIRWAY</c>.</summary>
	MapObjectType,

	/// <summary>The element's object's <c>MapGroupId</c>.</summary>
	MapGroupId,

	/// <summary>A line's <c>LineObjectId</c>.</summary>
	LineObjectId,

	/// <summary>A symbol's <c>SymbolId</c>, which its own labels carry too.</summary>
	SymbolId,

	/// <summary>An SAA boundary's or label's <c>SaaID</c>.</summary>
	SaaId,
}
