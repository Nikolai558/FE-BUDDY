using FeBuddy.Core.Application.Airac.Departures;
using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Departures.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Departures.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Departures;

/// <summary>
/// Covers <see cref="DepartureBuilder"/>: reading procedures from the DP files, the airports each one
/// serves, and locating their points.
/// </summary>
public sealed class DepartureBuilderTests
{
	private const string TestName = "TESTY";
	private const string TestArtcc = "ZZZ";
	private const string TestCode = "TESTY1.TESTY";

	private static DepartureLocateResult ReadAndLocate(NasrCsvDataCollection data)
	{
		DepartureProcedureReadResult read = DepartureBuilder.ReadProcedures(data);
		return DepartureBuilder.Locate(read.Procedures, data);
	}

	/// <summary>
	/// A procedure with bodies B1 (ALPHA-CHRLI) and B2 (BRAVO-CHRLI) and one transition
	/// (CHRLI-DELTA), with the given SERVED_ARPT and DP_APT assignments.
	/// </summary>
	private static NasrCsvDataCollection TwoBodyProcedure(string servedArpt, params (string Body, string Airport)[] assignments)
	{
		List<DpCsvDataModel.DpRte> routes =
		[
			.. DepartureTestData.Body(TestName, TestArtcc, TestCode, "B1", ["ALPHA", "CHRLI"]),
			.. DepartureTestData.Body(TestName, TestArtcc, TestCode, "B2", ["BRAVO", "CHRLI"]),
			.. DepartureTestData.Transition(TestName, TestArtcc, TestCode, "DELTA TRANSITION", "TESTY1.DELTA", ["CHRLI", "DELTA"]),
		];

		return DepartureTestData.Build(
			bases: [DepartureTestData.Base(TestName, TestArtcc, TestCode, servedArpt: servedArpt)],
			apts: [.. assignments.Select(a => DepartureTestData.Apt(TestName, TestArtcc, TestCode, a.Body, a.Airport))],
			routes: routes,
			fixes: DepartureTestData.SyntheticFixes("ALPHA", "BRAVO", "CHRLI", "DELTA"));
	}

	[Fact]
	public void read_procedures_throws_when_dp_data_was_never_parsed()
	{
		NasrCsvDataCollection data = new(); // Dp is null

		Assert.Throws<InvalidOperationException>(() => DepartureBuilder.ReadProcedures(data));
	}

	[Fact]
	public void read_procedures_reads_dotss2_with_its_identity_bodies_and_transitions()
	{
		DepartureProcedureReadResult result = DepartureBuilder.ReadProcedures(DepartureTestData.Dotss());

		DepartureProcedure procedure = Assert.Single(result.Procedures);
		Assert.Equal("DOTSS", procedure.DpName);
		Assert.Equal("ZLA", procedure.Artcc);
		Assert.Equal("DOTSS2.DOTSS", procedure.ComputerCode);
		Assert.Equal("DOTSS", procedure.CodeId);
		Assert.Equal("TWO", procedure.AmendmentNo);
		Assert.Equal(new DateOnly(2017, 8, 17), procedure.AmendmentEffectiveDate);
		Assert.Equal(new DateOnly(2026, 9, 3), procedure.CycleEffectiveDate);
		Assert.False(procedure.IsObstacleDeparture);
		Assert.Equal(["LAX"], procedure.ServedAirports);
		Assert.Equal(
			["DLREY-DOTSS", "DOCKR-DOTSS", "FABRA-DOTSS", "HIIPR-DOTSS"],
			procedure.Bodies.Select(b => b.Name));
		Assert.Equal(
			["CLEEE TRANSITION", "CNERY TRANSITION"],
			procedure.Transitions.Select(t => t.Name));
		Assert.Equal(["DOTSS2.CLEEE", "DOTSS2.CNERY"], procedure.Transitions.Select(t => t.TransitionCode));
		Assert.All(procedure.Bodies, body => Assert.Null(body.TransitionCode));
	}

	[Fact]
	public void read_procedures_trims_nasr_padding_from_the_point_type()
	{
		DepartureProcedureReadResult result = DepartureBuilder.ReadProcedures(DepartureTestData.Dotss());

		DepartureProcedure procedure = Assert.Single(result.Procedures);
		IEnumerable<DepartureRawPoint> points = procedure.Bodies.Concat(procedure.Transitions).SelectMany(r => r.Points);

		Assert.All(points, point => Assert.Equal("WP", point.PointType));
		Assert.Equal(
			["DLREY", "ENNEY", "NAANC", "HAYNK", "PEVEE", "HOLTZ", "DOTSS"],
			procedure.Bodies[0].Points.Select(p => p.Id));
	}

	[Fact]
	public void served_airports_are_the_union_of_served_arpt_and_dp_apt_without_duplicates()
	{
		NasrCsvDataCollection data = TwoBodyProcedure("AAA", ("B1", "AAA"), ("B2", "BBB"));

		DepartureProcedureReadResult result = DepartureBuilder.ReadProcedures(data);

		DepartureProcedure procedure = Assert.Single(result.Procedures);
		Assert.Equal(["AAA", "BBB"], procedure.ServedAirports);
	}

