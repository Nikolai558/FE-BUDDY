using FeBuddy.Wpf.Controls;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

namespace FeBuddy.UnitTests.Wpf.Controls;

/// <summary>
/// Covers the "What You'll Get" summary (issue #314): building a block's lines
/// (<see cref="SummaryLines"/>), keeping only the blocks for outputs that are on and merging those
/// that say the same (<see cref="SummaryBlock.ForOutputsOn"/>), and the <see cref="OutputSummary"/>
/// control tagging each block only when there is more than one. The control runs on a WPF thread
/// of its own.
/// </summary>
public sealed class OutputSummaryTests
{
	private const SubServiceOutputKinds Alias = SubServiceOutputKinds.Alias;
	private const SubServiceOutputKinds Geojson = SubServiceOutputKinds.Geojson;
	private const SubServiceOutputKinds Changes = SubServiceOutputKinds.ProcedureChanges;
	private const SubServiceOutputKinds Json = SubServiceOutputKinds.ProceduresJson;

	private static SummaryBlock Block(SubServiceOutputKinds outputs, params string[] lines)
	{
		SummaryLines built = new();

		foreach (string line in lines)
		{
			built.Add(SummaryJoin.And, line);
		}

		return new SummaryBlock(outputs, built.ToList());
	}

	// ---- the lines ----

	[Fact]
	public void the_first_line_has_no_join_and_an_unset_filter_is_left_out()
	{
		IReadOnlyList<SummaryLine> lines = new SummaryLines()
			.Add(SummaryJoin.And, null)
			.Add(SummaryJoin.Or, "every open airport")
			.Add(SummaryJoin.And, " ")
			.Add(SummaryJoin.Plus, "BRWNZ FIVE")
			.ToList();

		Assert.Equal([new SummaryLine(SummaryJoin.First, "every open airport"), new SummaryLine(SummaryJoin.Plus, "BRWNZ FIVE")], lines);
	}

	[Theory]
	[InlineData(SummaryJoin.First, "")]
	[InlineData(SummaryJoin.And, "AND")]
	[InlineData(SummaryJoin.Or, "OR")]
	[InlineData(SummaryJoin.Plus, "PLUS")]
	public void each_line_has_its_join_in_the_margin(SummaryJoin join, string expected)
	{
		Assert.Equal(expected, new SummaryLine(join, "x").JoinWord);
	}

	[Fact]
	public void words_are_listed_with_commas_and_the_conjunction_before_the_last()
	{
		Assert.Equal(string.Empty, SummaryLines.Join([], "or"));
		Assert.Equal("ZOB", SummaryLines.Join(["ZOB"], "or"));
		Assert.Equal("ZOB or ZNY", SummaryLines.Join(["ZOB", "ZNY"], "or"));
		Assert.Equal("IAP, STR and DP", SummaryLines.Join(["IAP", "STR", "DP"], "and"));
	}

	// ---- the blocks shown ----

	[Fact]
	public void only_the_blocks_for_outputs_that_are_on_are_shown_each_trimmed_to_them()
	{
		IReadOnlyList<SummaryBlock> shown = SummaryBlock.ForOutputsOn(
			Geojson | Json,
			Block(Changes | Json, "every chart"),
			Block(Alias, "every airport"));

		SummaryBlock block = Assert.Single(shown);
		Assert.Equal(Json, block.Outputs);
		Assert.Equal("every chart", Assert.Single(block.Lines).Text);
	}

	[Fact]
	public void blocks_that_say_the_same_are_merged_into_one()
	{
		IReadOnlyList<SummaryBlock> shown = SummaryBlock.ForOutputsOn(
			Alias | Geojson | Changes,
			Block(Geojson, "every airway"),
			Block(Changes, "every chart"),
			Block(Alias, "every airway"));

		Assert.Equal([Geojson | Alias, Changes], shown.Select(block => block.Outputs));
	}

	[Fact]
	public void with_every_output_off_nothing_is_shown()
	{
		Assert.Empty(SummaryBlock.ForOutputsOn(SubServiceOutputKinds.None, Block(Alias | Geojson, "every fix")));
	}

	// ---- the control ----

	[Fact]
	public void one_block_is_drawn_without_tags_since_the_cards_own_tags_say() =>
		StaThread.Run(() =>
		{
			OutputSummary summary = new() { Blocks = [Block(Alias | Geojson, "every fix")] };

			OutputSummaryItem item = Assert.Single(summary.Items);
			Assert.Empty(item.Tags);
			Assert.Equal("every fix", Assert.Single(item.Lines).Text);
		});

	[Fact]
	public void each_of_several_blocks_starts_with_its_outputs_tags() =>
		StaThread.Run(() =>
		{
			OutputSummary summary = new() { Blocks = [Block(Geojson, "every open airport", "inside the region"), Block(Alias, "every open airport")] };

			Assert.Equal(
				[new OutputTag(Geojson, true), new OutputTag(Alias, true)],
				summary.Items.Select(item => Assert.Single(item.Tags)));
			Assert.Equal(2, summary.Items[0].Lines.Count);
		});

	[Fact]
	public void no_blocks_draws_nothing() =>
		StaThread.Run(() =>
		{
			OutputSummary summary = new() { Blocks = [Block(Alias, "x")] };

			summary.Blocks = null;

			Assert.Empty(summary.Items);
		});
}
