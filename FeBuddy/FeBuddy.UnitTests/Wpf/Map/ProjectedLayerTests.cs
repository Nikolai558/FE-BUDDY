using System.Windows.Media;

using FeBuddy.Wpf.Map;
using FeBuddy.Wpf.Map.Models;

namespace FeBuddy.UnitTests.Wpf.Map;

/// <summary>
/// Covers <see cref="ProjectedLayer"/> and <see cref="ProjectedRun"/>: lines unwrapped the short way
/// across the 180th meridian, points folded into one world, and the x range that frames shapes
/// either side of the meridian (<see cref="ProjectedLayer.Covering"/>).
/// </summary>
public sealed class ProjectedLayerTests
{
	/// <summary>A line from 170E to 170W runs east over the meridian, not back across the world.</summary>
	[Fact]
	public void a_line_over_the_meridian_runs_the_short_way()
	{
		ProjectedRun east = ProjectedRun.From([new GeoPoint(50, 170), new GeoPoint(50, -170)], closed: false, geometry: 0);
		ProjectedRun west = ProjectedRun.From([new GeoPoint(50, -170), new GeoPoint(50, 170)], closed: false, geometry: 0);

		Assert.Equal(350.0 / 360.0, east.Xs[0], 1e-9);
		Assert.Equal(370.0 / 360.0, east.Xs[1], 1e-9);
		Assert.Equal(-10.0 / 360.0, west.Xs[1], 1e-9);
	}

	[Fact]
	public void an_ordinary_line_is_left_as_it_is()
	{
		ProjectedRun run = ProjectedRun.From([new GeoPoint(40, -100), new GeoPoint(41, -90), new GeoPoint(42, -80)], closed: true, geometry: 0);

		Assert.Equal(3, run.Xs.Length);
		Assert.Equal(80.0 / 360.0, run.Xs[0], 1e-9);
		Assert.Equal(90.0 / 360.0, run.Xs[1], 1e-9);
		Assert.Equal(100.0 / 360.0, run.Xs[2], 1e-9);
		Assert.True(run.Closed);
		Assert.Equal(run.Xs.Min(), run.MinX);
		Assert.Equal(run.Ys.Max(), run.MaxY);
	}

	/// <summary>A damaged file's wild longitudes project at once and stay near the world.</summary>
	[Fact]
	public void wild_longitudes_project_at_once_and_stay_near_the_world()
	{
		ProjectedRun run = ProjectedRun.From([new GeoPoint(0, 1e20), new GeoPoint(0, 0), new GeoPoint(0, -1e300)], closed: false, geometry: 0);

		Assert.All(run.Xs, x => Assert.InRange(x, -2.0, 3.0));
	}

	/// <summary>Points are folded into one world; lines too short to draw are left out.</summary>
	[Fact]
	public void points_fold_into_one_world_and_one_point_lines_are_dropped()
	{
		MapLayer layer = new("test",
		[
			new MapGeometry(MapGeometryKind.Point, [[new GeoPoint(13.4, 190.0)]], "GUM"),
			new MapGeometry(MapGeometryKind.Line, [[new GeoPoint(40, -100)]]),
			new MapGeometry(MapGeometryKind.Line, [[new GeoPoint(40, -100), new GeoPoint(41, -90)]]),
		], Brushes.Red);

		ProjectedLayer projected = ProjectedLayer.For(layer);

		ProjectedPoint point = Assert.Single(projected.Points);
		Assert.Equal(10.0 / 360.0, point.X, 1e-9);
		Assert.Equal("GUM", point.Label);
		ProjectedRun run = Assert.Single(projected.Runs);
		(double Min, double Max)[] spans = [.. projected.XSpans.OrderBy(s => s.Min)];
		Assert.Equal(2, spans.Length);
		Assert.Equal((point.X, point.X), spans[0]);
		Assert.Equal((run.MinX, run.MaxX), spans[1]);
		Assert.False(projected.IsEmpty);
		Assert.Same(projected, ProjectedLayer.For(layer));
	}

