using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Procedures.ChartRecall.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

using FeBuddy.UnitTests.Application.Airac.Procedures.Fixtures;

using DtppRecord = FeBuddy.Core.Infrastructure.Dtpp.Models.DtppMetafileXmlDataModel.Record;

namespace FeBuddy.UnitTests.Application.Airac.Procedures;

/// <summary>
/// Covers <see cref="ChartRecallAliasBuilder"/>: the line format, page numbers after the <c>c</c>,
/// minimums opening at the airport's page, deleted charts left out, the airport-identifier rule for
/// departures named after their airport, repeated and clashing commands, the summary counts and
/// message, and the unrecognized-chart warnings.
/// </summary>
public sealed class ChartRecallAliasBuilderTests
{
	private const string Detroit = "DETROIT METRO WAYNE COUNTY";

	private static DtppRecord Chart(
		string chartCode,
		string chartName,
		string pdfName,
		string aptIdent = "DTW",
		string airportName = Detroit,
		string? userAction = null,
		string? computerCode = null) =>
		ProcedureTestData.RecordRow(aptIdent, 50000, chartCode, chartName, pdfName, userAction: userAction, faanfd18: computerCode, airportName: airportName);

	private static ChartRecallBuildResult Build(params DtppRecord[] records) =>
		ChartRecallAliasBuilder.Build(ProcedureTestData.Dtpp("2609", records: records));

	[Fact]
	public void build_rejects_a_null_metafile() =>
		Assert.Throws<ArgumentNullException>(() => ChartRecallAliasBuilder.Build(null!));

	[Fact]
	public void each_code_becomes_an_openurl_line_with_the_airport_in_lower_case_and_a_trailing_c()
	{
		ChartRecallBuildResult result = Build(Chart("IAP", "ILS OR LOC RWY 22L", "00058IL22L.PDF"));

		Assert.Equal(
			[
				".dtwI22Lc .OPENURL https://aeronav.faa.gov/d-tpp/2609/00058IL22L.PDF  ; DETROIT METRO WAYNE COUNTY-ILS OR LOC RWY 22L",
				".dtwL22Lc .OPENURL https://aeronav.faa.gov/d-tpp/2609/00058IL22L.PDF  ; DETROIT METRO WAYNE COUNTY-ILS OR LOC RWY 22L",
			],
			result.Lines.Select(line => line.Text));

		ChartRecallAliasLine first = result.Lines[0];
		Assert.Equal("DTW", first.AirportId);
		Assert.Equal("IAP", first.ChartCode);
	}

	[Fact]
	public void a_charted_visual_keeps_its_lower_case_v()
	{
		ChartRecallBuildResult result = Build(Chart("IAP", "RIVER VISUAL RWY 19", "00443RIVER_VIS19.PDF", aptIdent: "DCA"));

		Assert.Equal([".dcavRIVER19c"], result.Lines.Select(line => line.Command));
	}

	[Fact]
	public void an_airport_identifier_with_digits_is_lower_cased_whole() =>
		Assert.Equal([".1u7APDc"], Build(Chart("APD", "AIRPORT DIAGRAM", "10475AD.PDF", aptIdent: "1U7")).Lines.Select(line => line.Command));

	[Fact]
	public void a_charts_second_and_later_pages_add_their_page_number_after_the_c()
	{
		ChartRecallBuildResult result = Build(
			Chart("DP", "HHOWE FOUR", "00058HHOWE.PDF", computerCode: "HHOWE4.HHOWE"),
			Chart("DP", "HHOWE FOUR, CONT.1", "00058HHOWE_C.PDF", computerCode: "HHOWE4.HHOWE"),
			Chart("DP", "HHOWE FOUR, CONT.2", "00058HHOWE_C2.PDF", computerCode: "HHOWE4.HHOWE"));

		Assert.Equal([".dtwHHOWEc", ".dtwHHOWEc2", ".dtwHHOWEc3"], result.Lines.Select(line => line.Command));
		Assert.EndsWith("  ; DETROIT METRO WAYNE COUNTY-HHOWE FOUR, CONT.1", result.Lines[1].Text, StringComparison.Ordinal);
	}

