using System.Windows.Media;

using FeBuddy.Core.Application.Airac.ArtccBoundaries;
using FeBuddy.Core.Domain.ArtccBoundaries.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Wpf.Map.Models;

namespace FeBuddy.Wpf.Map;

/// <summary>
/// Map layers built straight from a parsed AIRAC cycle, with no run and no files - the same
/// Core builders the AIRAC sub-services use, so the map shows exactly what a run would work from.
/// Safe to call off the UI thread: every brush is frozen.
/// </summary>
internal static class AiracMapLayers
{
	/// <summary>The zoom level from which ARTCC IDs are drawn inside their boundaries.</summary>
	private const double ArtccLabelMinZoom = 4.5;

	/// <summary>
	/// The ARTCC boundaries, one layer per altitude stratum (High, Low, Unlimited), each ring
	/// labelled with its ARTCC ID.
	/// </summary>
	/// <param name="data">The parsed cycle.</param>
	/// <returns>The layers, keyed by altitude.</returns>
	public static IReadOnlyDictionary<ArtccBoundaryAltitude, MapLayer> BuildArtccBoundaries(NasrCsvDataCollection data)
	{
		IReadOnlyList<ArtccBoundaryRing> rings = ArtccBoundaryBuilder.Read(data).Rings;

		return rings
			.GroupBy(ring => ring.Altitude)
			.ToDictionary(
				group => group.Key,
				group => new MapLayer(
					$"ARTCC {group.Key}",
					[.. group.SelectMany(ToGeometries)],
					Frozen(ArtccColor(group.Key)),
					thickness: 1.3)
				{
					LabelMinZoom = ArtccLabelMinZoom,
				});
	}

	/// <summary>The line colour for an ARTCC altitude stratum; the legend swatch uses it too.</summary>
	/// <param name="altitude">The stratum.</param>
	/// <returns>Its colour.</returns>
	public static Color ArtccColor(ArtccBoundaryAltitude altitude) => altitude switch
	{
		ArtccBoundaryAltitude.High => Color.FromRgb(0x7C, 0xC7, 0xF2),
		ArtccBoundaryAltitude.Low => Color.FromRgb(0x5A, 0xD1, 0xA0),
		_ => Color.FromRgb(0xB4, 0x8C, 0xF0),
	};

	private static IEnumerable<MapGeometry> ToGeometries(ArtccBoundaryRing ring)
	{
		if (ring.Points.Count < 2)
		{
			yield break;
		}

		List<GeoPoint> points = [.. ring.Points.Select(p => new GeoPoint(p.Latitude, p.Longitude))];
		yield return new MapGeometry(MapGeometryKind.Polygon, [points]);

		if (LabelPoint(points) is { } anchor)
		{
			yield return new MapGeometry(MapGeometryKind.Point, [[anchor]], ring.Location.LocationId);
		}
	}

	/// <summary>
	/// Where a ring's ID goes: the average of its vertices, worked out the short way round so a
	/// ring over the 180th meridian (Anchorage, Oakland Oceanic) is labelled inside itself.
	/// </summary>
	private static GeoPoint? LabelPoint(IReadOnlyList<GeoPoint> points)
	{
		double previous = points[0].Lon;
		double lonSum = 0, latSum = 0;

		foreach (GeoPoint point in points)
		{
			double lon = point.Lon;
			while (lon - previous > 180.0)
			{
				lon -= 360.0;
			}

			while (lon - previous < -180.0)
			{
				lon += 360.0;
			}

			lonSum += lon;
			latSum += point.Lat;
			previous = lon;
		}

		return new GeoPoint(latSum / points.Count, WebMercator.NormalizeLon(lonSum / points.Count));
	}

	private static SolidColorBrush Frozen(Color color)
	{
		SolidColorBrush brush = new(color);
		brush.Freeze();
		return brush;
	}
}
