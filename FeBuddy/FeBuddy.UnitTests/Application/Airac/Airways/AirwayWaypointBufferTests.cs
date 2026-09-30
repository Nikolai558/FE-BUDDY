using FeBuddy.Core.Application.Airac.Airways;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Domain.Airways.Models;
using FeBuddy.Core.Domain.Geo;

using NetTopologySuite.Geometries;

using Location = FeBuddy.Core.Domain.Geo.Models.Location;

namespace FeBuddy.UnitTests.Application.Airac.Airways;

/// <summary>
/// Verifies <see cref="AirwayWaypointBuffer"/> buffers real waypoints only - including a real
/// fix expressed on the opposite side of the antimeridian - and never buffers a synthetic
/// antimeridian-split or ROI-clip vertex - and uses the distances it is given for fixes and NAVAIDs.
/// </summary>
public sealed class AirwayWaypointBufferTests
{
	private static readonly GeometryFactory Factory = Wgs84.Factory;

	private static AirwayPoint Point(string id, double lat, double lon) => new(id, "WP", lat, lon, "fix");

	private static LineString Leg((double Lon, double Lat) a, (double Lon, double Lat) b) =>
		Factory.CreateLineString([new Coordinate(a.Lon, a.Lat), new Coordinate(b.Lon, b.Lat)]);

	private static Coordinate EndOf(LineString line) => line.Coordinates[^1];

	[Fact]
	public void a_real_pm180_fix_expressed_as_plus_180_is_still_buffered()
	{
		// RESEE is a real 5-character fix stored at -180; the westbound fragment reaches it
		// expressed as +180. It must still be buffered (2.5 NM), not treated as synthetic.
		AirwayPoint resee = Point("RESEE", 20.60750, -180.0);
		AirwayPoint start = Point("STRTA", 20.0, 150.0);

		LineString leg = Leg((150.0, 20.0), (180.0, 20.60750));

		AirwayBufferResult result = AirwayWaypointBuffer.Buffer(
			[leg], [start, resee], "TEST");

		LineString buffered = Assert.Single(result.LineStrings);
		Coordinate end = EndOf(buffered);

		// The endpoint moved off +/-180: it was pulled back by the fix buffer.
		Assert.NotEqual(180.0, Math.Abs(end.X), precision: 3);
	}

	[Fact]
	public void a_synthetic_vertex_with_no_matching_waypoint_is_not_buffered()
	{
		// Only real waypoints are supplied; the leg ends at a vertex the antimeridian split / ROI clip
		// invented (no matching waypoint).
		AirwayPoint a = Point("AAAAA", 20.0, 175.0);
		var synthetic = (Lon: 178.5, Lat: 20.4);

		LineString leg = Leg((175.0, 20.0), synthetic);

		AirwayBufferResult result = AirwayWaypointBuffer.Buffer(
			[leg], [a], "TEST");

		Coordinate end = EndOf(Assert.Single(result.LineStrings));

		// Unchanged - a synthetic vertex gets radius 0.
		Assert.Equal(synthetic.Lon, end.X, precision: 6);
		Assert.Equal(synthetic.Lat, end.Y, precision: 6);
	}

	[Fact]
	public void an_roi_clip_boundary_vertex_is_not_buffered()
	{
		AirwayPoint a = Point("AAAAA", 40.0, -80.0);
		var roiBoundary = (Lon: -78.0, Lat: 41.234); // a vertex NTS manufactured at the ROI edge

		LineString leg = Leg((-80.0, 40.0), roiBoundary);

		AirwayBufferResult result = AirwayWaypointBuffer.Buffer(
			[leg], [a], "TEST");

		Coordinate end = EndOf(Assert.Single(result.LineStrings));

		Assert.Equal(roiBoundary.Lon, end.X, precision: 9);
		Assert.Equal(roiBoundary.Lat, end.Y, precision: 9);
	}

