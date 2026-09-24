using FeBuddy.Core.Infrastructure.Markdown;
using FeBuddy.Core.Infrastructure.Markdown.Models;

namespace FeBuddy.UnitTests.Infrastructure.Markdown;

/// <summary>
/// Covers <see cref="MarkdownParser"/>: the block structure (headings, paragraphs, nested lists,
/// code, quotes, rules, comments) and the inline styles and links, using the shapes GitHub
/// release notes actually contain.
/// </summary>
public sealed class MarkdownParserTests
{
	private const string Issues = "https://github.com/owner/repo/issues/";

	// ------------------------------------------------------------------ blocks

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   \n\n  ")]
	public void parse_blank_input_returns_no_blocks(string? markdown)
	{
		Assert.Empty(MarkdownParser.Parse(markdown));
	}

	[Fact]
	public void parse_headings_read_level_and_strip_closing_hashes()
	{
		var blocks = MarkdownParser.Parse("# One\n## Two ##\n###### Six\n#\n####### Seven");

		Assert.Equal(5, blocks.Count);
		AssertHeading(blocks[0], 1, "One");
		AssertHeading(blocks[1], 2, "Two");
		AssertHeading(blocks[2], 6, "Six");
		AssertHeading(blocks[3], 1, string.Empty);
		Assert.Equal("####### Seven", Text(Assert.IsType<MarkdownParagraph>(blocks[4]).Spans));
	}

	[Fact]
	public void parse_paragraph_lines_join_soft_breaks_and_keep_hard_breaks()
	{
		var blocks = MarkdownParser.Parse("one\n  two  \nthree\\\nfour\n\nnext");

		Assert.Equal(2, blocks.Count);
		Assert.Equal("one two\nthree\nfour", Text(Assert.IsType<MarkdownParagraph>(blocks[0]).Spans));
		Assert.Equal("next", Text(Assert.IsType<MarkdownParagraph>(blocks[1]).Spans));
	}

	[Fact]
	public void parse_cr_lf_and_tabs_are_normalised()
	{
		var blocks = MarkdownParser.Parse("- a\r\n\t- b\r\n\r\ntext\rmore");

		var list = Assert.IsType<MarkdownList>(blocks[0]);
		var nested = Assert.IsType<MarkdownList>(list.Items[0].Blocks[1]);
		Assert.Equal("b", ItemText(nested, 0));
		Assert.Equal("text more", Text(Assert.IsType<MarkdownParagraph>(blocks[1]).Spans));
	}

	[Theory]
	[InlineData("---")]
	[InlineData("***")]
	[InlineData("_ _ _")]
	[InlineData("   - - - -")]
	public void parse_rules(string line)
	{
		var blocks = MarkdownParser.Parse($"above\n{line}\nbelow");

		Assert.Equal(3, blocks.Count);
		Assert.IsType<MarkdownRule>(blocks[1]);
	}

	[Fact]
	public void parse_html_comments_are_dropped()
	{
		var blocks = MarkdownParser.Parse("before\n<!-- one line -->\nmiddle\n<!--\nPostId: 1\n-->\nafter\n<!-- never closed\nlost");

		Assert.Equal(["before", "middle", "after"], blocks.Select(b => Text(((MarkdownParagraph)b).Spans)));
	}

	[Fact]
	public void parse_fenced_code_is_verbatim_and_dedented()
	{
		var blocks = MarkdownParser.Parse("text\n  ```csharp\n  var x = **1**;\n    indented\nshort\n  ```\nafter");

		Assert.Equal(3, blocks.Count);
		Assert.Equal("var x = **1**;\n  indented\nshort", Assert.IsType<MarkdownCodeBlock>(blocks[1]).Text);
		Assert.IsType<MarkdownParagraph>(blocks[2]);
	}

	[Fact]
	public void parse_unclosed_fence_runs_to_the_end()
	{
		var blocks = MarkdownParser.Parse("~~~\na\n```\nb");

		Assert.Equal("a\n```\nb", Assert.IsType<MarkdownCodeBlock>(Assert.Single(blocks)).Text);
	}

	[Fact]
	public void parse_quote_collects_marked_and_lazy_lines()
	{
		var blocks = MarkdownParser.Parse("> # Note\n> first\nlazy\n>\n> second\n## After");

		var quote = Assert.IsType<MarkdownQuote>(blocks[0]);
		AssertHeading(quote.Blocks[0], 1, "Note");
		Assert.Equal("first lazy", Text(Assert.IsType<MarkdownParagraph>(quote.Blocks[1]).Spans));
		Assert.Equal("second", Text(Assert.IsType<MarkdownParagraph>(quote.Blocks[2]).Spans));
		AssertHeading(blocks[1], 2, "After");
	}

