using FeBuddy.Core.Infrastructure.Telephony.Models;
using FeBuddy.Core.Infrastructure.Telephony.Parsers;

namespace FeBuddy.UnitTests.Infrastructure.Telephony.Parsers;

/// <summary>
/// Covers <see cref="TelephonyHtmlParser"/>: reading the register and U.S. special call signs pages
/// by heading regardless of column order, concatenating every matching table in page order, skipping
/// page furniture, the layout-changed failures (a table with only some expected headings, a
/// mismatched row, no matching table, matching tables with no rows), <see cref="TelephonyHtmlParser.CleanText"/>'s
/// entity/soft-hyphen/Unicode-hyphen/whitespace cleaning, the file-based overloads, and argument checks.
/// </summary>
public sealed class TelephonyHtmlParserTests : IDisposable
{
	private readonly string _testRoot =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_TelephonyHtmlParser_" + Guid.NewGuid().ToString("N"));

	public TelephonyHtmlParserTests() => Directory.CreateDirectory(_testRoot);

	public void Dispose()
	{
		try
		{
			Directory.Delete(_testRoot, recursive: true);
		}
		catch
		{
			// Best-effort.
		}
	}

	private string WriteFile(string html)
	{
		string path = Path.Combine(_testRoot, $"page_{Guid.NewGuid():N}.html");
		File.WriteAllText(path, html);
		return path;
	}

	// ---- fixtures: the FAA's real markup shape (one <table> per section, headings in <thead>, one <td> per column, text in a <p>) ----

	private const string ThreeLtrHeading = "3‐Ltr"; // U+2010 HYPHEN, as the FAA prints it
	private const string TelephonyCallSignHeading = "Telephony/Call ­Sign"; // soft hyphen before "Sign"

	private static string HeadingCell(string text) => $"<th class=\"entry\"><p class=\"p\"><strong class=\"ph b\">{text}</strong></p></th>";

	private static string DataCell(string text) => $"<td class=\"entry\"><p class=\"p\">{text}</p></td>";

	private static string Table(IReadOnlyList<string> headings, IEnumerable<IReadOnlyList<string>> rows) =>
		"<table class=\"table table-scroll\"><thead class=\"thead\"><tr class=\"row\">" +
		string.Concat(headings.Select(HeadingCell)) +
		"</tr></thead><tbody class=\"tbody\">" +
		string.Concat(rows.Select(row => "<tr class=\"row\">" + string.Concat(row.Select(DataCell)) + "</tr>")) +
		"</tbody></table>";

	private static readonly string[] RegisterHeadingsOnPage = ["Company", "Country", "Telephony", ThreeLtrHeading];
	private static readonly string[] SpecialHeadingsOnPage = [TelephonyCallSignHeading, "Identifier", "Company or Operating Agency", "Expiration Date"];

	private static string RegisterTable(params string[][] rows) => Table(RegisterHeadingsOnPage, rows);

	private static string SpecialTable(params string[][] rows) => Table(SpecialHeadingsOnPage, rows);

	private static readonly string[] AviancaRow = ["AEROVIAS DEL CONTINENTE AMERICANO S.A.", "COLOMBIA", "AVIANCA", "AVA"];
	private static readonly string[] AirSixRow = ["AIR SIX", "ARSIX", "NYC Environmental Protection (New Windsor, NY)", "24-Feb-2027"];

	// ---- register: parses, column order independent ----

	[Fact]
	public void the_register_page_parses_every_row_of_the_matching_table()
	{
		string html = RegisterTable(AviancaRow);

		List<TelephonyHtmlDataModel.Assignment> rows = TelephonyHtmlParser.ParseRegisterHtml(html);

		TelephonyHtmlDataModel.Assignment row = Assert.Single(rows);
		Assert.Equal("AEROVIAS DEL CONTINENTE AMERICANO S.A.", row.Company);
		Assert.Equal("COLOMBIA", row.Country);
		Assert.Equal("AVIANCA", row.Telephony);
		Assert.Equal("AVA", row.ThreeLetterDesignator);
	}

	[Fact]
	public void the_register_page_parses_regardless_of_column_order()
	{
		// Same data, columns in a different order than TelephonyHtmlDataModel.Assignment's own order.
		string[] reordered = [ThreeLtrHeading, "Telephony", "Company", "Country"];
		string html = Table(reordered, [["AVA", "AVIANCA", "AEROVIAS DEL CONTINENTE AMERICANO S.A.", "COLOMBIA"]]);

		TelephonyHtmlDataModel.Assignment row = Assert.Single(TelephonyHtmlParser.ParseRegisterHtml(html));

		Assert.Equal("AEROVIAS DEL CONTINENTE AMERICANO S.A.", row.Company);
		Assert.Equal("COLOMBIA", row.Country);
		Assert.Equal("AVIANCA", row.Telephony);
		Assert.Equal("AVA", row.ThreeLetterDesignator);
	}

