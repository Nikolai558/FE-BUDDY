using System.Globalization;

using FeBuddy.Core.Domain.Geo;

using NetTopologySuite.Geometries;

namespace FeBuddy.UnitTests.Domain.Geo;

/// <summary>
/// Covers <see cref="RoiFilter"/>: validating ROI input, point containment and clipping lines to the ROI.
/// </summary>
public sealed class RoiFilterTests
{
	[Fact]
	public void is_coordinate_valid_format_accepts_valid_coordinates()
	{
		Assert.True(RoiFilter.IsCoordinateValidFormat("38.0", "-85.0", "43.0", "-78.0", out string? error));
		Assert.Null(error);
	}

	[Theory]
	[InlineData("not-a-number", "-85.0", "43.0", "-78.0")]
	[InlineData("91.0", "-85.0", "43.0", "-78.0")] // latitude out of range
	[InlineData("38.0", "-361.0", "43.0", "-78.0")] // longitude past -360
	[InlineData("38.0", "-85.0", "43.0", "360.5")] // longitude past 360
	public void is_coordinate_valid_format_rejects_invalid_coordinates(string swLat, string swLon, string neLat, string neLon)
	{
		Assert.False(RoiFilter.IsCoordinateValidFormat(swLat, swLon, neLat, neLon, out string? error));
		Assert.NotNull(error);
	}

	/// <summary>Each corner, with each value that parses but isn't a number on the map.</summary>
	public static TheoryData<int, string> NotFinite()
	{
		TheoryData<int, string> data = new();

		for (int corner = 0; corner < 4; corner++)
		{
			foreach (string value in new[] { "NaN", "Infinity", "-Infinity" })
			{
				data.Add(corner, value);
			}
		}

		return data;
	}

	[Theory]
	[MemberData(nameof(NotFinite))]
	public void is_coordinate_valid_format_rejects_a_value_that_is_not_finite(int corner, string value)
	{
		string[] coordinates = ["38.0", "-85.0", "43.0", "-78.0"];
		string[] fields = ["Southwest Latitude", "Southwest Longitude", "Northeast Latitude", "Northeast Longitude"];
		coordinates[corner] = value;

		Assert.False(RoiFilter.IsCoordinateValidFormat(coordinates[0], coordinates[1], coordinates[2], coordinates[3], out string? error));
		Assert.Equal($"{fields[corner]} '{value}' is not a valid decimal coordinate.", error);
	}

	[Fact]
	public void is_coordinates_relative_position_valid_accepts_a_proper_box()
	{
		Assert.True(RoiFilter.IsCoordinatesRelativePositionValid(38.0, -85.0, 43.0, -78.0, out string? error));
		Assert.Null(error);
	}

	[Fact]
	public void is_coordinates_relative_position_valid_rejects_ne_lat_not_north_of_sw()
	{
		Assert.False(RoiFilter.IsCoordinatesRelativePositionValid(43.0, -85.0, 38.0, -78.0, out string? error));
		Assert.NotNull(error);
	}

	/// <summary>An ROI across the antimeridian (AM) has one edge past ±180, either way round: both forms are valid.</summary>
	[Theory]
	[InlineData("44.0", "-206.0", "90.0", "-124.0")]
	[InlineData("44.0", "154.0", "90.0", "236.0")]
	public void an_roi_across_the_antimeridian_is_valid_in_either_form(string swLat, string swLon, string neLat, string neLon)
	{
		Assert.True(RoiFilter.IsCoordinateValidFormat(swLat, swLon, neLat, neLon, out string? formatError));
		Assert.Null(formatError);
		Assert.True(RoiFilter.IsCoordinatesRelativePositionValid(
			double.Parse(swLat, CultureInfo.InvariantCulture), double.Parse(swLon, CultureInfo.InvariantCulture),
			double.Parse(neLat, CultureInfo.InvariantCulture), double.Parse(neLon, CultureInfo.InvariantCulture), out string? positionError));
		Assert.Null(positionError);
	}

	/// <summary>A Southwest Longitude east of the Northeast one is refused, saying how to write an ROI across the AM.</summary>
	[Fact]
	public void a_southwest_longitude_east_of_the_northeast_one_is_refused_with_how_to_cross_the_antimeridian()
	{
		Assert.False(RoiFilter.IsCoordinatesRelativePositionValid(44.0, 154.0, 90.0, -124.0, out string? error));
		Assert.Equal(
			"The Southwest Longitude must be west of the Northeast Longitude. For an ROI that crosses the antimeridian (AM), " +
			"take 360 off the Southwest Longitude (154 becomes -206), as drawing it on the Map does.",
			error);
	}

