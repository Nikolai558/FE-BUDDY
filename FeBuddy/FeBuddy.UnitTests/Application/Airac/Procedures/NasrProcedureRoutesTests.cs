using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

using FeBuddy.UnitTests.Application.Airac.Procedures.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Procedures;

/// <summary>
/// Covers <see cref="NasrProcedureRoutes"/>: NASR's departures and arrivals as routes - each departure
/// by its code and name together, each arrival by its code; the airports from <c>SERVED_ARPT</c> and
/// the <c>_APT</c> rows; <c>NOT ASSIGNED</c> as no code; the points trimmed, in order.
/// </summary>
public sealed class NasrProcedureRoutesTests
{
	[Fact]
	public void a_departure_is_a_dp_or_odp_route_with_its_code_airports_and_points()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.RouteNasr(departures:
		[
			ProcedureTestData.Departure("CLVIN3.CLVIN ", "CLVIN", "DTW, YIP", ("CLVIN", "WP   ", "K5 "), ("DXO", "VOR/DME", null)),
		]);
		nasr.Dp!.DpApt.Add(new DpCsvDataModel.DpApt { DpComputerCode = "CLVIN3.CLVIN", DpName = "CLVIN", ArptId = "ARB" });

		ProcedureRoute route = Assert.Single(NasrProcedureRoutes.Read(nasr));

		Assert.Equal(["DP", "ODP"], route.ChartCodes);
		Assert.Equal("CLVIN3.CLVIN", route.ComputerCode);
		Assert.Equal("CLVIN", route.Name);
		Assert.Equal(["ARB", "DTW", "YIP"], route.Airports.Order());
		Assert.Equal([new ProcedureRoutePoint("CLVIN", "WP", "K5"), new ProcedureRoutePoint("DXO", "VOR/DME", null)], route.Points);
	}

	/// <summary>Obstacle departures with no code all share NOT ASSIGNED, so a departure is known by its name too.</summary>
	[Fact]
	public void departures_that_share_not_assigned_keep_their_own_routes_and_have_no_code()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.RouteNasr(departures:
		[
			ProcedureTestData.Departure("NOT ASSIGNED", "O'HARE", "ORD", ("ORD", "VORTAC", null)),
			ProcedureTestData.Departure("NOT ASSIGNED", "MIDWAY", "MDW", ("CGT", "VOR/DME", null)),
		]);

		IReadOnlyList<ProcedureRoute> routes = NasrProcedureRoutes.Read(nasr);

		Assert.All(routes, route => Assert.Null(route.ComputerCode));
		Assert.Equal(["ORD"], routes[0].Points.Select(point => point.Id));
		Assert.Equal(["CGT"], routes[1].Points.Select(point => point.Id));
	}

	[Fact]
	public void an_arrival_is_a_star_route_known_by_its_code()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.RouteNasr(arrivals:
		[
			ProcedureTestData.Arrival("FERRL.GRAYT2", "GRAYT TWO (RNAV)", "DTW", ("FERRL", "WP", "K5"), ("GRAYT", "WP", "K5")),
		]);
		nasr.Star!.StarApt.Add(new StarCsvDataModel.StarApt { StarComputerCode = "FERRL.GRAYT2", ArptId = "YIP" });

		ProcedureRoute route = Assert.Single(NasrProcedureRoutes.Read(nasr));

		Assert.Equal(["STR"], route.ChartCodes);
		Assert.Equal("FERRL.GRAYT2", route.ComputerCode);
		Assert.Equal("GRAYT TWO (RNAV)", route.Name);
		Assert.Equal(["DTW", "YIP"], route.Airports.Order());
		Assert.Equal(["FERRL", "GRAYT"], route.Points.Select(point => point.Id));
	}

	[Fact]
	public void a_blank_code_is_no_code()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.RouteNasr(arrivals: [ProcedureTestData.Arrival("  ", "NAMELESS", "AAA")]);

		Assert.Null(Assert.Single(NasrProcedureRoutes.Read(nasr)).ComputerCode);
	}

	[Fact]
	public void missing_fields_read_as_empty()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.RouteNasr(arrivals: [ProcedureTestData.Arrival("A.B1", null!, null!, (null!, null!, null))]);
		nasr.Star!.StarApt.Add(new StarCsvDataModel.StarApt { StarComputerCode = "A.B1", ArptId = null! });

		ProcedureRoute route = Assert.Single(NasrProcedureRoutes.Read(nasr));

		Assert.Equal(string.Empty, route.Name);
		Assert.Empty(route.Airports);
		Assert.Equal([new ProcedureRoutePoint(string.Empty, string.Empty, null)], route.Points);
	}

	[Fact]
	public void read_needs_the_dp_and_star_groups()
	{
		Assert.Throws<ArgumentNullException>(() => NasrProcedureRoutes.Read(null!));
		Assert.Throws<InvalidOperationException>(() => NasrProcedureRoutes.Read(new NasrCsvDataCollection { Star = new StarCsvDataCollection() }));
		Assert.Throws<InvalidOperationException>(() => NasrProcedureRoutes.Read(new NasrCsvDataCollection { Dp = new DpCsvDataCollection() }));
	}
}