	[Fact]
	public void a_minimums_sheet_links_to_the_airports_own_page()
	{
		ChartRecallBuildResult result = Build(Chart("MIN", "TAKEOFF MINIMUMS", "EC3TO.PDF"));

		Assert.Equal("https://aeronav.faa.gov/d-tpp/2609/EC3TO.PDF#nameddest=(DTW)", Assert.Single(result.Lines).Url);
	}

	[Fact]
	public void hot_spots_and_lahso_open_the_whole_pdf()
	{
		ChartRecallBuildResult result = Build(Chart("HOT", "HOT SPOT", "EC3HOT.PDF"), Chart("LAH", "LAHSO", "EC3LAHSO.PDF"));

		Assert.Equal(["https://aeronav.faa.gov/d-tpp/2609/EC3HOT.PDF", "https://aeronav.faa.gov/d-tpp/2609/EC3LAHSO.PDF"], result.Lines.Select(line => line.Url));
	}

	[Fact]
	public void deleted_charts_and_placeholder_pdfs_get_no_command()
	{
		ChartRecallBuildResult result = Build(
			Chart("IAP", "VOR RWY 3", "DELETED_JOB.PDF", userAction: "D"),
			Chart("IAP", "VOR RWY 4", "00058V4.PDF", userAction: "D"),
			Chart("IAP", "VOR RWY 5", "DEL_APT_SERVED.PDF"),
			Chart("IAP", "VOR RWY 6", "  "),
			Chart("IAP", "VOR RWY 7", "00058V7.PDF", aptIdent: " "),
			Chart("IAP", "VOR RWY 21", "00058V21.PDF"));

		Assert.Equal([".dtwO21c"], result.Lines.Select(line => line.Command));
	}

	[Fact]
	public void a_departure_that_names_its_airport_uses_the_airport_identifier()
	{
		ChartRecallBuildResult result = Build(
			Chart("ODP", "TATALINA FOUR (OBSTACLE) (RNAV)", "10651TATALINA.PDF", aptIdent: "TLJ", airportName: "TATALINA LRRS"),
			Chart("DP", "KNIK THREE", "10651KNIK.PDF", aptIdent: "TLJ", airportName: "TATALINA LRRS"));

		Assert.Equal([".tljTLJc", ".tljKNIKc"], result.Lines.Select(line => line.Command));
		Assert.Equal(2, result.Summary.NamedFromChartNameCount);
		Assert.Equal(1, result.Summary.NamedAfterAirportCount);
	}

	[Fact]
	public void two_departures_that_name_their_airport_each_keep_their_own_name()
	{
		ChartRecallBuildResult result = Build(
			Chart("DP", "LEMOORE TWO", "05067LEMOORE.PDF", aptIdent: "NLC", airportName: "LEMOORE NAS (REEVES FLD)"),
			Chart("DP", "REEVES FIVE", "05067REEVES.PDF", aptIdent: "NLC", airportName: "LEMOORE NAS (REEVES FLD)"));

		Assert.Equal([".nlcLEMOOREc", ".nlcREEVESc"], result.Lines.Select(line => line.Command));
		Assert.Equal(0, result.Summary.NamedAfterAirportCount);
	}

	[Fact]
	public void a_departure_keeps_its_name_when_another_charts_code_already_is_the_airport_identifier()
	{
		ChartRecallBuildResult result = Build(
			Chart("ODP", "PASCO ONE (OBSTACLE)", "00474PASCO.PDF", aptIdent: "PSC", airportName: "TRI-CITIES", computerCode: "PSC1.PSC"),
			Chart("DP", "TRI-CITIES EIGHT", "00474TRI-CITIES.PDF", aptIdent: "PSC", airportName: "TRI-CITIES"));

		Assert.Equal([".pscPSCc", ".pscTRICITIESc"], result.Lines.Select(line => line.Command));
	}