	[Fact]
	public void the_special_call_signs_page_parses_every_row_of_the_matching_table()
	{
		string html = SpecialTable(AirSixRow);

		TelephonyHtmlDataModel.SpecialCallSign row = Assert.Single(TelephonyHtmlParser.ParseSpecialCallSignsHtml(html));

		Assert.Equal("AIR SIX", row.Telephony);
		Assert.Equal("ARSIX", row.Identifier);
		Assert.Equal("NYC Environmental Protection (New Windsor, NY)", row.Agency);
		Assert.Equal("24-Feb-2027", row.ExpirationDate);
	}

	[Fact]
	public void the_special_call_signs_page_parses_regardless_of_column_order()
	{
		string[] reordered = ["Expiration Date", "Company or Operating Agency", "Identifier", TelephonyCallSignHeading];
		string html = Table(reordered, [["24-Feb-2027", "NYC Environmental Protection (New Windsor, NY)", "ARSIX", "AIR SIX"]]);

		TelephonyHtmlDataModel.SpecialCallSign row = Assert.Single(TelephonyHtmlParser.ParseSpecialCallSignsHtml(html));

		Assert.Equal("AIR SIX", row.Telephony);
		Assert.Equal("ARSIX", row.Identifier);
	}

	// ---- multiple tables, page furniture ----

	[Fact]
	public void every_matching_table_is_concatenated_in_page_order()
	{
		string html = RegisterTable(AviancaRow) + RegisterTable(["RYANAIR DAC", "IRELAND", "RYAN AIR", "RYA"]);

		List<TelephonyHtmlDataModel.Assignment> rows = TelephonyHtmlParser.ParseRegisterHtml(html);

		Assert.Equal(2, rows.Count);
		Assert.Equal("AVA", rows[0].ThreeLetterDesignator);
		Assert.Equal("RYA", rows[1].ThreeLetterDesignator);
	}

	[Fact]
	public void a_page_furniture_table_with_none_of_the_expected_headings_is_skipped()
	{
		string furniture = "<table><thead><tr><th>Foo</th><th>Bar</th></tr></thead><tbody><tr><td>1</td><td>2</td></tr></tbody></table>";
		string html = furniture + RegisterTable(AviancaRow);

		List<TelephonyHtmlDataModel.Assignment> rows = TelephonyHtmlParser.ParseRegisterHtml(html);

		Assert.Single(rows);
	}

	[Fact]
	public void a_layout_table_without_headings_is_skipped()
	{
		string layout = "<table><tr><td>Chapter 3</td><td>Section 1</td></tr></table>";
		string html = layout + RegisterTable(AviancaRow);

		List<TelephonyHtmlDataModel.Assignment> rows = TelephonyHtmlParser.ParseRegisterHtml(html);

		Assert.Equal("AVA", Assert.Single(rows).ThreeLetterDesignator);
	}

	// ---- layout failures ----

