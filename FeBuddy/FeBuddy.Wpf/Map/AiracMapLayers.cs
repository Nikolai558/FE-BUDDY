using System.Windows.Media;

using FeBuddy.Core.Application.Airac.Airports;
using FeBuddy.Core.Application.Airac.ArtccBoundaries;
using FeBuddy.Core.Application.Airac.Navaids;
using FeBuddy.Core.Domain.Airports.Models;
using FeBuddy.Core.Domain.ArtccBoundaries.Models;
using FeBuddy.Core.Domain.Navaids.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Wpf.Map.Models;

namespace FeBuddy.Wpf.Map;

/// <summary>The live layers the Map screen can build from a parsed AIRAC cycle.</summary>
public enum AiracLayerKind
{
	/// <summary>Every ARTCC boundary ring, High, Low and Unlimited together.</summary>
	ArtccBoundaries,

	/// <summary>Airports with a control tower, plus their runways once zoomed in.</summary>
	ToweredAirports,

	/// <summary>VOR, VOR/DME, VORTAC and TACAN stations.</summary>
	Navaids,
}

/// <summary>
/// Map layers built straight from a parsed AIRAC cycle, with no run and no files - the same
/// Core builders the AIRAC sub-services use, so the map shows exactly what a run would work from.
/// Deliberately few: the reference layers an FE steers by. Anything more specific (fixes,
/// airways, procedures, one boundary stratum) comes from a run's output files instead.
/// Safe to call off the UI thread: every brush is frozen.
/// </summary>
internal static class AiracMapLayers
{
	/// <summary>The NAVAID types drawn: the VOR family and TACANs, the stations airways hang off.</summary>
	private static readonly HashSet<string> VorTypes = new(StringComparer.OrdinalIgnoreCase) { "VOR", "VOR/DME", "VORTAC", "TACAN" };

	/// <summary>The name shown next to a layer's switch.</summary>
	/// <param name="kind">The layer.</param>
	/// <returns>Its display name.</returns>
	public static string Name(AiracLayerKind kind) => kind switch
	{
		AiracLayerKind.ArtccBoundaries => "ARTCC Boundaries",
		AiracLayerKind.ToweredAirports => "Towered Airports",
		_ => "VOR / VORTAC / TACAN",
	};

	/// <summary>A line explaining what the layer shows, for its tooltip.</summary>
	/// <param name="kind">The layer.</param>
	/// <returns>The explanation.</returns>
	public static string Description(AiracLayerKind kind) => kind switch
	{
		AiracLayerKind.ArtccBoundaries => "High, Low and Unlimited boundaries together. For one stratum on its own, load its file from the run output instead.",
		AiracLayerKind.ToweredAirports => "Airports with a control tower, labelled with their FAA ID. Runways appear as you zoom in.",
		_ => "VOR, VOR/DME, VORTAC and TACAN stations, labelled with their ID as you zoom in.",
	};

	/// <summary>The colour a layer is drawn in; the legend swatch uses it too.</summary>
	/// <param name="kind">The layer.</param>
	/// <returns>Its colour.</returns>
	public static Color Color(AiracLayerKind kind) => kind switch
	{
		AiracLayerKind.ArtccBoundaries => System.Windows.Media.Color.FromRgb(0x7C, 0xC7, 0xF2),
		AiracLayerKind.ToweredAirports => System.Windows.Media.Color.FromRgb(0x5A, 0xD1, 0xA0),
		_ => System.Windows.Media.Color.FromRgb(0xB4, 0x8C, 0xF0),
	};

	/// <summary>Builds one live layer (it can be several map layers, e.g. airports and their runways).</summary>
	/// <param name="kind">The layer to build.</param>
	/// <param name="data">The parsed cycle.</param>
	/// <returns>The map layers, bottom first.</returns>
	public static IReadOnlyList<MapLayer> Build(AiracLayerKind kind, NasrCsvDataCollection data) => kind switch
	{
		AiracLayerKind.ArtccBoundaries => BuildArtccBoundaries(data),
		AiracLayerKind.ToweredAirports => BuildToweredAirports(data),
		_ => BuildNavaids(data),
	};

	private static IReadOnlyList<MapLayer> BuildArtccBoundaries(NasrCsvDataCollection data)
	{
		IReadOnlyList<ArtccBoundaryRing> rings = ArtccBoundaryBuilder.Read(data).Rings;

		// A centre's High and Low rings often share a label spot; the label placer keeps one.
		return
		[
			new MapLayer(Name(AiracLayerKind.ArtccBoundaries), [.. rings.SelectMany(ToGeometries)], Frozen(AiracLayerKind.ArtccBoundaries), thickness: 1.3)
			{
				LabelMinZoom = 4.5,
			},
		];
	}

	private static IReadOnlyList<MapLayer> BuildToweredAirports(NasrCsvDataCollection data)
	{
		List<Airport> towered = [.. AirportBuilder.BuildAll(data).Airports
			.Where(a => !string.IsNullOrWhiteSpace(a.TowerType) && !string.Equals(a.TowerType, "No-TWR", StringComparison.OrdinalIgnoreCase))];
		Brush brush = Frozen(AiracLayerKind.ToweredAirports);

		MapLayer runways = new(
			"Runways",
			[.. towered.SelectMany(a => a.Runways)
				.Where(r => r.HasGeometry)
				.Select(r => new MapGeometry(MapGeometryKind.Line,
					[[new GeoPoint(r.FirstEnd!.Latitude, r.FirstEnd.Longitude), new GeoPoint(r.SecondEnd!.Latitude, r.SecondEnd.Longitude)]]))],
			brush,
			thickness: 2.0)
		{
			MinZoom = 9.0,
			QuietBelowMinZoom = true,
		};

		MapLayer airports = new(
			Name(AiracLayerKind.ToweredAirports),
			[.. towered.Select(a => new MapGeometry(MapGeometryKind.Point, [[new GeoPoint(a.Latitude, a.Longitude)]], a.FaaId))],
			brush,
			pointRadius: 2.5)
		{
			LabelMinZoom = 6.0,
			LabelBesideSymbol = true,
		};

		return [runways, airports];
	}

	private static IReadOnlyList<MapLayer> BuildNavaids(NasrCsvDataCollection data)
	{
		IEnumerable<Navaid> vors = NavaidBuilder.BuildAll(data).Navaids.Where(n => VorTypes.Contains(n.NavType));

		return
		[
			new MapLayer(
				Name(AiracLayerKind.Navaids),
				[.. vors.Select(n => new MapGeometry(MapGeometryKind.Point, [[new GeoPoint(n.Latitude, n.Longitude)]], n.NavId))],
				Frozen(AiracLayerKind.Navaids),
				pointRadius: 4.5)
			{
				MinZoom = 4.5,
				LabelMinZoom = 6.5,
				PointShape = MapPointShape.Hexagon,
				LabelBesideSymbol = true,
			},
		];
	}

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

	private static SolidColorBrush Frozen(AiracLayerKind kind)
	{
		SolidColorBrush brush = new(Color(kind));
		brush.Freeze();
		return brush;
	}
}
