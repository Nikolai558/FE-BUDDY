using FeBuddy.Core.Domain.Geo;

using NetTopologySuite.Geometries;

using Location = FeBuddy.Core.Domain.Geo.Models.Location;

namespace FeBuddy.UnitTests.Domain.Geo;

/// <summary>
/// Covers <see cref="RadiusFilter"/>: keeping what is inside a circle, dropping what is outside,
/// and cutting segments exactly where they cross it.
/// </summary>
public sealed class RadiusFilterTests
{
	// One degree of latitude is 60 NM on GeoMath's sphere to within a fraction of a percent, so a
	// 60 NM circle round (40, -75) reaches roughly from 39 to 41 north.
	private static readonly Location Center = new(40.0, -75.0);

	private const double RadiusNm = 60;

	/// <summary>How close to the circle a cut has to land, in NM (about 2 m).</summary>
	private const double Tolerance = 0.001;

	private static LineString Line(params (double Lat, double Lon)[] points) =>
		Wgs84.Factory.CreateLineString([.. points.Select(p => new Coordinate(p.Lon, p.Lat))]);

	private static double DistanceFromCenter(Coordinate coordinate) =>
		GeoMath.Distance(Center, new Location(coordinate.Y, coordinate.X), round: false);

	[Fact]
	public void a_line_wholly_inside_is_kept_exactly()
	{
		LineString line = Line((40.1, -75.1), (40.2, -74.9), (39.9, -74.8));

		IReadOnlyList<LineString> clipped = RadiusFilter.ClipLines([line], Center, RadiusNm);

		LineString only = Assert.Single(clipped);
		Assert.Equal(line.Coordinates, only.Coordinates);
	}

	[Fact]
	public void a_line_wholly_outside_is_dropped()
	{
		LineString line = Line((42.0, -75.0), (42.5, -74.0));

		Assert.Empty(RadiusFilter.ClipLines([line], Center, RadiusNm));
	}

	[Fact]
	public void a_segment_leaving_the_circle_is_cut_where_it_crosses()
	{
		LineString line = Line((40.0, -75.0), (42.0, -75.0));

		LineString only = Assert.Single(RadiusFilter.ClipLines([line], Center, RadiusNm));

		Assert.Equal(2, only.NumPoints);
		Assert.Equal(new Coordinate(-75.0, 40.0), only.Coordinates[0]);
		Assert.Equal(RadiusNm, DistanceFromCenter(only.Coordinates[1]), Tolerance);
		Assert.Equal(-75.0, only.Coordinates[1].X, 6);
	}

	[Fact]
	public void a_segment_entering_the_circle_starts_where_it_crosses()
	{
		LineString line = Line((38.0, -75.0), (40.0, -75.0));

		LineString only = Assert.Single(RadiusFilter.ClipLines([line], Center, RadiusNm));

		Assert.Equal(RadiusNm, DistanceFromCenter(only.Coordinates[0]), Tolerance);
		Assert.Equal(new Coordinate(-75.0, 40.0), only.Coordinates[1]);
	}

	[Fact]
	public void a_segment_passing_through_with_both_ends_outside_keeps_its_middle()
	{
		LineString line = Line((40.0, -77.0), (40.0, -73.0));

		LineString only = Assert.Single(RadiusFilter.ClipLines([line], Center, RadiusNm));

		Assert.Equal(2, only.NumPoints);
		Assert.Equal(RadiusNm, DistanceFromCenter(only.Coordinates[0]), Tolerance);
		Assert.Equal(RadiusNm, DistanceFromCenter(only.Coordinates[1]), Tolerance);
		Assert.True(only.Coordinates[0].X < -75 && only.Coordinates[1].X > -75);
	}

	[Fact]
	public void a_segment_passing_close_by_but_outside_is_dropped()
	{
		// Along 41.5 N the nearest point is 90 NM from the centre.
		LineString line = Line((41.5, -77.0), (41.5, -73.0));

		Assert.Empty(RadiusFilter.ClipLines([line], Center, RadiusNm));
	}

