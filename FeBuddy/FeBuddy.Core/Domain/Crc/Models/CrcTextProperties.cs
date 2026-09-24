namespace FeBuddy.Core.Domain.Crc.Models;

/// <summary>
/// CRC ERAM property values for a Text feature (a rendered Point label), matching the
/// contract at
/// <see href="https://github.com/KCSanders7070/CRC_GeoJson_Concepts/blob/main/CRC_Geojsons.md">
/// CRC_Geojsons.md</see>.
/// </summary>
/// <remarks>
/// Both <see cref="Filters"/> and <see cref="Text"/> can never be auto-assigned by CRC, so
/// both are required here. The remaining properties (<see cref="Bcg"/>, <see cref="Size"/>,
/// <see cref="Underline"/>, <see cref="XOffset"/>, <see cref="YOffset"/>,
/// <see cref="Opaque"/>) are optional; when null, CRC applies its own auto-assigned fallback
/// instead.
/// </remarks>
public sealed record CrcTextProperties
{
	/// <summary>Brightness Control Group. Valid range: 1-40. Auto-assigned to 1 if null.</summary>
	public int? Bcg { get; init; }

	/// <summary>
	/// ERAM filter group membership. Each entry must be in the range 0-40. Required by CRC;
	/// a Text Feature with no filters assigned (via this or a per-feature override) will not
	/// display on an ERAM window.
	/// </summary>
	public required IReadOnlyList<int> Filters { get; init; }

	/// <summary>
	/// The rendered text, one array entry per line. Required by CRC; a Text Feature with no
	/// text assigned (via this or a per-feature override) will not display on an ERAM window.
	/// </summary>
	public required IReadOnlyList<string> Text { get; init; }

	/// <summary>Text size. Valid range: 0-5. Auto-assigned to 1 if null.</summary>
	public int? Size { get; init; }

	/// <summary>Whether the text is underlined. Auto-assigned to false if null.</summary>
	public bool? Underline { get; init; }

	/// <summary>Horizontal pixel offset from the feature's point. Must be &gt;= 0. Auto-assigned to 0 if null.</summary>
	public int? XOffset { get; init; }

	/// <summary>Vertical pixel offset from the feature's point. Must be &gt;= 0. Auto-assigned to 0 if null.</summary>
	public int? YOffset { get; init; }

	/// <summary>Whether the text is rendered with an opaque background. Auto-assigned to false if null.</summary>
	public bool? Opaque { get; init; }
}
