using FEBuddyLibrary.Models.Services.General;
using FEBuddyLibrary.Services.Airways;
using FEBuddyLibrary.Services.General;

using NetTopologySuite.Geometries;

namespace UnitTests.Services.General;

public class RoiFilterTests
{
	[Fact]
	public void IsCoordinateValidFormat_accepts_valid_coordinates()
	{
		Assert.True(RoiFilter.IsCoordinateValidFormat("38.0", "-85.0", "43.0", "-78.0", out string? error));
		Assert.Null(error);
	}

	[Theory]
	[InlineData("not-a-number", "-85.0", "43.0", "-78.0")]
	[InlineData("91.0", "-85.0", "43.0", "-78.0")] // latitude out of range
	[InlineData("38.0", "-181.0", "43.0", "-78.0")] // longitude out of range
	public void IsCoordinateValidFormat_rejects_invalid_coordinates(string swLat, string swLon, string neLat, string neLon)
	{
		Assert.False(RoiFilter.IsCoordinateValidFormat(swLat, swLon, neLat, neLon, out string? error));
		Assert.NotNull(error);
	}

	[Fact]
	public void IsCoordinatesRelativePositionValid_accepts_a_proper_box()
	{
		Assert.True(RoiFilter.IsCoordinatesRelativePositionValid(38.0, -85.0, 43.0, -78.0, out string? error));
		Assert.Null(error);
	}

	[Fact]
	public void IsCoordinatesRelativePositionValid_rejects_ne_lat_not_north_of_sw()
	{
		Assert.False(RoiFilter.IsCoordinatesRelativePositionValid(43.0, -85.0, 38.0, -78.0, out string? error));
		Assert.NotNull(error);
	}

	[Fact]
	public void IsCoordinatesRelativePositionValid_rejects_a_roi_that_crosses_the_antimeridian()
	{
		Assert.False(RoiFilter.IsCoordinatesRelativePositionValid(38.0, 170.0, 43.0, -170.0, out string? error));
		Assert.Contains("antimeridian", error, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void Contains_is_true_for_a_point_inside_the_roi_and_false_outside()
	{
		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0);

		Assert.True(RoiFilter.Contains(roi, 40.0, -80.0));
		Assert.False(RoiFilter.Contains(roi, 50.0, -80.0));
	}

	[Fact]
	public void ClipLineGeometry_returns_null_when_geometry_is_entirely_outside_the_roi()
	{
		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0);
		LineString farAway = AirwayGeometryBuilder.GeometryFactory.CreateLineString(new[]
		{
			new Coordinate(-120.0, 34.0),
			new Coordinate(-121.0, 35.0)
		});

		Geometry? result = RoiFilter.ClipLineGeometry(farAway, roi, AirwayGeometryBuilder.GeometryFactory);

		Assert.Null(result);
	}

	[Fact]
	public void ClipLineGeometry_returns_a_linestring_when_fully_inside_the_roi()
	{
		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0);
		LineString inside = AirwayGeometryBuilder.GeometryFactory.CreateLineString(new[]
		{
			new Coordinate(-82.0, 40.0),
			new Coordinate(-81.0, 41.0)
		});

		Geometry? result = RoiFilter.ClipLineGeometry(inside, roi, AirwayGeometryBuilder.GeometryFactory);

		LineString clipped = Assert.IsType<LineString>(result);
		Assert.Equal(2, clipped.NumPoints);
	}

	[Fact]
	public void ClipLineGeometry_truncates_a_line_that_crosses_the_roi_boundary()
	{
		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0);

		// Runs straight through the ROI's eastern boundary at lon = -78.
		LineString crossing = AirwayGeometryBuilder.GeometryFactory.CreateLineString(new[]
		{
			new Coordinate(-80.0, 40.0),
			new Coordinate(-76.0, 40.0)
		});

		Geometry? result = RoiFilter.ClipLineGeometry(crossing, roi, AirwayGeometryBuilder.GeometryFactory);

		LineString clipped = Assert.IsType<LineString>(result);
		Assert.All(clipped.Coordinates, c => Assert.True(c.X <= -78.0));
	}
}
