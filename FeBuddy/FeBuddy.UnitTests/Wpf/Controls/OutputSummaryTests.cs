using FeBuddy.Wpf.Controls;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

namespace FeBuddy.UnitTests.Wpf.Controls;

/// <summary>
/// Covers the "What You'll Get" summary (issues #314, #333): building a block's lines
/// (<see cref="SummaryLines"/>), keeping only the blocks for outputs that are on and merging those
/// that say the same (<see cref="SummaryBlock.ForOutputsOn"/>), and the <see cref="OutputSummary"/>
/// control heading every block with its outputs' tags. The control runs on a WPF thread of its own.
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
			built.Add(SummaryJoin.AndOnly, line);
		}

		return new SummaryBlock(outputs, built.ToList());
	}

	// ---- the lines ----

	[Fact]
	public void the_first_line_has_no_chip_and_an_unset_filter_is_left_out()
	{
		SummaryLines built = new();
		Assert.True(built.IsEmpty);

		IReadOnlyList<SummaryLine> lines = built
			.Add(SummaryJoin.AndOnly, null)
			.Add(SummaryJoin.AlongWith, "Every open airport")
			.Add(SummaryJoin.AndOnly, " ")
			.Add(SummaryJoin.AlongWith, "BRWNZ FIVE")
			.ToList();

		Assert.False(built.IsEmpty);
		Assert.Equal([new SummaryLine(SummaryJoin.First, "Every open airport"), new SummaryLine(SummaryJoin.AlongWith, "BRWNZ FIVE")], lines);
	}

	/// <summary>A narrowing reads "but only" right below the first line and "and only" anywhere else, whichever is asked for.</summary>
	[Fact]
	public void a_narrowing_reads_but_only_below_the_first_line_and_and_only_after_that()
	{
		IReadOnlyList<SummaryLine> straight = new SummaryLines()
			.Add(SummaryJoin.First, "Every STAR")
			.Add(SummaryJoin.AndOnly, "those amended this cycle")
			.Add(SummaryJoin.ButOnly, "those inside the region")
			.ToList();

		Assert.Equal([SummaryJoin.First, SummaryJoin.ButOnly, SummaryJoin.AndOnly], straight.Select(line => line.Join));

		IReadOnlyList<SummaryLine> placed = new SummaryLines()
			.Add(SummaryJoin.First, "Every STAR")
			.Add(SummaryJoin.For, "airports in ZOB")
			.Add(SummaryJoin.ButOnly, "those amended this cycle")
			.ToList();

		Assert.Equal([SummaryJoin.First, SummaryJoin.For, SummaryJoin.AndOnly], placed.Select(line => line.Join));
	}

	/// <summary>"Outputs include" lines each say how they narrow the block, the first one too.</summary>
	[Fact]
	public void a_builder_told_to_can_keep_the_first_lines_chip()
	{
		IReadOnlyList<SummaryLine> lines = new SummaryLines(joinFirstLine: true)
			.Add(SummaryJoin.WithOnly, "these types: IAP")
			.ToList();

		Assert.Equal([new SummaryLine(SummaryJoin.WithOnly, "these types: IAP")], lines);
	}

	[Theory]
	[InlineData(SummaryJoin.First, "", false)]
	[InlineData(SummaryJoin.For, "for", true)]
	[InlineData(SummaryJoin.In, "in", true)]
	[InlineData(SummaryJoin.ButOnly, "but only", true)]
	[InlineData(SummaryJoin.AndOnly, "and only", true)]
	[InlineData(SummaryJoin.WithOnly, "with only", true)]
	[InlineData(SummaryJoin.AlongWith, "along with", true)]
	public void each_line_after_the_first_starts_with_a_chip(SummaryJoin join, string expected, bool hasJoin)
	{
		SummaryLine line = new(join, "x");

		Assert.Equal(expected, line.JoinWord);
		Assert.Equal(hasJoin, line.HasJoin);
	}

	[Fact]
	public void words_are_listed_with_the_oxford_comma()
	{
		Assert.Equal(string.Empty, SummaryLines.Join([], "or"));
		Assert.Equal("ZOB", SummaryLines.Join(["ZOB"], "or"));
		Assert.Equal("ZOB or ZNY", SummaryLines.Join(["ZOB", "ZNY"], "or"));
		Assert.Equal("IAP, STAR, and DP", SummaryLines.Join(["IAP", "STAR", "DP"], "and"));
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
	public void blocks_with_the_same_lines_but_different_outputs_include_stay_apart()
	{
		SummaryLine[] changedOnly = [new SummaryLine(SummaryJoin.First, "only those changed")];

		IReadOnlyList<SummaryBlock> shown = SummaryBlock.ForOutputsOn(
			Changes | Json,
			Block(Changes, "every chart") with { Includes = changedOnly },
			Block(Json, "every chart"),
			Block(Alias, "every chart") with { Includes = changedOnly });

		Assert.Equal([Changes, Json], shown.Select(block => block.Outputs));
		Assert.Equal(changedOnly, shown[0].Includes);
		Assert.Empty(shown[1].Includes);
	}

	[Fact]
	public void with_every_output_off_nothing_is_shown()
	{
		Assert.Empty(SummaryBlock.ForOutputsOn(SubServiceOutputKinds.None, Block(Alias | Geojson, "every fix")));
	}

	// ---- the control ----

	[Fact]
	public void even_a_lone_block_is_headed_by_its_outputs_tags() =>
		StaThread.Run(() =>
		{
			OutputSummary summary = new() { Blocks = [Block(Alias | Geojson, "every fix")] };

			OutputSummaryItem item = Assert.Single(summary.Items);
			Assert.Equal([new OutputTag(Alias, true), new OutputTag(Geojson, true)], item.Tags);
			Assert.Equal("every fix", Assert.Single(item.Lines).Text);
			Assert.False(item.HasIncludes);
		});

	[Fact]
	public void each_of_several_blocks_starts_with_its_outputs_tags_and_keeps_its_outputs_include() =>
		StaThread.Run(() =>
		{
			SummaryLine[] includes = [new SummaryLine(SummaryJoin.WithOnly, "these types: IAP")];
			OutputSummary summary = new()
			{
				Blocks = [Block(Geojson, "every open airport", "inside the region") with { Includes = includes }, Block(Alias, "every open airport")],
			};

			Assert.Equal(
				[new OutputTag(Geojson, true), new OutputTag(Alias, true)],
				summary.Items.Select(item => Assert.Single(item.Tags)));
			Assert.Equal(2, summary.Items[0].Lines.Count);
			Assert.True(summary.Items[0].HasIncludes);
			Assert.Equal(includes, summary.Items[0].Includes);
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
