using System.Runtime.CompilerServices;

using FeBuddy.Wpf.Map.Models;

namespace FeBuddy.Wpf.Map;

/// <summary>
/// A <see cref="MapLayer"/> projected once into Web-Mercator world units, so every frame only
/// has to scale and translate numbers instead of re-running the projection. Built on first use
/// and kept for as long as the layer lives.
/// <para>
/// Each line is <i>unwrapped</i>: where two neighbouring points are more than 180 degrees of
/// longitude apart, the second is moved a whole world east or west so the line runs the short
/// way across the 180th meridian. Its x then leaves 0..1, which is fine - the map draws every
/// layer once per visible copy of the world, so the line appears whole on either side of the seam.
/// </para>
/// </summary>
internal sealed class ProjectedLayer
{
	private static readonly ConditionalWeakTable<MapLayer, ProjectedLayer> Cache = [];

	private ProjectedLayer(MapLayer layer)
	{
		List<ProjectedRun> runs = [];
		List<ProjectedPoint> points = [];

		foreach (MapGeometry geometry in layer.Geometries)
		{
			if (geometry.Kind == MapGeometryKind.Point)
			{
				foreach (IReadOnlyList<GeoPoint> run in geometry.Parts)
				{
					if (run.Count > 0)
					{
						points.Add(new ProjectedPoint(
							WebMercator.LonToWorldX(WebMercator.NormalizeLon(run[0].Lon)),
							WebMercator.LatToWorldY(run[0].Lat),
							geometry.Label));
					}
				}

				continue;
			}

			bool closed = geometry.Kind == MapGeometryKind.Polygon;
			foreach (IReadOnlyList<GeoPoint> run in geometry.Parts)
			{
				if (run.Count >= 2)
				{
					runs.Add(ProjectedRun.From(run, closed));
				}
			}
		}

		Runs = runs;
		Points = points;

		MinX = MinY = double.MaxValue;
		MaxX = MaxY = double.MinValue;
		foreach (ProjectedRun run in runs)
		{
			Grow(run.MinX, run.MinY);
			Grow(run.MaxX, run.MaxY);
		}

		foreach (ProjectedPoint point in points)
		{
			Grow(point.X, point.Y);
		}

		IsEmpty = runs.Count == 0 && points.Count == 0;
	}

	/// <summary>The layer's lines and rings.</summary>
	public IReadOnlyList<ProjectedRun> Runs { get; }

	/// <summary>The layer's points, labelled or not.</summary>
	public IReadOnlyList<ProjectedPoint> Points { get; }

	/// <summary>Whether there is nothing to draw.</summary>
	public bool IsEmpty { get; }

	/// <summary>The layer's world-unit bounding box (x may run past 0..1, see the class remarks).</summary>
	public double MinX { get; private set; }

	/// <inheritdoc cref="MinX" />
	public double MaxX { get; private set; }

	/// <inheritdoc cref="MinX" />
	public double MinY { get; private set; }

	/// <inheritdoc cref="MinX" />
	public double MaxY { get; private set; }

	/// <summary>The x range each line, ring and point covers, for <see cref="Covering"/>.</summary>
	public IEnumerable<(double Min, double Max)> XSpans =>
		Runs.Select(run => (run.MinX, run.MaxX)).Concat(Points.Select(point => (point.X, point.X)));

	/// <summary>The projection of <paramref name="layer"/>, built on the first call.</summary>
	/// <param name="layer">The layer.</param>
	/// <returns>Its world-unit projection.</returns>
	public static ProjectedLayer For(MapLayer layer) => Cache.GetValue(layer, static l => new ProjectedLayer(l));

