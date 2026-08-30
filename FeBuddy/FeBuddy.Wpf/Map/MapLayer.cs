using System.Windows.Media;

namespace FeBuddy.Wpf.Map;

/// <summary>A named set of geometries drawn with one stroke style.</summary>
public sealed class MapLayer
{
    public MapLayer(
        string name,
        IReadOnlyList<MapGeometry> geometries,
        Brush stroke,
        double thickness = 1.4,
        double pointRadius = 3.5)
    {
        Name = name;
        Geometries = geometries;
        Stroke = stroke;
        Thickness = thickness;
        PointRadius = pointRadius;
        Extent = ComputeExtent(geometries);
    }

    public string Name { get; }

    public IReadOnlyList<MapGeometry> Geometries { get; }

    public Brush Stroke { get; }

    public double Thickness { get; }

    public double PointRadius { get; }

    /// <summary>Bounding box of every coordinate in the layer, or null if empty.</summary>
    public GeoBounds? Extent { get; }

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
