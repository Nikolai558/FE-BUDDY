using System.Windows.Media;

namespace FeBuddy.Wpf.Map.Models;

/// <summary>A named set of geometries drawn with one stroke style.</summary>
/// <param name="name">The layer's name, e.g. the file it came from.</param>
/// <param name="geometries">The shapes to draw.</param>
/// <param name="stroke">The line and point colour.</param>
/// <param name="thickness">The line width, in device-independent pixels.</param>
/// <param name="pointRadius">The radius of a drawn point, in device-independent pixels.</param>
public sealed class MapLayer(
	string name,
	IReadOnlyList<MapGeometry> geometries,
	Brush stroke,
	double thickness = 1.4,
	double pointRadius = 3.5)
{
	/// <summary>The layer's name.</summary>
	public string Name { get; } = name;

	/// <summary>The shapes to draw.</summary>
	public IReadOnlyList<MapGeometry> Geometries { get; } = geometries;

	/// <summary>The line and point colour.</summary>
	public Brush Stroke { get; } = stroke;

	/// <summary>The line width.</summary>
	public double Thickness { get; } = thickness;

	/// <summary>The radius of a drawn point.</summary>
	public double PointRadius { get; } = pointRadius;

	/// <summary>Bounding box of every coordinate in the layer, or <see langword="null"/> when it is empty.</summary>
	public GeoBounds? Extent { get; } = ComputeExtent(geometries);

	private static GeoBounds? ComputeExtent(IReadOnlyList<MapGeometry> geometries)
	{
		double minLat = double.MaxValue, minLon = double.MaxValue;
		double maxLat = double.MinValue, maxLon = double.MinValue;
		var any = false;

		foreach (var geometry in geometries)
		{
			foreach (var run in geometry.Parts)
			{
				foreach (var p in run)
				{
					any = true;
					if (p.Lat < minLat) minLat = p.Lat;
					if (p.Lat > maxLat) maxLat = p.Lat;
					if (p.Lon < minLon) minLon = p.Lon;
					if (p.Lon > maxLon) maxLon = p.Lon;
				}
			}
		}

		return any
			? new GeoBounds(new GeoPoint(minLat, minLon), new GeoPoint(maxLat, maxLon))
			: null;
	}
}
