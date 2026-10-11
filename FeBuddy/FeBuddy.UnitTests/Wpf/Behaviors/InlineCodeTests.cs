using System.Windows.Controls;
using System.Windows.Documents;

using FeBuddy.Wpf.Behaviors;

namespace FeBuddy.UnitTests.Wpf.Behaviors;

/// <summary>
/// Covers <see cref="InlineCode"/>: where the text is split into code and plain parts, and the
/// runs it gives a TextBlock.
/// </summary>
public sealed class InlineCodeTests
{
	[Fact]
	public void text_between_backticks_is_code()
	{
		Assert.Equal(
			[("Files are written to ", false), (@"Geojson\", true), (" in the cycle's folder.", false)],
			InlineCode.Split(@"Files are written to `Geojson\` in the cycle's folder."));
	}

	[Fact]
	public void several_code_parts_and_code_at_either_end_split_cleanly()
	{
		Assert.Equal(
			[("Aliases", true), (" and ", false), ("Combined_Alias.txt", true)],
			InlineCode.Split("`Aliases` and `Combined_Alias.txt`"));
	}

	[Theory]
	[InlineData("No backticks at all")]
	[InlineData("An unpaired ` stays as written")]
	[InlineData("Two with nothing between `` stay too")]
	public void text_with_no_pair_is_all_plain(string text)
	{
		Assert.Equal([(text, false)], InlineCode.Split(text));
	}

	[Fact]
	public void an_unpaired_backtick_after_a_pair_stays_as_written()
	{
		Assert.Equal([("Aliases", true), (" then ` alone", false)], InlineCode.Split("`Aliases` then ` alone"));
	}

	[Fact]
	public void no_text_has_no_parts()
	{
		Assert.Empty(InlineCode.Split(string.Empty));
		Assert.Throws<ArgumentNullException>(() => InlineCode.Split(null!));
	}

	[Fact]
	public void a_markdown_link_is_split_from_the_text_around_it()
	{
		Assert.Equal(
			[("Check them out ", null), ("HERE", "https://docs.virtualnas.net/data-admin/video-maps/#map-defaults"), (".", null)],
			InlineCode.SplitLinks("Check them out [HERE](https://docs.virtualnas.net/data-admin/video-maps/#map-defaults)."));
		Assert.Equal(
			[("FAA", "https://www.faa.gov/"), (" and ", null), ("GNG", "http://gng.aero-nav.com/")],
			InlineCode.SplitLinks("[FAA](https://www.faa.gov/) and [GNG](http://gng.aero-nav.com/)"));
	}

	[Theory]
	[InlineData("No link at all")]
	[InlineData("A [bracket] on its own")]
	[InlineData("Not a web address: [file](C:\\temp\\x.txt)")]
	[InlineData("A relative one: [guide](docs/Users/User-Guide.md)")]
	[InlineData("No label: [](https://www.faa.gov/)")]
	[InlineData("Unclosed: [FAA](https://www.faa.gov/")]
	public void text_with_no_web_link_is_all_plain(string text)
	{
		Assert.Equal([(text, (string?)null)], InlineCode.SplitLinks(text));
	}

	[Fact]
	public void a_bracket_before_a_link_stays_in_the_text()
	{
		Assert.Equal(
			[("[x] see ", null), ("FAA", "https://www.faa.gov/")],
			InlineCode.SplitLinks("[x] see [FAA](https://www.faa.gov/)"));
		Assert.Empty(InlineCode.SplitLinks(string.Empty));
		Assert.Throws<ArgumentNullException>(() => InlineCode.SplitLinks(null!));
	}

	/// <summary>A link outside the backticks becomes a hyperlink with its address as the tooltip; one inside them stays code.</summary>
	[Fact]
	public void the_text_block_gets_a_hyperlink_for_a_link_outside_the_backticks()
	{
		StaThread.Run(() =>
		{
			TextBlock text = new();

			InlineCode.SetText(text, "See [HERE](https://www.faa.gov/) and `[x](https://a.b/)`");

			Inline[] inlines = [.. text.Inlines];
			Assert.Equal(4, inlines.Length);
			Hyperlink link = Assert.IsType<Hyperlink>(inlines[1]);
			Assert.Equal("HERE", Assert.IsType<Run>(Assert.Single(link.Inlines)).Text);
			Assert.Equal("https://www.faa.gov/", link.ToolTip);
			Assert.Equal("[x](https://a.b/)", Assert.IsType<Run>(inlines[3]).Text);
		});
	}

	/// <summary>Each part becomes a run; only the code runs are sized from the TextBlock (13/14.5 of it).</summary>
	[Fact]
	public void the_text_block_gets_a_run_per_part_with_code_sized_from_it()
	{
		StaThread.Run(() =>
		{
			TextBlock text = new() { FontSize = 14.5 };

			InlineCode.SetText(text, "Written to `Aliases\\` in the cycle's folder.");

			Run[] runs = [.. text.Inlines.Cast<Run>()];
			Assert.Equal(["Written to ", "Aliases\\", " in the cycle's folder."], runs.Select(run => run.Text));
			Assert.Equal(13, runs[1].FontSize, precision: 6);
			Assert.Equal("Written to `Aliases\\` in the cycle's folder.", InlineCode.GetText(text));

			text.FontSize = 12.5;
			Assert.Equal(12.5 * 13 / 14.5, runs[1].FontSize, precision: 6);

			InlineCode.SetText(text, null);
			Assert.Empty(text.Inlines);
		});
	}
}
