using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Procedures.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac;

/// <summary>
/// Covers <see cref="DuplicateAliasReport"/>: reading a line's command, finding commands shared
/// within one file and across files ignoring case, tying each line to its airport's ARTCC for every
/// kind of alias file, the report's layout and group order, and where and how it is written.
/// </summary>
public sealed class DuplicateAliasReportTests : IDisposable
{
	private static readonly DateTime Generated = new(2026, 9, 27, 18, 42, 0, DateTimeKind.Utc);

	private readonly string _directory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_DuplicateAliasReport_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		if (Directory.Exists(_directory))
		{
			Directory.Delete(_directory, recursive: true);
		}
	}

	private static NasrCsvDataCollection Nasr() => ProcedureTestData.Nasr(
	[
		ProcedureTestData.AptBaseRow("DTW", icaoId: "KDTW", respArtccId: "ZOB"),
		ProcedureTestData.AptBaseRow("ORF", icaoId: "KORF", respArtccId: "ZDC"),
		ProcedureTestData.AptBaseRow("EDW", icaoId: "KEDW", respArtccId: "ZLA"),
		ProcedureTestData.AptBaseRow("ANC", icaoId: "PANC", respArtccId: "ZAN"),
		ProcedureTestData.AptBaseRow("1U7", respArtccId: "ZLC"),
		ProcedureTestData.AptBaseRow("APT", respArtccId: "ZME"),
		ProcedureTestData.AptBaseRow("NOA", respArtccId: " "),
		ProcedureTestData.AptBaseRow("NUL", respArtccId: null),
	]);

	/// <summary>Writes an alias file into its own sub-folder (so two files can share a name) and returns its path.</summary>
	private string AliasFile(string fileName, params string[] lines)
	{
		string folder = Path.Combine(_directory, Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(folder);

		string path = Path.Combine(folder, fileName);
		File.WriteAllLines(path, lines);
		return path;
	}

	[Theory]
	[InlineData(".dtwI22Lc .OPENURL https://example/1.PDF  ; X", ".dtwI22Lc")]
	[InlineData("  .aptDTW .ECHO \\nAPT:", ".aptDTW")]
	[InlineData(".J60F\t.FF A B", ".J60F")]
	[InlineData(".solo", ".solo")]
	[InlineData("", null)]
	[InlineData(".", null)]
	[InlineData("not a command", null)]
	public void command_of_is_the_text_up_to_the_first_space(string line, string? expected) =>
		Assert.Equal(expected, DuplicateAliasReport.CommandOf(line));

	[Fact]
	public void a_command_shared_across_files_is_found_ignoring_case()
	{
		string departures = AliasFile("Departures.txt", ".orfNUTIYf .FF A B", "", ".orfOTHERf .FF C");
		string arrivals = AliasFile("Arrivals.txt", ".ORFNUTIYF .FF D E");

		DuplicateAliasCommand duplicate = Assert.Single(DuplicateAliasReport.Find([departures, arrivals], Nasr()));

		Assert.Equal(".orfNUTIYf", duplicate.Command);
		Assert.Equal(
			[
				new DuplicateAliasLine("Departures.txt", ".orfNUTIYf .FF A B", "ZDC"),
				new DuplicateAliasLine("Arrivals.txt", ".ORFNUTIYF .FF D E", "ZDC"),
			],
			duplicate.Lines);
	}

	[Fact]
	public void files_with_no_shared_command_have_no_duplicates()
	{
		string airways = AliasFile("Airways.txt", ".J60F .FF A B", ".J61F .FF C", "", "; a comment");

		Assert.Empty(DuplicateAliasReport.Find([airways], Nasr()));
	}

	[Theory]
	[InlineData("Airports.txt", ".aptDTW .ECHO one", "ZOB")]
	[InlineData("Airports.txt", ".aptKDTW .ECHO one", "ZOB")]
	[InlineData("Airports.txt", ".aptZZZZ .ECHO one", null)]
	[InlineData("Airports.txt", ".dtwOTHER .ECHO one", null)]
	[InlineData("Departures.txt", ".dtwHHOWEf .FF A", "ZOB")]
	[InlineData("Arrivals.txt", ".dtwFOREYf .FF A", "ZOB")]
	[InlineData("FAA_CHART_RECALL.txt", ".edwAPDc .OPENURL x", "ZLA")]
	[InlineData("FAA_CHART_RECALL.txt", ".ancvHIGHWAY25Rc .OPENURL x", "ZAN")]
	[InlineData("FAA_CHART_RECALL.txt", ".1u71U7c .OPENURL x", "ZLC")]
	[InlineData("FAA_CHART_RECALL.txt", ".zzzAPDc .OPENURL x", null)]
	[InlineData("FAA_CHART_RECALL.txt", ".noaAPDc .OPENURL x", null)]
	[InlineData("FAA_CHART_RECALL.txt", ".nulAPDc .OPENURL x", null)]
	[InlineData("Airways.txt", ".J60F .FF A", null)]
	[InlineData("NAVAIDs.txt", ".navDTW .echo x", null)]
	public void each_line_is_tied_to_its_airports_artcc(string fileName, string line, string? expectedArtcc)
	{
		string file = AliasFile(fileName, line, line);

		DuplicateAliasCommand duplicate = Assert.Single(DuplicateAliasReport.Find([file], Nasr()));

		Assert.All(duplicate.Lines, duplicateLine => Assert.Equal(expectedArtcc, duplicateLine.ArtccId));
	}

	[Fact]
	public void missing_nasr_airport_data_leaves_every_line_without_an_artcc()
	{
		string file = AliasFile("FAA_CHART_RECALL.txt", ".edwAPDc .OPENURL a", ".edwAPDc .OPENURL b");

		DuplicateAliasCommand duplicate = Assert.Single(DuplicateAliasReport.Find([file], new NasrCsvDataCollection()));

		Assert.All(duplicate.Lines, line => Assert.Null(line.ArtccId));
	}

	[Fact]
	public void the_report_lists_each_artcc_primary_facility_first_and_other_last()
	{
		string chartRecall = AliasFile("FAA_CHART_RECALL.txt",
			".edwAPDc .OPENURL https://aeronav.faa.gov/d-tpp/2609/00500AD.PDF  ; EDWARDS AFB-AIRPORT DIAGRAM",
			".edwAPDc .OPENURL https://aeronav.faa.gov/d-tpp/2609/00500ADROGERSLAKEBED.PDF  ; EDWARDS AFB-AIRPORT DIAGRAM (ROGERS LAKEBED)",
			".dtwAPDc .OPENURL a",
			".dtwAPDc .OPENURL b");
		string airways = AliasFile("Airways.txt", ".J60F .FF A", ".j60f .FF B");

		IReadOnlyList<DuplicateAliasCommand> duplicates = DuplicateAliasReport.Find([chartRecall, airways], Nasr());
		string text = DuplicateAliasReport.Format(duplicates, [chartRecall, airways], "2609", "ZOB", Generated);

		string expected = string.Join(Environment.NewLine,
			"FE-Buddy duplicate alias commands - AIRAC 2609",
			"Generated 2026-09-27 18:42Z",
			"",
			"Each command below is used by more than one line of the alias files this run wrote, so CRC can only",
			"run one of them. Commands are compared ignoring case, the way CRC matches them.",
			"Solutions are required at ARTCC level. Consult the FE-Buddy developers if unable to resolve at a local level.",
			"",
			"Files checked: FAA_CHART_RECALL.txt, Airways.txt",
			"",
			"Summary: 3 duplicate command(s) on 6 line(s) - ZOB 1, ZLA 1, OTHER 1",
			"OTHER holds the commands FE-Buddy can't tie to an airport's ARTCC, such as airways and NAVAIDs.",
			"",
			"ZOB",
			"\t.dtwAPDc  (2 lines)",
			"\t\tFAA_CHART_RECALL.txt  .dtwAPDc .OPENURL a",
			"\t\tFAA_CHART_RECALL.txt  .dtwAPDc .OPENURL b",
			"",
			"ZLA",
			"\t.edwAPDc  (2 lines)",
			"\t\tFAA_CHART_RECALL.txt  .edwAPDc .OPENURL https://aeronav.faa.gov/d-tpp/2609/00500AD.PDF  ; EDWARDS AFB-AIRPORT DIAGRAM",
			"\t\tFAA_CHART_RECALL.txt  .edwAPDc .OPENURL https://aeronav.faa.gov/d-tpp/2609/00500ADROGERSLAKEBED.PDF  ; EDWARDS AFB-AIRPORT DIAGRAM (ROGERS LAKEBED)",
			"",
			"OTHER",
			"\t.J60F  (2 lines)",
			"\t\tAirways.txt  .J60F .FF A",
			"\t\tAirways.txt  .j60f .FF B",
			"");

		Assert.Equal(expected, text);
	}

	[Fact]
	public void without_a_primary_facility_the_artccs_are_alphabetical_and_a_command_spanning_two_is_under_both()
	{
		// .aptKDTW is KDTW's airport card in Airports.txt (ZOB), and - ignoring case - a chart
		// command at the airport APT (ZME) in FAA_CHART_RECALL.txt.
		string airports = AliasFile("Airports.txt", ".aptKDTW .ECHO card");
		string departures = AliasFile("Departures.txt", ".orfSAMEf .FF A");
		string arrivals = AliasFile("Arrivals.txt", ".ORFSAMEF .FF B");
		string chartRecall = AliasFile("FAA_CHART_RECALL.txt", ".aptKDTW .OPENURL chart", ".edwAPDc .OPENURL a", ".edwAPDc .OPENURL b");
		string[] files = [airports, departures, arrivals, chartRecall];

		IReadOnlyList<DuplicateAliasCommand> duplicates = DuplicateAliasReport.Find(files, Nasr());
		string text = DuplicateAliasReport.Format(duplicates, files, "2609", null, Generated);

		Assert.Equal(3, duplicates.Count);
		Assert.Contains("Summary: 3 duplicate command(s) on 6 line(s) - ZDC 1, ZLA 1, ZME 1, ZOB 1", text, StringComparison.Ordinal);
		Assert.Equal(2, text.Split($"\t.aptKDTW  (2 lines)").Length - 1);

		string[] groupOrder = ["ZDC", "ZLA", "ZME", "ZOB"];
		int[] positions = [.. groupOrder.Select(group => text.IndexOf($"{Environment.NewLine}{group}{Environment.NewLine}", StringComparison.Ordinal))];
		Assert.All(positions, position => Assert.True(position > 0));
		Assert.Equal(positions.Order(), positions);
		Assert.DoesNotContain("OTHER", text, StringComparison.Ordinal);
	}

	[Fact]
	public void a_clean_run_says_there_is_nothing_to_fix()
	{
		string airways = AliasFile("Airways.txt", ".J60F .FF A");

		string text = DuplicateAliasReport.Format([], [airways], "2609", "ZOB", Generated);

		Assert.EndsWith($"Files checked: Airways.txt{Environment.NewLine}{Environment.NewLine}Summary: no duplicate alias commands.{Environment.NewLine}", text, StringComparison.Ordinal);
	}

	[Fact]
	public void write_puts_the_report_in_the_cycle_folder_as_utf8_without_a_bom()
	{
		string chartRecall = AliasFile("FAA_CHART_RECALL.txt", ".edwAPDc .OPENURL a", ".EDWAPDC .OPENURL b");
		string cycleFolder = Path.Combine(_directory, "AIRAC_2609");

		DuplicateAliasReportResult result = DuplicateAliasReport.Write([chartRecall], Nasr(), "2609", cycleFolder, "ZLA", Generated);

		Assert.Equal(Path.Combine(cycleFolder, "Duplicate_Alias_Commands.txt"), result.FilePath);
		Assert.Single(result.Duplicates);
		Assert.Equal(2, result.LineCount);

		byte[] bytes = File.ReadAllBytes(result.FilePath);
		Assert.False(bytes is [0xEF, 0xBB, 0xBF, ..]);
		Assert.StartsWith("FE-Buddy duplicate alias commands - AIRAC 2609", File.ReadAllText(result.FilePath), StringComparison.Ordinal);
	}

	[Fact]
	public void write_rejects_missing_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => DuplicateAliasReport.Write(null!, Nasr(), "2609", _directory, null, Generated));
		Assert.Throws<ArgumentNullException>(() => DuplicateAliasReport.Write([], null!, "2609", _directory, null, Generated));
		Assert.Throws<ArgumentException>(() => DuplicateAliasReport.Write([], Nasr(), " ", _directory, null, Generated));
		Assert.Throws<ArgumentException>(() => DuplicateAliasReport.Write([], Nasr(), "2609", "", null, Generated));
	}
}