	[Fact]
	public void parse_quote_ends_at_blank_line()
	{
		var blocks = MarkdownParser.Parse("> quoted\n\nplain");

		Assert.IsType<MarkdownQuote>(blocks[0]);
		Assert.IsType<MarkdownParagraph>(blocks[1]);
	}

	[Fact]
	public void parse_bullet_list_with_nesting_and_continuation()
	{
		const string markdown = """
            ## Change log:
            - Top one
              - Child one
              - Child two that wraps
                onto a second line.
            * Top two
            lazy continuation
            + Top three
            """;

		var blocks = MarkdownParser.Parse(markdown);

		Assert.Equal(2, blocks.Count);
		var list = Assert.IsType<MarkdownList>(blocks[1]);
		Assert.False(list.Ordered);
		Assert.Equal(3, list.Items.Count);
		Assert.Equal("Top one", ItemText(list, 0));
		Assert.Equal("Top two lazy continuation", ItemText(list, 1));
		Assert.Equal("Top three", ItemText(list, 2));

		var nested = Assert.IsType<MarkdownList>(list.Items[0].Blocks[1]);
		Assert.Equal(2, nested.Items.Count);
		Assert.Equal("Child two that wraps onto a second line.", ItemText(nested, 1));
	}

	[Fact]
	public void parse_ordered_list_keeps_start_number()
	{
		var blocks = MarkdownParser.Parse("3. three\n4) four\n- bullet");

		var ordered = Assert.IsType<MarkdownList>(blocks[0]);
		Assert.True(ordered.Ordered);
		Assert.Equal(3, ordered.Start);
		Assert.Equal(2, ordered.Items.Count);

		var bullets = Assert.IsType<MarkdownList>(blocks[1]);
		Assert.False(bullets.Ordered);
		Assert.Equal(1, bullets.Start);
	}

	[Fact]
	public void parse_loose_list_blank_lines_between_items_keep_one_list()
	{
		var blocks = MarkdownParser.Parse("- a\n\n- b\n\n  more of b\n\n\nafter");

		var list = Assert.IsType<MarkdownList>(blocks[0]);
		Assert.Equal(2, list.Items.Count);
		Assert.Equal(2, list.Items[1].Blocks.Count);
		Assert.Equal("more of b", Text(((MarkdownParagraph)list.Items[1].Blocks[1]).Spans));
		Assert.Equal("after", Text(Assert.IsType<MarkdownParagraph>(blocks[1]).Spans));
	}

	[Fact]
	public void parse_list_ends_at_heading_or_blank_then_text()
	{
		var blocks = MarkdownParser.Parse("- a\n# Heading\n- b\n\ntext");

		Assert.Equal(4, blocks.Count);
		Assert.IsType<MarkdownList>(blocks[0]);
		Assert.IsType<MarkdownHeading>(blocks[1]);
		Assert.IsType<MarkdownList>(blocks[2]);
		Assert.IsType<MarkdownParagraph>(blocks[3]);
	}

	[Fact]
	public void parse_list_shallow_nested_marker_still_nests()
	{
		var blocks = MarkdownParser.Parse("10. parent\n  - child");

		var list = Assert.IsType<MarkdownList>(Assert.Single(blocks));
		var nested = Assert.IsType<MarkdownList>(list.Items[0].Blocks[1]);
		Assert.Equal("child", ItemText(nested, 0));
	}

	[Fact]
	public void parse_list_empty_item_and_wide_gap()
	{
		var blocks = MarkdownParser.Parse("-\n-      code-ish");

		var list = Assert.IsType<MarkdownList>(Assert.Single(blocks));
		Assert.Equal(2, list.Items.Count);
		Assert.Empty(list.Items[0].Blocks);
		Assert.Equal("code-ish", ItemText(list, 1));
	}

	[Fact]
	public void parse_the_v290_release_notes_shape()
	{
		const string markdown = """
            ## Instructions to install:
            - If new/fresh install: [Download](https://github.com/o/r/releases/latest/download/Setup.exe) and run it.


            ## Change log:
            - Bug #166 - Wrong coordinates.
              - On PCs that use a comma as the decimal separator, coordinate conversions produced
                incorrect values. FE-BUDDY now always uses "." internally.
            - (Dev notes)
              - Migrated from .NET 6 to .NET 10.
            """;

		var blocks = MarkdownParser.Parse(markdown, Issues);

		Assert.Equal(4, blocks.Count);
		AssertHeading(blocks[0], 2, "Instructions to install:");
		var install = Assert.IsType<MarkdownList>(blocks[1]);
		var installSpans = ((MarkdownParagraph)install.Items[0].Blocks[0]).Spans;
		Assert.Contains(installSpans, s => s.Text == "Download" && s.Url == "https://github.com/o/r/releases/latest/download/Setup.exe");

		var changes = Assert.IsType<MarkdownList>(blocks[3]);
		Assert.Equal(2, changes.Items.Count);
		var bugSpans = ((MarkdownParagraph)changes.Items[0].Blocks[0]).Spans;
		Assert.Contains(bugSpans, s => s.Text == "#166" && s.Url == Issues + "166");

		var detail = Assert.IsType<MarkdownList>(changes.Items[0].Blocks[1]);
		Assert.Equal(
			"On PCs that use a comma as the decimal separator, coordinate conversions produced incorrect values. FE-BUDDY now always uses \".\" internally.",
			ItemText(detail, 0));
	}

