namespace FeBuddy.Core.Domain.Crc.Models;

/// <summary>
/// The properties of an <c>isLineDefaults</c> Feature: the values CRC applies to every Line
/// Feature in the same file that does not override them. See
/// <see href="https://github.com/KCSanders7070/CRC_GeoJson_Concepts/blob/main/CRC_Geojsons.md">
/// CRC_Geojsons.md</see>.
/// </summary>
/// <remarks>
/// A defaults Feature is not a drawn Feature with its own values - it is an instruction to CRC
/// about the rest of the file, so it only ever carries the properties CRC reads as defaults.
/// FE-Buddy requires every one of them: the user chooses each value rather than leaving any to
/// CRC's own fallback. Per-feature overrides use <see cref="CrcLineProperties"/> instead.
/// </remarks>
public sealed record CrcLineDefaults
{
	/// <summary>Brightness Control Group, 1-40.</summary>
	public required int Bcg { get; init; }

	/// <summary>ERAM filter groups, each 0-40; at least one.</summary>
	public required IReadOnlyList<int> Filters { get; init; }

	/// <summary>Line style: <c>solid</c>, <c>shortDashed</c>, <c>longDashed</c> or <c>longDashShortDash</c>.</summary>
	public required string Style { get; init; }

	/// <summary>Line thickness, 1-3.</summary>
	public required int Thickness { get; init; }

	/// <summary>The same values as a per-feature override, for a Feature that must not take the file's defaults.</summary>
	/// <returns>The override properties.</returns>
	public CrcLineProperties ToFeatureProperties() => new()
	{
		Bcg = Bcg,
		Filters = Filters,
		Style = Style,
		Thickness = Thickness,
	};
}

/// <summary>The properties of an <c>isSymbolDefaults</c> Feature. Every one is required; see <see cref="CrcLineDefaults"/>.</summary>
public sealed record CrcSymbolDefaults
{
	/// <summary>Brightness Control Group, 1-40.</summary>
	public required int Bcg { get; init; }

	/// <summary>ERAM filter groups, each 0-40; at least one.</summary>
	public required IReadOnlyList<int> Filters { get; init; }

	/// <summary>
	/// Symbol style, one of the CRC symbol styles, or <see langword="null"/> when every Symbol
	/// Feature in the file carries its own <c>style</c> property instead - NAVAIDs, for example,
	/// styled per NAVAID type rather than one style for the whole file.
	/// </summary>
	public required string? Style { get; init; }

	/// <summary>Symbol size, 1-4.</summary>
	public required int Size { get; init; }

	/// <summary>The same values as a per-feature override, for a Feature that must not take the file's defaults.</summary>
	/// <returns>The override properties.</returns>
	public CrcSymbolProperties ToFeatureProperties() => new()
	{
		Bcg = Bcg,
		Filters = Filters,
		Style = Style,
		Size = Size,
	};
}

/// <summary>
/// The properties of an <c>isTextDefaults</c> Feature. Every one is required; see
/// <see cref="CrcLineDefaults"/>.
/// </summary>
/// <remarks>
/// There is deliberately no <c>text</c>: CRC never reads a label from a defaults Feature, so
/// every Text Feature carries its own.
/// </remarks>
public sealed record CrcTextDefaults
{
	/// <summary>Brightness Control Group, 1-40.</summary>
	public required int Bcg { get; init; }

	/// <summary>ERAM filter groups, each 0-40; at least one.</summary>
	public required IReadOnlyList<int> Filters { get; init; }

	/// <summary>Text size, 0-5.</summary>
	public required int Size { get; init; }

	/// <summary>Whether the text is underlined.</summary>
	public required bool Underline { get; init; }

	/// <summary>Whether the text has an opaque background.</summary>
	public required bool Opaque { get; init; }

	/// <summary>Horizontal offset from the Feature's point, in pixels. Any integer.</summary>
	public required int XOffset { get; init; }

	/// <summary>Vertical offset from the Feature's point, in pixels. Any integer.</summary>
	public required int YOffset { get; init; }
}
