using System.Globalization;
using System.Text;

using FeBuddy.Core.Application.AliasGuide;
using FeBuddy.Core.Application.AliasGuide.Models;

namespace FeBuddy.UnitTests.Application.AliasGuide;

/// <summary>
/// Covers <see cref="AliasGuideWriter"/>: each format's file name; the web page's sections,
/// contents links, lead, colour pills, columns that never wrap and footer; the Markdown's title,
/// contents line, tables, lists and footer; how the version and date are named; the heading
/// anchors; and exporting into a folder (UTF-8 with no byte order mark, one file per format,
/// replacing a guide already there).
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

	private static AliasGuideOptions Options(string version = "3.0.0") => new(version, GeneratedUtc);

	private static string Html(AliasGuideOptions? options = null) => AliasGuideWriter.Write(AliasGuideFormat.Html, options ?? Options());

	private static string Markdown(AliasGuideOptions? options = null) => AliasGuideWriter.Write(AliasGuideFormat.Markdown, options ?? Options());

	private static string[] Lines(string text) => text.Split(Environment.NewLine);

	/// <summary>The lines without their indent, for checking what the web page says rather than how its source is laid out.</summary>
	private static string[] Trimmed(string text) => [.. Lines(text).Select(line => line.Trim())];

	/// <summary>The lines from the first that is <paramref name="first"/> (trimmed), as many as <paramref name="count"/>.</summary>
	private static string[] LinesFrom(string[] lines, string first, int count) =>
		[.. lines.SkipWhile(line => line.Trim() != first).Take(count)];

	private string TempFile(string name) => Path.Combine(Folder(), name);

	private string Folder()
	{
		Directory.CreateDirectory(_outputDirectory);
		return _outputDirectory;
	}

	private static string ReadBack(string path) => Encoding.UTF8.GetString(File.ReadAllBytes(path));

	// ---- the file names ----

	[Theory]
	[InlineData(AliasGuideFormat.Html, "FE-Buddy Alias Command Guide.html")]
	[InlineData(AliasGuideFormat.Markdown, "FE-Buddy Alias Command Guide.md")]
	public void each_format_has_its_own_fixed_file_name(AliasGuideFormat format, string fileName)
	{
		Assert.Equal(fileName, AliasGuideWriter.FileName(format));
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

		Assert.Equal(["notation", "isr", "data-display", "chart-recall"], anchors);
		Assert.All(anchors, anchor => Assert.Contains($"<section id=\"{anchor}\">", html));
	}

	[Fact]
	public void the_web_page_lead_links_to_fe_buddy()
	{
		Assert.Contains(
			$"<p class=\"lead\">These alias commands are made by <a href=\"{RepositoryUrl}\">FE-Buddy</a> every AIRAC cycle.</p>",
			Html());
	}

	[Fact]
	public void the_web_page_footer_says_when_it_was_updated_and_its_opening_comment_credits_fe_buddy()
	{
		string html = Html();

		Assert.Contains("<footer>Page updated on 1 October 2026.</footer>", html);
		Assert.Contains("made by FE-Buddy v3.0.0 on 1 October 2026.", html);
	}

	[Fact]
	public void the_web_page_says_plain_text_is_typed_as_shown_with_an_example_in_quotes()
	{
		Assert.Contains("<li>Plain text, typed exactly as shown. For example: \"<code class=\"cmd\">.apt</code>\"</li>", Html());
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
	public void a_command_never_wraps_and_the_syntax_and_example_columns_fit_their_longest_line()
	{
		string html = Html();

		Assert.Contains(".commands .syntax, .commands .examples { width: 1%; white-space: nowrap; }", html);
		Assert.DoesNotContain("<wbr>", html);
		Assert.DoesNotContain("white-space: normal", html);
	}

	[Fact]
	public void a_syntax_breaks_only_where_the_guide_says()
	{
		string[] lines = Trimmed(Html());

		Assert.Contains(
			"<td class=\"syntax\"><code class=\"cmd\">.apt<br><span class=\"part k-airport\" title=\"Airport ID\">FAA or ICAO airport ID</span></code></td>",
			lines);
		Assert.Contains(
			"<td class=\"syntax\"><code class=\"cmd\">.<span class=\"part k-airport\" title=\"Airport ID\">airport ID</span><br>"
			+ "<span class=\"part k-ident\" title=\"ID or name\">arrival</span><br>f</code></td>",
			lines);
	}

	[Fact]
	public void each_example_is_a_command_on_a_line_of_its_own_in_the_examples_cell()
	{
		Assert.Equal(
			[
				"<td class=\"examples\">",
				"<code class=\"cmd\">.apt<span class=\"part k-airport\" title=\"Airport ID\">DTW</span></code>",
				"<code class=\"cmd\">.apt<span class=\"part k-airport\" title=\"Airport ID\">KDTW</span></code>",
				"</td>",
			],
			LinesFrom(Trimmed(Html()), "<td class=\"examples\">", 4));
	}

	[Fact]
	public void a_commands_details_are_bullets_under_its_description()
	{
		Assert.Equal(
			[
				"<p>Shows the airport&#39;s card:</p>",
				"<ul>",
				"<li>FAA and ICAO IDs, name, tower type and ARTCC</li>",
				"<li>Longest runway, elevation and traffic pattern altitude</li>",
				"<li>FSS, CTAF and weather frequency</li>",
				"<li>Attended hours (for towered airspace only)</li>",
				"<li>Class of airspace, with the hours it is in effect</li>",
				"</ul>",
			],
			LinesFrom(Trimmed(Html()), "<p>Shows the airport&#39;s card:</p>", 8));
	}

	/// <summary>A bullet with bullets of its own has its text on a line, then its list under it, each level two spaces in.</summary>
	[Fact]
	public void a_nested_list_is_nested_lists_its_source_indented_like_an_outline()
	{
		string[] lines = LinesFrom(Lines(Html()), "<strong>One command per approach</strong>", 12);
		int indent = lines[0].Length - lines[0].TrimStart().Length;

		Assert.Equal(
			[
				"<strong>One command per approach</strong>",
				"<ul>",
				"  <li>",
				"    A chart for more than one approach has a command for each:",
				"    <ul>",
				"      <li>",
				"        ILS OR LOC RWY 22L at DTW is both:",
				"        <ul>",
				"          <li><code class=\"cmd\">.<span class=\"part k-airport\" title=\"Airport ID\">dtw</span><span class=\"part k-type\" title=\"Approach type\">I</span><span class=\"part k-runway\" title=\"Runway\">22L</span>c</code></li>",
				"          <li><code class=\"cmd\">.<span class=\"part k-airport\" title=\"Airport ID\">dtw</span><span class=\"part k-type\" title=\"Approach type\">L</span><span class=\"part k-runway\" title=\"Runway\">22L</span>c</code></li>",
				"        </ul>",
				"      </li>",
			],
			lines.Select(line => line[indent..]));
	}

	[Fact]
	public void a_line_break_in_a_paragraph_is_a_br()
	{
		Assert.Contains(
			"<p>FE-Buddy uses the eight approach types in common use across the FAA.<br>A <code>/DME</code> approach adds",
			Html());
	}

	[Fact]
	public void code_with_only_typed_text_has_no_class()
	{
		string html = Html();

		Assert.Contains("<code>ROG4.RZC</code>", html);
		Assert.Contains("<code>AALAN.BLAID2</code>", html);
	}

	[Fact]
	public void code_with_a_pill_in_it_is_a_cmd_with_no_background_of_its_own()
	{
		const string command =
			"<code class=\"cmd\">."
			+ "<span class=\"part k-airport\" title=\"Airport ID\">anc</span>"
			+ "<span class=\"part k-ident\" title=\"ID or name\">TURNAGAIN</span>c</code>";

		// "at ANC is" puts it in the sentence, not the Example column.
		Assert.Contains("at ANC is " + command, Html());
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
	public void the_web_page_escapes_apostrophes_and_quotes_in_text()
	{
		string html = Html();

		Assert.Contains("Shows the airport&#39;s card", html);
		Assert.Contains("Only a GPS approach with no &quot;RNAV&quot; in its name", html);
	}

	// ---- the Markdown ----

	[Fact]
	public void the_markdown_starts_with_the_title_then_the_lead_linking_to_fe_buddy()
	{
		string markdown = Markdown();

		Assert.StartsWith("# FE-Buddy Alias Command Guide", markdown, StringComparison.Ordinal);
		Assert.Equal(
			$"These alias commands are made by [FE-Buddy]({RepositoryUrl}) every AIRAC cycle.",
			Lines(markdown)[2]);
	}

	[Fact]
	public void the_markdown_contents_line_links_every_section()
	{
		string contents = Assert.Single(Lines(Markdown()), line => line.StartsWith("**Contents:**", StringComparison.Ordinal));

		Assert.Equal(
			"**Contents:** [How to read this guide](#how-to-read-this-guide) · [In-Scope Reference (ISR)](#in-scope-reference-isr) · "
			+ "[Data Display](#data-display) · [Chart Recall](#chart-recall)",
			contents);
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
	public void a_command_row_shows_each_syntax_line_and_each_example_on_a_line_of_its_own()
	{
		string markdown = Markdown();

		Assert.Contains("| `.apt`<br>`<FAA or ICAO airport ID>` |", markdown);
		Assert.Contains("| `.<airport ID>`<br>`<arrival>`<br>`f` |", markdown);
		Assert.Contains("`.aptDTW`<br>`.aptKDTW`", markdown);
	}

	[Fact]
	public void a_commands_details_follow_its_description_each_after_a_bullet()
	{
		Assert.Contains(
			"| Shows the NAVAID's card:<br>• ID, name, type and frequency<br>• The ARTCCs it is in, for high and low altitude airspace"
			+ "<br>*When entering the name, leave out spaces and special characters.*",
			Markdown());
	}

	[Fact]
	public void an_optional_part_is_in_square_brackets()
	{
		string markdown = Markdown();

		Assert.Contains("`.<airport ID>`<br>`<approach type>`<br>`[variant]`<br>`<runway>`<br>`c`", markdown);
		Assert.Contains("`.<airport ID>`<br>`<chart code>`<br>`c`<br>`[page]`", markdown);
	}

	[Fact]
	public void a_line_break_in_a_markdown_paragraph_is_a_br()
	{
		Assert.Contains(
			Lines(Markdown()),
			line => line.StartsWith("FE-Buddy uses the eight approach types in common use across the FAA.<br>A `/DME` approach adds", StringComparison.Ordinal));
	}

	/// <summary>Data Display's procedure names end their example in its <c>f</c>, Chart Recall's in its <c>c</c>.</summary>
	[Fact]
	public void each_sections_procedure_names_end_in_that_sections_own_command()
	{
		string[] lines = Lines(Markdown());
		const string prefix = "- A chart with no computer code is spelled out in full instead";

		Assert.Equal(
			["`.ancTURNAGAINf`.", "`.ancTURNAGAINc`."],
			lines.Where(line => line.StartsWith(prefix, StringComparison.Ordinal)).Select(line => line[(line.LastIndexOf(' ') + 1)..]));
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
	public void a_nested_list_is_markdown_bullets_each_level_two_spaces_in()
	{
		Assert.Equal(
			[
				"- **One command per approach**",
				"  - A chart for more than one approach has a command for each:",
				"    - ILS OR LOC RWY 22L at DTW is both:",
				"      - `.dtwI22Lc`",
				"      - `.dtwL22Lc`",
				"- **Variant letters** (X, Y, Z...)",
			],
			LinesFrom(Lines(Markdown()), "- **One command per approach**", 6));
	}

	[Fact]
	public void how_to_read_this_guide_explains_the_markdown_brackets_then_the_shared_notes()
	{
		string[] lines = LinesFrom(Lines(Markdown()), "## How to read this guide", 12);

		Assert.Equal("- Plain text, typed exactly as shown. For example: \"`.apt`\"", lines[2]);
		Assert.StartsWith("- `<airport ID>` Angle brackets:", lines[3], StringComparison.Ordinal);
		Assert.StartsWith("- `[page]` Square brackets: optional.", lines[4], StringComparison.Ordinal);
		Assert.Equal(
			[
				"- **Commands are not case-sensitive**",
				"  - These two work the same:",
				"    - `.aptdtw`",
				"    - `.aptDTW`",
				"- **Airport IDs**",
				"  - Use the FAA ID, not the ICAO ID, unless the command says otherwise. For example, `DTW`, not `KDTW`.",
			],
			lines[5..11]);
	}

	[Fact]
	public void the_markdown_ends_by_saying_when_the_page_was_updated()
	{
		string[] lines = Lines(Markdown());

		Assert.Equal("*Page updated on 1 October 2026.*", lines[^2]);
		Assert.Empty(lines[^1]);
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

	// ---- the version and date ----

	[Theory]
	[InlineData("3.0.0", "v3.0.0 on 1 October 2026")]
	[InlineData("3.0.0-alpha.3", "v3.0.0-alpha.3 on 1 October 2026")]
	[InlineData("dev", "dev on 1 October 2026")]
	[InlineData("", " on 1 October 2026")]
	public void the_credit_has_a_v_before_a_version_that_is_a_number(string version, string expected)
	{
		Assert.Equal(expected, AliasGuideWriter.Credit(Options(version)));
	}

	[Theory]
	[InlineData("3.0.0", "v3.0.0 on 1 October 2026")]
	[InlineData("3.0.0-alpha.3", "v3.0.0-alpha.3 on 1 October 2026")]
	[InlineData("dev", "dev on 1 October 2026")]
	public void the_web_pages_opening_comment_carries_the_credit_and_the_markdown_does_not(string version, string credit)
	{
		AliasGuideOptions options = Options(version);

		Assert.Contains($"  FE-Buddy Alias Command Guide, made by FE-Buddy {credit}.", Lines(Html(options)));
		Assert.DoesNotContain(credit, Markdown(options));
	}

	[Fact]
	public void the_date_has_no_leading_zero_and_is_in_english_whatever_the_machines_culture()
	{
		CultureInfo original = CultureInfo.CurrentCulture;

		try
		{
			CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

			AliasGuideOptions options = new("3.0.0", new DateTime(2027, 3, 9, 23, 59, 0, DateTimeKind.Utc));

			Assert.Equal("v3.0.0 on 9 March 2027", AliasGuideWriter.Credit(options));
			Assert.Equal("Page updated on 9 March 2027.", AliasGuideWriter.Updated(options));
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
	[InlineData(AliasGuideFormat.Html, '<')]
	[InlineData(AliasGuideFormat.Markdown, '#')]
	public void export_writes_the_guide_into_the_folder_as_utf8_with_no_byte_order_mark(AliasGuideFormat format, char firstCharacter)
	{
		string folder = Folder();
		AliasGuideOptions options = Options();

		string path = Assert.Single(AliasGuideWriter.Export(folder, [format], options));

		Assert.Equal(Path.Combine(folder, AliasGuideWriter.FileName(format)), path);
		Assert.Equal((byte)firstCharacter, File.ReadAllBytes(path)[0]);
		Assert.Equal(AliasGuideWriter.Write(format, options), ReadBack(path));
		Assert.Single(Directory.GetFiles(folder));
	}

	[Fact]
	public void export_writes_both_formats_side_by_side_in_the_order_asked()
	{
		string folder = Folder();

		IReadOnlyList<string> written = AliasGuideWriter.Export(folder, [AliasGuideFormat.Markdown, AliasGuideFormat.Html, AliasGuideFormat.Markdown], Options());

		Assert.Equal(
			[Path.Combine(folder, "FE-Buddy Alias Command Guide.md"), Path.Combine(folder, "FE-Buddy Alias Command Guide.html")],
			written);
		Assert.Equal(Markdown(), ReadBack(written[0]));
		Assert.Equal(Html(), ReadBack(written[1]));
	}

	[Fact]
	public void export_replaces_a_guide_that_is_already_there()
	{
		string path = TempFile(AliasGuideWriter.FileName(AliasGuideFormat.Html));
		File.WriteAllText(path, new string('x', 500_000));

		AliasGuideWriter.Export(_outputDirectory, [AliasGuideFormat.Html], Options());

		Assert.Equal(Html(), ReadBack(path));
	}

	[Fact]
	public void export_with_no_formats_writes_nothing()
	{
		string folder = Folder();

		Assert.Empty(AliasGuideWriter.Export(folder, [], Options()));
		Assert.Empty(Directory.GetFiles(folder));
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void export_rejects_a_blank_folder(string folder)
	{
		Assert.Throws<ArgumentException>(() => AliasGuideWriter.Export(folder, [AliasGuideFormat.Html], Options()));
	}

	[Fact]
	public void export_rejects_a_null_folder_or_formats()
	{
		Assert.Throws<ArgumentNullException>(() => AliasGuideWriter.Export(null!, [AliasGuideFormat.Html], Options()));
		Assert.Throws<ArgumentNullException>(() => AliasGuideWriter.Export(Folder(), null!, Options()));
	}

	[Fact]
	public void export_with_null_options_writes_nothing()
	{
		string folder = Folder();

		Assert.Throws<ArgumentNullException>(() => AliasGuideWriter.Export(folder, [AliasGuideFormat.Html, AliasGuideFormat.Markdown], null!));

		Assert.Empty(Directory.GetFiles(folder));
	}
}