	// ----------------------------------------------------------------- inlines

	[Fact]
	public void parse_inlines_null_throws()
	{
		Assert.Throws<ArgumentNullException>(() => MarkdownParser.ParseInlines(null!));
	}

	[Fact]
	public void parse_inlines_emphasis()
	{
		var spans = MarkdownParser.ParseInlines("a **bold** *it* __b2__ _i2_ ***both*** ~~gone~~ z");

		Assert.Contains(new MarkdownSpan("bold", MarkdownStyle.Bold), spans);
		Assert.Contains(new MarkdownSpan("it", MarkdownStyle.Italic), spans);
		Assert.Contains(new MarkdownSpan("b2", MarkdownStyle.Bold), spans);
		Assert.Contains(new MarkdownSpan("i2", MarkdownStyle.Italic), spans);
		Assert.Contains(new MarkdownSpan("both", MarkdownStyle.Bold | MarkdownStyle.Italic), spans);
		Assert.Contains(new MarkdownSpan("gone", MarkdownStyle.Strikethrough), spans);
		Assert.Equal("a bold it b2 i2 both gone z", Text(spans));
	}

	[Fact]
	public void parse_inlines_nested_emphasis()
	{
		var spans = MarkdownParser.ParseInlines("*a **b** c*");

		Assert.Equal(
			[
				new MarkdownSpan("a ", MarkdownStyle.Italic),
				new MarkdownSpan("b", MarkdownStyle.Italic | MarkdownStyle.Bold),
				new MarkdownSpan(" c", MarkdownStyle.Italic),
			],
			spans);
	}

	[Theory]
	[InlineData("5 * 3 * 2")]
	[InlineData("snake_case_name")]
	[InlineData("**unclosed")]
	[InlineData("~single~")]
	[InlineData("a ** b")]
	[InlineData("trailing *")]
	[InlineData("*open *")]
	[InlineData("_a_b")]
	[InlineData("**")]
	public void parse_inlines_non_emphasis_stays_literal(string text)
	{
		var span = Assert.Single(MarkdownParser.ParseInlines(text));

		Assert.Equal(new MarkdownSpan(text), span);
	}

	[Fact]
	public void parse_inlines_underscore_closes_at_end_of_text()
	{
		Assert.Equal([new MarkdownSpan("x", MarkdownStyle.Italic)], MarkdownParser.ParseInlines("_x_"));
	}

	[Fact]
	public void parse_inlines_emphasis_skips_escapes_and_code()
	{
		var spans = MarkdownParser.ParseInlines("*a \\* `b*` c*");

		Assert.Equal(
			[
				new MarkdownSpan("a * ", MarkdownStyle.Italic),
				new MarkdownSpan("b*", MarkdownStyle.Italic | MarkdownStyle.Code),
				new MarkdownSpan(" c", MarkdownStyle.Italic),
			],
			spans);
	}

	[Fact]
	public void parse_inlines_emphasis_past_unclosed_code()
	{
		Assert.Equal([new MarkdownSpan("a `b", MarkdownStyle.Bold)], MarkdownParser.ParseInlines("**a `b**"));
	}

	[Fact]
	public void parse_inlines_code_spans()
	{
		var spans = MarkdownParser.ParseInlines("use `x` or `` a`b `` or ` padded ` or ``` odd");

		Assert.Contains(new MarkdownSpan("x", MarkdownStyle.Code), spans);
		Assert.Contains(new MarkdownSpan("a`b", MarkdownStyle.Code), spans);
		Assert.Contains(new MarkdownSpan("padded", MarkdownStyle.Code), spans);
		Assert.EndsWith(" or ``` odd", spans[^1].Text);
	}

	[Fact]
	public void parse_inlines_escapes()
	{
		Assert.Equal([new MarkdownSpan("*not* [x] \\n")], MarkdownParser.ParseInlines("\\*not\\* \\[x\\] \\n"));
	}

