using FeBuddy.Core.Application.Airac.Airways;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Domain.Airways.Models;

using NetTopologySuite.Geometries;

namespace FeBuddy.UnitTests.Application.Airac.Airways;

/// <summary>
/// Verifies <see cref="AirwayWaypointBuffer"/> buffers real waypoints only - including a real
/// fix expressed on the opposite side of the antimeridian - and never buffers a synthetic
/// antimeridian-split or ROI-clip vertex (remediation plan 3.10).
/// </summary>
public sealed class AirwayWaypointBufferTests
{
	private static readonly GeometryFactory Factory = AirwayGeometryBuilder.GeometryFactory;

	private static AirwayPoint Point(string id, double lat, double lon) => new(id, "WP", lat, lon, "fix");

	private static LineString Leg((double Lon, double Lat) a, (double Lon, double Lat) b) =>
		Factory.CreateLineString(new[] { new Coordinate(a.Lon, a.Lat), new Coordinate(b.Lon, b.Lat) });

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
			new[] { leg }, new[] { start, resee }, Factory, "TEST");

		LineString buffered = Assert.Single(result.LineStrings);
		Coordinate end = EndOf(buffered);

		// The endpoint moved off +/-180: it was pulled back by the fix buffer.
		Assert.NotEqual(180.0, Math.Abs(end.X), precision: 3);
	}

	[Fact]
	public void a_synthetic_vertex_with_no_matching_waypoint_is_not_buffered()
	{
		// Only real waypoints are supplied; the leg ends at a vertex the AM split / ROI clip
		// invented (no matching waypoint).
		AirwayPoint a = Point("AAAAA", 20.0, 175.0);
		var synthetic = (Lon: 178.5, Lat: 20.4);

		LineString leg = Leg((175.0, 20.0), synthetic);

		AirwayBufferResult result = AirwayWaypointBuffer.Buffer(
			new[] { leg }, new[] { a }, Factory, "TEST");

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
			new[] { leg }, new[] { a }, Factory, "TEST");

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
			new[] { leg }, new[] { a, b }, Factory, "TEST");

		Coordinate[] buffered = Assert.Single(result.LineStrings).Coordinates;

		Assert.NotEqual(-80.0, buffered[0].X, precision: 6);
		Assert.NotEqual(-81.0, buffered[^1].X, precision: 6);
	}
}
