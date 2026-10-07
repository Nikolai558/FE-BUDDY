using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

using FeBuddy.Wpf.Controls;
using FeBuddy.Wpf.Map;
using FeBuddy.Wpf.Map.Models;

namespace FeBuddy.UnitTests.Wpf.Controls;

/// <summary>
/// Covers <see cref="MapCanvas"/>: which copies of the world a shape is drawn in, framing layers
/// the short way round the 180th meridian, the zoom as a percentage of the home view, and letting
/// go of a map whose layer list lives on. Each test runs on a WPF thread of its own
/// (<see cref="StaThread"/>).
/// </summary>
public sealed class MapCanvasTests
{
	/// <summary>The view the copy tests look through: world x 0.1 to 0.4.</summary>
	private static readonly MapCanvas.WorldRect View = new(0.1, 0.4, 0.3, 0.5);

	/// <summary>Every copy of the world in which a span overlaps the view, and no other.</summary>
	[Theory]
	[InlineData(0.2, 0.3, 0, 0)]
	[InlineData(-0.9, -0.8, 1, 1)]
	[InlineData(1.2, 1.3, -1, -1)]
	[InlineData(0.35, 1.15, -1, 0)]
	public void a_span_shows_in_every_copy_it_overlaps(double x0, double x1, int first, int last) =>
		StaThread.Run(() => Assert.Equal((first, last), MapCanvas.Copies(x0, x1, View)));

	/// <summary>
	/// A layer holding a line drawn on past 180 and a shape just east of it spans more than a world:
	/// both copies are drawn, so neither goes missing.
	/// </summary>
	[Fact]
	public void a_layer_either_side_of_the_meridian_is_drawn_in_both_copies() =>
		StaThread.Run(() =>
		{
			(int first, int last) = MapCanvas.Copies(0.01, 1.03, new MapCanvas.WorldRect(0.012, 0.018, 0.3, 0.31));

			Assert.True(first <= -1 && last >= 0, $"copies {first}..{last}");
		});

	/// <summary>However wild a span (a damaged file), the copies stay few and the offsets small.</summary>
	[Theory]
	[InlineData(-1e20, 1e20)]
	[InlineData(0.5, 1e15)]
	[InlineData(1e15, 1e15 + 0.1)]
	public void a_wild_span_gives_few_copies(double x0, double x1) =>
		StaThread.Run(() =>
		{
			(int first, int last) = MapCanvas.Copies(x0, x1, View);

			Assert.InRange(last - first + 1, 0, 9);
			Assert.InRange(first, -1_000_000, 1_000_000);
		});

	/// <summary>Shapes either side of the 180th meridian are framed across it, not across the whole world.</summary>
	[Fact]
	public void layers_over_the_meridian_are_framed_the_short_way() =>
		StaThread.Run(() =>
		{
			MapCanvas map = Sized();
			MapLayer aleutians = Layer(
				new MapGeometry(MapGeometryKind.Line, [[new GeoPoint(52, 172), new GeoPoint(51.5, 179.5)]]),
				new MapGeometry(MapGeometryKind.Line, [[new GeoPoint(51.8, -179.5), new GeoPoint(53, -168)]]));

			Assert.True(map.FrameLayers([aleutians]));

			MapViewState view = map.GetView()!.Value;
			Assert.InRange(Math.Abs(CentreLon(view)), 175.0, 180.0);
			Assert.True(view.Scale > 900 * 10, $"the world is {view.Scale:0} px wide");
		});

	/// <summary>The US and Guam are framed across the Pacific: the middle of 144.8E round to 80W is 147.6W.</summary>
	[Fact]
	public void the_us_and_guam_are_framed_across_the_pacific() =>
		StaThread.Run(() =>
		{
			MapCanvas map = Sized();
			MapLayer layer = Layer(
				new MapGeometry(MapGeometryKind.Line, [[new GeoPoint(47, -122), new GeoPoint(25, -80)]]),
				new MapGeometry(MapGeometryKind.Point, [[new GeoPoint(13.4, 144.8)]]));

			Assert.True(map.FrameLayers([layer]));

			Assert.Equal(-147.6, CentreLon(map.GetView()!.Value), 0.5);
		});

	[Fact]
	public void nothing_to_frame_leaves_the_view_alone() =>
		StaThread.Run(() =>
		{
			MapCanvas map = Sized();
			MapViewState? before = map.GetView();

			Assert.False(map.FrameLayers([Layer()]));
			Assert.Equal(before, map.GetView());
		});

