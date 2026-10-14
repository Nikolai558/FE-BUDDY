using System.Text.Json.Nodes;

using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Procedures.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Procedures;

/// <summary>
/// Covers Procedure Charts' procedures-with-a-fix-in-my-facility rule end to end through
/// <see cref="ProcedureService.Run"/> (Feature #367): what it adds, and how both documents label it;
/// the run with the option off, or no facility set, as it was before; the previous cycle's route for a
/// deleted chart; and the Chart Recall alias file, which it never touches.
/// </summary>
[Collection("AppLog")]
public sealed class ProcedureServiceFixInFacilityTests : IDisposable
{
	private readonly string _outputDirectory = Path.Combine(Path.GetTempPath(), "FeBuddyTests_ProcedureFixInFacility_" + Guid.NewGuid().ToString("N"));

	/// <summary>Deletes the output folder.</summary>
	public void Dispose()
	{
		if (Directory.Exists(_outputDirectory))
		{
			Directory.Delete(_outputDirectory, recursive: true);
		}
	}

	private Dictionary<string, string> Settings(params (string Key, string Value)[] overrides)
	{
		Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputDirectory"] = _outputDirectory,
			["Facilities"] = "ZDC",
			["PrimaryFacility"] = "ZDC",
			["JsonFields"] = "chartType",
		};

		foreach ((string key, string value) in overrides)
		{
			settings[key] = value;
		}

