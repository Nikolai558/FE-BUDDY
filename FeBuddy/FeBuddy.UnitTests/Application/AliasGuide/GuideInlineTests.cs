using FeBuddy.Core.Application.AliasGuide;
using FeBuddy.Core.Application.AliasGuide.Models;

namespace FeBuddy.UnitTests.Application.AliasGuide;

/// <summary>
/// Covers <see cref="GuideInline"/>: plain, <c>**bold**</c> and <c>`code`</c> runs (a code run
/// carrying its command parts), the order they come in, and the text that is a format exception
/// rather than quietly shown wrong.
/// </summary>
public sealed class GuideInlineTests
{
	[Fact]
	public void text_with_no_markup_is_one_plain_run()
	{
		InlineRun run = Assert.Single(GuideInline.Parse("Just some words."));

		Assert.Equal(InlineStyle.Plain, run.Style);
		Assert.Equal("Just some words.", run.Text);
		Assert.Empty(run.Parts);
	}

	[Fact]
	public void text_between_double_asterisks_is_a_bold_run()
	{
		InlineRun run = Assert.Single(GuideInline.Parse("**One command per approach.**"));

		Assert.Equal(InlineStyle.Bold, run.Style);
		Assert.Equal("One command per approach.", run.Text);
		Assert.Empty(run.Parts);
	}

	[Fact]
	public void text_between_backticks_is_a_code_run_with_its_command_parts()
	{
		InlineRun run = Assert.Single(GuideInline.Parse("`.apt{a:DTW}`"));

		Assert.Equal(InlineStyle.Code, run.Style);
		Assert.Equal(".apt{a:DTW}", run.Text);
		Assert.Equal(
			[
				new CommandPart(".apt", CommandPartKind.Typed, IsPlaceholder: false, IsOptional: false),
				new CommandPart("DTW", CommandPartKind.Airport, IsPlaceholder: false, IsOptional: false),
			],
			run.Parts);
	}

	[Fact]
	public void code_with_no_brackets_is_a_single_typed_part()
	{
		InlineRun run = Assert.Single(GuideInline.Parse("`DOTSS2.DOTSS`"));

		Assert.Equal(
			[new CommandPart("DOTSS2.DOTSS", CommandPartKind.Typed, IsPlaceholder: false, IsOptional: false)],
			run.Parts);
	}

	[Fact]
	public void plain_bold_and_code_runs_come_in_the_order_written()
	{
		IReadOnlyList<InlineRun> runs = GuideInline.Parse("Type **either** `.apt{a:DTW}` or `.apt{a:KDTW}`.");

		Assert.Equal(
			[
				("Type ", InlineStyle.Plain),
				("either", InlineStyle.Bold),
				(" ", InlineStyle.Plain),
				(".apt{a:DTW}", InlineStyle.Code),
				(" or ", InlineStyle.Plain),
				(".apt{a:KDTW}", InlineStyle.Code),
				(".", InlineStyle.Plain),
			],
			runs.Select(run => (run.Text, run.Style)));
	}

	[Fact]
	public void asterisks_inside_code_and_a_single_asterisk_are_not_markup()
	{
		InlineRun code = Assert.Single(GuideInline.Parse("`a**b`"));
		InlineRun plain = Assert.Single(GuideInline.Parse("2 * 3"));

		Assert.Equal((InlineStyle.Code, "a**b"), (code.Style, code.Text));
		Assert.Equal((InlineStyle.Plain, "2 * 3"), (plain.Style, plain.Text));
	}

	[Fact]
	public void no_text_has_no_runs()
	{
		Assert.Empty(GuideInline.Parse(string.Empty));
	}

	[Theory]
	[InlineData("An `unclosed backtick")]
	[InlineData("An **unclosed bold")]
	[InlineData("Nothing between `` the backticks")]
	[InlineData("Nothing between **** the asterisks")]
	[InlineData("Code with bad markup: `[q:x]`")]
	public void unclosed_empty_or_malformed_markup_is_a_format_exception(string text)
	{
		Assert.Throws<FormatException>(() => GuideInline.Parse(text));
	}

	[Fact]
	public void null_text_is_rejected()
	{
		Assert.Throws<ArgumentNullException>(() => GuideInline.Parse(null!));
	}
}
