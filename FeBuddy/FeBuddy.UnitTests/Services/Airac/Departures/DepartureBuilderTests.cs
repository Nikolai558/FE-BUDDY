using FeBuddy.Core.Models.NASR.CSV;
using FeBuddy.Core.Models.Services.Airac.Departures;
using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Services.Airac.Departures;
using FeBuddy.Core.Services.General;

using FeBuddy.UnitTests.Services.Airac.Departures.Fixtures;

namespace FeBuddy.UnitTests.Services.Airac.Departures;

public class DepartureBuilderTests
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
		List<DpCsvDataModel.DpRte> routes = new();
		routes.AddRange(DepartureTestData.Body(TestName, TestArtcc, TestCode, "B1", new[] { "ALPHA", "CHRLI" }));
		routes.AddRange(DepartureTestData.Body(TestName, TestArtcc, TestCode, "B2", new[] { "BRAVO", "CHRLI" }));
		routes.AddRange(DepartureTestData.Transition(TestName, TestArtcc, TestCode, "DELTA TRANSITION", "TESTY1.DELTA", new[] { "CHRLI", "DELTA" }));

		return DepartureTestData.Build(
			bases: new[] { DepartureTestData.Base(TestName, TestArtcc, TestCode, servedArpt: servedArpt) },
			apts: assignments.Select(a => DepartureTestData.Apt(TestName, TestArtcc, TestCode, a.Body, a.Airport)).ToList(),
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
		Assert.Equal(new[] { "LAX" }, procedure.ServedAirports);
		Assert.Equal(
			new[] { "DLREY-DOTSS", "DOCKR-DOTSS", "FABRA-DOTSS", "HIIPR-DOTSS" },
			procedure.Bodies.Select(b => b.Name));
		Assert.Equal(
			new[] { "CLEEE TRANSITION", "CNERY TRANSITION" },
			procedure.Transitions.Select(t => t.Name));
		Assert.Equal(new[] { "DOTSS2.CLEEE", "DOTSS2.CNERY" }, procedure.Transitions.Select(t => t.TransitionCode));
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
			new[] { "DLREY", "ENNEY", "NAANC", "HAYNK", "PEVEE", "HOLTZ", "DOTSS" },
			procedure.Bodies[0].Points.Select(p => p.Id));
	}

	[Fact]
	public void served_airports_are_the_union_of_served_arpt_and_dp_apt_without_duplicates()
	{
		NasrCsvDataCollection data = TwoBodyProcedure("AAA", ("B1", "AAA"), ("B2", "BBB"));

		DepartureProcedureReadResult result = DepartureBuilder.ReadProcedures(data);

		DepartureProcedure procedure = Assert.Single(result.Procedures);
		Assert.Equal(new[] { "AAA", "BBB" }, procedure.ServedAirports);
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

		foreach ((string Id, double Lat, double Lon) fix in DepartureTestData.DotssFixes)
		{
			DeparturePoint point = Assert.Single(airportProcedure.Points, p => p.Id == fix.Id);
			Assert.Equal(fix.Lat, point.Latitude);
			Assert.Equal(fix.Lon, point.Longitude);
		}
	}

	[Fact]
	public void each_airport_gets_only_its_dp_apt_bodies_plus_every_transition()
	{
		NasrCsvDataCollection data = TwoBodyProcedure("AAA", ("B1", "AAA"), ("B2", "BBB"));

		DepartureLocateResult result = ReadAndLocate(data);

		Assert.Equal(2, result.AirportProcedures.Count);

		DepartureAirportProcedure aaa = Assert.Single(result.AirportProcedures, p => p.AirportId == "AAA");
		Assert.Equal(new[] { "B1", "DELTA TRANSITION" }, aaa.Routes.Select(r => r.Name));

		DepartureAirportProcedure bbb = Assert.Single(result.AirportProcedures, p => p.AirportId == "BBB");
		Assert.Equal(new[] { "B2", "DELTA TRANSITION" }, bbb.Routes.Select(r => r.Name));
	}

	[Fact]
	public void an_airport_only_in_served_arpt_gets_every_body()
	{
		NasrCsvDataCollection data = TwoBodyProcedure("CCC", ("B1", "AAA"));

		DepartureLocateResult result = ReadAndLocate(data);

		DepartureAirportProcedure ccc = Assert.Single(result.AirportProcedures, p => p.AirportId == "CCC");
		Assert.Equal(new[] { "B1", "B2", "DELTA TRANSITION" }, ccc.Routes.Select(r => r.Name));

		DepartureAirportProcedure aaa = Assert.Single(result.AirportProcedures, p => p.AirportId == "AAA");
		Assert.Equal(new[] { "B1", "DELTA TRANSITION" }, aaa.Routes.Select(r => r.Name));
	}

	[Fact]
	public void a_point_missing_from_fix_base_skips_the_pair_with_a_warning_naming_it()
	{
		NasrCsvDataCollection data = DepartureTestData.Build(
			bases: new[] { DepartureTestData.Base(TestName, TestArtcc, TestCode, servedArpt: "AAA") },
			routes: DepartureTestData.Body(TestName, TestArtcc, TestCode, "B1", new[] { "ALPHA", "MISNG" }),
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
			bases: new[] { DepartureTestData.Base(TestName, TestArtcc, TestCode, servedArpt: "AAA") });

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
			bases: new[] { DepartureTestData.Base(TestName, TestArtcc, TestCode, servedArpt: "AAA") },
			routes: DepartureTestData.Body(TestName, TestArtcc, TestCode, "ALPHA", new[] { "ALPHA" }),
			fixes: DepartureTestData.SyntheticFixes("ALPHA"));

		DepartureLocateResult result = ReadAndLocate(data);

		DepartureAirportProcedure airportProcedure = Assert.Single(result.AirportProcedures);
		Assert.False(airportProcedure.HasDrawableRoute);
		Assert.Equal(new[] { "ALPHA" }, airportProcedure.Points.Select(p => p.Id));
	}
}
