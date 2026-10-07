using System.Windows;
using System.Windows.Media;

using FeBuddy.Wpf.Map;
using FeBuddy.Wpf.Map.Models;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// Ctrl + click on the map (issue #327): what is under the pointer, and the shapes shown in the
/// properties panel drawn highlighted.
/// </summary>
/// <remarks>
/// <para>
/// Only what is drawn can be clicked: the <see cref="Layers"/> (not the base map), at a zoom that
/// draws them, and of a layer's points only the dots and labels that were drawn - a label the
/// label placer left out, or dots held back for being too many, are not there to click. A line or
/// ring is hit within a few pixels of it; a polygon only at its outline, as that is all that is drawn.
/// </para>
/// <para>
/// The shapes come back the one drawn on top first: the last layer first, and within a layer the
/// nearest first. A ring and its label share their feature, so they come back once.
/// </para>
/// </remarks>
public sealed partial class MapCanvas
{
	/// <summary>How close (pixels) a Ctrl + click has to be to a line or a dot to land on it.</summary>
	private const double InspectSlop = 5.0;

	/// <summary>Identifies the <see cref="Highlighted"/> dependency property.</summary>
	public static readonly DependencyProperty HighlightedProperty = DependencyProperty.Register(
		nameof(Highlighted), typeof(IReadOnlyList<MapHit>), typeof(MapCanvas),
		new PropertyMetadata(null, (d, _) => ((MapCanvas)d).RedrawHighlight()));

	private readonly DrawingVisual _highlightVisual = new();

	/// <summary>The layers whose dots the last redraw held back: there were too many in view.</summary>
	private readonly HashSet<MapLayer> _dotsHeldBack = [];

	/// <summary>Every label the last redraw drew, and where.</summary>
	private readonly List<PlacedLabel> _placedLabels = [];

	/// <summary>Raised when a Ctrl + click lands on the map, with what is under the pointer.</summary>
	public event EventHandler<MapInspection>? Inspected;

	/// <summary>Shapes to draw highlighted (those in the properties panel), or <see langword="null"/> for none.</summary>
	public IReadOnlyList<MapHit>? Highlighted
	{
		get => (IReadOnlyList<MapHit>?)GetValue(HighlightedProperty);
		set => SetValue(HighlightedProperty, value);
	}

	/// <summary>What a Ctrl + click at <paramref name="pos"/> lands on.</summary>
	/// <param name="pos">The pointer, in the map's own coordinates.</param>
	/// <returns>Where it was and what is there; <see langword="null"/> above or below the world, or before the map has laid out.</returns>
	internal MapInspection? Inspect(Point pos)
	{
		double worldY = WorldY(pos.Y);
		if (!_framed || worldY is < 0.0 or > 1.0)
		{
			return null;
		}

		GeoPoint at = new(
			Math.Round(WebMercator.WorldYToLat(worldY), 5),
			Math.Round(WebMercator.NormalizeLon(WebMercator.WorldXToLon(WorldX(pos.X))), 5));

		return new MapInspection(at, HitTest(pos));
	}

	/// <summary>The shapes under <paramref name="pos"/>, the one drawn on top first.</summary>
	/// <param name="pos">The pointer, in the map's own coordinates.</param>
	/// <returns>Up to <see cref="MapInspection.MaxHits"/> shapes; none when nothing is there.</returns>
	internal IReadOnlyList<MapHit> HitTest(Point pos)
	{
		if (Layers is not { } layers || ActualWidth < 1 || ActualHeight < 1)
		{
			return [];
		}

		List<MapLayer> drawn = [.. layers];
		List<(MapHit Hit, int Order, double Distance)> found = [];

		for (int order = 0; order < drawn.Count; order++)
		{
			MapLayer layer = drawn[drawn.Count - 1 - order];
			ProjectedLayer projected = ProjectedLayer.For(layer);
			if (projected.IsEmpty || Zoom < layer.MinZoom)
			{
				continue;
			}

			Dictionary<int, (double Distance, int Part)> nearest = [];
			HitRuns(layer, projected, pos, nearest);
			HitPoints(layer, projected, pos, nearest);

			foreach ((int index, (double distance, int part)) in nearest)
			{
				MapGeometry geometry = layer.Geometries[index];
				GeoPoint? point = part >= 0 && part < geometry.Parts.Count ? geometry.Parts[part][0] : null;
				found.Add((new MapHit(layer, geometry, point) { Index = index }, order, distance));
			}
		}

		return
		[
			.. found
				.OrderBy(f => f.Order)
				.ThenBy(f => f.Distance)
				.Select(f => f.Hit)
				.DistinctBy(hit => (hit.Layer, (object?)hit.Geometry.Feature ?? hit.Geometry))
				.Take(MapInspection.MaxHits),
		];
	}

