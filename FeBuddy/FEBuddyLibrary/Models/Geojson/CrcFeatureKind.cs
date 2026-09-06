namespace FEBuddyLibrary.Models.Geojson;

/// <summary>
/// Identifies which of the three CRC ERAM GeoJSON feature families a property set,
/// validation rule, or isDefaults Feature applies to.
/// </summary>
/// <remarks>
/// See <see href="https://github.com/KCSanders7070/CRC_GeoJson_Concepts/blob/main/CRC_Geojsons.md">
/// CRC_Geojsons.md</see> for the full property contract. Each kind maps to its own
/// isDefaults flag on the non-rendered defaults Feature:
/// <see cref="Line"/> -&gt; <c>isLineDefaults</c>, <see cref="Symbol"/> -&gt; <c>isSymbolDefaults</c>,
/// <see cref="Text"/> -&gt; <c>isTextDefaults</c>.
/// </remarks>
public enum CrcFeatureKind
{
	/// <summary>A LineString/MultiLineString feature (an airway line, boundary, etc.).</summary>
	Line,

	/// <summary>A Point feature rendered as a CRC symbol (a NAVAID, fix, waypoint, etc.).</summary>
	Symbol,

	/// <summary>A Point feature rendered as CRC text (a waypoint identifier label, etc.).</summary>
	Text
}
