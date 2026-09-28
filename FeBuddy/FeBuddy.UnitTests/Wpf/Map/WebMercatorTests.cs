using FeBuddy.Wpf.Map;

namespace FeBuddy.UnitTests.Wpf.Map;

/// <summary>
/// Covers <see cref="WebMercator"/>: the world square's edges, longitude folding, and the one-step
/// unwrap that lines crossing the 180th meridian rely on.
/// </summary>
public sealed class WebMercatorTests
{
	/// <summary>The square world's top and bottom edges, worked out exactly.</summary>
	[Fact]
	public void max_latitude_is_the_square_worlds_edge()
	{
		Assert.Equal(85.0511287798, WebMercator.MaxLatitude, 1e-9);
		Assert.Equal(0.0, WebMercator.LatToWorldY(WebMercator.MaxLatitude), 1e-12);
		Assert.Equal(1.0, WebMercator.LatToWorldY(-WebMercator.MaxLatitude), 1e-12);
	}

	/// <summary>World y never leaves 0..1, however far north or south - a box from pole to pole fills the world exactly.</summary>
	[Theory]
	[InlineData(90.0, 0.0)]
	[InlineData(89.9, 0.0)]
	[InlineData(0.0, 0.5)]
	[InlineData(-89.9, 1.0)]
	[InlineData(-90.0, 1.0)]
	public void lat_to_world_y_stays_in_the_world(double lat, double expected) =>
		Assert.Equal(expected, WebMercator.LatToWorldY(lat), 1e-12);

	[Theory]
	[InlineData(51.5)]
	[InlineData(-33.9)]
	[InlineData(0.0)]
	public void world_y_round_trips(double lat) =>
		Assert.Equal(lat, WebMercator.WorldYToLat(WebMercator.LatToWorldY(lat)), 1e-9);

	[Theory]
	[InlineData(-180.0, 0.0)]
	[InlineData(0.0, 0.5)]
	[InlineData(180.0, 1.0)]
	public void longitude_maps_onto_world_x(double lon, double x)
	{
		Assert.Equal(x, WebMercator.LonToWorldX(lon), 1e-12);
		Assert.Equal(lon, WebMercator.WorldXToLon(x), 1e-9);
	}

	/// <summary>Any longitude folds into -180 (included) to 180 (not).</summary>
	[Theory]
	[InlineData(0.0, 0.0)]
	[InlineData(190.0, -170.0)]
	[InlineData(-190.0, 170.0)]
	[InlineData(180.0, -180.0)]
	[InlineData(-180.0, -180.0)]
	[InlineData(720.0 + 45.0, 45.0)]
	public void normalize_lon_folds_into_one_world(double lon, double expected) =>
		Assert.Equal(expected, WebMercator.NormalizeLon(lon), 1e-9);

	/// <summary>The next point of a line is moved whole worlds to lie the short way from the last.</summary>
	[Theory]
	[InlineData(-170.0, 170.0, 190.0)]
	[InlineData(170.0, -170.0, -190.0)]
	[InlineData(-80.0, -100.0, -80.0)]
	[InlineData(10.0, 350.0, 370.0)]
	public void unwrap_lon_takes_the_short_way(double lon, double previous, double expected) =>
		Assert.Equal(expected, WebMercator.UnwrapLon(lon, previous), 1e-9);

	/// <summary>A wildly wrong longitude (a damaged file's 1e20) unwraps at once, to a finite value near the last.</summary>
	[Theory]
	[InlineData(1e20)]
	[InlineData(-1e300)]
	public void unwrap_lon_of_a_wild_value_is_immediate_and_near(double lon)
	{
		double unwrapped = WebMercator.UnwrapLon(lon, previous: 0.0);

		Assert.True(double.IsFinite(unwrapped));
		Assert.InRange(unwrapped, -180.0, 180.0);
	}

	[Fact]
	public void zoom_and_scale_convert_both_ways()
	{
		Assert.Equal(WebMercator.TileSize, WebMercator.ZoomToScale(0));
		Assert.Equal(6.5, WebMercator.ScaleToZoom(WebMercator.ZoomToScale(6.5)), 1e-12);
	}
}