		return settings;
	}

	/// <summary>
	/// IAD in ZDC; PHL and TTN in ZNY, served by JIIMS FOUR, whose route crosses ZDC at DUCKY; PHL's ILS
	/// doesn't; and EWR's KOOPR, deleted this cycle, whose route last cycle crossed ZDC at OTTTO.
	/// </summary>
	private static NasrCsvDataCollection Nasr(bool koopr = false) => ProcedureTestData.RouteNasr(
		apt:
		[
			ProcedureTestData.AptBaseRow("IAD", respArtccId: "ZDC"),
			ProcedureTestData.AptBaseRow("PHL", respArtccId: "ZNY"),
			ProcedureTestData.AptBaseRow("TTN", respArtccId: "ZNY"),
			ProcedureTestData.AptBaseRow("EWR", respArtccId: "ZNY"),
		],
		arrivals:
		[
			ProcedureTestData.Arrival("TRISH.TRISH5", "TRISH FIVE (RNAV)", "IAD", ("TRISH", "WP", "K6")),
			ProcedureTestData.Arrival("WEVVE.JIIMS4", "JIIMS FOUR (RNAV)", "PHL TTN", ("WEVVE", "WP", "K6"), ("DUCKY", "WP", "K6")),
			.. koopr ? [ProcedureTestData.Arrival("OTTTO.KOOPR1", "KOOPR ONE", "EWR", ("OTTTO", "WP", "K6"))] : Array.Empty<(StarCsvDataModel.StarBase, IEnumerable<StarCsvDataModel.StarRte>)>(),
		],
		fixes:
		[
			ProcedureTestData.FixRow("TRISH", "ZDC", "ZDC"),
			ProcedureTestData.FixRow("DUCKY", "ZDC", "ZDC"),
			ProcedureTestData.FixRow("OTTTO", "ZDC", "ZDC"),
			ProcedureTestData.FixRow("WEVVE", "ZNY", "ZNY"),
		]);

	private static DtppMetafileDataCollection Dtpp() => ProcedureTestData.Dtpp("2611",
		airports:
		[
			ProcedureTestData.AirportRow("IAD", alnum: 1),
			ProcedureTestData.AirportRow("PHL", alnum: 2),
			ProcedureTestData.AirportRow("TTN", alnum: 3),
			ProcedureTestData.AirportRow("EWR", alnum: 4),
		],
		records:
		[
			ProcedureTestData.RecordRow("IAD", 10, "STR", "TRISH FIVE (RNAV)", "00005TRISH.PDF", userAction: "C", faanfd18: "TRISH.TRISH5"),
			ProcedureTestData.RecordRow("PHL", 10, "IAP", "ILS RWY 9R", "00178IL9R.PDF", userAction: "C"),
			ProcedureTestData.RecordRow("PHL", 20, "STR", "JIIMS FOUR (RNAV)", "00178JIIMS.PDF", userAction: "C", faanfd18: "WEVVE.JIIMS4"),
			ProcedureTestData.RecordRow("TTN", 20, "STR", "JIIMS FOUR (RNAV)", "00178JIIMS.PDF", userAction: "C", faanfd18: "WEVVE.JIIMS4"),
			ProcedureTestData.RecordRow("EWR", 30, "STR", "KOOPR ONE", "DELETED_JOB.PDF", userAction: "D", faanfd18: "OTTTO.KOOPR1"),
		]);

	private string Markdown() => File.ReadAllText(Path.Combine(_outputDirectory, "Publication_Docs", "Procedure_Changes.md"));

	private string Json() => File.ReadAllText(Path.Combine(_outputDirectory, "Publication_Docs", "Procedures.json"));

	private string Alias() => File.ReadAllText(Path.Combine(_outputDirectory, "Aliases", "Faa_Chart_Recall.txt"));

	[Fact]
	public void neighbors_charts_with_a_fix_in_the_facility_are_added_and_labeled_in_both_documents()
	{
		ProcedureServiceResult result = ProcedureService.Run(Nasr(), Dtpp(), null, Settings(), previousNasr: Nasr(koopr: true));

		string markdown = Markdown();
		Assert.Contains(
			"## ZDC\r\n" +
			"- IAD\r\n" +
			"  - [TRISH FIVE (RNAV)](https://aeronav.faa.gov/d-tpp/2611/compare_pdf/00005TRISH_cmp.pdf)\r\n",
			markdown);
		Assert.Contains(
			"- PHL\r\n" +
			"  - [JIIMS FOUR (RNAV)](https://aeronav.faa.gov/d-tpp/2611/compare_pdf/00178JIIMS_cmp.pdf)\r\n" +
			"    - In ZDC: its route has a fix there (DUCKY)\r\n" +
			"    - Also serves: TTN\r\n",
			markdown);
		Assert.Contains("    - KOOPR ONE (previous chart not available)\r\n      - In ZDC: its route has a fix there (OTTTO)\r\n", markdown);
		Assert.DoesNotContain("ILS RWY 9R", markdown);

		JsonArray airports = JsonNode.Parse(Json())!["airports"]!.AsArray();
		JsonObject iad = airports.Single(a => a!["airportId"]!.GetValue<string>() == "IAD")!["procedures"]![0]!.AsObject();
		JsonObject phl = airports.Single(a => a!["airportId"]!.GetValue<string>() == "PHL")!["procedures"]![0]!.AsObject();
		Assert.False(iad.ContainsKey("includedBecause"));
		Assert.Equal("routeFixInFacility", phl["includedBecause"]!.GetValue<string>());
		Assert.Equal(["DUCKY"], phl["facilityFixes"]!.AsArray().Select(fix => fix!.GetValue<string>()));

		Assert.Contains(result.Messages, m => m.Level == LogLevel.Info && m.Text == "Added 3 departure(s) and arrival(s) at 3 other airport(s), whose route has a fix in ZDC.");
	}

	[Fact]
	public void without_the_previous_cycles_nasr_a_deleted_chart_is_checked_against_this_cycles_routes_and_says_so()
	{
		ProcedureServiceResult result = ProcedureService.Run(Nasr(), Dtpp(), null, Settings());

		Assert.DoesNotContain("KOOPR", Markdown());
		Assert.Contains(result.Messages, m => m.Level == LogLevel.Info && m.Text.StartsWith("The previous cycle's NASR data isn't loaded", StringComparison.Ordinal));
	}

	/// <summary>Off, the run writes what it wrote before the rule; and on, Faa_Chart_Recall.txt is the same either way.</summary>
	[Fact]
	public void with_the_option_off_the_documents_are_as_before_and_chart_recall_never_changes()
	{
		ProcedureService.Run(Nasr(), Dtpp(), null, Settings(("IncludeProceduresWithFixInFacility", "Y")), previousNasr: Nasr(koopr: true));
		string aliasOn = Alias();

		ProcedureServiceResult off = ProcedureService.Run(Nasr(), Dtpp(), null, Settings(("IncludeProceduresWithFixInFacility", "N")), previousNasr: Nasr(koopr: true));

		Assert.Equal(aliasOn, Alias());
		Assert.Equal(1, off.AirportCount);
		Assert.DoesNotContain("its route has a fix there", Markdown(), StringComparison.Ordinal);
		Assert.DoesNotContain("includedBecause", Json(), StringComparison.Ordinal);
		Assert.DoesNotContain(off.Messages, m => m.Text.Contains("fix in", StringComparison.Ordinal));
	}

	[Fact]
	public void with_no_facility_set_the_run_warns_once_and_writes_what_it_did_before()
	{
		ProcedureService.Run(Nasr(), Dtpp(), null, Settings(("PrimaryFacility", ""), ("IncludeProceduresWithFixInFacility", "N")));
		string markdownBefore = Markdown();
		string jsonBefore = Json();

		ProcedureServiceResult result = ProcedureService.Run(Nasr(), Dtpp(), null, Settings(("PrimaryFacility", "")));

		Assert.Equal(markdownBefore, Markdown());
		Assert.Equal(jsonBefore, Json());
		Assert.Single(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains("no facility is set in Settings ▸ Facility", StringComparison.Ordinal));
	}

	[Fact]
	public void with_nothing_to_add_the_run_says_so()
	{
		ProcedureServiceResult result = ProcedureService.Run(Nasr(), Dtpp(), null, Settings(("ChartTypes", "IAP")));

		Assert.Contains(result.Messages, m => m.Text == "No departure or arrival at another airport has a fix in ZDC that isn't already included.");
	}
}
