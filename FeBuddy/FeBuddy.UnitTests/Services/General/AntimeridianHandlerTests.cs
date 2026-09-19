using FeBuddy.Core.Services.Airac.Airways;
using FeBuddy.Core.Services.General;

using NetTopologySuite.Geometries;

namespace FeBuddy.UnitTests.Services.General;

/// <summary>
/// Verifies <see cref="AntimeridianHandler.Split"/> never emits a degenerate LineString
/// (fewer than two distinct coordinates), including when a segment endpoint lies exactly on
/// +/-180 or a coordinate is repeated (remediation plan 3.9).
/// </summary>
public sealed class AntimeridianHandlerTests
{
	private static readonly GeometryFactory Factory = AirwayGeometryBuilder.GeometryFactory;

	private static LineString Line(params (double Lon, double Lat)[] points) =>
		Factory.CreateLineString(points.Select(p => new Coordinate(p.Lon, p.Lat)).ToArray());

	private static bool HasTwoDistinctCoordinates(LineString line)
	{
		Coordinate first = line.Coordinates[0];
		return line.Coordinates.Skip(1).Any(c => c.X != first.X || c.Y != first.Y);
	}

	[Fact]
	public void a_normal_crossing_splits_into_two_valid_linestrings()
	{
		IReadOnlyList<LineString> result = AntimeridianHandler.Split(Line((170, 10), (-170, 12)), Factory);

		Assert.Equal(2, result.Count);
		Assert.All(result, ls => Assert.True(HasTwoDistinctCoordinates(ls)));
	}

	[Theory]
	[InlineData(-180.0)]
	[InlineData(180.0)]
	public void a_segment_ending_exactly_on_the_antimeridian_produces_no_degenerate_linestring(double endLon)
	{
		// The endpoint sits exactly on +/-180 - the crossing point the split computes is
		// identical to it, which used to create a two-point LineString of the same coordinate.
		IReadOnlyList<LineString> result = AntimeridianHandler.Split(Line((175, 20), (endLon, 20.6075)), Factory);

		Assert.NotEmpty(result);
		Assert.All(result, ls =>
		{
			Assert.True(ls.Coordinates.Length >= 2);
			Assert.True(HasTwoDistinctCoordinates(ls), "emitted a zero-length LineString");
			Assert.True(ls.Length > 0);
		});
	}

	[Fact]
	public void repeated_consecutive_coordinates_do_not_create_a_zero_length_linestring()
	{
		IReadOnlyList<LineString> result = AntimeridianHandler.Split(Line((10, 10), (10, 10), (11, 11)), Factory);

		Assert.All(result, ls => Assert.True(ls.Length > 0));
	}

	[Fact]
	public void a_line_that_never_crosses_is_returned_unchanged()
	{
		LineString input = Line((10, 10), (20, 20));

		IReadOnlyList<LineString> result = AntimeridianHandler.Split(input, Factory);

		Assert.Same(input, Assert.Single(result));
	}
}
