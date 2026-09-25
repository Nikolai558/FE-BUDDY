using FeBuddy.Core.Application.Airac.Arrivals;
using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Arrivals.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Arrivals.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Arrivals;

/// <summary>
/// Covers <see cref="ArrivalBuilder"/>: reading procedures from the STAR files, the airports each
/// one serves, multi-ARTCC handling, and locating their points.
/// </summary>
public sealed class ArrivalBuilderTests
{
	private const string TestName = "TESTY";
	private const string TestArtcc = "ZZZ";
	private const string TestCode = "TRANS.TESTY1";

	private static ArrivalLocateResult ReadAndLocate(NasrCsvDataCollection data)
	{
		ArrivalProcedureReadResult read = ArrivalBuilder.ReadProcedures(data);
		return ArrivalBuilder.Locate(read.Procedures, data);
	}

	/// <summary>
	/// A procedure with bodies B1 (through ALPHA-CHRLI) and B2 (through BRAVO-CHRLI) and one
	/// transition (CHRLI-DELTA), with the given SERVED_ARPT and STAR_APT assignments.
	/// </summary>
	private static NasrCsvDataCollection TwoBodyProcedure(string servedArpt, params (string Body, string Airport)[] assignments)
	{
		List<StarCsvDataModel.StarRte> routes =
		[
			.. ArrivalTestData.Body(TestArtcc, TestCode, "B1", ["ALPHA", "CHRLI"]),
			.. ArrivalTestData.Body(TestArtcc, TestCode, "B2", ["BRAVO", "CHRLI"]),
			.. ArrivalTestData.Transition(TestArtcc, TestCode, "DELTA TRANSITION", "DELTA.TESTY1", ["CHRLI", "DELTA"]),
		];

		return ArrivalTestData.Build(
			bases: [ArrivalTestData.Base(TestName, TestArtcc, TestCode, servedArpt: servedArpt)],
			apts: [.. assignments.Select(a => ArrivalTestData.Apt(TestArtcc, TestCode, a.Body, a.Airport))],
			routes: routes,
			fixes: ArrivalTestData.SyntheticFixes("ALPHA", "BRAVO", "CHRLI", "DELTA"));
	}

	[Fact]
	public void read_procedures_throws_when_star_data_was_never_parsed()
	{
		NasrCsvDataCollection data = new(); // Star is null

		Assert.Throws<InvalidOperationException>(() => ArrivalBuilder.ReadProcedures(data));
	}

	[Fact]
	public void read_procedures_reads_blaid2_with_its_identity_body_and_transitions()
	{
		ArrivalProcedureReadResult result = ArrivalBuilder.ReadProcedures(ArrivalTestData.Blaid());

		ArrivalProcedure procedure = Assert.Single(result.Procedures);
		Assert.Equal("BLAID", procedure.ArrivalName);
		Assert.Equal("ZLA", procedure.ArtccText);
		Assert.Equal(["ZLA"], procedure.Artccs);
		Assert.Empty(procedure.ArtccByAirport);
		Assert.Equal("AALAN.BLAID2", procedure.ComputerCode);
		Assert.Equal("BLAID", procedure.CodeId);
		Assert.Equal("TWO", procedure.AmendmentNo);
		Assert.Equal(new DateOnly(2024, 1, 25), procedure.AmendmentEffectiveDate);
		Assert.Equal(new DateOnly(2026, 9, 3), procedure.CycleEffectiveDate);
		Assert.Equal(["LAS"], procedure.ServedAirports);
		Assert.Equal(["AALAN-BLAID"], procedure.Bodies.Select(b => b.Name));
		Assert.Equal(
			["BRYCE CANYON TRANSITION", "ENOCH TRANSITION", "PAGE TRANSITION"],
			procedure.Transitions.Select(t => t.Name));
		Assert.Equal(["BCE.BLAID2", "EHK.BLAID2", "PGA.BLAID2"], procedure.Transitions.Select(t => t.TransitionCode));
		Assert.All(procedure.Bodies, body => Assert.Null(body.TransitionCode));
		Assert.Equal("ZLA", procedure.ArtccFor("LAS"));
	}