	[Fact]
	public void the_airport_rule_is_worked_out_per_airport()
	{
		ChartRecallBuildResult result = Build(
			Chart("DP", "DENVER THREE", "09077DENVER.PDF", aptIdent: "DEN", airportName: "DENVER INTL"),
			Chart("DP", "DENVER THREE", "09078DENVER.PDF", aptIdent: "APA", airportName: "CENTENNIAL"));

		Assert.Equal([".denDENc", ".apaDENVERc"], result.Lines.Select(line => line.Command));
	}

	[Fact]
	public void a_line_repeating_another_exactly_is_written_once()
	{
		ChartRecallBuildResult result = Build(
			Chart("APD", "AIRPORT DIAGRAM", "00058AD.PDF"),
			Chart("APD", "AIRPORT DIAGRAM", "00058AD.PDF"));

		Assert.Single(result.Lines);
	}

	[Fact]
	public void two_charts_that_share_a_command_are_both_written()
	{
		ChartRecallBuildResult result = Build(
			Chart("APD", "AIRPORT DIAGRAM", "00500AD.PDF", aptIdent: "EDW", airportName: "EDWARDS AFB"),
			Chart("APD", "AIRPORT DIAGRAM (ROGERS LAKEBED)", "00500ADROGERSLAKEBED.PDF", aptIdent: "EDW", airportName: "EDWARDS AFB"));

		Assert.Equal([".edwAPDc", ".edwAPDc"], result.Lines.Select(line => line.Command));
	}

	[Fact]
	public void airports_keep_the_metafiles_order()
	{
		ChartRecallBuildResult result = Build(
			Chart("APD", "AIRPORT DIAGRAM", "1.PDF", aptIdent: "ZZZ"),
			Chart("APD", "AIRPORT DIAGRAM", "2.PDF", aptIdent: "AAA"),
			Chart("HOT", "HOT SPOT", "3.PDF", aptIdent: "ZZZ"));

		Assert.Equal([".zzzAPDc", ".zzzHSc", ".aaaAPDc"], result.Lines.Select(line => line.Command));
	}

	[Fact]
	public void the_summary_counts_commands_airports_and_each_rule_once_per_chart()
	{
		ChartRecallBuildResult result = Build(
			Chart("IAP", "ILS OR LOC RWY 22L", "A.PDF"),
			Chart("IAP", "HI-ILS OR LOC RWY 15", "B.PDF"),
			Chart("IAP", "HI-ILS OR LOC RWY 15, CONT.1", "C.PDF"),
			Chart("IAP", "ILS OR LOC/NDB RWY 10", "D.PDF"),
			Chart("MIN", "ALTERNATE MINIMUMS", "E.PDF"),
			Chart("APD", "AIRPORT DIAGRAM", "F.PDF", aptIdent: "AAA"));

		ChartRecallSummary summary = result.Summary;
		Assert.Equal(2, summary.AirportCount);
		Assert.Equal(4, summary.CommandCount);
		Assert.Equal(3, summary.CommandsByChartType["IAP"]);
		Assert.Equal(1, summary.CommandsByChartType["APD"]);
		Assert.Equal(1, summary.SkippedByReason[ChartRecallSkipReason.HighAltitude]);
		Assert.Equal(1, summary.SkippedByReason[ChartRecallSkipReason.LocNdb]);
		Assert.Equal(1, summary.SkippedByReason[ChartRecallSkipReason.AlternateMinimums]);
	}

