using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// One output tag: a small coloured chip saying which output a card's settings affect - Alias,
/// GeoJSON, Changes (<c>Procedure_Changes.md</c>) or JSON (<c>Procedures.json</c>) - struck through
/// while that output is off on the General tab. Drawn by the <c>OutputTag</c> template in
/// Theme/Controls.Surfaces.xaml, on a <see cref="Card"/>'s title row, an <see cref="OutputLine"/> and
/// the General tab's column headings.
/// </summary>
/// <param name="Kind">The output.</param>
/// <param name="IsOn">Whether it is on.</param>
public sealed record OutputTag(SubServiceOutputKinds Kind, bool IsOn)
{
	/// <summary>The chip's text, e.g. <c>GeoJSON</c>.</summary>
	public string Label => Kind switch
	{
		SubServiceOutputKinds.Alias => "Alias",
		SubServiceOutputKinds.Geojson => "GeoJSON",
		SubServiceOutputKinds.ProcedureChanges => "Changes",
		SubServiceOutputKinds.ProceduresJson => "JSON",
		_ => Kind.ToString(),
	};

	/// <summary>The Alias column's heading on the General tab.</summary>
	public static OutputTag AliasHeading { get; } = new(SubServiceOutputKinds.Alias, IsOn: true) { IsHeading = true };

	/// <summary>The GeoJSON column's heading on the General tab.</summary>
	public static OutputTag GeojsonHeading { get; } = new(SubServiceOutputKinds.Geojson, IsOn: true) { IsHeading = true };

	/// <summary>The Procedure Changes column's heading on the General tab.</summary>
	public static OutputTag ChangesHeading { get; } = new(SubServiceOutputKinds.ProcedureChanges, IsOn: true) { IsHeading = true };

	/// <summary>The Procedures JSON column's heading on the General tab.</summary>
	public static OutputTag JsonHeading { get; } = new(SubServiceOutputKinds.ProceduresJson, IsOn: true) { IsHeading = true };

	/// <summary>Whether the tag heads a column of the General tab's table rather than marking a card.</summary>
	public bool IsHeading { get; init; }

	/// <summary>What the chip says on hover: what a card's settings affect, or for a heading, which file the column is.</summary>
	public string ToolTip => IsHeading
		? $"Whether each sub-service writes {Name(Kind)}."
		: IsOn
			? $"Affects {Name(Kind)}."
			: $"Affects {Name(Kind)}, which is off on the General tab.";

	/// <summary>The tags for a card: one per output it affects, in the General tab's column order.</summary>
	/// <param name="outputs">The outputs the card affects.</param>
	/// <param name="on">The outputs that are on.</param>
	/// <returns>The tags; none when the card affects no output.</returns>
	public static IReadOnlyList<OutputTag> For(SubServiceOutputKinds outputs, SubServiceOutputKinds on) =>
		[.. OutputKinds.InOrder.Where(kind => outputs.HasFlag(kind)).Select(kind => new OutputTag(kind, on.HasFlag(kind)))];

	/// <summary>
	/// What a card says while every output it affects is off, e.g. <c>GeoJSON is off for Departures.
	/// Turn it on in the General tab.</c>; <see langword="null"/> while one is on, or the card affects none.
	/// </summary>
	/// <param name="outputs">The outputs the card affects.</param>
	/// <param name="on">The outputs that are on.</param>
	/// <param name="owner">The sub-service the card is on, e.g. <c>Departures</c>.</param>
	/// <returns>The note, or <see langword="null"/>.</returns>
	public static string? OffNote(SubServiceOutputKinds outputs, SubServiceOutputKinds on, string owner)
	{
		if (outputs == SubServiceOutputKinds.None || (outputs & on) != SubServiceOutputKinds.None)
		{
			return null;
		}

		string[] names = [.. OutputKinds.InOrder.Where(kind => outputs.HasFlag(kind)).Select(NoteName)];
		string first = char.ToUpperInvariant(names[0][0]) + names[0][1..];
		string all = names.Length == 1 ? first : $"{string.Join(", ", [first, .. names[1..^1]])} and {names[^1]}";

		return names.Length == 1
			? $"{all} is off for {owner}. Turn it on in the General tab."
			: $"{all} are off for {owner}. Turn one on in the General tab.";
	}

	/// <summary>An output's name in a sentence, e.g. <c>the alias file</c>.</summary>
	private static string Name(SubServiceOutputKinds kind) => kind switch
	{
		SubServiceOutputKinds.Geojson => "the GeoJSON files",
		_ => NoteName(kind),
	};

	/// <summary>An output's name as the subject of "is off", e.g. <c>the alias file</c> or <c>GeoJSON</c>.</summary>
	private static string NoteName(SubServiceOutputKinds kind) => kind switch
	{
		SubServiceOutputKinds.Alias => "the alias file",
		SubServiceOutputKinds.Geojson => "GeoJSON",
		SubServiceOutputKinds.ProcedureChanges => "Procedure_Changes.md",
		SubServiceOutputKinds.ProceduresJson => "Procedures.json",
		_ => kind.ToString(),
	};
}