	[Fact]
	public void read_procedures_trims_nasr_padding_from_the_point_type()
	{
		ArrivalProcedureReadResult result = ArrivalBuilder.ReadProcedures(ArrivalTestData.Blaid());

		ArrivalProcedure procedure = Assert.Single(result.Procedures);
		IEnumerable<ArrivalRawPoint> reportingPoints = procedure.Bodies.Concat(procedure.Transitions)
			.SelectMany(r => r.Points)
			.Where(p => p.Id is "AALAN" or "HOLDM" or "BLAID");

		Assert.All(reportingPoints, point => Assert.Equal("RP", point.PointType));
		Assert.Equal(["AALAN", "BLAID"], procedure.Bodies[0].Points.Select(p => p.Id));
	}

	[Fact]
	public void served_airports_are_the_union_of_served_arpt_and_star_apt_without_duplicates()
	{
		NasrCsvDataCollection data = TwoBodyProcedure("AAA", ("B1", "AAA"), ("B2", "BBB"));

		ArrivalProcedureReadResult result = ArrivalBuilder.ReadProcedures(data);

		ArrivalProcedure procedure = Assert.Single(result.Procedures);
		Assert.Equal(["AAA", "BBB"], procedure.ServedAirports);
	}

	[Fact]
	public void star_apt_and_star_rte_rows_for_a_different_code_or_artcc_are_not_joined()
	{
		NasrCsvDataCollection data = ArrivalTestData.Build(
			bases:
			[
				ArrivalTestData.Base(TestName, TestArtcc, TestCode, servedArpt: "AAA"),
				ArrivalTestData.Base("OTHER", "ZYY", "TRANS.OTHER1", servedArpt: "BBB"),
			],
			apts:
			[
				ArrivalTestData.Apt(TestArtcc, TestCode, "B1", "AAA"),
				ArrivalTestData.Apt("ZYY", "TRANS.OTHER1", "B1", "BBB"),
			],
			routes:
			[
				.. ArrivalTestData.Body(TestArtcc, TestCode, "B1", ["ALPHA", "CHRLI"]),
				.. ArrivalTestData.Body("ZYY", "TRANS.OTHER1", "B1", ["ECHOX", "FOXTR"]),
			]);

		ArrivalProcedureReadResult result = ArrivalBuilder.ReadProcedures(data);

		Assert.Equal(2, result.Procedures.Count);
		ArrivalProcedure testy = Assert.Single(result.Procedures, p => p.CodeId == "TESTY");
		Assert.Equal(["ALPHA", "CHRLI"], Assert.Single(testy.Bodies).Points.Select(p => p.Id));
		Assert.Equal(["AAA"], testy.ServedAirports);
	}