	/// <summary>Each run and point knows its shape, so one shape's can be found to draw it highlighted.</summary>
	[Fact]
	public void each_shapes_runs_and_points_can_be_found()
	{
		MapLayer layer = new("test",
		[
			new MapGeometry(MapGeometryKind.Line, [[new GeoPoint(40, -100), new GeoPoint(41, -90)], [new GeoPoint(42, -100), new GeoPoint(43, -90)]]),
			new MapGeometry(MapGeometryKind.Point, [[new GeoPoint(40, -100)], [new GeoPoint(41, -95)]]),
		], Brushes.Red);

		ProjectedLayer projected = ProjectedLayer.For(layer);

		Assert.Equal(2, projected.Of(0).Runs.Count);
		Assert.Empty(projected.Of(0).Points);
		Assert.Equal([0, 1], projected.Of(1).Points.Select(p => p.Part));
		Assert.All(projected.Of(1).Points, p => Assert.Equal(1, p.Geometry));
		Assert.Empty(projected.Of(7).Runs);
	}

	[Fact]
	public void a_layer_with_nothing_to_draw_is_empty()
	{
		ProjectedLayer projected = ProjectedLayer.For(new MapLayer("empty", [new MapGeometry(MapGeometryKind.Line, [[new GeoPoint(1, 1)]])], Brushes.Red));

		Assert.True(projected.IsEmpty);
		Assert.Empty(projected.XSpans);
	}

	/// <summary>The narrowest x range covering every span, going round the world where that is shorter.</summary>
	/// <param name="because">What the case is.</param>
	/// <param name="bounds">Each span's min and max, one pair after another.</param>
	/// <param name="min">The expected range's start.</param>
	/// <param name="max">The expected range's end.</param>
	[Theory]
	[InlineData("one span", new[] { 0.2, 0.3 }, 0.2, 0.3)]
	[InlineData("the Aleutians, either side of 180", new[] { 0.978, 0.99, 0.01, 0.02 }, 0.978, 1.02)]
	[InlineData("a line over 180 and a shape just east of it", new[] { 0.978, 1.03, 0.01, 0.02 }, 0.978, 1.03)]
	[InlineData("a line unwrapped past 1", new[] { 0.95, 1.05 }, 0.95, 1.05)]
	[InlineData("a line unwrapped below 0", new[] { -0.05, 0.05 }, 0.95, 1.05)]
	[InlineData("the US and Guam, across the Pacific", new[] { 0.15, 0.32, 0.9, 0.9 }, 0.9, 1.32)]
	[InlineData("the US and Europe, straight across", new[] { 0.15, 0.32, 0.5, 0.55 }, 0.15, 0.55)]
	[InlineData("overlapping spans join", new[] { 0.1, 0.3, 0.2, 0.4, 0.35, 0.5 }, 0.1, 0.5)]
	[InlineData("one point", new[] { 0.5, 0.5 }, 0.5, 0.5)]
	[InlineData("a shape as wide as the world", new[] { 0.0, 1.0 }, 0.0, 1.0)]
	[InlineData("shapes all the way round", new[] { 0.0, 0.4, 0.4, 0.8, 0.8, 1.0 }, 0.0, 1.0)]
	public void covering_takes_the_short_way_round(string because, double[] bounds, double min, double max)
	{
		(double Min, double Max) covered = ProjectedLayer.Covering(bounds.Chunk(2).Select(pair => (pair[0], pair[1])))!.Value;

		Assert.True(Math.Abs(covered.Min - min) < 1e-9 && Math.Abs(covered.Max - max) < 1e-9, $"{because}: got {covered}, expected ({min}, {max})");
	}

	[Fact]
	public void covering_nothing_is_null() => Assert.Null(ProjectedLayer.Covering([]));
}