	[Fact]
	public void locate_places_dotss2_at_lax_with_every_route_and_twenty_distinct_points()
	{
		DepartureLocateResult result = ReadAndLocate(DepartureTestData.Dotss());

		DepartureAirportProcedure airportProcedure = Assert.Single(result.AirportProcedures);
		Assert.Equal("LAX", airportProcedure.AirportId);
		Assert.Equal(0, result.SkippedCount);
		Assert.Equal(6, airportProcedure.Routes.Count);
		Assert.Equal(20, airportProcedure.Points.Count);
		Assert.Equal(20, airportProcedure.Points.Select(p => p.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
		Assert.True(airportProcedure.HasDrawableRoute);
	}

	[Fact]
	public void located_points_carry_their_fix_base_coordinates()
	{
		DepartureLocateResult result = ReadAndLocate(DepartureTestData.Dotss());

		DepartureAirportProcedure airportProcedure = Assert.Single(result.AirportProcedures);

		foreach ((string Id, double Lat, double Lon) in DepartureTestData.DotssFixes)
		{
			DeparturePoint point = Assert.Single(airportProcedure.Points, p => p.Id == Id);
			Assert.Equal(Lat, point.Latitude);
			Assert.Equal(Lon, point.Longitude);
		}
	}

	[Fact]
	public void each_airport_gets_only_its_dp_apt_bodies_plus_every_transition()
	{
		NasrCsvDataCollection data = TwoBodyProcedure("AAA", ("B1", "AAA"), ("B2", "BBB"));

		DepartureLocateResult result = ReadAndLocate(data);

		Assert.Equal(2, result.AirportProcedures.Count);

		DepartureAirportProcedure aaa = Assert.Single(result.AirportProcedures, p => p.AirportId == "AAA");
		Assert.Equal(["B1", "DELTA TRANSITION"], aaa.Routes.Select(r => r.Name));

		DepartureAirportProcedure bbb = Assert.Single(result.AirportProcedures, p => p.AirportId == "BBB");
		Assert.Equal(["B2", "DELTA TRANSITION"], bbb.Routes.Select(r => r.Name));
	}

	[Fact]
	public void an_airport_only_in_served_arpt_gets_every_body()
	{
		NasrCsvDataCollection data = TwoBodyProcedure("CCC", ("B1", "AAA"));

		DepartureLocateResult result = ReadAndLocate(data);

		DepartureAirportProcedure ccc = Assert.Single(result.AirportProcedures, p => p.AirportId == "CCC");
		Assert.Equal(["B1", "B2", "DELTA TRANSITION"], ccc.Routes.Select(r => r.Name));

		DepartureAirportProcedure aaa = Assert.Single(result.AirportProcedures, p => p.AirportId == "AAA");
		Assert.Equal(["B1", "DELTA TRANSITION"], aaa.Routes.Select(r => r.Name));
	}

	[Fact]
	public void a_point_missing_from_fix_base_skips_the_pair_with_a_warning_naming_it()
	{
		NasrCsvDataCollection data = DepartureTestData.Build(
			bases: [DepartureTestData.Base(TestName, TestArtcc, TestCode, servedArpt: "AAA")],
			routes: DepartureTestData.Body(TestName, TestArtcc, TestCode, "B1", ["ALPHA", "MISNG"]),
			fixes: DepartureTestData.SyntheticFixes("ALPHA"));

		DepartureLocateResult result = ReadAndLocate(data);

		Assert.Empty(result.AirportProcedures);
		Assert.Equal(1, result.SkippedCount);
		Assert.Contains(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains("MISNG"));
	}

	[Fact]
	public void a_procedure_with_no_dp_rte_rows_produces_nothing_with_an_info_message()
	{
		NasrCsvDataCollection data = DepartureTestData.Build(
			bases: [DepartureTestData.Base(TestName, TestArtcc, TestCode, servedArpt: "AAA")]);

		DepartureProcedureReadResult read = DepartureBuilder.ReadProcedures(data);
		DepartureProcedure procedure = Assert.Single(read.Procedures);
		Assert.False(procedure.HasRoutes);

		DepartureLocateResult result = DepartureBuilder.Locate(read.Procedures, data);

		Assert.Empty(result.AirportProcedures);
		Assert.Equal(0, result.SkippedCount);
		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Info, message.Level);
		Assert.Contains(TestName, message.Text);
	}

	[Fact]
	public void a_single_point_procedure_has_no_drawable_route()
	{
		NasrCsvDataCollection data = DepartureTestData.Build(
			bases: [DepartureTestData.Base(TestName, TestArtcc, TestCode, servedArpt: "AAA")],
			routes: DepartureTestData.Body(TestName, TestArtcc, TestCode, "ALPHA", ["ALPHA"]),
			fixes: DepartureTestData.SyntheticFixes("ALPHA"));

		DepartureLocateResult result = ReadAndLocate(data);

		DepartureAirportProcedure airportProcedure = Assert.Single(result.AirportProcedures);
		Assert.False(airportProcedure.HasDrawableRoute);
		Assert.Equal(["ALPHA"], airportProcedure.Points.Select(p => p.Id));
	}

	[Fact]
	public void a_dp_base_row_with_no_name_is_skipped_with_a_warning()
	{
		NasrCsvDataCollection data = DepartureTestData.Build(
			bases:
			[
				DepartureTestData.Base("  ", TestArtcc, "NONAME1.NONAME", servedArpt: "AAA"),
				DepartureTestData.Base(TestName, TestArtcc, TestCode, servedArpt: "AAA"),
			]);

		DepartureProcedureReadResult read = DepartureBuilder.ReadProcedures(data);

		Assert.Equal(TestName, Assert.Single(read.Procedures).DpName);
		ServiceMessage warning = Assert.Single(read.Messages);
		Assert.Equal(LogLevel.Warning, warning.Level);
		Assert.Contains($"ARTCC '{TestArtcc}' has no DP_NAME", warning.Text);
	}

	[Fact]
	public void procedures_are_read_in_artcc_then_name_order()
	{
		NasrCsvDataCollection data = DepartureTestData.Build(
			bases:
			[
				DepartureTestData.Base("ZULU", "ZAB", "ZULU1.ZULU"),
				DepartureTestData.Base("ALPHA", "ZAB", "ALPHA1.ALPHA"),
				DepartureTestData.Base("MIKE", "ZAA", "MIKE1.MIKE"),
			]);

		DepartureProcedureReadResult read = DepartureBuilder.ReadProcedures(data);

		Assert.Equal(["MIKE", "ALPHA", "ZULU"], read.Procedures.Select(p => p.DpName));
	}

	[Fact]
	public void an_airport_assigned_only_unknown_bodies_and_no_transitions_gets_an_info_message()
	{
		NasrCsvDataCollection data = DepartureTestData.Build(
			bases: [DepartureTestData.Base(TestName, TestArtcc, TestCode)],
			apts:
			[
				DepartureTestData.Apt(TestName, TestArtcc, TestCode, "B1", "AAA"),
				DepartureTestData.Apt(TestName, TestArtcc, TestCode, "GHOST", "BBB"),
			],
			routes: DepartureTestData.Body(TestName, TestArtcc, TestCode, "B1", ["ALPHA", "CHRLI"]),
			fixes: DepartureTestData.SyntheticFixes("ALPHA", "CHRLI"));

		DepartureLocateResult result = ReadAndLocate(data);

		Assert.Equal("AAA", Assert.Single(result.AirportProcedures).AirportId);
		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Info, message.Level);
		Assert.Contains("at BBB: DP_APT assigns it no body that DP_RTE lists", message.Text);
	}

