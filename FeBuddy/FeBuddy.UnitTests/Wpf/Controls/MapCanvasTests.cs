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
/// the short way round the 180th meridian, and letting go of a map whose layer list lives on.
/// Each test runs on a WPF thread of its own (<see cref="StaThread"/>).
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

	private static MapLayer Layer(params MapGeometry[] geometries) => new("test", geometries, Brushes.OrangeRed);

	/// <summary>The view's centre as a longitude in -180..180.</summary>
	private static double CentreLon(MapViewState view) => WebMercator.NormalizeLon(WebMercator.WorldXToLon(view.CenterX));
}