	/// <summary>The lines and rings within reach of <paramref name="pos"/>: the distance to the nearest of each shape's.</summary>
	private void HitRuns(MapLayer layer, ProjectedLayer projected, Point pos, Dictionary<int, (double Distance, int Part)> nearest)
	{
		if (projected.Runs.Count == 0)
		{
			return;
		}

		double reach = (InspectSlop + (layer.Thickness / 2.0)) / _scale;
		double x = WorldX(pos.X), y = WorldY(pos.Y);
		WorldRect near = new(x - reach, x + reach, y - reach, y + reach);
		(int first, int last) = Copies(projected.MinX, projected.MaxX, near);

		for (int k = first; k <= last; k++)
		{
			foreach (ProjectedRun run in projected.Runs)
			{
				if (run.MaxX + k < near.X0 || run.MinX + k > near.X1 || run.MaxY < near.Y0 || run.MinY > near.Y1)
				{
					continue;
				}

				double distance = DistanceToRun(run, x - k, y);
				if (distance <= reach)
				{
					Keep(nearest, run.Geometry, distance * _scale, part: -1);
				}
			}
		}
	}

	/// <summary>The dots and labels drawn under <paramref name="pos"/>.</summary>
	private void HitPoints(MapLayer layer, ProjectedLayer projected, Point pos, Dictionary<int, (double Distance, int Part)> nearest)
	{
		if (projected.Points.Count == 0)
		{
			return;
		}

		foreach (PlacedLabel label in _placedLabels)
		{
			if (ReferenceEquals(label.Layer, layer) && label.Box.Contains(pos))
			{
				Keep(nearest, label.Geometry, (pos - CenterOf(label.Box)).Length, label.Part);
			}
		}

		if (_dotsHeldBack.Contains(layer))
		{
			return;
		}

		double reach = layer.PointRadius + InspectSlop;
		double x = WorldX(pos.X), y = WorldY(pos.Y), worldReach = reach / _scale;
		(int first, int last) = Copies(projected.MinX, projected.MaxX, new WorldRect(x - worldReach, x + worldReach, y - worldReach, y + worldReach));

		for (int k = first; k <= last; k++)
		{
			foreach (ProjectedPoint point in projected.Points)
			{
				// A labelled point on a layer that draws only its label has no dot to click.
				if (point.Label is not null && !layer.LabelBesideSymbol)
				{
					continue;
				}

				double distance = new Vector((point.X + k - x) * _scale, (point.Y - y) * _scale).Length;
				if (distance <= reach)
				{
					Keep(nearest, point.Geometry, distance, point.Part);
				}
			}
		}
	}

	private static void Keep(Dictionary<int, (double Distance, int Part)> nearest, int geometry, double distance, int part)
	{
		if (!nearest.TryGetValue(geometry, out var best) || distance < best.Distance)
		{
			nearest[geometry] = (distance, part);
		}
	}

	/// <summary>How far, in world units, (<paramref name="x"/>, <paramref name="y"/>) is from the nearest part of a run.</summary>
	internal static double DistanceToRun(ProjectedRun run, double x, double y)
	{
		double best = double.MaxValue;
		int count = run.Xs.Length;

		for (int i = 1; i < count; i++)
		{
			best = Math.Min(best, DistanceToSegment(x, y, run.Xs[i - 1], run.Ys[i - 1], run.Xs[i], run.Ys[i]));
		}

		if (run.Closed)
		{
			best = Math.Min(best, DistanceToSegment(x, y, run.Xs[count - 1], run.Ys[count - 1], run.Xs[0], run.Ys[0]));
		}

		return best;
	}

