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
			[("Aliases", true), (" and ", false), ("Upload_to_vNAS", true)],
			InlineCode.Split("`Aliases` and `Upload_to_vNAS`"));
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