	[Fact]
	public void a_table_with_only_some_of_the_expected_headings_throws()
	{
		string[] partial = ["Company", "Country", "Telephony"]; // missing 3-Ltr
		string html = Table(partial, [["A", "B", "C"]]);

		InvalidDataException ex = Assert.Throws<InvalidDataException>(() => TelephonyHtmlParser.ParseRegisterHtml(html));
		Assert.Contains("changed the page's layout", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void a_row_with_the_wrong_cell_count_throws()
	{
		string html =
			"<table><thead><tr><th>Company</th><th>Country</th><th>Telephony</th><th>" + ThreeLtrHeading + "</th></tr></thead>" +
			"<tbody><tr><td>A</td><td>B</td><td>C</td></tr></tbody></table>"; // 3 cells, 4 headings

		InvalidDataException ex = Assert.Throws<InvalidDataException>(() => TelephonyHtmlParser.ParseRegisterHtml(html));
		Assert.Contains("row of 3 cell(s)", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void no_matching_table_throws()
	{
		string html = "<table><thead><tr><th>Foo</th><th>Bar</th></tr></thead><tbody><tr><td>1</td><td>2</td></tr></tbody></table>";

		InvalidDataException ex = Assert.Throws<InvalidDataException>(() => TelephonyHtmlParser.ParseRegisterHtml(html));
		Assert.Contains("has no table headed", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void matching_tables_with_zero_rows_throws()
	{
		string html = Table(RegisterHeadingsOnPage, []);

		InvalidDataException ex = Assert.Throws<InvalidDataException>(() => TelephonyHtmlParser.ParseRegisterHtml(html));
		Assert.Contains("no rows in them", ex.Message, StringComparison.Ordinal);
	}

	// ---- blank cells ----

	[Fact]
	public void a_blank_telephony_cell_reads_as_an_empty_string()
	{
		string html = RegisterTable(["SOME COMPANY", "UNITED STATES", "", "ABC"]);

		TelephonyHtmlDataModel.Assignment row = Assert.Single(TelephonyHtmlParser.ParseRegisterHtml(html));

		Assert.Equal(string.Empty, row.Telephony);
	}

	// ---- Parse: register + optional special call signs ----

	[Fact]
	public void parse_with_a_null_special_call_signs_path_leaves_it_empty()
	{
		string registerPath = WriteFile(RegisterTable(AviancaRow));

		TelephonyDataCollection data = TelephonyHtmlParser.Parse(registerPath, null);

		Assert.Single(data.Assignments);
		Assert.Empty(data.SpecialCallSigns);
	}

	[Fact]
	public void parse_reads_both_pages_when_both_paths_are_given()
	{
		string registerPath = WriteFile(RegisterTable(AviancaRow));
		string specialPath = WriteFile(SpecialTable(AirSixRow));

		TelephonyDataCollection data = TelephonyHtmlParser.Parse(registerPath, specialPath);

		Assert.Single(data.Assignments);
		Assert.Single(data.SpecialCallSigns);
	}

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	public void parse_rejects_a_blank_register_path(string path) =>
		Assert.Throws<ArgumentException>(() => TelephonyHtmlParser.Parse(path, null));

	[Fact]
	public void parse_rejects_a_null_register_path() =>
		Assert.Throws<ArgumentNullException>(() => TelephonyHtmlParser.Parse(null!, null));

	// ---- file-based ParseRegister / ParseSpecialCallSigns ----

	[Fact]
	public void parse_register_reads_a_file()
	{
		string path = WriteFile(RegisterTable(AviancaRow));

		List<TelephonyHtmlDataModel.Assignment> rows = TelephonyHtmlParser.ParseRegister(path);

		Assert.Single(rows);
	}

	[Fact]
	public void parse_special_call_signs_reads_a_file()
	{
		string path = WriteFile(SpecialTable(AirSixRow));

		List<TelephonyHtmlDataModel.SpecialCallSign> rows = TelephonyHtmlParser.ParseSpecialCallSigns(path);

		Assert.Single(rows);
	}

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	public void parse_register_rejects_a_blank_path(string path) =>
		Assert.Throws<ArgumentException>(() => TelephonyHtmlParser.ParseRegister(path));

	[Fact]
	public void parse_register_rejects_a_null_path() =>
		Assert.Throws<ArgumentNullException>(() => TelephonyHtmlParser.ParseRegister(null!));

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	public void parse_special_call_signs_rejects_a_blank_path(string path) =>
		Assert.Throws<ArgumentException>(() => TelephonyHtmlParser.ParseSpecialCallSigns(path));

	[Fact]
	public void parse_special_call_signs_rejects_a_null_path() =>
		Assert.Throws<ArgumentNullException>(() => TelephonyHtmlParser.ParseSpecialCallSigns(null!));

	[Fact]
	public void parse_register_html_rejects_null_html() =>
		Assert.Throws<ArgumentNullException>(() => TelephonyHtmlParser.ParseRegisterHtml(null!));

	// ---- CleanText ----

	[Theory]
	[InlineData("<p class=\"p\">Hello <b>World</b></p>", "Hello World")]
	[InlineData("AT&amp;T", "AT&T")]
	[InlineData("Air&#xAD;ways", "Airways")]
	[InlineData("Telephony/Call &#xAD;Sign", "Telephony/Call Sign")]
	[InlineData("A&nbsp;B", "A B")]
	[InlineData("  A   B  \n\t C  ", "A B C")]
	[InlineData("3‐Ltr", "3-Ltr")]
	[InlineData("3–Ltr", "3-Ltr")]
	[InlineData("3−Ltr", "3-Ltr")]
	[InlineData("", "")]
	public void clean_text_decodes_entities_removes_soft_hyphens_and_collapses_whitespace(string html, string expected) =>
		Assert.Equal(expected, TelephonyHtmlParser.CleanText(html));
}
