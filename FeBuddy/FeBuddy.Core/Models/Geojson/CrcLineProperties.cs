namespace FeBuddy.Core.Models.Geojson;

/// <summary>
/// CRC ERAM property values for a Line feature (LineString/MultiLineString), matching the
/// contract at
/// <see href="https://github.com/KCSanders7070/CRC_GeoJson_Concepts/blob/main/CRC_Geojsons.md">
/// CRC_Geojsons.md</see>.
/// </summary>
/// <remarks>
/// <see cref="Filters"/> can never be auto-assigned by CRC, so it is required here. The
/// remaining properties (<see cref="Bcg"/>, <see cref="Style"/>, <see cref="Thickness"/>) are
/// optional; when null, CRC applies its own auto-assigned fallback (bcg=1, style="solid",
/// thickness=1) instead.
/// </remarks>
public sealed record CrcLineProperties
{
	/// <summary>Brightness Control Group. Valid range: 1-40. Auto-assigned to 1 if null.</summary>
	public int? Bcg { get; init; }

	/// <summary>
	/// ERAM filter group membership. Each entry must be in the range 0-40. Required by CRC;
	/// a line Feature with no filters assigned (via this or a per-feature override) will not
	/// display on an ERAM window.
	/// </summary>
	public required IReadOnlyList<int> Filters { get; init; }

	/// <summary>
	/// Line style. Valid values: "solid", "shortDashed", "longDashed", "longDashShortDash".
	/// Auto-assigned to "solid" if null.
	/// </summary>
	public string? Style { get; init; }

	/// <summary>Line thickness. Valid range: 1-3. Auto-assigned to 1 if null.</summary>
	public int? Thickness { get; init; }
}