	/// <summary>With no home view saved, the contiguous US (where Home goes then) is 100%.</summary>
	[Fact]
	public void the_contiguous_us_is_100_percent_when_no_home_is_saved() =>
		StaThread.Run(() =>
		{
			MapCanvas map = Framed();
			map.ZoomBy(3);
			Redrawn();
			Assert.Equal(300, map.ZoomPercent, 3);

			map.ResetView();
			Redrawn();

			Assert.Equal(100, map.ZoomPercent, 3);
		});

	[Fact]
	public void zooming_to_a_percentage_lands_on_it_and_a_saved_home_is_the_new_100() =>
		StaThread.Run(() =>
		{
			MapCanvas map = Framed();

			map.ZoomToPercent(250);
			Redrawn();
			Assert.Equal(250, map.ZoomPercent, 3);

			map.HomeZoom = 6;
			map.GoTo(new MapHome(41.4, -81.8, 6));
			Redrawn();
			Assert.Equal(100, map.ZoomPercent, 3);

			map.ZoomBy(2);
			Redrawn();
			Assert.Equal(200, map.ZoomPercent, 3);
		});

	[Theory]
	[InlineData(0)]
	[InlineData(-50)]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	public void a_percentage_that_is_not_above_zero_changes_nothing(double percent) =>
		StaThread.Run(() =>
		{
			MapCanvas map = Framed();
			MapViewState? before = map.GetView();

			map.ZoomToPercent(percent);

			Assert.Equal(before, map.GetView());
		});

	/// <summary>The map's limits cap a percentage: zoom 18 is as close as it goes.</summary>
	[Fact]
	public void a_percentage_past_the_closest_zoom_stops_there() =>
		StaThread.Run(() =>
		{
			MapCanvas map = Framed();

			map.ZoomToPercent(1e12);

			Assert.Equal(18, WebMercator.ScaleToZoom(map.GetView()!.Value.Scale), 6);
		});

	/// <summary>
	/// A map listens to its layer list weakly: a closed popup's map is let go while the list every
	/// map shares lives on.
	/// </summary>
	[Fact]
	public void a_map_is_not_kept_alive_by_its_layer_list() =>
		StaThread.Run(() =>
		{
			ObservableCollection<MapLayer> shared = [];
			WeakReference map = MapShowing(shared);

			for (int i = 0; i < 3; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}

			Assert.False(map.IsAlive);
			shared.Add(Layer());   // and the list carries on without it
		});

	// ============================ Ctrl + click ============================

	/// <summary>The middle of the 900 x 560 map <see cref="Looking"/> builds.</summary>
	private static readonly Point Middle = new(450, 280);

	/// <summary>A line is hit within a few pixels of it, and not further off.</summary>
	[Fact]
	public void a_line_is_hit_near_it_and_not_further_off() =>
		StaThread.Run(() =>
		{
			MapLayer layer = Layer(new MapGeometry(MapGeometryKind.Line, [[new GeoPoint(40, -100), new GeoPoint(40, -99)]]));
			MapCanvas map = Looking(layer);

			MapHit hit = Assert.Single(map.HitTest(Middle + new Vector(0, 4)));
			Assert.Same(layer.Geometries[0], hit.Geometry);
			Assert.Null(hit.Point);
			Assert.Empty(map.HitTest(Middle + new Vector(0, 20)));
		});

	/// <summary>A polygon is drawn as its outline, so only that is hit, not its inside.</summary>
	[Fact]
	public void a_polygon_is_hit_at_its_outline_not_inside() =>
		StaThread.Run(() =>
		{
			MapLayer layer = Layer(new MapGeometry(MapGeometryKind.Polygon,
				[[new GeoPoint(39.9, -99.6), new GeoPoint(39.9, -99.4), new GeoPoint(40.1, -99.4), new GeoPoint(40.1, -99.6)]]));
			MapCanvas map = Looking(layer);

			Assert.Empty(map.HitTest(Middle));

			// The west edge, 0.1 degrees (about 73 pixels) west: the one that closes the ring.
			Assert.Single(map.HitTest(Middle + new Vector(-WebMercator.ZoomToScale(10) / 3600.0, 0)));
		});

	/// <summary>A dot is hit within its radius and a little more; a MultiPoint says which of its points.</summary>
	[Fact]
	public void a_dot_is_hit_and_says_which_point() =>
		StaThread.Run(() =>
		{
			MapLayer layer = Layer(new MapGeometry(MapGeometryKind.Point, [[new GeoPoint(41, -99)], [new GeoPoint(40, -99.5)]]));
			MapCanvas map = Looking(layer);

			Assert.Equal(new GeoPoint(40, -99.5), Assert.Single(map.HitTest(Middle + new Vector(6, 0))).Point);
			Assert.Empty(map.HitTest(Middle + new Vector(12, 0)));
		});

