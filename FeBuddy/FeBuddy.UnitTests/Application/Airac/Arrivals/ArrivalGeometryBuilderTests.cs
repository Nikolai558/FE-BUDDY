using FeBuddy.Core.Application.Airac.Arrivals;
using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Domain.Arrivals.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using NetTopologySuite.Geometries;

using FeBuddy.UnitTests.Application.Airac.Arrivals.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Arrivals;

/// <summary>
/// Covers <see cref="ArrivalGeometryBuilder"/>: joining a transition in front of the body it
/// reaches, and the paths a transition or body that reaches nothing else gets on its own.
/// </summary>
public sealed class ArrivalGeometryBuilderTests
{
	private static ArrivalLocateResult ReadAndLocate(NasrCsvDataCollection data)
	{
		ArrivalProcedureReadResult read = ArrivalBuilder.ReadProcedures(data);
		return ArrivalBuilder.Locate(read.Procedures, data);
	}

	[Fact]
	public void a_transition_ending_at_a_bodys_first_point_is_joined_in_front_of_it()
	{
		ArrivalPoint bce = new("BCE", "VORTAC", 37.69, -112.14);
		ArrivalPoint holdm = new("HOLDM", "RP", 36.68, -115.52);
		ArrivalPoint aalan = new("AALAN", "RP", 36.42, -115.35);
		ArrivalPoint blaid = new("BLAID", "RP", 36.14, -115.09);

		ArrivalRoute transition = new("BRYCE CANYON TRANSITION", ArrivalRouteKind.Transition, [bce, holdm, aalan]);
		ArrivalRoute body = new("AALAN-BLAID", ArrivalRouteKind.Body, [aalan, blaid]);

		List<IReadOnlyList<(string Key, Coordinate Coordinate)>> paths = ArrivalGeometryBuilder.Paths([transition, body]);

		IReadOnlyList<(string Key, Coordinate Coordinate)> path = Assert.Single(paths);
		Assert.Equal(["BCE", "HOLDM", "AALAN", "BLAID"], path.Select(p => p.Key));
	}

	[Fact]
	public void the_real_blaid2_procedure_joins_each_transition_in_front_of_the_body()
	{
		ArrivalLocateResult result = ReadAndLocate(ArrivalTestData.Blaid());
		ArrivalAirportProcedure airportProcedure = Assert.Single(result.AirportProcedures);

		List<IReadOnlyList<(string Key, Coordinate Coordinate)>> paths = ArrivalGeometryBuilder.Paths(airportProcedure.Routes);

		Assert.Equal(3, paths.Count);
		Assert.Contains(paths, p => p.Select(x => x.Key).SequenceEqual(["BCE", "HOLDM", "AALAN", "BLAID"]));
		Assert.Contains(paths, p => p.Select(x => x.Key).SequenceEqual(["EHK", "HOLDM", "AALAN", "BLAID"]));
		Assert.Contains(paths, p => p.Select(x => x.Key).SequenceEqual(["PGA", "HOLDM", "AALAN", "BLAID"]));
	}

	[Fact]
	public void one_transition_reaching_two_bodies_gives_two_paths()
	{
		ArrivalPoint start = new("START", "RP", 40.0, -100.0);
		ArrivalPoint shared = new("SHARD", "RP", 40.1, -100.1);
		ArrivalPoint bodyOneEnd = new("ENDAA", "RP", 40.2, -100.2);
		ArrivalPoint bodyTwoEnd = new("ENDBB", "RP", 40.3, -100.3);

		ArrivalRoute transition = new("TWO BODY TRANSITION", ArrivalRouteKind.Transition, [start, shared]);
		ArrivalRoute bodyA = new("BODY A", ArrivalRouteKind.Body, [shared, bodyOneEnd]);
		ArrivalRoute bodyB = new("BODY B", ArrivalRouteKind.Body, [shared, bodyTwoEnd]);

		List<IReadOnlyList<(string Key, Coordinate Coordinate)>> paths = ArrivalGeometryBuilder.Paths([transition, bodyA, bodyB]);

		Assert.Equal(2, paths.Count);
		Assert.Contains(paths, p => p.Select(x => x.Key).SequenceEqual(["START", "SHARD", "ENDAA"]));
		Assert.Contains(paths, p => p.Select(x => x.Key).SequenceEqual(["START", "SHARD", "ENDBB"]));
	}