	/// <summary>
	/// The narrowest x range that covers every span, going round the world where that is shorter:
	/// shapes either side of the 180th meridian (the Aleutians, say) are covered across it, not
	/// across the whole world.
	/// </summary>
	/// <remarks>
	/// Each span is moved whole worlds to start in 0..1, and one that then runs past 1 - a line drawn
	/// on across the 180th meridian - is split there, its end carried round to the start of the world,
	/// so it joins the shapes it overlaps on that side. The overlapping ones are joined; the widest
	/// gap left between them, going round the world, is the part not covered.
	/// </remarks>
	/// <param name="spans">Each shape's x range; x may run past 0..1.</param>
	/// <returns>The range, which may run past 1; <see langword="null"/> when there are no spans.</returns>
	public static (double Min, double Max)? Covering(IEnumerable<(double Min, double Max)> spans)
	{
		List<(double Start, double End)> arcs = [];

		foreach ((double min, double max) in spans)
		{
			if (max - min >= 1.0)
			{
				return (0.0, 1.0);   // one shape as wide as the world covers it all
			}

			double start = min - Math.Floor(min);
			double end = start + (max - min);

			if (end > 1.0)
			{
				arcs.Add((start, 1.0));
				arcs.Add((0.0, end - 1.0));
			}
			else
			{
				arcs.Add((start, end));
			}
		}

		if (arcs.Count == 0)
		{
			return null;
		}

		arcs.Sort((a, b) => a.Start.CompareTo(b.Start));

		List<(double Start, double End)> joined = [arcs[0]];
		foreach ((double start, double end) in arcs.Skip(1))
		{
			if (start <= joined[^1].End)
			{
				joined[^1] = (joined[^1].Start, Math.Max(joined[^1].End, end));
			}
			else
			{
				joined.Add((start, end));
			}
		}

		// The gap after each joined range: up to the next one's start, or round to the first's.
		int widest = 0;
		double widestGap = double.MinValue;
		for (int i = 0; i < joined.Count; i++)
		{
			double next = i + 1 < joined.Count ? joined[i + 1].Start : joined[0].Start + 1.0;
			double gap = next - joined[i].End;
			if (gap > widestGap)
			{
				(widest, widestGap) = (i, gap);
			}
		}

		if (widestGap <= 0.0)
		{
			return (0.0, 1.0);   // no gap anywhere: the shapes go all the way round
		}

		// Covered: from the range after the widest gap, round to the end of the one before it.
		return widest + 1 < joined.Count
			? (joined[widest + 1].Start, joined[widest].End + 1.0)
			: (joined[0].Start, joined[widest].End);
	}

	private void Grow(double x, double y)
	{
		MinX = Math.Min(MinX, x);
		MaxX = Math.Max(MaxX, x);
		MinY = Math.Min(MinY, y);
		MaxY = Math.Max(MaxY, y);
	}
}

/// <summary>One projected line or ring, with its bounding box for quick off-screen culling.</summary>
internal sealed class ProjectedRun
{
	private ProjectedRun(double[] xs, double[] ys, bool closed)
	{
		Xs = xs;
		Ys = ys;
		Closed = closed;
		MinX = xs.Min();
		MaxX = xs.Max();
		MinY = ys.Min();
		MaxY = ys.Max();
	}

	/// <summary>World x of each vertex, unwrapped across the 180th meridian.</summary>
	public double[] Xs { get; }

	/// <summary>World y of each vertex.</summary>
	public double[] Ys { get; }

	/// <summary>Whether the run is a ring (drawn closed).</summary>
	public bool Closed { get; }

	/// <summary>The run's bounding box, in world units.</summary>
	public double MinX { get; }

	/// <inheritdoc cref="MinX" />
	public double MaxX { get; }

	/// <inheritdoc cref="MinX" />
	public double MinY { get; }

	/// <inheritdoc cref="MinX" />
	public double MaxY { get; }

	public static ProjectedRun From(IReadOnlyList<GeoPoint> run, bool closed)
	{
		double[] xs = new double[run.Count];
		double[] ys = new double[run.Count];

		double previousLon = WebMercator.NormalizeLon(run[0].Lon);
		xs[0] = WebMercator.LonToWorldX(previousLon);
		ys[0] = WebMercator.LatToWorldY(run[0].Lat);

		for (int i = 1; i < run.Count; i++)
		{
			// Take the short way round: a jump of more than half the world is a crossing of the
			// 180th meridian, not a line drawn back across every continent.
			double lon = WebMercator.UnwrapLon(run[i].Lon, previousLon);

			xs[i] = WebMercator.LonToWorldX(lon);
			ys[i] = WebMercator.LatToWorldY(run[i].Lat);
			previousLon = lon;
		}

		return new ProjectedRun(xs, ys, closed);
	}
}

/// <summary>
/// One projected point, with its label when it has one: drawn in the point's place, or beside its
/// symbol on a layer with <see cref="MapLayer.LabelBesideSymbol"/>.
/// </summary>
/// <param name="X">World x, in 0..1.</param>
/// <param name="Y">World y.</param>
/// <param name="Label">Its text, or <see langword="null"/> for a plain dot.</param>
internal readonly record struct ProjectedPoint(double X, double Y, string? Label);
