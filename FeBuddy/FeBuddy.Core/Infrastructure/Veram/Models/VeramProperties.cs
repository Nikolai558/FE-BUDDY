namespace FeBuddy.Core.Infrastructure.Veram.Models;

/// <summary>
/// Display properties as a vERAM GeoMap writes them: an object's <c>LineDefaults</c>,
/// <c>SymbolDefaults</c> or <c>TextDefaults</c>, or one <c>Element</c>'s own overrides. A value
/// the XML leaves out (or leaves blank) is <see langword="null"/>.
/// </summary>
/// <remarks>
/// Values are as vERAM wrote them: <see cref="Style"/> is vERAM's spelling (<c>Solid</c>,
/// <c>Vor</c>), not yet CRC's. Checking them against what CRC can draw is the conversion's job.
/// </remarks>
public sealed record VeramProperties
{
	/// <summary>No properties at all.</summary>
	public static VeramProperties None { get; } = new();

	/// <summary>Brightness control group.</summary>
	public int? Bcg { get; init; }

	/// <summary>Filter groups; <see langword="null"/> when the XML gives none.</summary>
	public IReadOnlyList<int>? Filters { get; init; }

	/// <summary>Line or symbol style, as vERAM spells it.</summary>
	public string? Style { get; init; }

	/// <summary>Line thickness.</summary>
	public int? Thickness { get; init; }

	/// <summary>Symbol or text size.</summary>
	public int? Size { get; init; }

	/// <summary>Whether text is underlined.</summary>
	public bool? Underline { get; init; }

	/// <summary>Whether text has an opaque background.</summary>
	public bool? Opaque { get; init; }

	/// <summary>Text offset across, in pixels.</summary>
	public int? XOffset { get; init; }

	/// <summary>Text offset down, in pixels.</summary>
	public int? YOffset { get; init; }

	/// <summary>Whether any property is set.</summary>
	public bool IsEmpty => this == None;

	/// <inheritdoc />
	/// <remarks>Compares <see cref="Filters"/> by content, so two elements with the same overrides group together.</remarks>
	public bool Equals(VeramProperties? other) =>
		other is not null
		&& Bcg == other.Bcg
		&& (Filters ?? []).SequenceEqual(other.Filters ?? [])
		&& (Filters is null) == (other.Filters is null)
		&& Style == other.Style
		&& Thickness == other.Thickness
		&& Size == other.Size
		&& Underline == other.Underline
		&& Opaque == other.Opaque
		&& XOffset == other.XOffset
		&& YOffset == other.YOffset;

	/// <inheritdoc />
	public override int GetHashCode()
	{
		HashCode hash = new();
		hash.Add(Bcg);

		foreach (int filter in Filters ?? [])
		{
			hash.Add(filter);
		}

		hash.Add(Style);
		hash.Add(Thickness);
		hash.Add(Size);
		hash.Add(Underline);
		hash.Add(Opaque);
		hash.Add(XOffset);
		hash.Add(YOffset);
		return hash.ToHashCode();
	}
}