	[Fact]
	public void locate_places_blaid2_at_las_with_every_route_and_six_distinct_points()
	{
		ArrivalLocateResult result = ReadAndLocate(ArrivalTestData.Blaid());

		ArrivalAirportProcedure airportProcedure = Assert.Single(result.AirportProcedures);
		Assert.Equal("LAS", airportProcedure.AirportId);
		Assert.Equal("ZLA", airportProcedure.Artcc);
		Assert.Equal(0, result.SkippedCount);
		Assert.Equal(4, airportProcedure.Routes.Count);
		Assert.Equal(6, airportProcedure.Points.Count);
		Assert.Equal(6, airportProcedure.Points.Select(p => p.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
		Assert.True(airportProcedure.HasDrawableRoute);
	}

	[Fact]
	public void located_points_carry_their_fix_and_nav_base_coordinates()
	{
		ArrivalLocateResult result = ReadAndLocate(ArrivalTestData.Blaid());

		ArrivalAirportProcedure airportProcedure = Assert.Single(result.AirportProcedures);

		foreach ((string Id, double Lat, double Lon) in ArrivalTestData.BlaidFixes.Concat(ArrivalTestData.BlaidNavaids))
		{
			ArrivalPoint point = Assert.Single(airportProcedure.Points, p => p.Id == Id);
			Assert.Equal(Lat, point.Latitude);
			Assert.Equal(Lon, point.Longitude);
		}
	}

	[Fact]
	public void routes_for_returns_transitions_first_then_the_airports_bodies()
	{
		ArrivalProcedureReadResult read = ArrivalBuilder.ReadProcedures(ArrivalTestData.Blaid());
		ArrivalProcedure procedure = Assert.Single(read.Procedures);

		List<ArrivalRawRoute> routes = ArrivalBuilder.RoutesFor(procedure, ArrivalTestData.LasId);

		Assert.Equal(
			["BRYCE CANYON TRANSITION", "ENOCH TRANSITION", "PAGE TRANSITION", "AALAN-BLAID"],
			routes.Select(r => r.Name));
	}

	[Fact]
	public void located_points_at_las_are_in_flying_order_transitions_then_body()
	{
		ArrivalLocateResult result = ReadAndLocate(ArrivalTestData.Blaid());

		ArrivalAirportProcedure airportProcedure = Assert.Single(result.AirportProcedures);

		Assert.Equal(["BCE", "HOLDM", "AALAN", "EHK", "PGA", "BLAID"], airportProcedure.Points.Select(p => p.Id));
	}

	[Fact]
	public void each_airport_gets_only_its_star_apt_bodies_plus_every_transition()
	{
		NasrCsvDataCollection data = TwoBodyProcedure("AAA", ("B1", "AAA"), ("B2", "BBB"));

		ArrivalLocateResult result = ReadAndLocate(data);

		Assert.Equal(2, result.AirportProcedures.Count);

		ArrivalAirportProcedure aaa = Assert.Single(result.AirportProcedures, p => p.AirportId == "AAA");
		Assert.Equal(["DELTA TRANSITION", "B1"], aaa.Routes.Select(r => r.Name));

		ArrivalAirportProcedure bbb = Assert.Single(result.AirportProcedures, p => p.AirportId == "BBB");
		Assert.Equal(["DELTA TRANSITION", "B2"], bbb.Routes.Select(r => r.Name));
	}

	[Fact]
	public void an_airport_only_in_served_arpt_gets_every_body()
	{
		NasrCsvDataCollection data = TwoBodyProcedure("CCC", ("B1", "AAA"));

		ArrivalLocateResult result = ReadAndLocate(data);

		ArrivalAirportProcedure ccc = Assert.Single(result.AirportProcedures, p => p.AirportId == "CCC");
		Assert.Equal(["DELTA TRANSITION", "B1", "B2"], ccc.Routes.Select(r => r.Name));

		ArrivalAirportProcedure aaa = Assert.Single(result.AirportProcedures, p => p.AirportId == "AAA");
		Assert.Equal(["DELTA TRANSITION", "B1"], aaa.Routes.Select(r => r.Name));
	}

	[Fact]
	public void two_bodies_sharing_a_name_but_different_body_seq_are_kept_separate_and_matched_by_sequence()
	{
		// Modelled on the real APLES.EMMLN3: one BODY_SEQ per runway group, sharing a body name.
		const string code = "APLES.EMMLN3";
		const string artcc = "ZLA";
		const string name = "EMMLN";

		List<StarCsvDataModel.StarRte> routes =
		[
			.. ArrivalTestData.Body(artcc, code, "APLES-SLI", ["APLES", "CAPTZ", "RRIZE", "PDZZZ", "DOWDD", "AHEIM", "SLI11"], bodySeq: 1),
			.. ArrivalTestData.Body(artcc, code, "APLES-SLI", ["APLES", "CAPTZ", "RRIZE", "PDZZZ", "RNDAL", "POXKU", "SLI11"], bodySeq: 2),
		];

		NasrCsvDataCollection data = ArrivalTestData.Build(
			bases: [ArrivalTestData.Base(name, artcc, code, amendmentNo: "THREE", servedArpt: "FUL LGB SLI SNA")],
			apts:
			[
				ArrivalTestData.Apt(artcc, code, "APLES-SLI", "FUL", bodySeq: 1),
				ArrivalTestData.Apt(artcc, code, "APLES-SLI", "LGB", bodySeq: 1),
				ArrivalTestData.Apt(artcc, code, "APLES-SLI", "SLI", bodySeq: 1),
				ArrivalTestData.Apt(artcc, code, "APLES-SLI", "SNA", bodySeq: 2),
			],
			routes: routes,
			fixes: ArrivalTestData.SyntheticFixes(
				"APLES", "CAPTZ", "RRIZE", "PDZZZ", "DOWDD", "AHEIM", "SLI11", "RNDAL", "POXKU"));

		ArrivalLocateResult result = ReadAndLocate(data);

		ArrivalAirportProcedure ful = Assert.Single(result.AirportProcedures, p => p.AirportId == "FUL");
		ArrivalRoute fulBody = Assert.Single(ful.Routes);
		Assert.Equal(["APLES", "CAPTZ", "RRIZE", "PDZZZ", "DOWDD", "AHEIM", "SLI11"], fulBody.Points.Select(p => p.Id));

		ArrivalAirportProcedure sna = Assert.Single(result.AirportProcedures, p => p.AirportId == "SNA");
		ArrivalRoute snaBody = Assert.Single(sna.Routes);
		Assert.Equal(["APLES", "CAPTZ", "RRIZE", "PDZZZ", "RNDAL", "POXKU", "SLI11"], snaBody.Points.Select(p => p.Id));
	}

	[Fact]
	public void a_point_missing_from_fix_base_skips_the_pair_with_a_warning_naming_it()
	{
		NasrCsvDataCollection data = ArrivalTestData.Build(
			bases: [ArrivalTestData.Base(TestName, TestArtcc, TestCode, servedArpt: "AAA")],
			routes: ArrivalTestData.Body(TestArtcc, TestCode, "B1", ["ALPHA", "MISNG"]),
			fixes: ArrivalTestData.SyntheticFixes("ALPHA"));

		ArrivalLocateResult result = ReadAndLocate(data);

		Assert.Empty(result.AirportProcedures);
		Assert.Equal(1, result.SkippedCount);
		Assert.Contains(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains("MISNG"));
	}

	[Fact]
	public void a_procedure_with_no_star_rte_rows_produces_nothing_with_an_info_message()
	{
		NasrCsvDataCollection data = ArrivalTestData.Build(
			bases: [ArrivalTestData.Base(TestName, TestArtcc, TestCode, servedArpt: "AAA")]);

		ArrivalProcedureReadResult read = ArrivalBuilder.ReadProcedures(data);
		ArrivalProcedure procedure = Assert.Single(read.Procedures);
		Assert.False(procedure.HasRoutes);

		ArrivalLocateResult result = ArrivalBuilder.Locate(read.Procedures, data);

		Assert.Empty(result.AirportProcedures);
		Assert.Equal(0, result.SkippedCount);
		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Info, message.Level);
		Assert.Contains(TestName, message.Text);
	}

	[Fact]
	public void a_single_point_procedure_has_no_drawable_route()
	{
		NasrCsvDataCollection data = ArrivalTestData.Build(
			bases: [ArrivalTestData.Base(TestName, TestArtcc, TestCode, servedArpt: "AAA")],
			routes: ArrivalTestData.Body(TestArtcc, TestCode, "ALPHA", ["ALPHA"]),
			fixes: ArrivalTestData.SyntheticFixes("ALPHA"));

		ArrivalLocateResult result = ReadAndLocate(data);

		ArrivalAirportProcedure airportProcedure = Assert.Single(result.AirportProcedures);
		Assert.False(airportProcedure.HasDrawableRoute);
		Assert.Equal(["ALPHA"], airportProcedure.Points.Select(p => p.Id));
	}

	[Fact]
	public void a_star_base_row_with_a_blank_computer_code_is_skipped_with_a_warning()
	{
		NasrCsvDataCollection data = ArrivalTestData.Build(
			bases:
			[
				ArrivalTestData.Base("NONAME", TestArtcc, "  ", servedArpt: "AAA"),
				ArrivalTestData.Base(TestName, TestArtcc, TestCode, servedArpt: "AAA"),
			]);

		ArrivalProcedureReadResult result = ArrivalBuilder.ReadProcedures(data);

		Assert.Equal(TestName, Assert.Single(result.Procedures).ArrivalName);
		ServiceMessage warning = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Warning, warning.Level);
		Assert.Contains("has no STAR_COMPUTER_CODE", warning.Text);
	}