	[Fact]
	public void a_second_procedure_with_the_same_identifier_at_an_airport_is_skipped()
	{
		// The same procedure published under two ARTCCs: two procedures, one identifier.
		const string otherArtcc = "ZYY";

		List<DpCsvDataModel.DpRte> routes =
		[
			.. DepartureTestData.Body(TestName, TestArtcc, TestCode, "B1", ["ALPHA", "CHRLI"]),
			.. DepartureTestData.Body(TestName, otherArtcc, TestCode, "B1", ["ALPHA", "CHRLI"]),
		];

		NasrCsvDataCollection data = DepartureTestData.Build(
			bases:
			[
				DepartureTestData.Base(TestName, TestArtcc, TestCode, servedArpt: "AAA"),
				DepartureTestData.Base(TestName, otherArtcc, TestCode, servedArpt: "AAA"),
			],
			routes: routes,
			fixes: DepartureTestData.SyntheticFixes("ALPHA", "CHRLI"));

		DepartureLocateResult result = ReadAndLocate(data);

		Assert.Single(result.AirportProcedures);
		Assert.Equal(1, result.SkippedCount);
		Assert.Contains(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains("already uses the identifier 'TESTY'"));
	}

	[Fact]
	public void a_route_whose_rows_name_no_points_is_dropped()
	{
		List<DpCsvDataModel.DpRte> routes =
		[
			.. DepartureTestData.Body(TestName, TestArtcc, TestCode, "B1", ["ALPHA", "CHRLI"]),
			.. DepartureTestData.Transition(TestName, TestArtcc, TestCode, "EMPTY TRANSITION", "TESTY1.EMPTY", [" ", ""]),
		];

		NasrCsvDataCollection data = DepartureTestData.Build(
			bases: [DepartureTestData.Base(TestName, TestArtcc, TestCode, servedArpt: "AAA")],
			routes: routes);

		DepartureProcedure procedure = Assert.Single(DepartureBuilder.ReadProcedures(data).Procedures);

		Assert.Single(procedure.Bodies);
		Assert.Empty(procedure.Transitions);
	}
}