	[Fact]
	public void a_segment_heading_for_the_circle_but_stopping_short_is_dropped()
	{
		// On the centre's own meridian, so the great circle it lies on runs straight through the circle.
		LineString line = Line((43.0, -75.0), (42.0, -75.0));

		Assert.Empty(RadiusFilter.ClipLines([line], Center, RadiusNm));
	}

	[Fact]
	public void a_line_that_leaves_and_comes_back_becomes_one_piece_per_visit()
	{
		LineString line = Line((40.0, -75.0), (42.0, -75.0), (42.0, -74.5), (40.0, -74.5));

		IReadOnlyList<LineString> clipped = RadiusFilter.ClipLines([line], Center, RadiusNm);

		Assert.Equal(2, clipped.Count);
		Assert.Equal(new Coordinate(-75.0, 40.0), clipped[0].Coordinates[0]);
		Assert.Equal(RadiusNm, DistanceFromCenter(clipped[0].Coordinates[^1]), Tolerance);
		Assert.Equal(RadiusNm, DistanceFromCenter(clipped[1].Coordinates[0]), Tolerance);
		Assert.Equal(new Coordinate(-74.5, 40.0), clipped[1].Coordinates[^1]);
	}

	[Fact]
	public void consecutive_inside_segments_stay_one_line()
	{
		LineString line = Line((40.0, -75.0), (40.3, -75.0), (40.3, -74.7), (42.0, -74.7));

		LineString only = Assert.Single(RadiusFilter.ClipLines([line], Center, RadiusNm));

		Assert.Equal(4, only.NumPoints);
		Assert.Equal(RadiusNm, DistanceFromCenter(only.Coordinates[3]), Tolerance);
	}

	[Fact]
	public void zero_length_segments_draw_nothing_and_do_not_split_the_line()
	{
		LineString repeated = Line((40.1, -75.0), (40.1, -75.0));
		LineString withRepeat = Line((40.0, -75.0), (40.1, -75.0), (40.1, -75.0), (40.2, -75.0));

		Assert.Empty(RadiusFilter.ClipLines([repeated], Center, RadiusNm));

		LineString only = Assert.Single(RadiusFilter.ClipLines([withRepeat], Center, RadiusNm));
		Assert.Equal(3, only.NumPoints);
	}

	[Fact]
	public void every_line_is_clipped_in_order()
	{
		LineString inside = Line((40.0, -75.0), (40.1, -75.0));
		LineString outside = Line((45.0, -75.0), (45.1, -75.0));
		LineString alsoInside = Line((39.9, -75.0), (39.8, -75.0));

		IReadOnlyList<LineString> clipped = RadiusFilter.ClipLines([inside, outside, alsoInside], Center, RadiusNm);

		Assert.Equal([inside.Coordinates, alsoInside.Coordinates], clipped.Select(l => l.Coordinates));
	}

	[Fact]
	public void works_across_the_antimeridian()
	{
		Location guam = new(13.5, 179.9);
		LineString line = Line((13.5, 179.5), (13.5, -179.5));

		LineString only = Assert.Single(RadiusFilter.ClipLines([line], guam, 6));

		Assert.Equal(6, GeoMath.Distance(guam, new Location(only.Coordinates[0].Y, only.Coordinates[0].X), round: false), Tolerance);
		Assert.Equal(6, GeoMath.Distance(guam, new Location(only.Coordinates[1].Y, only.Coordinates[1].X), round: false), Tolerance);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void a_radius_that_is_not_positive_is_rejected(double radius)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => RadiusFilter.ClipLines([], Center, radius));
	}

	[Fact]
	public void null_arguments_are_rejected()
	{
		Assert.Throws<ArgumentNullException>(() => RadiusFilter.ClipLines(null!, Center, RadiusNm));
		Assert.Throws<ArgumentNullException>(() => RadiusFilter.ClipLines([], null!, RadiusNm));
	}
}
