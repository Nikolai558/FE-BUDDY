using System.Text;
using System.Text.Json;

using FeBuddy.Core.Application.AliasGuide;
using FeBuddy.Core.Application.AliasGuide.Models;

namespace FeBuddy.UnitTests.Application.AliasGuide;

/// <summary>
/// Covers <see cref="AliasPracticeWriter"/>: the page (a whole document in the guide's look, with
/// a filter for each section, its credit and date, and the script built into FE-Buddy), the
/// questions' JSON (every question, every command's parts, HTML and explanation, nothing that could
/// end the script block), and exporting it into a folder. The script itself runs in a browser, so
/// it is checked there rather than here.
/// </summary>
public sealed class AliasPracticeWriterTests : IDisposable
{
	private static readonly AliasGuideOptions Options = new("3.0.0", new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc));

	private readonly string _folder = Path.Combine(Path.GetTempPath(), "FeBuddyTests_AliasPractice_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		if (Directory.Exists(_folder))
		{
			Directory.Delete(_folder, recursive: true);
		}
	}

	private static string Html() => AliasPracticeWriter.Write(Options);

	private static JsonElement Data() => JsonDocument.Parse(AliasPracticeWriter.Data()).RootElement;

	private static JsonElement QuestionNamed(string name) =>
		Data().GetProperty("questions").EnumerateArray().First(question => question.GetProperty("name").GetString() == name);

	// ---- the page ----

	[Fact]
	public void the_practice_is_saved_under_its_own_fixed_name()
	{
		Assert.Equal("FE-Buddy Alias Command Practice.html", AliasPracticeWriter.FileName);
	}

	[Fact]
	public void the_page_is_a_whole_document_with_the_practices_title()
	{
		string html = Html();

		Assert.StartsWith("<!DOCTYPE html>", html, StringComparison.Ordinal);
		Assert.Contains("<title>FE-Buddy Alias Command Practice</title>", html);
		Assert.Contains("<h1>FE-Buddy Alias Command Practice</h1>", html);
		Assert.EndsWith("</html>" + Environment.NewLine, html, StringComparison.Ordinal);
	}

	[Fact]
	public void the_page_looks_like_the_guide_with_a_colour_each_for_right_and_wrong()
	{
		string html = Html();

		Assert.Contains("--part-airport: #5ec8f2;", html);
		Assert.Contains("--good: #4ade80;", html);
		Assert.Contains("--bad: #fb7185;", html);
	}

	[Fact]
	public void the_page_says_when_it_was_updated_and_its_opening_comment_credits_fe_buddy()
	{
		string html = Html();

		Assert.Contains("<footer>Page updated on 1 October 2026.</footer>", html);
		Assert.Contains("FE-Buddy Alias Command Practice, made by FE-Buddy v3.0.0 on 1 October 2026.", html);
	}

	[Fact]
	public void the_page_can_show_all_the_questions_or_one_sections()
	{
		string html = Html();

		Assert.Contains("data-section=\"\" aria-pressed=\"true\">All</button>", html);
		Assert.Contains("data-section=\"In-Scope Reference\" aria-pressed=\"false\">In-Scope Reference</button>", html);
		Assert.Contains("data-section=\"Data Display\" aria-pressed=\"false\">Data Display</button>", html);
		Assert.Contains("data-section=\"Chart Recall\" aria-pressed=\"false\">Chart Recall</button>", html);
	}

	[Fact]
	public void the_page_carries_the_questions_and_the_script_that_runs_them()
	{
		string html = Html();

		Assert.Contains("<script type=\"application/json\" id=\"practice-data\">" + Environment.NewLine + AliasPracticeWriter.Data(), html);
		Assert.Contains(AliasPracticeWriter.Script(), html);
		Assert.Contains("<noscript>", html);
	}

	[Fact]
	public void the_script_is_built_into_fe_buddy()
	{
		string script = AliasPracticeWriter.Script();

		Assert.Contains("function diagnose(input, q)", script);
		Assert.Contains("document.getElementById('practice-data')", script);
	}

	[Fact]
	public void the_page_fetches_nothing()
	{
		string html = Html();

		Assert.DoesNotContain("<link", html);
		Assert.DoesNotContain("src=", html);
		Assert.DoesNotContain("fetch(", html);
	}

	[Fact]
	public void write_rejects_null_options()
	{
		Assert.Throws<ArgumentNullException>(() => AliasPracticeWriter.Write(null!));
	}

	// ---- the questions' JSON ----

	[Fact]
	public void the_data_has_every_question_and_the_approach_type_names()
	{
		JsonElement data = Data();

		Assert.Equal(AliasPracticeContent.Questions.Count, data.GetProperty("questions").GetArrayLength());
		Assert.Equal("ILS", data.GetProperty("typeNames").GetProperty("I").GetString());
	}

	[Fact]
	public void a_question_says_its_section_action_subject_name_and_detail()
	{
		JsonElement question = QuestionNamed("ILS OR LOC RWY 16R");

		Assert.Equal("Chart Recall", question.GetProperty("section").GetString());
		Assert.Equal("Recall the chart", question.GetProperty("action").GetString());
		Assert.Equal("approach", question.GetProperty("subject").GetString());
		Assert.Equal("SLC · SALT LAKE CITY INTL", question.GetProperty("detail").GetString());
		Assert.Equal(JsonValueKind.Null, question.GetProperty("identifierHint").ValueKind);
	}

	[Fact]
	public void each_command_has_its_parts_its_html_and_what_each_part_is()
	{
		JsonElement answer = QuestionNamed("ILS OR LOC RWY 16R").GetProperty("answers")[0];
		JsonElement[] parts = [.. answer.GetProperty("parts").EnumerateArray()];

		Assert.Equal(
			[(".", "typed"), ("slc", "airport"), ("I", "approachType"), ("16R", "runway"), ("c", "typed")],
			parts.Select(part => (part.GetProperty("t").GetString(), part.GetProperty("k").GetString())));
		Assert.StartsWith("<code class=\"cmd\">.<span class=\"part k-airport\"", answer.GetProperty("html").GetString(), StringComparison.Ordinal);

		JsonElement[] breakdown = [.. answer.GetProperty("breakdown").EnumerateArray()];
		Assert.Equal(parts.Length, breakdown.Length);
		Assert.Equal("<code>.</code>", breakdown[0].GetProperty("html").GetString());
		Assert.Equal("The approach type code: I is ILS.", breakdown[2].GetProperty("text").GetString());
		Assert.Contains("class=\"part k-type\"", breakdown[2].GetProperty("html").GetString());
	}

	[Fact]
	public void a_procedures_notes_and_name_hint_are_html()
	{
		JsonElement question = QuestionNamed("SALT LAKE FOUR");

		Assert.Equal("departure", question.GetProperty("subject").GetString());
		Assert.Contains("<code>SLC4.TCH</code>", question.GetProperty("notes")[0].GetString());
		Assert.Contains("<code>SLC4.TCH</code> is <code>SLC</code>", question.GetProperty("identifierHint").GetString());
	}

	[Fact]
	public void the_data_holds_nothing_that_could_end_its_script_block()
	{
		Assert.DoesNotContain("<", AliasPracticeWriter.Data(), StringComparison.Ordinal);
	}

	// ---- Export ----

	[Fact]
	public void export_writes_the_page_into_the_folder_as_utf8_with_no_byte_order_mark()
	{
		Directory.CreateDirectory(_folder);

		string path = AliasPracticeWriter.Export(_folder, Options);

		Assert.Equal(Path.Combine(_folder, AliasPracticeWriter.FileName), path);
		Assert.Equal((byte)'<', File.ReadAllBytes(path)[0]);
		Assert.Equal(Html(), Encoding.UTF8.GetString(File.ReadAllBytes(path)));
	}

	[Fact]
	public void export_replaces_a_practice_page_that_is_already_there()
	{
		Directory.CreateDirectory(_folder);
		string path = Path.Combine(_folder, AliasPracticeWriter.FileName);
		File.WriteAllText(path, new string('x', 300_000));

		AliasPracticeWriter.Export(_folder, Options);

		Assert.Equal(Html(), File.ReadAllText(path));
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void export_rejects_a_blank_folder(string folder)
	{
		Assert.Throws<ArgumentException>(() => AliasPracticeWriter.Export(folder, Options));
	}

	[Fact]
	public void export_rejects_a_null_folder_or_options()
	{
		Assert.Throws<ArgumentNullException>(() => AliasPracticeWriter.Export(null!, Options));
		Assert.Throws<ArgumentNullException>(() => AliasPracticeWriter.Export(_folder, null!));
	}
}