	/// <summary>The shape drawn on top comes first: the last layer's.</summary>
	[Fact]
	public void the_shape_on_top_comes_first() =>
		StaThread.Run(() =>
		{
			MapLayer below = Layer(new MapGeometry(MapGeometryKind.Line, [[new GeoPoint(40, -100), new GeoPoint(40, -99)]]));
			MapLayer above = Layer(new MapGeometry(MapGeometryKind.Line, [[new GeoPoint(40, -100), new GeoPoint(40, -99)]]));
			MapCanvas map = Looking(below, above);

			Assert.Equal([above, below], map.HitTest(Middle).Select(h => h.Layer));
		});

	/// <summary>What isn't drawn can't be clicked: a layer below its zoom, or one not in the list (the base map).</summary>
	[Fact]
	public void what_is_not_drawn_is_not_hit() =>
		StaThread.Run(() =>
		{
			MapLayer waiting = new("waiting", [new MapGeometry(MapGeometryKind.Line, [[new GeoPoint(40, -100), new GeoPoint(40, -99)]])], Brushes.Red) { MinZoom = 12 };
			MapCanvas map = Looking(waiting);
			map.BaseLayers = [Layer(new MapGeometry(MapGeometryKind.Line, [[new GeoPoint(40, -100), new GeoPoint(40, -99)]]))];

			Assert.Empty(map.HitTest(Middle));
		});

	/// <summary>A ring and its label share their feature, so a click on both brings them back once.</summary>
	[Fact]
	public void a_ring_and_its_label_come_back_once() =>
		StaThread.Run(() =>
		{
			MapFeature zob = MapFeature.FromData("ARTCC boundary", [new("ID", "ZOB")]);
			MapFeature other = MapFeature.FromData("ARTCC boundary", [new("ID", "ZNY")]);

			// The ring's edge runs through the middle, where its label is drawn.
			MapLayer shared = Layer(
				new MapGeometry(MapGeometryKind.Polygon, [[new GeoPoint(40, -100), new GeoPoint(40, -99), new GeoPoint(41, -99)]]) { Feature = zob },
				new MapGeometry(MapGeometryKind.Point, [[new GeoPoint(40, -99.5)]], "ZOB") { Feature = zob });
			MapLayer apart = Layer(
				new MapGeometry(MapGeometryKind.Polygon, [[new GeoPoint(40, -100), new GeoPoint(40, -99), new GeoPoint(41, -99)]]) { Feature = zob },
				new MapGeometry(MapGeometryKind.Point, [[new GeoPoint(40, -99.5)]], "ZOB") { Feature = other });

			Assert.Same(zob, Assert.Single(Looking(shared).HitTest(Middle)).Geometry.Feature);
			Assert.Equal(2, Looking(apart).HitTest(Middle).Count);
		});

	/// <summary>A text feature is nothing but its label: before the label is drawn there is nothing to click.</summary>
	[Fact]
	public void a_text_feature_is_hit_only_once_its_label_is_drawn() =>
		StaThread.Run(() =>
		{
			MapLayer layer = Layer(new MapGeometry(MapGeometryKind.Point, [[new GeoPoint(40, -99.5)]], "CLEVELAND"));
			MapCanvas map = new() { Layers = [layer] };
			map.Measure(new Size(900, 560));
			map.Arrange(new Rect(0, 0, 900, 560));
			map.GoTo(new MapHome(40, -99.5, 10));

			Assert.Empty(map.HitTest(Middle));

			Redrawn();

			Assert.Same(layer.Geometries[0], Assert.Single(map.HitTest(Middle + new Vector(20, 0))).Geometry);
		});

	/// <summary>Dots held back for being too many in view aren't drawn, so they aren't there to click.</summary>
	[Fact]
	public void dots_held_back_are_not_hit() =>
		StaThread.Run(() =>
		{
			List<IReadOnlyList<GeoPoint>> many = [.. Enumerable.Range(0, 8_100).Select(i => (IReadOnlyList<GeoPoint>)[new GeoPoint(40 + ((i / 90) * 0.001), -99.5 + ((i % 90) * 0.001))])];
			MapCanvas map = Looking(Layer(new MapGeometry(MapGeometryKind.Point, many)));

			Assert.Empty(map.HitTest(Middle));
		});