	[Fact]
	public void equal_longitudes_are_refused()
	{
		Assert.False(RoiFilter.IsCoordinatesRelativePositionValid(38.0, -80.0, 43.0, -80.0, out string? error));
		Assert.Equal("The Southwest Longitude must be west of the Northeast Longitude.", error);
	}

	[Theory]
	[InlineData(-180.0, 180.0)]
	[InlineData(-206.0, 154.0)]
	[InlineData(-300.0, 300.0)]
	public void a_box_360_degrees_or_more_wide_is_refused(double swLon, double neLon)
	{
		Assert.False(RoiFilter.IsCoordinatesRelativePositionValid(38.0, swLon, 43.0, neLon, out string? error));
		Assert.Equal("The Region of Interest must be less than 360° wide.", error);
	}

	/// <summary>The longitudes the wrong way round, read as an ROI across the AM: 360 off the west edge, centred as the Map writes it.</summary>
	[Theory]
	[InlineData("154", "-124", -206.0, -124.0)]
	[InlineData("170", "-170", -190.0, -170.0)]
	[InlineData("179.5", "-179.5", -180.5, -179.5)]
	[InlineData("-100", "-150", -100.0, 210.0)] // -460 … -150, moved a whole 360 to centre it
	public void longitudes_the_wrong_way_round_read_as_crossing_the_antimeridian(string swLon, string neLon, double west, double east)
	{
		RegionOfInterest? roi = RoiFilter.AsCrossingAntimeridian("44", swLon, "90", neLon);

		Assert.Equal(new RegionOfInterest(44, west, 90, east), roi);
		Assert.True(RoiFilter.IsCoordinatesRelativePositionValid(roi!.SwLat, roi.SwLon, roi.NeLat, roi.NeLon, out _));
	}

	/// <summary>Only longitudes the wrong way round, and nothing else wrong, read as an ROI across the AM.</summary>
	[Theory]
	[InlineData("44", "-130", "90", "-124")] // valid already
	[InlineData("90", "154", "44", "-124")] // the latitudes are wrong too
	[InlineData("44", "abc", "90", "-124")] // not a number
	[InlineData("44", "-80", "90", "-80")] // equal, not the wrong way round
	[InlineData("44", "170", "90", "-200")] // 360 off the west edge still leaves it east of the east edge
	public void anything_else_wrong_is_not_read_as_crossing_the_antimeridian(string swLat, string swLon, string neLat, string neLon)
	{
		Assert.Null(RoiFilter.AsCrossingAntimeridian(swLat, swLon, neLat, neLon));
	}

	[Fact]
	public void contains_is_true_for_a_point_inside_the_roi_and_false_outside()
	{
		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0);

