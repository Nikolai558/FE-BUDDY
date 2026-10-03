using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

using FeBuddy.Wpf.Behaviors;

namespace FeBuddy.UnitTests.Wpf.Behaviors;

/// <summary>
/// Covers <see cref="InlineMarkdown"/>: the inlines a TextBlock gets for bold, italic and code, the
/// text it hands back as set, and what setting new text or none does.
/// </summary>
public sealed class InlineMarkdownTests
{
	[Fact]
	public void bold_text_is_a_semi_bold_inline_between_the_plain_parts()
	{
		StaThread.Run(() =>
		{
			TextBlock text = new();

			InlineMarkdown.SetText(text, "Only what **you** pick");

			Inline[] inlines = [.. text.Inlines];
			Assert.Equal(3, inlines.Length);
			Assert.NotEqual(FontWeights.SemiBold, inlines[0].FontWeight);
			Assert.NotEqual(FontWeights.SemiBold, inlines[2].FontWeight);

			Span middle = Assert.IsType<Span>(inlines[1]);
			Assert.Equal(FontWeights.SemiBold, middle.FontWeight);
			Run run = Assert.IsType<Run>(Assert.Single(middle.Inlines));
			Assert.Equal("you", run.Text);
		});
	}

	[Fact]
	public void italic_text_is_an_italic_inline()
	{
		StaThread.Run(() =>
		{
			TextBlock text = new();

			InlineMarkdown.SetText(text, "an *italic* word");

			Inline[] inlines = [.. text.Inlines];
			Assert.Equal(3, inlines.Length);
			Assert.Equal(FontStyles.Italic, inlines[1].FontStyle);
		});
	}

	[Fact]
	public void a_code_span_is_an_inline_of_its_own()
	{
		StaThread.Run(() =>
		{
			TextBlock text = new();

			InlineMarkdown.SetText(text, "Saved in `Upload_to_vNAS` now");

			Inline[] inlines = [.. text.Inlines];
			Assert.Equal(3, inlines.Length);

			Span code = Assert.IsType<Span>(inlines[1]);
			Run run = Assert.IsType<Run>(Assert.Single(code.Inlines));
			Assert.Equal("Upload_to_vNAS", run.Text);
			Assert.Equal(12, code.FontSize, precision: 6);
		});
	}

	[Fact]
	public void get_text_returns_what_was_set_markup_included()
	{
		StaThread.Run(() =>
		{
			TextBlock text = new();

			Assert.Null(InlineMarkdown.GetText(text));

			InlineMarkdown.SetText(text, "Only what **you** pick, in `Upload_to_vNAS`.");

			Assert.Equal("Only what **you** pick, in `Upload_to_vNAS`.", InlineMarkdown.GetText(text));
		});
	}

	[Fact]
	public void setting_new_text_replaces_the_inlines_and_none_clears_them()
	{
		StaThread.Run(() =>
		{
			TextBlock text = new();

			InlineMarkdown.SetText(text, "one **two** three");
			InlineMarkdown.SetText(text, "four");

			Run run = Assert.IsType<Run>(Assert.Single(Assert.IsType<Span>(Assert.Single(text.Inlines)).Inlines));
			Assert.Equal("four", run.Text);

			InlineMarkdown.SetText(text, null);

			Assert.Empty(text.Inlines);
			Assert.Null(InlineMarkdown.GetText(text));
		});
	}

	[Fact]
	public void a_null_text_block_is_rejected()
	{
		Assert.Throws<ArgumentNullException>(() => InlineMarkdown.SetText(null!, "text"));
		Assert.Throws<ArgumentNullException>(() => InlineMarkdown.GetText(null!));
	}
}
