namespace FEBuddyLibrary.Models.Geojson;

/// <summary>
/// CRC ERAM property values for a Symbol feature (a rendered Point), matching the contract at
/// <see href="https://github.com/KCSanders7070/CRC_GeoJson_Concepts/blob/main/CRC_Geojsons.md">
/// CRC_Geojsons.md</see>.
/// </summary>
/// <remarks>
/// <see cref="Filters"/> can never be auto-assigned by CRC, so it is required here. The
/// remaining properties (<see cref="Bcg"/>, <see cref="Style"/>, <see cref="Size"/>) are
/// optional; when null, CRC applies its own auto-assigned fallback (bcg=1, style="vor",
/// size=1) instead.
/// </remarks>
public sealed record CrcSymbolProperties
{
	/// <summary>Brightness Control Group. Valid range: 1-40. Auto-assigned to 1 if null.</summary>
	public int? Bcg { get; init; }

	/// <summary>
	/// ERAM filter group membership. Each entry must be in the range 0-40. Required by CRC;
	/// a Symbol Feature with no filters assigned (via this or a per-feature override) will
	/// not display on an ERAM window.
	/// </summary>
	public required IReadOnlyList<int> Filters { get; init; }

	/// <summary>
	/// Symbol type. Valid values: "obstruction1", "obstruction2", "heliport", "nuclear",
	/// "emergencyAirport", "radar", "iaf", "rnavOnlyWaypoint", "rnav", "airwayIntersections",
	/// "ndb", "vor", "otherWaypoints", "airport", "satelliteAirport", "tacan".
	/// Auto-assigned to "vor" if null.
	/// </summary>
	public string? Style { get; init; }

	/// <summary>Symbol size. Valid range: 1-4. Auto-assigned to 1 if null.</summary>
	public int? Size { get; init; }
}