	[Fact]
	public void a_leg_between_two_ordinary_waypoints_is_still_buffered_at_both_ends()
	{
		AirwayPoint a = Point("AAAAA", 40.0, -80.0);
		AirwayPoint b = Point("BBBBB", 41.0, -81.0);

		LineString leg = Leg((-80.0, 40.0), (-81.0, 41.0));

		AirwayBufferResult result = AirwayWaypointBuffer.Buffer(
			[leg], [a, b], "TEST");

		Coordinate[] buffered = Assert.Single(result.LineStrings).Coordinates;

		Assert.NotEqual(-80.0, buffered[0].X, precision: 6);
		Assert.NotEqual(-81.0, buffered[^1].X, precision: 6);
	}

	[Fact]
	public void the_chosen_distances_are_used_for_fixes_and_for_navaids()
	{
		// A 60 NM leg due north from a fix to a NAVAID.
		AirwayPoint fix = Point("AAAAA", 40.0, -80.0);
		AirwayPoint navaid = Point("ABC", 41.0, -80.0);

		AirwayBufferResult result = AirwayWaypointBuffer.Buffer(
			[Leg((-80.0, 40.0), (-80.0, 41.0))], [fix, navaid], "TEST", fixRadiusNm: 1.5, navaidRadiusNm: 7.25);

		Coordinate[] buffered = Assert.Single(result.LineStrings).Coordinates;

		Assert.Equal(1.5, DistanceNm(buffered[0], fix), precision: 3);
		Assert.Equal(7.25, DistanceNm(buffered[^1], navaid), precision: 3);
	}

	[Fact]
	public void a_distance_of_zero_leaves_that_end_where_it_was()
	{
		AirwayPoint fix = Point("AAAAA", 40.0, -80.0);
		AirwayPoint navaid = Point("ABC", 41.0, -80.0);

		AirwayBufferResult result = AirwayWaypointBuffer.Buffer(
			[Leg((-80.0, 40.0), (-80.0, 41.0))], [fix, navaid], "TEST", fixRadiusNm: 0, navaidRadiusNm: 5);

		Coordinate[] buffered = Assert.Single(result.LineStrings).Coordinates;

		Assert.Equal(0.0, DistanceNm(buffered[0], fix), precision: 6);
		Assert.Equal(5.0, DistanceNm(buffered[^1], navaid), precision: 3);
	}

	[Fact]
	public void a_leg_is_dropped_only_when_shorter_than_its_chosen_distances_together()
	{
		// Two fixes 8 NM apart.
		AirwayPoint a = Point("AAAAA", 40.0, -80.0);
		AirwayPoint b = Point("BBBBB", 40.0 + (8.0 / 60.0), -80.0);
		LineString leg = Leg((a.Longitude, a.Latitude), (b.Longitude, b.Latitude));

		// 2.5 + 2.5 = 5 NM: kept. 4.5 + 4.5 = 9 NM: dropped, with an Info message.
		Assert.Single(AirwayWaypointBuffer.Buffer([leg], [a, b], "TEST").LineStrings);

		AirwayBufferResult dropped = AirwayWaypointBuffer.Buffer([leg], [a, b], "TEST", fixRadiusNm: 4.5, navaidRadiusNm: 4.5);

		Assert.Empty(dropped.LineStrings);
		Assert.Contains("9.0 NM", Assert.Single(dropped.Messages).Text);
	}

	[Theory]
	[InlineData(-0.1, 5.0)]
	[InlineData(10.01, 5.0)]
	[InlineData(double.NaN, 5.0)]
	[InlineData(2.5, -1.0)]
	[InlineData(2.5, double.PositiveInfinity)]
	public void a_distance_outside_zero_to_ten_nm_is_rejected(double fixRadiusNm, double navaidRadiusNm)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			AirwayWaypointBuffer.Buffer([], [], "TEST", fixRadiusNm, navaidRadiusNm));
	}

	private static double DistanceNm(Coordinate coordinate, AirwayPoint point) =>
		GeoMath.Distance(new Location(coordinate.Y, coordinate.X), new Location(point.Latitude, point.Longitude), round: false);
}