	private static double DistanceToSegment(double x, double y, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0;
		double lengthSquared = (dx * dx) + (dy * dy);
		double t = lengthSquared == 0 ? 0 : Math.Clamp((((x - x0) * dx) + ((y - y0) * dy)) / lengthSquared, 0.0, 1.0);
		double px = x0 + (t * dx) - x, py = y0 + (t * dy) - y;
		return Math.Sqrt((px * px) + (py * py));
	}

	private static Point CenterOf(Rect box) => new(box.Left + (box.Width / 2.0), box.Top + (box.Height / 2.0));

	/// <summary>
	/// Draws the <see cref="Highlighted"/> shapes over their layers: a dark halo and an amber line
	/// along each line and ring, an amber ring round each dot, and a box round a text feature's label.
	/// A shape whose layer is no longer drawn is left out.
	/// </summary>
	private void RedrawHighlight()
	{
		using DrawingContext dc = _highlightVisual.RenderOpen();
		if (Highlighted is not { Count: > 0 } hits || Layers is not { } layers || ActualWidth < 1 || ActualHeight < 1)
		{
			return;
		}

		HashSet<MapLayer> drawn = [.. layers];
		Brush accent = Theme("Brush.Accent", Color.FromRgb(0xF4, 0xB7, 0x40));
		Brush shadow = FrozenBrush.Of(Color.FromArgb(0xB0, 0x03, 0x06, 0x0A));
		Pen ring = new(accent, 2.0);
		ring.Freeze();

		foreach (MapHit hit in hits)
		{
			if (!drawn.Contains(hit.Layer) || Zoom < hit.Layer.MinZoom)
			{
				continue;
			}

			ProjectedLayer projected = ProjectedLayer.For(hit.Layer);
			(IReadOnlyList<ProjectedRun> runs, IReadOnlyList<ProjectedPoint> points) = projected.Of(hit.Index);
			WorldRect view = ViewWorld(hit.Layer.Thickness + 8);
			(int first, int last) = Copies(projected.MinX, projected.MaxX, view);

			if (runs.Count > 0)
			{
				StreamGeometry geometry = RunsGeometry(runs, view, first, last);
				dc.DrawGeometry(null, RoundPen(shadow, hit.Layer.Thickness + 5), geometry);
				dc.DrawGeometry(null, RoundPen(accent, hit.Layer.Thickness + 1.5), geometry);
			}

			// Snug where a label sits beside the dot, so the ring doesn't cross it.
			double radius = hit.Layer.PointRadius + (hit.Layer.LabelBesideSymbol ? 2.5 : 4.0);
			for (int k = first; k <= last; k++)
			{
				foreach (ProjectedPoint point in points)
				{
					if (point.Label is null || hit.Layer.LabelBesideSymbol)
					{
						dc.DrawEllipse(null, ring, new Point(ScreenX(point.X + k), ScreenY(point.Y)), radius, radius);
					}
				}
			}

			// A label beside its dot has the dot ringed already; a text feature is only its label, so that is boxed.
			foreach (PlacedLabel label in _placedLabels)
			{
				if (!hit.Layer.LabelBesideSymbol && ReferenceEquals(label.Layer, hit.Layer) && label.Geometry == hit.Index)
				{
					Rect box = label.Box;
					box.Inflate(3, 2);
					dc.DrawRoundedRectangle(null, ring, box, 3, 3);
				}
			}
		}
	}

	private static Pen RoundPen(Brush brush, double thickness)
	{
		Pen pen = new(brush, thickness) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
		pen.Freeze();
		return pen;
	}

	/// <summary>A label the last redraw drew: whose it is, and the box it was drawn in.</summary>
	private readonly record struct PlacedLabel(MapLayer Layer, int Geometry, int Part, Rect Box);
}