	/// <summary>Inspect says where the click was, as the cursor read-out does, with what is there.</summary>
	[Fact]
	public void inspect_gives_the_place_and_the_shapes() =>
		StaThread.Run(() =>
		{
			MapLayer layer = Layer(new MapGeometry(MapGeometryKind.Line, [[new GeoPoint(40, -100), new GeoPoint(40, -99)]]));
			MapCanvas map = Looking(layer);

			MapInspection inspection = map.Inspect(Middle)!;

			Assert.Equal(new GeoPoint(40, -99.5), inspection.At);
			Assert.Single(inspection.Hits);
			Assert.Null(new MapCanvas().Inspect(Middle));
		});

	/// <summary>A highlighted shape is drawn over its layer; one whose layer has gone is not.</summary>
	[Fact]
	public void a_highlighted_shape_is_drawn_only_while_its_layer_is() =>
		StaThread.Run(() =>
		{
			MapLayer layer = Layer(
				new MapGeometry(MapGeometryKind.Line, [[new GeoPoint(40, -100), new GeoPoint(40, -99)]]),
				new MapGeometry(MapGeometryKind.Point, [[new GeoPoint(40, -99.5)]], "CLE"));
			MapCanvas map = Looking(layer);
			DrawingVisual highlight = (DrawingVisual)VisualTreeHelper.GetChild(map, 1);

			map.Highlighted = [.. map.HitTest(Middle), new MapHit(layer, layer.Geometries[1], new GeoPoint(40, -99.5)) { Index = 1 }];
			Assert.NotNull(highlight.Drawing);
			Assert.False(highlight.Drawing.Bounds.IsEmpty);

			map.Highlighted = [new MapHit(Layer(), layer.Geometries[0], null)];
			Assert.True(highlight.Drawing is null || highlight.Drawing.Bounds.IsEmpty);
		});

	/// <summary>The distance to a run is to its nearest segment, the closing one of a ring included.</summary>
	[Fact]
	public void the_distance_to_a_ring_includes_its_closing_edge()
	{
		ProjectedRun ring = ProjectedRun.From([new GeoPoint(0, 0), new GeoPoint(0, 10), new GeoPoint(10, 10)], closed: true, geometry: 0);
		ProjectedRun line = ProjectedRun.From([new GeoPoint(0, 0), new GeoPoint(0, 10), new GeoPoint(10, 10)], closed: false, geometry: 0);
		double x = WebMercator.LonToWorldX(4), y = WebMercator.LatToWorldY(5);

		Assert.True(MapCanvas.DistanceToRun(ring, x, y) < MapCanvas.DistanceToRun(line, x, y));
	}

	/// <summary>
	/// A sized map centred on 40N 99.5W at zoom 10 - about 2.8 pixels to 0.001 degrees of longitude -
	/// showing <paramref name="layers"/>, redrawn once so its labels are placed.
	/// </summary>
	private static MapCanvas Looking(params MapLayer[] layers)
	{
		MapCanvas map = new() { Layers = layers };
		map.Measure(new Size(900, 560));
		map.Arrange(new Rect(0, 0, 900, 560));
		map.GoTo(new MapHome(40, -99.5, 10));
		Redrawn();
		return map;
	}

	/// <summary>A map that has shown <paramref name="layers"/> and redrawn, which nothing but the list refers to.</summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference MapShowing(ObservableCollection<MapLayer> layers)
	{
		MapCanvas map = new() { Layers = layers };
		layers.Add(Layer());

		// Let the queued redraw run, so the dispatcher does not hold the map either.
		Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
		return new WeakReference(map);
	}

	private static MapCanvas Sized()
	{
		MapCanvas map = new();
		map.Measure(new Size(900, 560));
		map.Arrange(new Rect(0, 0, 900, 560));
		return map;
	}

	/// <summary>
	/// A sized map looking at the contiguous US, as a map in a window opens. Laid out by hand, a map
	/// gets no size-changed call, so it is framed here.
	/// </summary>
	private static MapCanvas Framed()
	{
		MapCanvas map = Sized();
		map.ResetView();
		return map;
	}

	/// <summary>Lets the queued redraw run; it is what reports the zoom.</summary>
	private static void Redrawn() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);

	private static MapLayer Layer(params MapGeometry[] geometries) => new("test", geometries, Brushes.OrangeRed);

	/// <summary>The view's centre as a longitude in -180..180.</summary>
	private static double CentreLon(MapViewState view) => WebMercator.NormalizeLon(WebMercator.WorldXToLon(view.CenterX));
}
