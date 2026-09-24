using FeBuddy.Core.Application.Airac.Departures;
using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Domain.Departures.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using NetTopologySuite.Geometries;

using FeBuddy.UnitTests.Application.Airac.Departures.Fixtures;

namespace FeBuddy.UnitTests.Domain.Geo;

/// <summary>
/// Covers <see cref="LineStringMerger"/> directly and through
/// <see cref="DepartureGeometryBuilder"/> on the real DOTSS2 procedure.
/// </summary>
public class LineStringMergerTests
{
	private static readonly GeometryFactory Factory = new();

	private static readonly Dictionary<string, Coordinate> HandlerCoordinates = new()
	{
		["A"] = new Coordinate(-100.0, 40.0),
		["B"] = new Coordinate(-101.0, 41.0),
		["C"] = new Coordinate(-102.0, 42.0),
		["D"] = new Coordinate(-103.0, 43.0),
	};

	private static IReadOnlyList<(string Key, Coordinate Coordinate)> Path(params string[] keys) =>
		keys.Select(key => (key, HandlerCoordinates[key])).ToList();

	private static (string, string) Segment(string a, string b) =>
		string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);

	private static MultiLineString BuildDotss()
	{
		NasrCsvDataCollection data = DepartureTestData.Dotss();
		DepartureProcedureReadResult read = DepartureBuilder.ReadProcedures(data);
		DepartureLocateResult located = DepartureBuilder.Locate(read.Procedures, data);
		DepartureAirportProcedure airportProcedure = Assert.Single(located.AirportProcedures);

		MultiLineString? geometry = DepartureGeometryBuilder.Build(airportProcedure);

		Assert.NotNull(geometry);
		return geometry!;
	}

	[Fact]
	public void dotss2_at_lax_is_five_linestrings_with_the_longest_run_as_the_base()
	{
		MultiLineString geometry = BuildDotss();

		Assert.Equal(5, geometry.NumGeometries);
		Assert.Equal(11, ((LineString)geometry.GetGeometryN(0)).NumPoints);
	}

	[Fact]
	public void dotss2_at_lax_draws_every_route_segment_exactly_once()
	{
		Dictionary<(double Lon, double Lat), string> idsByCoordinate = DepartureTestData.DotssFixes
			.ToDictionary(fix => (fix.Lon, fix.Lat), fix => fix.Id);

		MultiLineString geometry = BuildDotss();

		List<(string, string)> drawn = new();

		for (int i = 0; i < geometry.NumGeometries; i++)
		{
			Coordinate[] coordinates = geometry.GetGeometryN(i).Coordinates;

			for (int j = 1; j < coordinates.Length; j++)
			{
				string from = idsByCoordinate[(coordinates[j - 1].X, coordinates[j - 1].Y)];
				string to = idsByCoordinate[(coordinates[j].X, coordinates[j].Y)];
				drawn.Add(Segment(from, to));
			}
		}

		IEnumerable<string[]> routes = DepartureTestData.DotssBodies.Select(b => b.Points)
			.Concat(DepartureTestData.DotssTransitions.Select(t => t.Points));

		HashSet<(string, string)> expected = new();

		foreach (string[] route in routes)
		{
			for (int j = 1; j < route.Length; j++)
			{
				expected.Add(Segment(route[j - 1], route[j]));
			}
		}

		Assert.Equal(drawn.Count, drawn.Distinct().Count());
		Assert.True(expected.SetEquals(drawn), "The drawn segments differ from the segments of the six routes.");
	}

	[Fact]
	public void two_identical_paths_make_one_line()
	{
		IReadOnlyList<LineString> lines = LineStringMerger.Merge(
			new[] { Path("A", "B", "C"), Path("A", "B", "C") });

		LineString line = Assert.Single(lines);
		Assert.Equal(3, line.NumPoints);
	}

	[Fact]
	public void a_path_and_its_reverse_make_one_line()
	{
		IReadOnlyList<LineString> lines = LineStringMerger.Merge(
			new[] { Path("A", "B", "C"), Path("C", "B", "A") });

		LineString line = Assert.Single(lines);
		Assert.Equal(3, line.NumPoints);
	}

	[Fact]
	public void repeated_consecutive_points_draw_a_single_segment()
	{
		IReadOnlyList<LineString> lines = LineStringMerger.Merge(
			new[] { Path("A", "A", "B") });

		LineString line = Assert.Single(lines);
		Assert.Equal(2, line.NumPoints);
		Assert.Equal(HandlerCoordinates["A"], line.GetCoordinateN(0));
		Assert.Equal(HandlerCoordinates["B"], line.GetCoordinateN(1));
	}

	[Fact]
	public void no_path_with_two_distinct_points_gives_no_lines()
	{
		IReadOnlyList<LineString> lines = LineStringMerger.Merge(
			new[] { Path("A"), Path("B", "B") });

		Assert.Empty(lines);
	}

	[Fact]
	public void a_branch_starts_on_the_point_where_it_leaves_the_base()
	{
		IReadOnlyList<LineString> lines = LineStringMerger.Merge(
			new[] { Path("A", "B", "C"), Path("D", "B") });

		Assert.Equal(2, lines.Count);
		Assert.Equal(3, lines[0].NumPoints);
		Assert.Equal(new[] { HandlerCoordinates["D"], HandlerCoordinates["B"] }, lines[1].Coordinates);
	}
}
