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

	/// <summary>The projection of <paramref name="layer"/>, built on the first call.</summary>
	/// <param name="layer">The layer.</param>
	/// <returns>Its world-unit projection.</returns>
	public static ProjectedLayer For(MapLayer layer) => Cache.GetValue(layer, static l => new ProjectedLayer(l));

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
			double lon = run[i].Lon;
			while (lon - previousLon > 180.0)
			{
				lon -= 360.0;
			}

			while (lon - previousLon < -180.0)
			{
				lon += 360.0;
			}

			xs[i] = WebMercator.LonToWorldX(lon);
			ys[i] = WebMercator.LatToWorldY(run[i].Lat);
			previousLon = lon;
		}

		return new ProjectedRun(xs, ys, closed);
	}
}

/// <summary>One projected point, with the label drawn in its place when it has one.</summary>
/// <param name="X">World x, in 0..1.</param>
/// <param name="Y">World y.</param>
/// <param name="Label">Its text, or <see langword="null"/> for a plain dot.</param>
internal readonly record struct ProjectedPoint(double X, double Y, string? Label);
