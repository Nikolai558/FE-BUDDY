using System.Globalization;
using System.Text;

using FeBuddy.Core.Application.AliasGuide;
using FeBuddy.Core.Application.AliasGuide.Models;

namespace FeBuddy.UnitTests.Application.AliasGuide;

/// <summary>
/// Covers <see cref="AliasGuideWriter"/>: the format a file name asks for; the web page's sections,
/// contents links, colour pills and footer; the Markdown's title, contents line, tables, lists and
/// footer; how the facility, version and date are named; the heading anchors; and exporting to a
/// file (UTF-8 with no byte order mark, replacing what is there).
/// </summary>
public sealed class AliasGuideWriterTests : IDisposable
{
	private const string RepositoryUrl = "https://github.com/Nikolai558/FE-BUDDY";

	private static readonly DateTime GeneratedUtc = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_AliasGuideWriter_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		if (Directory.Exists(_outputDirectory))
		{
			Directory.Delete(_outputDirectory, recursive: true);
		}
	}

	private static AliasGuideOptions Options(string? facility = "ZOB", string version = "3.0.0") => new(facility, version, GeneratedUtc);

	private static string Html(AliasGuideOptions? options = null) => AliasGuideWriter.Write(AliasGuideFormat.Html, options ?? Options());

	private static string Markdown(AliasGuideOptions? options = null) => AliasGuideWriter.Write(AliasGuideFormat.Markdown, options ?? Options());

	private static string[] Lines(string text) => text.Split(Environment.NewLine);

	private string TempFile(string name)
	{
		Directory.CreateDirectory(_outputDirectory);
		return Path.Combine(_outputDirectory, name);
	}

	private static string ReadBack(string path) => Encoding.UTF8.GetString(File.ReadAllBytes(path));

	// ---- the format a file name asks for ----

	[Theory]
	[InlineData("guide.md")]
	[InlineData("guide.MD")]
	[InlineData("guide.markdown")]
	[InlineData("guide.Markdown")]
	[InlineData(@"C:\Guides\FE-Buddy Alias Command Guide.md")]
	public void a_markdown_extension_asks_for_markdown(string path)
	{
		Assert.Equal(AliasGuideFormat.Markdown, AliasGuideWriter.FormatFor(path));
	}

	[Theory]
	[InlineData("guide.html")]
	[InlineData("guide.htm")]
	[InlineData("guide.txt")]
	[InlineData("guide.md.html")]
	[InlineData("guide.mdx")]
	[InlineData("guide")]
	[InlineData("")]
	[InlineData(null)]
	public void anything_else_asks_for_a_web_page(string? path)
	{
		Assert.Equal(AliasGuideFormat.Html, AliasGuideWriter.FormatFor(path!));
	}

	// ---- the web page ----

	[Fact]
	public void the_web_page_is_a_whole_document_with_the_guides_title()
	{
		string html = Html();

		Assert.StartsWith("<!DOCTYPE html>", html, StringComparison.Ordinal);
		Assert.Contains("<title>FE-Buddy Alias Command Guide</title>", html);
		Assert.Contains("<h1>FE-Buddy Alias Command Guide</h1>", html);
		Assert.EndsWith("</html>" + Environment.NewLine, html, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("about")]
	[InlineData("notation")]
	[InlineData("isr")]
	[InlineData("data-display")]
	[InlineData("chart-recall")]
	public void the_web_page_has_each_section_and_a_contents_link_to_it(string id)
	{
		string html = Html();

		Assert.Contains($"<section id=\"{id}\">", html);
		Assert.Contains($"<a href=\"#{id}\">", html);
	}

	[Fact]
	public void every_contents_link_on_the_web_page_points_at_a_section_and_in_page_order()
	{
		string html = Html();
		string[] anchors = [.. html.Split("href=\"#").Skip(1).Select(rest => rest[..rest.IndexOf('"')])];

		Assert.Equal(["about", "notation", "isr", "data-display", "chart-recall"], anchors);
		Assert.All(anchors, anchor => Assert.Contains($"<section id=\"{anchor}\">", html));
	}

	[Fact]
	public void the_web_page_says_which_alias_file_the_commands_are_merged_into()
	{
		Assert.Contains("merged into the ZOB alias file", Html());
	}

	[Fact]
	public void the_web_page_footer_credits_fe_buddy_with_its_version_and_the_date()
	{
		string html = Html();

		Assert.Contains($"<footer>Made with <a href=\"{RepositoryUrl}\">FE-Buddy</a> v3.0.0 on 1 October 2026.</footer>", html);
		Assert.Contains("made by FE-Buddy v3.0.0 on 1 October 2026.", html);
	}

	[Fact]
	public void a_part_to_replace_is_a_pill_with_its_kinds_title()
	{
		Assert.Contains("<span class=\"part k-airport\" title=\"Airport ID\">DTW</span>", Html());
	}

	[Fact]
	public void an_optional_part_is_a_dashed_pill_titled_optional()
	{
		Assert.Contains("<span class=\"part k-page opt\" title=\"Page number (optional)\">page</span>", Html());
	}

	/// <summary>The title is also the colour key's label: every kind of part the key lists is one the guide really uses.</summary>
	[Theory]
	[InlineData("k-airport", "Airport ID", "DTW")]
	[InlineData("k-ident", "ID or name", "CGT")]
	[InlineData("k-type", "Approach type", "I")]
	[InlineData("k-variant", "Variant or circling letter", "Z")]
	[InlineData("k-runway", "Runway", "22L")]
	[InlineData("k-page", "Page number", "2")]
	public void each_kind_of_part_has_its_own_colour_and_a_place_in_the_colour_key(string colourClass, string title, string example)
	{
		string html = Html();

		Assert.Contains($"<span class=\"part {colourClass}\" title=\"{title}\">{example}</span>", html);
		Assert.Contains($"<li><code class=\"cmd\"><span class=\"part {colourClass}\">{title}</span></code></li>", html);
	}

	[Fact]
	public void a_long_command_can_wrap_between_its_parts()
	{
		Assert.Contains("<wbr>", Html());
	}

	[Fact]
	public void code_with_only_typed_text_has_no_class()
	{
		string html = Html();

		Assert.Contains("<code>DOTSS2.DOTSS</code>", html);
		Assert.Contains("<code>AALAN.BLAID2</code>", html);
	}

	[Fact]
	public void code_with_a_pill_in_it_is_a_cmd_with_no_background_of_its_own()
	{
		const string command =
			"<code class=\"cmd\">.<wbr>"
			+ "<span class=\"part k-airport\" title=\"Airport ID\">dtw</span><wbr>"
			+ "<span class=\"part k-type\" title=\"Approach type\">I</span><wbr>"
			+ "<span class=\"part k-runway\" title=\"Runway\">22L</span><wbr>c</code>";

		// "at DTW is" puts it in the sentence, not the Example column.
		Assert.Contains("at DTW is " + command, Html());
	}

	[Fact]
	public void the_web_page_escapes_text_that_would_read_as_markup()
	{
		string html = Html();

		Assert.Contains("<span class=\"note\">CRC STARS &amp; ERAM.</span>", html);
		Assert.DoesNotContain("STARS & ERAM", html);
	}

	[Fact]
	public void the_web_page_has_no_scripts_and_fetches_nothing()
	{
		string html = Html();

		Assert.DoesNotContain("<script", html);
		Assert.DoesNotContain("<link", html);
		Assert.DoesNotContain("src=", html);
	}

	[Fact]
	public void the_web_page_escapes_the_apostrophe_when_there_is_no_facility()
	{
		Assert.Contains("merged into your facility&#39;s alias file.", Html(Options(facility: null)));
	}

	// ---- the Markdown ----

	[Fact]
	public void the_markdown_starts_with_the_title_then_the_lead()
	{
		string markdown = Markdown();

		Assert.StartsWith("# FE-Buddy Alias Command Guide", markdown, StringComparison.Ordinal);
		Assert.Equal(
			"These alias commands are made by FE-Buddy every AIRAC cycle and merged into the ZOB alias file.",
			Lines(markdown)[2]);
	}

	[Fact]
	public void the_markdown_contents_line_links_every_section()
	{
		string contents = Assert.Single(Lines(Markdown()), line => line.StartsWith("**Contents:**", StringComparison.Ordinal));

		Assert.Contains("[About alias commands](#about-alias-commands)", contents);
		Assert.Contains("[How to read this guide](#how-to-read-this-guide)", contents);
		Assert.Contains("[In-Scope Reference (ISR)](#in-scope-reference-isr)", contents);
		Assert.Contains("[Data Display](#data-display)", contents);
		Assert.Contains("[Chart Recall](#chart-recall)", contents);
	}

	[Fact]
	public void every_contents_link_in_the_markdown_points_at_a_heading_it_has()
	{
		string[] lines = Lines(Markdown());
		string contents = Assert.Single(lines, line => line.StartsWith("**Contents:**", StringComparison.Ordinal));

		foreach (string link in contents["**Contents:** ".Length..].Split(" · "))
		{
			int split = link.IndexOf("](#", StringComparison.Ordinal);
			string title = link[1..split];
			string slug = link[(split + 3)..^1];

			Assert.Contains("## " + title, lines);
			Assert.Equal(AliasGuideMarkdownWriter.Slug(title), slug);
		}
	}

	[Fact]
	public void a_command_row_shows_its_syntax_with_angle_brackets_and_each_example_on_a_line_of_its_own()
	{
		string markdown = Markdown();

		Assert.Contains("| `.apt<FAA or ICAO airport ID>` |", markdown);
		Assert.Contains("`.aptDTW`<br>`.aptKDTW`", markdown);
	}

	[Fact]
	public void an_optional_part_is_in_square_brackets()
	{
		string markdown = Markdown();

		Assert.Contains("`.<airport ID><approach type>[variant]<runway>c`", markdown);
		Assert.Contains("`.<airport ID><chart code>c[page]`", markdown);
	}

	[Fact]
	public void a_commands_notes_follow_its_description_in_italics()
	{
		Assert.Contains("<br>*CRC STARS & ERAM.*", Markdown());
	}

	[Fact]
	public void a_plain_table_is_a_markdown_table()
	{
		string[] lines = Lines(Markdown());

		Assert.Contains("### Approach type codes", lines);
		Assert.Contains("| Approach | Code | Example |", lines);
		Assert.Contains("| --- | --- | --- |", lines);
		Assert.Contains("| ILS | `I` | `.dtwI22Lc` |", lines);
	}

	[Fact]
	public void a_list_is_markdown_bullets()
	{
		string[] lines = Lines(Markdown());

		Assert.Contains(lines, line => line.StartsWith("- **One command per approach.**", StringComparison.Ordinal));
	}

	[Fact]
	public void how_to_read_this_guide_explains_the_markdown_brackets_then_the_shared_notes()
	{
		string[] lines = Lines(Markdown());

		Assert.Contains("- `.apt` Plain text: type it exactly as shown.", lines);
		Assert.Contains(lines, line => line.StartsWith("- `<airport ID>` Angle brackets:", StringComparison.Ordinal));
		Assert.Contains(lines, line => line.StartsWith("- `[page]` Square brackets: optional.", StringComparison.Ordinal));
		Assert.Contains(lines, line => line.StartsWith("- Commands are not case-sensitive: `.aptdtw` works the same as `.aptDTW`.", StringComparison.Ordinal));
	}

	[Fact]
	public void the_markdown_ends_with_the_credit_line()
	{
		Assert.Contains($"*Made with [FE-Buddy]({RepositoryUrl}) v3.0.0 on 1 October 2026.*", Lines(Markdown()));
	}

	[Theory]
	[InlineData("In-Scope Reference (ISR)", "in-scope-reference-isr")]
	[InlineData("How to read this guide", "how-to-read-this-guide")]
	[InlineData("Chart Recall", "chart-recall")]
	[InlineData("Departures, obstacle departures and arrivals", "departures-obstacle-departures-and-arrivals")]
	[InlineData("Snake_case & Title!", "snake_case--title")]
	public void a_heading_anchor_is_lower_case_with_hyphens_and_no_punctuation(string heading, string expected)
	{
		Assert.Equal(expected, AliasGuideMarkdownWriter.Slug(heading));
	}

	// ---- the facility ----

	[Theory]
	[InlineData(null, "your facility's alias file")]
	[InlineData("", "your facility's alias file")]
	[InlineData("  ", "your facility's alias file")]
	[InlineData("@#!", "your facility's alias file")]
	[InlineData("ZOB", "the ZOB alias file")]
	[InlineData(" zob\n", "the ZOB alias file")]
	[InlineData("Z`O*B", "the ZOB alias file")]
	public void the_facility_is_named_by_its_letters_and_digits_only(string? facility, string expected)
	{
		string[] lines = Lines(Markdown(Options(facility)));

		Assert.Contains($"These alias commands are made by FE-Buddy every AIRAC cycle and merged into {expected}.", lines);
	}

	[Fact]
	public void a_facility_with_markup_characters_cannot_break_either_format()
	{
		AliasGuideOptions options = Options("Z`O*B");

		Assert.Contains("merged into the ZOB alias file.", Html(options));
		Assert.Contains("merged into the ZOB alias file.", Markdown(options));
	}

	// ---- the version and date ----

	[Theory]
	[InlineData("3.0.0", "v3.0.0 on 1 October 2026")]
	[InlineData("3.0.0-alpha.3", "v3.0.0-alpha.3 on 1 October 2026")]
	[InlineData("dev", "dev on 1 October 2026")]
	[InlineData("", " on 1 October 2026")]
	public void the_credit_has_a_v_before_a_version_that_is_a_number(string version, string expected)
	{
		Assert.Equal(expected, AliasGuideWriter.Credit(Options(version: version)));
	}

	[Theory]
	[InlineData("3.0.0", "v3.0.0 on 1 October 2026")]
	[InlineData("3.0.0-alpha.3", "v3.0.0-alpha.3 on 1 October 2026")]
	[InlineData("dev", "dev on 1 October 2026")]
	public void both_footers_carry_the_credit(string version, string credit)
	{
		AliasGuideOptions options = Options(version: version);

		Assert.Contains($"</a> {credit}.</footer>", Html(options));
		Assert.Contains($"*Made with [FE-Buddy]({RepositoryUrl}) {credit}.*", Lines(Markdown(options)));
	}

	[Fact]
	public void the_date_has_no_leading_zero_and_is_in_english_whatever_the_machines_culture()
	{
		CultureInfo original = CultureInfo.CurrentCulture;

		try
		{
			CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

			string credit = AliasGuideWriter.Credit(new AliasGuideOptions("ZOB", "3.0.0", new DateTime(2027, 3, 9, 23, 59, 0, DateTimeKind.Utc)));

			Assert.Equal("v3.0.0 on 9 March 2027", credit);
		}
		finally
		{
			CultureInfo.CurrentCulture = original;
		}
	}

	[Fact]
	public void writing_the_same_options_twice_gives_the_same_text()
	{
		Assert.Equal(Html(), Html());
		Assert.Equal(Markdown(), Markdown());
	}

	// ---- the colour key's labels ----

	[Theory]
	[InlineData("Typed", "Typed as shown")]
	[InlineData("Airport", "Airport ID")]
	[InlineData("Identifier", "ID or name")]
	[InlineData("ApproachType", "Approach type")]
	[InlineData("Variant", "Variant or circling letter")]
	[InlineData("Runway", "Runway")]
	[InlineData("Page", "Page number")]
	public void each_kind_of_part_has_its_label(string kindName, string expected)
	{
		Assert.Equal(expected, AliasGuideContent.KindLabel(Enum.Parse<CommandPartKind>(kindName)));
	}

	[Fact]
	public void every_kind_of_part_has_a_label_of_its_own()
	{
		string[] labels = [.. Enum.GetValues<CommandPartKind>().Select(AliasGuideContent.KindLabel)];

		Assert.All(labels, label => Assert.False(string.IsNullOrWhiteSpace(label)));
		Assert.Equal(labels.Length, labels.Distinct().Count());
	}

	// ---- Write and Export ----

	[Fact]
	public void write_rejects_null_options()
	{
		Assert.Throws<ArgumentNullException>(() => AliasGuideWriter.Write(AliasGuideFormat.Html, null!));
		Assert.Throws<ArgumentNullException>(() => AliasGuideWriter.Write(AliasGuideFormat.Markdown, null!));
	}

	[Theory]
	[InlineData(AliasGuideFormat.Html, "guide.html", '<')]
	[InlineData(AliasGuideFormat.Markdown, "guide.md", '#')]
	public void export_writes_the_guide_as_utf8_with_no_byte_order_mark(AliasGuideFormat format, string fileName, char firstCharacter)
	{
		string path = TempFile(fileName);
		AliasGuideOptions options = Options();

		AliasGuideWriter.Export(path, format, options);

		byte[] bytes = File.ReadAllBytes(path);
		Assert.Equal((byte)firstCharacter, bytes[0]);
		Assert.Equal(AliasGuideWriter.Write(format, options), ReadBack(path));
	}

	[Fact]
	public void export_replaces_a_file_that_is_already_there()
	{
		string path = TempFile("guide.html");
		File.WriteAllText(path, new string('x', 500_000));

		AliasGuideWriter.Export(path, AliasGuideFormat.Html, Options());

		Assert.Equal(Html(), ReadBack(path));
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void export_rejects_a_blank_path(string path)
	{
		Assert.Throws<ArgumentException>(() => AliasGuideWriter.Export(path, AliasGuideFormat.Html, Options()));
	}

	[Fact]
	public void export_rejects_a_null_path()
	{
		Assert.Throws<ArgumentNullException>(() => AliasGuideWriter.Export(null!, AliasGuideFormat.Html, Options()));
	}

	[Fact]
	public void export_with_null_options_writes_nothing()
	{
		string path = TempFile("guide.html");

		Assert.Throws<ArgumentNullException>(() => AliasGuideWriter.Export(path, AliasGuideFormat.Html, null!));

		Assert.False(File.Exists(path));
	}
}
