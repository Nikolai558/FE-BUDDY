namespace FeBuddy.Core.Infrastructure.Eram.Models;

/// <summary>
/// Display properties as an ERAM GeoMap writes them: an object's <c>DefaultLineProperties</c>,
/// <c>DefaultSymbolProperties</c> or <c>TextDefaultProperties</c>, or one element's own overrides.
/// A value the XML leaves out is <see langword="null"/>.
/// </summary>
/// <remarks>
/// <para>
/// Values are as ERAM wrote them: <see cref="Style"/> is ERAM's spelling (<c>Solid</c>,
/// <c>RNAVOnlyWaypoint</c>), not yet CRC's. Checking them against what CRC can draw is the
/// conversion's job.
/// </para>
/// <para>
/// ERAM's <c>Color</c> (always <c>White</c> in practice) and <c>DisplaySetting</c> have no CRC
/// equivalent and are not kept. ERAM text has no opaque background setting.
/// </para>
/// </remarks>
public sealed record EramProperties
{
	/// <summary>No properties at all.</summary>
	public static EramProperties None { get; } = new();

	/// <summary>Brightness control group (<c>BCGGroup</c>).</summary>
	public int? Bcg { get; init; }

	/// <summary>Filter groups (<c>FilterGroup</c>); <see langword="null"/> when the XML gives none.</summary>
	public IReadOnlyList<int>? Filters { get; init; }

	/// <summary>Line or symbol style (<c>LineStyle</c> / <c>SymbolStyle</c>), as ERAM spells it.</summary>
	public string? Style { get; init; }

	/// <summary>Line thickness.</summary>
	public int? Thickness { get; init; }

	/// <summary>Symbol or text size (<c>FontSize</c>).</summary>
	public int? Size { get; init; }

	/// <summary>Whether text is underlined.</summary>
	public bool? Underline { get; init; }

	/// <summary>Text offset across, in pixels (<c>XPixelOffset</c>).</summary>
	public int? XOffset { get; init; }

	/// <summary>Text offset down, in pixels (<c>YPixelOffset</c>).</summary>
	public int? YOffset { get; init; }

	/// <summary>Whether any property is set.</summary>
	public bool IsEmpty => this == None;

	/// <inheritdoc />
	/// <remarks>Compares <see cref="Filters"/> by content, so two elements with the same overrides group together.</remarks>
	public bool Equals(EramProperties? other) =>
		other is not null
		&& Bcg == other.Bcg
		&& (Filters ?? []).SequenceEqual(other.Filters ?? [])
		&& (Filters is null) == (other.Filters is null)
		&& Style == other.Style
		&& Thickness == other.Thickness
		&& Size == other.Size
		&& Underline == other.Underline
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
		hash.Add(XOffset);
		hash.Add(YOffset);
		return hash.ToHashCode();
	}
}