	[Fact]
	public void parse_inlines_links()
	{
		var spans = MarkdownParser.ParseInlines("see [the **docs**](https://x.test/a_(b) \"Title\") and [mail](<mailto:a@b.test>)");

		Assert.Equal(
			[
				new MarkdownSpan("see "),
				new MarkdownSpan("the ", MarkdownStyle.None, "https://x.test/a_(b)"),
				new MarkdownSpan("docs", MarkdownStyle.Bold, "https://x.test/a_(b)"),
				new MarkdownSpan(" and "),
				new MarkdownSpan("mail", MarkdownStyle.None, "mailto:a@b.test"),
			],
			spans);
	}

	[Theory]
	[InlineData("[rel](docs/readme.md)")]
	[InlineData("[js](javascript:alert(1))")]
	[InlineData("[file](file:///c:/x)")]
	public void parse_inlines_unsafe_or_relative_links_are_unlinked(string text)
	{
		var spans = MarkdownParser.ParseInlines(text);

		Assert.All(spans, s => Assert.Null(s.Url));
	}

	[Fact]
	public void parse_inlines_empty_link_label_shows_the_url()
	{
		Assert.Equal([new MarkdownSpan("https://x.test", MarkdownStyle.None, "https://x.test")], MarkdownParser.ParseInlines("[](https://x.test)"));
	}

	[Fact]
	public void parse_inlines_image_becomes_a_link_to_the_image()
	{
		Assert.Equal(
			[new MarkdownSpan("shot", MarkdownStyle.None, "https://x.test/a.png"), new MarkdownSpan(" !")],
			MarkdownParser.ParseInlines("![shot](https://x.test/a.png) !"));
	}

	[Theory]
	[InlineData("[no target]")]
	[InlineData("[no close")]
	[InlineData("[x](unclosed")]
	[InlineData("[x] (gap)")]
	[InlineData("[x]")]
	public void parse_inlines_broken_links_stay_literal(string text)
	{
		Assert.Equal([new MarkdownSpan(text)], MarkdownParser.ParseInlines(text));
	}

	[Fact]
	public void parse_inlines_autolink()
	{
		Assert.Equal(
			[new MarkdownSpan("a "), new MarkdownSpan("https://x.test/p", MarkdownStyle.None, "https://x.test/p"), new MarkdownSpan(" <b>")],
			MarkdownParser.ParseInlines("a <https://x.test/p> <b>"));
	}

	[Fact]
	public void parse_inlines_bare_urls_trim_sentence_punctuation()
	{
		var spans = MarkdownParser.ParseInlines("Go to https://x.test/a. Or (https://x.test/b_(c)). hello");

		Assert.Contains(new MarkdownSpan("https://x.test/a", MarkdownStyle.None, "https://x.test/a"), spans);
		Assert.Contains(new MarkdownSpan("https://x.test/b_(c)", MarkdownStyle.None, "https://x.test/b_(c)"), spans);
		Assert.Equal(new MarkdownSpan("). hello"), spans[^1]);
	}

	[Fact]
	public void parse_inlines_bare_url_needs_a_word_boundary()
	{
		Assert.Equal([new MarkdownSpan("xhttps://x.test")], MarkdownParser.ParseInlines("xhttps://x.test"));
	}

	[Fact]
	public void parse_inlines_bare_url_inside_a_link_is_not_relinked()
	{
		Assert.Equal(
			[new MarkdownSpan("https://a.test", MarkdownStyle.None, "https://b.test")],
			MarkdownParser.ParseInlines("[https://a.test](https://b.test)"));
	}

	[Fact]
	public void parse_inlines_issue_references()
	{
		var spans = MarkdownParser.ParseInlines("Bug #166, (#7) C#9 #12a #", Issues);

		Assert.Equal(
			[
				new MarkdownSpan("Bug "),
				new MarkdownSpan("#166", MarkdownStyle.None, Issues + "166"),
				new MarkdownSpan(", ("),
				new MarkdownSpan("#7", MarkdownStyle.None, Issues + "7"),
				new MarkdownSpan(") C#9 #12a #"),
			],
			spans);
	}

	[Fact]
	public void parse_inlines_issue_references_off_without_a_base()
	{
		Assert.Equal([new MarkdownSpan("Bug #166")], MarkdownParser.ParseInlines("Bug #166"));
	}

	// ----------------------------------------------------------------- helpers

	private static string Text(IEnumerable<MarkdownSpan> spans) => string.Concat(spans.Select(s => s.Text));

	private static string ItemText(MarkdownList list, int index)
		=> Text(Assert.IsType<MarkdownParagraph>(list.Items[index].Blocks[0]).Spans);

	private static void AssertHeading(MarkdownBlock block, int level, string text)
	{
		var heading = Assert.IsType<MarkdownHeading>(block);
		Assert.Equal(level, heading.Level);
		Assert.Equal(text, Text(heading.Spans));
	}
}