	[Fact]
	public void a_blank_arrival_name_with_a_code_whose_digit_does_not_match_yields_no_identifier_and_is_skipped()
	{
		NasrCsvDataCollection data = ArrivalTestData.Build(
			bases:
			[
				ArrivalTestData.Base("", TestArtcc, "TRANS.TESTY9", amendmentNo: "ONE", servedArpt: "AAA"),
				ArrivalTestData.Base(TestName, TestArtcc, TestCode, servedArpt: "AAA"),
			]);

		ArrivalProcedureReadResult result = ArrivalBuilder.ReadProcedures(data);

		Assert.Equal(TestName, Assert.Single(result.Procedures).ArrivalName);
		ServiceMessage warning = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Warning, warning.Level);
		Assert.Contains("no usable STAR_COMPUTER_CODE and no usable ARRIVAL_NAME", warning.Text);
	}

	[Fact]
	public void two_star_base_rows_with_the_same_code_and_artcc_are_both_skipped_with_one_warning()
	{
		NasrCsvDataCollection data = ArrivalTestData.Build(
			bases:
			[
				ArrivalTestData.Base(TestName, TestArtcc, TestCode, servedArpt: "AAA"),
				ArrivalTestData.Base("OTHER", TestArtcc, TestCode, servedArpt: "BBB"),
			]);

		ArrivalProcedureReadResult result = ArrivalBuilder.ReadProcedures(data);

		Assert.Empty(result.Procedures);
		ServiceMessage warning = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Warning, warning.Level);
		Assert.Contains($"STAR_COMPUTER_CODE '{TestCode}'", warning.Text);
	}

	[Fact]
	public void procedures_are_read_in_artcc_then_name_order()
	{
		NasrCsvDataCollection data = ArrivalTestData.Build(
			bases:
			[
				ArrivalTestData.Base("ZULU", "ZAB", "TRANS.ZULU1"),
				ArrivalTestData.Base("ALPHA", "ZAB", "TRANS.ALPHA1"),
				ArrivalTestData.Base("MIKE", "ZAA", "TRANS.MIKE1"),
			]);

		ArrivalProcedureReadResult result = ArrivalBuilder.ReadProcedures(data);

		Assert.Equal(["MIKE", "ALPHA", "ZULU"], result.Procedures.Select(p => p.ArrivalName));
	}

	[Fact]
	public void an_unparseable_amendment_or_cycle_date_reads_as_null()
	{
		NasrCsvDataCollection data = ArrivalTestData.Build(
			bases:
			[
				ArrivalTestData.Base(
					TestName, TestArtcc, TestCode, amendEffDate: "NOT A DATE", effDate: "ALSO NOT A DATE", servedArpt: "AAA"),
			]);

		ArrivalProcedure procedure = Assert.Single(ArrivalBuilder.ReadProcedures(data).Procedures);

		Assert.Null(procedure.AmendmentEffectiveDate);
		Assert.Null(procedure.CycleEffectiveDate);
		Assert.Equal("NOT A DATE", procedure.AmendmentEffectiveDateText);
	}

	[Fact]
	public void an_airport_assigned_only_unknown_bodies_and_no_transitions_gets_an_info_message()
	{
		NasrCsvDataCollection data = ArrivalTestData.Build(
			bases: [ArrivalTestData.Base(TestName, TestArtcc, TestCode)],
			apts:
			[
				ArrivalTestData.Apt(TestArtcc, TestCode, "B1", "AAA"),
				ArrivalTestData.Apt(TestArtcc, TestCode, "GHOST", "BBB"),
			],
			routes: ArrivalTestData.Body(TestArtcc, TestCode, "B1", ["ALPHA", "CHRLI"]),
			fixes: ArrivalTestData.SyntheticFixes("ALPHA", "CHRLI"));

		ArrivalLocateResult result = ReadAndLocate(data);

		Assert.Equal("AAA", Assert.Single(result.AirportProcedures).AirportId);
		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Info, message.Level);
		Assert.Contains("at BBB: STAR_APT assigns it no body that STAR_RTE lists", message.Text);
	}

	[Fact]
	public void a_second_procedure_with_the_same_identifier_at_an_airport_is_skipped()
	{
		// The same procedure published under two ARTCCs: two procedures, one identifier.
		const string otherArtcc = "ZYY";

		List<StarCsvDataModel.StarRte> routes =
		[
			.. ArrivalTestData.Body(TestArtcc, TestCode, "B1", ["ALPHA", "CHRLI"]),
			.. ArrivalTestData.Body(otherArtcc, TestCode, "B1", ["ALPHA", "CHRLI"]),
		];

		NasrCsvDataCollection data = ArrivalTestData.Build(
			bases:
			[
				ArrivalTestData.Base(TestName, TestArtcc, TestCode, servedArpt: "AAA"),
				ArrivalTestData.Base(TestName, otherArtcc, TestCode, servedArpt: "AAA"),
			],
			routes: routes,
			fixes: ArrivalTestData.SyntheticFixes("ALPHA", "CHRLI"));

		ArrivalLocateResult result = ReadAndLocate(data);

		Assert.Single(result.AirportProcedures);
		Assert.Equal(1, result.SkippedCount);
		Assert.Contains(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains("already uses the identifier 'TESTY'"));
	}

	[Fact]
	public void a_route_whose_rows_name_no_points_is_dropped()
	{
		List<StarCsvDataModel.StarRte> routes =
		[
			.. ArrivalTestData.Body(TestArtcc, TestCode, "B1", ["ALPHA", "CHRLI"]),
			.. ArrivalTestData.Transition(TestArtcc, TestCode, "EMPTY TRANSITION", "TRANS.EMPTY1", [" ", ""]),
		];

		NasrCsvDataCollection data = ArrivalTestData.Build(
			bases: [ArrivalTestData.Base(TestName, TestArtcc, TestCode, servedArpt: "AAA")],
			routes: routes);

		ArrivalProcedure procedure = Assert.Single(ArrivalBuilder.ReadProcedures(data).Procedures);

		Assert.Single(procedure.Bodies);
		Assert.Empty(procedure.Transitions);
	}

	[Fact]
	public void a_multi_artcc_star_splits_and_upper_cases_its_artcc_text()
	{
		NasrCsvDataCollection data = ArrivalTestData.Build(
			bases: [ArrivalTestData.Base(TestName, "zdc zny", TestCode, servedArpt: "DOV ILG")]);

		ArrivalProcedure procedure = Assert.Single(ArrivalBuilder.ReadProcedures(data).Procedures);

		Assert.Equal("zdc zny", procedure.ArtccText);
		Assert.Equal(["ZDC", "ZNY"], procedure.Artccs);
	}

	[Fact]
	public void artcc_by_airport_maps_each_served_airports_own_resp_artcc()
	{
		NasrCsvDataCollection data = ArrivalTestData.Build(
			bases: [ArrivalTestData.Base(TestName, "ZDC ZNY", TestCode, servedArpt: "DOV ILG")],
			airports:
			[
				ArrivalTestData.Airport("DOV", 39.13, -75.47, respArtccId: "ZDC"),
				ArrivalTestData.Airport("ILG", 39.68, -75.61, respArtccId: "ZNY"),
			]);

		ArrivalProcedure procedure = Assert.Single(ArrivalBuilder.ReadProcedures(data).Procedures);

		Assert.Equal("ZDC", procedure.ArtccByAirport["DOV"]);
		Assert.Equal("ZNY", procedure.ArtccByAirport["ILG"]);
		Assert.Equal("ZDC", procedure.ArtccFor("DOV"));
		Assert.Equal("ZNY", procedure.ArtccFor("ILG"));
	}

	[Fact]
	public void an_airport_whose_resp_artcc_is_not_shared_or_is_missing_falls_back_to_the_first_artcc()
	{
		NasrCsvDataCollection data = ArrivalTestData.Build(
			bases: [ArrivalTestData.Base(TestName, "ZDC ZNY", TestCode, servedArpt: "DOV ILG 33N")],
			airports:
			[
				ArrivalTestData.Airport("DOV", 39.13, -75.47, respArtccId: "ZOB"), // not one of ZDC/ZNY
				// ILG and 33N have no APT_BASE row at all.
			]);

		ArrivalProcedure procedure = Assert.Single(ArrivalBuilder.ReadProcedures(data).Procedures);

		Assert.Empty(procedure.ArtccByAirport);
		Assert.Equal("ZDC", procedure.ArtccFor("DOV"));
		Assert.Equal("ZDC", procedure.ArtccFor("ILG"));
		Assert.Equal("ZDC", procedure.ArtccFor("33N"));
	}

	[Fact]
	public void a_single_artcc_star_has_an_empty_artcc_by_airport_even_if_apt_base_disagrees()
	{
		NasrCsvDataCollection data = ArrivalTestData.Build(
			bases: [ArrivalTestData.Base(TestName, TestArtcc, TestCode, servedArpt: "AAA")],
			airports: [ArrivalTestData.Airport("AAA", 40.0, -100.0, respArtccId: "ZOA")]);

		ArrivalProcedure procedure = Assert.Single(ArrivalBuilder.ReadProcedures(data).Procedures);

		Assert.Empty(procedure.ArtccByAirport);
		Assert.Equal(TestArtcc, procedure.ArtccFor("AAA"));
	}

	[Fact]
	public void apt_base_not_parsed_leaves_artcc_by_airport_empty()
	{
		NasrCsvDataCollection data = ArrivalTestData.Build(
			bases: [ArrivalTestData.Base(TestName, "ZDC ZNY", TestCode, servedArpt: "DOV ILG")]);
		data.Apt = null;

		ArrivalProcedure procedure = Assert.Single(ArrivalBuilder.ReadProcedures(data).Procedures);

		Assert.Empty(procedure.ArtccByAirport);
		Assert.Equal("ZDC", procedure.ArtccFor("DOV"));
	}

	[Fact]
	public void artcc_for_with_no_artcc_at_all_returns_empty()
	{
		NasrCsvDataCollection data = ArrivalTestData.Build(
			bases: [ArrivalTestData.Base(TestName, "", TestCode, servedArpt: "AAA")]);

		ArrivalProcedure procedure = Assert.Single(ArrivalBuilder.ReadProcedures(data).Procedures);

		Assert.Empty(procedure.Artccs);
		Assert.Equal(string.Empty, procedure.ArtccFor("AAA"));
	}

	[Fact]
	public void located_copies_carry_each_airports_own_artcc_for_a_shared_star()
	{
		NasrCsvDataCollection data = ArrivalTestData.Build(
			bases: [ArrivalTestData.Base(TestName, "ZDC ZNY", TestCode, servedArpt: "DOV ILG")],
			routes: ArrivalTestData.Body("ZDC ZNY", TestCode, "B1", ["ALPHA", "BRAVO"]),
			fixes: ArrivalTestData.SyntheticFixes("ALPHA", "BRAVO"),
			airports:
			[
				ArrivalTestData.Airport("DOV", 39.13, -75.47, respArtccId: "ZDC"),
				ArrivalTestData.Airport("ILG", 39.68, -75.61, respArtccId: "ZNY"),
			]);

		ArrivalLocateResult result = ReadAndLocate(data);

		Assert.Equal("ZDC", Assert.Single(result.AirportProcedures, p => p.AirportId == "DOV").Artcc);
		Assert.Equal("ZNY", Assert.Single(result.AirportProcedures, p => p.AirportId == "ILG").Artcc);
	}
}
