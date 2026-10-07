using System.Windows.Controls;

using FeBuddy.Wpf.Controls;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

namespace FeBuddy.UnitTests.Wpf.Controls;

/// <summary>
/// Covers the AIRAC Service's output tags (<see cref="OutputTag"/>): one tag per output a card
/// affects, in the General tab's column order, struck through while off; the note a card shows
/// while all of them are off; and a <see cref="Card"/> and an <see cref="OutputLine"/> following the
/// outputs and the sub-service set once on the page. The controls run on a WPF thread of their own.
/// </summary>
public sealed class OutputTagTests
{
	private const SubServiceOutputKinds Alias = SubServiceOutputKinds.Alias;
	private const SubServiceOutputKinds Geojson = SubServiceOutputKinds.Geojson;
	private const SubServiceOutputKinds Changes = SubServiceOutputKinds.ProcedureChanges;
	private const SubServiceOutputKinds Json = SubServiceOutputKinds.ProceduresJson;

	// ---- the tags and the note ----

	[Fact]
	public void a_card_gets_a_tag_per_output_in_column_order_each_on_or_off()
	{
		Assert.Equal(
			[new OutputTag(Alias, true), new OutputTag(Geojson, false)],
			OutputTag.For(Geojson | Alias, on: Alias | Changes));
		Assert.Empty(OutputTag.For(SubServiceOutputKinds.None, OutputKinds.Every));
	}

	[Fact]
	public void each_tag_has_a_short_name()
	{
		Assert.Equal(["Alias", "GeoJSON", "Changes", "JSON"], OutputTag.For(OutputKinds.Every, OutputKinds.Every).Select(tag => tag.Label));
	}

	[Fact]
	public void a_tag_says_what_it_affects_and_whether_that_is_off()
	{
		Assert.Equal("Affects the GeoJSON files.", new OutputTag(Geojson, true).ToolTip);
		Assert.Equal("Affects the alias file, which is off on the General tab.", new OutputTag(Alias, false).ToolTip);
		Assert.Equal("Whether each sub-service writes Procedures.json.", OutputTag.JsonHeading.ToolTip);
	}

	[Fact]
	public void the_general_tabs_headings_are_one_tag_per_column_always_on()
	{
		OutputTag[] headings = [OutputTag.AliasHeading, OutputTag.GeojsonHeading, OutputTag.ChangesHeading, OutputTag.JsonHeading];

		Assert.Equal(OutputKinds.InOrder, headings.Select(tag => tag.Kind));
		Assert.All(headings, tag => Assert.True(tag.IsOn && tag.IsHeading));
	}

	[Theory]
	[InlineData(Geojson, "GeoJSON is off for Departures. Turn it on in the General tab.")]
	[InlineData(Alias | Geojson, "The alias file and GeoJSON are off for Departures. Turn one on in the General tab.")]
	[InlineData(Alias | Changes | Json, "The alias file, Procedure_Changes.md and Procedures.json are off for Departures. Turn one on in the General tab.")]
	public void with_every_output_off_a_card_says_which_and_where_to_turn_one_on(SubServiceOutputKinds outputs, string expected)
	{
		Assert.Equal(expected, OutputTag.OffNote(outputs, on: SubServiceOutputKinds.None, "Departures"));
	}

	[Theory]
	[InlineData(Alias | Geojson, Alias)]
	[InlineData(SubServiceOutputKinds.None, SubServiceOutputKinds.None)]
	public void with_one_output_on_or_none_to_affect_a_card_has_no_note(SubServiceOutputKinds outputs, SubServiceOutputKinds on)
	{
		Assert.Null(OutputTag.OffNote(outputs, on, "Departures"));
	}

	// ---- the controls ----

	/// <summary>A card on an AIRAC Service tab: the page says which outputs are on and whose they are.</summary>
	private static (ContentControl Page, Card Card, OutputLine Line) Page(SubServiceOutputKinds on, string? owner = "Departures")
	{
		Card card = new() { Header = "Procedures", Outputs = Alias | Geojson };
		OutputLine line = new() { Kind = Geojson, Text = "Only the procedures inside the region." };
		ContentControl page = new() { Content = new StackPanel { Children = { card, line } } };
		Card.SetOutputsOn(page, on);
		Card.SetOutputsOwner(page, owner);
		return (page, card, line);
	}

	[Fact]
	public void a_card_tags_its_outputs_and_stays_enabled_while_one_is_on() =>
		StaThread.Run(() =>
		{
			(_, Card card, OutputLine line) = Page(on: Alias);

			Assert.Equal([new OutputTag(Alias, true), new OutputTag(Geojson, false)], card.OutputTags);
			Assert.Null(card.OutputsOffNote);
			Assert.True(card.IsEnabled);
			Assert.Equal(new OutputTag(Geojson, false), line.Output);
		});

	[Fact]
	public void a_card_greys_out_with_a_note_while_every_output_it_affects_is_off_and_comes_back_when_one_is_on() =>
		StaThread.Run(() =>
		{
			(ContentControl page, Card card, OutputLine line) = Page(on: Changes);

			Assert.False(card.IsEnabled);
			Assert.Equal("The alias file and GeoJSON are off for Departures. Turn one on in the General tab.", card.OutputsOffNote);

			Card.SetOutputsOn(page, Geojson);

			Assert.True(card.IsEnabled);
			Assert.Null(card.OutputsOffNote);
			Assert.Equal(new OutputTag(Geojson, true), line.Output);
		});

	/// <summary>Off the AIRAC Service (File Conversions shares the cards), nothing says whose outputs they are: no tags, no greying.</summary>
	[Fact]
	public void a_card_off_the_airac_service_shows_no_tags_and_is_never_greyed_by_them() =>
		StaThread.Run(() =>
		{
			(_, Card card, _) = Page(on: SubServiceOutputKinds.None, owner: null);

			Assert.Empty(card.OutputTags);
			Assert.Null(card.OutputsOffNote);
			Assert.True(card.IsEnabled);
		});

	[Fact]
	public void a_line_with_no_output_has_no_tag() =>
		StaThread.Run(() => Assert.Null(new OutputLine { Text = "Nothing" }.Output));
}