	[Fact]
	public void the_summary_message_names_the_file_the_counts_and_the_rules()
	{
		ChartRecallBuildResult result = Build(
			Chart("IAP", "ILS OR LOC RWY 22L", "A.PDF"),
			Chart("IAP", "COPTER ILS OR LOC RWY 30", "B.PDF"),
			Chart("DP", "KNIK THREE", "C.PDF"));

		ServiceMessage summary = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Info, summary.Level);
		Assert.Equal(
			"FAA_CHART_RECALL.txt: 3 command(s) for 1 airport(s) - IAP 2, DP 1. No command, by rule: 1 COPTER approach(es). " +
			"1 departure(s)/STAR(s) had no computer code, so were named from the chart name (0 of them after their airport).",
			summary.Text);
	}

	[Fact]
	public void the_summary_names_every_rule_that_left_a_chart_out()
	{
		ChartRecallBuildResult result = Build(
			Chart("IAP", "HI-TACAN RWY 5", "1.PDF"),
			Chart("IAP", "COPTER VOR RWY 36", "2.PDF"),
			Chart("IAP", "ILS PRM RWY 09R", "3.PDF"),
			Chart("IAP", "ILS RWY 04R (SA CAT I)", "4.PDF"),
			Chart("IAP", "CONVERGING ILS RWY 17C", "5.PDF"),
			Chart("DAU", "RNAV DP AAUP", "6.PDF"),
			Chart("IAP", "VOR-1 RWY 14L", "7.PDF"),
			Chart("IAP", "GLS RWY 19L", "8.PDF"),
			Chart("IAP", "ILS OR LOC/NDB RWY 10", "9.PDF"),
			Chart("MIN", "ALTERNATE MINIMUMS", "10.PDF"));

		string summary = Assert.Single(result.Messages).Text;

		Assert.Equal(Enum.GetValues<ChartRecallSkipReason>().Length, result.Summary.SkippedByReason.Count);
		Assert.Contains(
			"No command, by rule: 1 HI- approach(es), 1 COPTER approach(es), 1 PRM chart(s), 1 CAT II/III or SA CAT approach(es), " +
			"1 CONVERGING approach(es), 1 AAUP page(s), 1 numbered approach(es) such as VOR-1, 1 GLS approach(es) or part(s), " +
			"1 LOC/NDB part(s), 1 alternate minimums sheet(s).",
			summary,
			StringComparison.Ordinal);
	}

	[Fact]
	public void an_empty_metafile_builds_no_line_and_a_bare_summary()
	{
		ChartRecallBuildResult result = Build();

		Assert.Empty(result.Lines);
		Assert.Equal("FAA_CHART_RECALL.txt: 0 command(s) for 0 airport(s).", Assert.Single(result.Messages).Text);
	}

	[Fact]
	public void an_unrecognized_approach_type_is_one_warning_naming_its_charts()
	{
		ChartRecallBuildResult result = Build(
			Chart("IAP", "SDF RWY 36", "A.PDF", aptIdent: "AAA"),
			Chart("IAP", "SDF RWY 36, CONT.1", "B.PDF", aptIdent: "AAA"),
			Chart("IAP", "SDF RWY 4", "C.PDF", aptIdent: "BBB"));

		ServiceMessage warning = Assert.Single(result.Messages, message => message.Level == LogLevel.Warning);
		Assert.Contains("FE-Buddy doesn't recognize the approach type 'SDF', so 2 chart(s) got no command for it (AAA SDF RWY 36, BBB SDF RWY 4)", warning.Text, StringComparison.Ordinal);
		Assert.Empty(result.Lines);
	}

	[Fact]
	public void a_warning_names_at_most_five_charts()
	{
		DtppRecord[] records = [.. Enumerable.Range(1, 7).Select(i => Chart("IAP", $"SDF RWY {i}", $"{i}.PDF"))];

		ServiceMessage warning = Assert.Single(Build(records).Messages, message => message.Level == LogLevel.Warning);

		Assert.Contains("7 chart(s) got no command for it (DTW SDF RWY 1, DTW SDF RWY 2, DTW SDF RWY 3, DTW SDF RWY 4, DTW SDF RWY 5, ...)", warning.Text, StringComparison.Ordinal);
	}

	[Fact]
	public void more_than_twenty_unrecognized_things_are_summed_up_in_one_last_warning()
	{
		DtppRecord[] records = [.. Enumerable.Range(1, 23).Select(i => Chart($"X{i}", "SOMETHING NEW", $"{i}.PDF"))];

		List<ServiceMessage> warnings = [.. Build(records).Messages.Where(message => message.Level == LogLevel.Warning)];

		Assert.Equal(21, warnings.Count);
		Assert.StartsWith("FAA Chart Recall: 3 more thing(s) FE-Buddy doesn't recognize", warnings[^1].Text, StringComparison.Ordinal);
	}
}