		Assert.True(RoiFilter.Contains(roi, 40.0, -80.0));
		Assert.False(RoiFilter.Contains(roi, 50.0, -80.0));
	}

	/// <summary>A point is inside an ROI across the AM on either side of it, and on each of its edges.</summary>
	[Theory]
	[InlineData(60.0, 160.0, true)]
	[InlineData(60.0, 179.9, true)]
	[InlineData(60.0, -179.9, true)]
	[InlineData(60.0, -130.0, true)]
	[InlineData(60.0, 150.0, false)]
	[InlineData(60.0, -120.0, false)]
	[InlineData(60.0, 0.0, false)]
	[InlineData(60.0, 154.0, true)] // the west edge
	[InlineData(60.0, -124.0, true)] // the east edge
	[InlineData(44.0, 170.0, true)] // the south edge
	[InlineData(90.0, -150.0, true)] // the north edge
	[InlineData(43.9, 170.0, false)]
	public void contains_works_across_the_antimeridian(double latitude, double longitude, bool inside)
	{
		RegionOfInterest roi = new(44.0, -206.0, 90.0, -124.0);

		Assert.Equal(inside, RoiFilter.Contains(roi, latitude, longitude));
		Assert.Equal(inside, RoiFilter.Contains(roi with { SwLon = 154.0, NeLon = 236.0 }, latitude, longitude));
	}

	[Fact]
	public void clip_lines_returns_nothing_when_the_lines_are_entirely_outside_the_roi()
	{
		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0);
		LineString farAway = Wgs84.Factory.CreateLineString(
		[
			new Coordinate(-120.0, 34.0),
			new Coordinate(-121.0, 35.0)
		]);

		IReadOnlyList<LineString> result = RoiFilter.ClipLines([farAway], roi);

		Assert.Empty(result);
	}

	[Fact]
	public void clip_lines_returns_the_whole_line_when_fully_inside_the_roi()
	{
		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0);
		LineString inside = Wgs84.Factory.CreateLineString(
		[
			new Coordinate(-82.0, 40.0),
			new Coordinate(-81.0, 41.0)
		]);

		IReadOnlyList<LineString> result = RoiFilter.ClipLines([inside], roi);

		LineString clipped = Assert.Single(result);
		Assert.Equal(2, clipped.NumPoints);
	}

	[Fact]
	public void clip_lines_truncates_a_line_that_crosses_the_roi_boundary()
	{
		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0);

		// Runs straight through the ROI's eastern boundary at lon = -78.
		LineString crossing = Wgs84.Factory.CreateLineString(
		[
			new Coordinate(-80.0, 40.0),
			new Coordinate(-76.0, 40.0)
		]);

		IReadOnlyList<LineString> result = RoiFilter.ClipLines([crossing], roi);

		LineString clipped = Assert.Single(result);
		Assert.All(clipped.Coordinates, c => Assert.True(c.X <= -78.0));
	}

	[Fact]
	public void clip_lines_keeps_the_line_and_drops_a_point_where_the_line_only_touches_a_corner()
	{
		RegionOfInterest roi = new(0.0, 0.0, 10.0, 10.0);

		// Touches the ROI's north-west corner, leaves, then crosses into it: the intersection is
		// a point plus a line.
		LineString line = Wgs84.Factory.CreateLineString(
		[
			new Coordinate(-1, 11), new Coordinate(0, 10), new Coordinate(-1, 9), new Coordinate(5, 5),
		]);

		IReadOnlyList<LineString> result = RoiFilter.ClipLines([line], roi);

		Assert.Single(result);
	}

	private static readonly RegionOfInterest AcrossTheAntimeridian = new(44.0, -206.0, 90.0, -124.0);

	/// <summary>An airway already split at the AM keeps its piece on each side, with coordinates within -180..180.</summary>
	[Fact]
	public void clip_lines_keeps_an_airways_pieces_either_side_of_the_antimeridian()
	{
		LineString west = Line((170, 50), (180, 51));
		LineString east = Line((-180, 51), (-170, 52));

		IReadOnlyList<LineString> result = RoiFilter.ClipLines([west, east], AcrossTheAntimeridian);

		Assert.Equal(2, result.Count);
		Assert.Contains(result, piece => piece.Coordinates.All(c => c.X is >= 170 and <= 180));
		Assert.Contains(result, piece => piece.Coordinates.All(c => c.X is >= -180 and <= -170));
	}

	/// <summary>
	/// A line across the AM that was never split - its sub-service's split is off - is split there and
	/// clipped to the ROI on each side, not drawn the long way round the world.
	/// </summary>
	[Fact]
	public void clip_lines_splits_a_line_across_the_antimeridian_and_clips_each_side()
	{
		LineString line = Line((150, 50), (-110, 52));

		IReadOnlyList<LineString> result = RoiFilter.ClipLines([line], AcrossTheAntimeridian);

		Assert.Equal(2, result.Count);
		LineString westSide = Assert.Single(result, piece => piece.Coordinates.All(c => c.X > 0));
		LineString eastSide = Assert.Single(result, piece => piece.Coordinates.All(c => c.X < 0));
		Assert.Equal(154, westSide.Coordinates.Min(c => c.X), precision: 9);
		Assert.Equal(180, westSide.Coordinates.Max(c => c.X), precision: 9);
		Assert.Equal(-180, eastSide.Coordinates.Min(c => c.X), precision: 9);
		Assert.Equal(-124, eastSide.Coordinates.Max(c => c.X), precision: 9);
	}

	/// <summary>An ROI written past ±180 that doesn't cross the AM clips like the same box within -180..180.</summary>
	[Fact]
	public void clip_lines_clips_to_a_box_written_past_180_that_does_not_cross()
	{
		LineString line = Line((150, 50), (175, 50));

		LineString clipped = Assert.Single(RoiFilter.ClipLines([line], new RegionOfInterest(44.0, -200.0, 90.0, -190.0)));

		Assert.Equal(160, clipped.Coordinates.Min(c => c.X), precision: 9);
		Assert.Equal(170, clipped.Coordinates.Max(c => c.X), precision: 9);
	}

	/// <summary>An ROI whose corners were never validated, with no height, can't be clipped to.</summary>
	[Fact]
	public void clip_lines_refuses_an_roi_with_no_height()
	{
		Assert.Throws<InvalidOperationException>(() => RoiFilter.ClipLines([Line((0, 0), (1, 1))], new RegionOfInterest(10, -80, 10, -70)));
	}

	private static LineString Line(params (double Lon, double Lat)[] points) =>
		Wgs84.Factory.CreateLineString([.. points.Select(p => new Coordinate(p.Lon, p.Lat))]);
}
