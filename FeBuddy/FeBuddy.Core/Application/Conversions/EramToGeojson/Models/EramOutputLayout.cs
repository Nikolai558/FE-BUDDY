namespace FeBuddy.Core.Application.Conversions.EramToGeojson.Models;

/// <summary>
/// How the ERAM to GeoJSON conversion lays out its files - the three layouts of the original
/// ERAM_2_GEOJSON tool, named as it named them, and Raw Plus. Every map is
/// <c>&lt;GeomapId&gt;_&lt;LabelLine1&gt;-&lt;LabelLine2&gt;</c>, e.g. <c>CENTER_CENTER-MAP</c>.
/// </summary>
public enum EramOutputLayout
{
	/// <summary>
	/// A folder per map, then a folder per set of filters - <c>Filter_01\</c>, or
	/// <c>Multi-Filter_02_03_08\</c> for several - holding <c>Filter_01_Lines</c>,
	/// <c>Filter_01_Symbols</c> and <c>Filter_01_Text</c>.
	/// </summary>
	ByFilters,

	/// <summary>
	/// A folder per map, then a file per shared look, named after it, e.g.
	/// <c>BCG 01_Filters 01_Type AAV_Group 64_Object ZOB3NM_Style Solid_Thick 1_Lines</c>.
	/// </summary>
	ByAttributes,

	/// <summary>
	/// One file per map, <c>CENTER_CENTER-MAP.geojson</c>, one Feature per element, each carrying
	/// its own properties, and no isDefaults Features.
	/// </summary>
	Raw,

	/// <summary>
	/// Raw's one file per map with every Feature carrying its own properties, but lines that carry
	/// the same properties are joined and symbols that do are grouped into one Feature.
	/// </summary>
	RawPlus,
}