	[Fact]
	public void a_body_no_transition_reaches_is_its_own_path()
	{
		ArrivalPoint a = new("AAAAA", "RP", 40.0, -100.0);
		ArrivalPoint b = new("BBBBB", "RP", 40.1, -100.1);
		ArrivalRoute body = new("LONE BODY", ArrivalRouteKind.Body, [a, b]);

		List<IReadOnlyList<(string Key, Coordinate Coordinate)>> paths = ArrivalGeometryBuilder.Paths([body]);

		IReadOnlyList<(string Key, Coordinate Coordinate)> path = Assert.Single(paths);
		Assert.Equal(["AAAAA", "BBBBB"], path.Select(p => p.Key));
	}

	[Fact]
	public void a_transition_reaching_no_body_is_its_own_path()
	{
		// Real example: MAKAH1's LAVAS transition ends at HONUU, but the body starts at MAKAH.
		ArrivalPoint lavas = new("LAVAS", "RP", 48.0, -124.0);
		ArrivalPoint honuu = new("HONUU", "RP", 48.1, -124.1);
		ArrivalPoint makah = new("MAKAH", "RP", 48.2, -124.2);
		ArrivalPoint end = new("ENDXX", "RP", 48.3, -124.3);

		ArrivalRoute transition = new("LAVAS TRANSITION", ArrivalRouteKind.Transition, [lavas, honuu]);
		ArrivalRoute body = new("MAKAH BODY", ArrivalRouteKind.Body, [makah, end]);

		List<IReadOnlyList<(string Key, Coordinate Coordinate)>> paths = ArrivalGeometryBuilder.Paths([transition, body]);

		Assert.Equal(2, paths.Count);
		Assert.Contains(paths, p => p.Select(x => x.Key).SequenceEqual(["LAVAS", "HONUU"]));
		Assert.Contains(paths, p => p.Select(x => x.Key).SequenceEqual(["MAKAH", "ENDXX"]));
	}

	[Fact]
	public void build_returns_null_when_nothing_is_drawable()
	{
		ArrivalAirportProcedure single = ArrivalTestData.AirportProcedure(
			ArrivalTestData.Procedure(), "AAA", new ArrivalPoint("ALPHA", "RP", 40.0, -100.0));

		Assert.Null(ArrivalGeometryBuilder.Build(single));
	}

	[Fact]
	public void build_returns_a_multilinestring_when_there_is_something_to_draw()
	{
		ArrivalPoint alpha = new("ALPHA", "RP", 42.0, -83.0);
		ArrivalPoint bravo = new("BRAVO", "RP", 42.1, -83.1);

		ArrivalAirportProcedure airportProcedure = new()
		{
			Procedure = ArrivalTestData.Procedure(),
			AirportId = "DTW",
			Artcc = "ZOB",
			Routes = [new ArrivalRoute("BODY", ArrivalRouteKind.Body, [alpha, bravo])],
			Points = [alpha, bravo],
		};

		MultiLineString? geometry = ArrivalGeometryBuilder.Build(airportProcedure);

		Assert.NotNull(geometry);
	}

	[Fact]
	public void a_transition_that_does_not_reach_any_body_is_drawn_as_its_own_line()
	{
		ArrivalPoint alpha = new("ALPHA", "RP", 42.0, -83.0);
		ArrivalPoint bravo = new("BRAVO", "RP", 42.1, -83.1);
		ArrivalPoint xray = new("XRAYY", "RP", 43.0, -84.0);
		ArrivalPoint yank = new("YANKE", "RP", 43.1, -84.1);

		ArrivalAirportProcedure airportProcedure = new()
		{
			Procedure = ArrivalTestData.Procedure(),
			AirportId = "DTW",
			Artcc = "ZOB",
			Routes =
			[
				new ArrivalRoute("XRAYY TRANSITION", ArrivalRouteKind.Transition, [xray, yank]),
				new ArrivalRoute("BODY", ArrivalRouteKind.Body, [alpha, bravo]),
			],
			Points = [xray, yank, alpha, bravo],
		};

		MultiLineString? geometry = ArrivalGeometryBuilder.Build(airportProcedure);

		Assert.Equal(2, geometry!.NumGeometries);
	}
}
