using System.Globalization;

using FeBuddy.Core.Application.Airac.Airports;
using FeBuddy.Core.Application.Airac.ArtccBoundaries;
using FeBuddy.Core.Application.Airac.Navaids;
using FeBuddy.Core.Application.Map.Models;
using FeBuddy.Core.Domain.Airports.Models;
using FeBuddy.Core.Domain.ArtccBoundaries.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Domain.Map.Models;
using FeBuddy.Core.Domain.Navaids;
using FeBuddy.Core.Domain.Navaids.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Application.Map;

/// <summary>
/// The Map screen's live layers, built straight from a parsed AIRAC cycle with no run and no files - the same
/// Core builders the AIRAC sub-services use, so the map shows exactly what a run would work from. Deliberately
/// few: the reference layers an FE steers by. Anything more specific (fixes, airways, procedures, one boundary
/// stratum) comes from a run's output files instead. What each layer is and the shapes it holds are decided
/// here; how it is drawn is the map's.
/// </summary>
public static class AiracMapData
{
	/// <summary>The NAVAID types drawn: the VOR family and TACANs, the stations airways hang off.</summary>
	private static readonly HashSet<string> VorTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		NavaidTypes.Vor, NavaidTypes.VorDme, NavaidTypes.Vortac, NavaidTypes.Tacan,
	};

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

	/// <summary>
	/// Every ARTCC boundary ring, High, Low and Unlimited together, each followed by a point carrying its ID as a
	/// label. A centre's High and Low rings often share a label spot; the map's label placer keeps one.
	/// </summary>
	/// <param name="data">The parsed cycle.</param>
	/// <returns>The rings and their labels.</returns>
	public static IReadOnlyList<MapGeometry> ArtccBoundaries(NasrCsvDataCollection data) =>
		[.. ArtccBoundaryBuilder.Read(data).Rings.SelectMany(ToGeometries)];

	/// <summary>The airports with a control tower (<see cref="Airport.IsTowered"/>), and their runways.</summary>
	/// <param name="data">The parsed cycle.</param>
	/// <returns>The runways and the airports.</returns>
	public static ToweredAirportShapes ToweredAirports(NasrCsvDataCollection data)
	{
		List<Airport> towered = [.. AirportBuilder.BuildAll(data).Airports.Where(a => a.IsTowered)];

		List<MapGeometry> runways = [.. towered.SelectMany(a => a.Runways
			.Where(r => r.HasGeometry)
			.Select(r => new MapGeometry(MapGeometryKind.Line,
				[[new GeoPoint(r.FirstEnd!.Latitude, r.FirstEnd.Longitude), new GeoPoint(r.SecondEnd!.Latitude, r.SecondEnd.Longitude)]])
			{
				Feature = RunwayFeature(a, r),
			}))];

		List<MapGeometry> airports = [.. towered.Select(a => new MapGeometry(MapGeometryKind.Point, [[new GeoPoint(a.Latitude, a.Longitude)]], a.FaaId)
		{
			Feature = AirportFeature(a),
		})];

		return new ToweredAirportShapes(runways, airports);
	}

	/// <summary>The VOR, VOR/DME, VORTAC and TACAN stations, each labelled with its ID.</summary>
	/// <param name="data">The parsed cycle.</param>
	/// <returns>A point for each.</returns>
	public static IReadOnlyList<MapGeometry> Navaids(NasrCsvDataCollection data) =>
		[.. NavaidBuilder.BuildAll(data).Navaids
			.Where(n => VorTypes.Contains(n.NavType))
			.Select(n => new MapGeometry(MapGeometryKind.Point, [[new GeoPoint(n.Latitude, n.Longitude)]], n.NavId)
			{
				Feature = NavaidFeature(n),
			})];

	/// <summary>
	/// Where a ring's ID goes: the average of its vertices, worked out the short way round so a
	/// ring over the 180th meridian (Anchorage, Oakland Oceanic) is labelled by itself, not on the
	/// far side of the world. An average is only roughly the middle: a ring with a deep bend in it
	/// can have its label just outside.
	/// </summary>
	/// <param name="points">The ring's points, at least one.</param>
	/// <returns>The label's spot.</returns>
	internal static GeoPoint LabelPoint(IReadOnlyList<GeoPoint> points)
	{
		double previous = points[0].Lon;
		double lonSum = 0, latSum = 0;

		foreach (GeoPoint point in points)
		{
			double lon = WebMercator.UnwrapLon(point.Lon, previous);
			lonSum += lon;
			latSum += point.Lat;
			previous = lon;
		}

		return new GeoPoint(latSum / points.Count, WebMercator.NormalizeLon(lonSum / points.Count));
	}

	private static IEnumerable<MapGeometry> ToGeometries(ArtccBoundaryRing ring)
	{
		if (ring.Points.Count < 2)
		{
			yield break;
		}

		// The ring and its label are one thing to a Ctrl+click.
		MapFeature feature = MapFeature.FromData("ARTCC boundary",
		[
			new("ID", ring.Location.LocationId),
			new("Name", ring.Location.LocationName),
			new("Stratum", ring.Altitude.ToString()),
			new("Type", ring.Type),
		]);

		List<GeoPoint> points = [.. ring.Points.Select(p => new GeoPoint(p.Latitude, p.Longitude))];
		yield return new MapGeometry(MapGeometryKind.Polygon, [points]) { Feature = feature };
		yield return new MapGeometry(MapGeometryKind.Point, [[LabelPoint(points)]], ring.Location.LocationId) { Feature = feature };
	}

	private static MapFeature AirportFeature(Airport airport) => MapFeature.FromData("Airport",
	[
		new("FAA ID", airport.FaaId),
		new("ICAO ID", airport.IcaoId ?? string.Empty),
		new("Name", airport.Name),
		new("Tower", airport.TowerType),
		new("ARTCC", airport.RespArtccId),
		new("Elevation", airport.Elevation is { } elevation ? $"{elevation.ToString("0.#", CultureInfo.InvariantCulture)} ft" : string.Empty),
		new("Airspace", airport.ClassAirspace ?? string.Empty),
		new("CTAF", airport.CtafFrequency ?? string.Empty),
	]);

	private static MapFeature RunwayFeature(Airport airport, AirportRunway runway) => MapFeature.FromData("Runway",
	[
		new("Airport", airport.FaaId),
		new("Runway", runway.RunwayId),
		new("Length", $"{runway.Length.ToString("N0", CultureInfo.InvariantCulture)} ft"),
		new("Surface", runway.SurfaceType ?? string.Empty),
	]);

	private static MapFeature NavaidFeature(Navaid navaid) => MapFeature.FromData("NAVAID",
	[
		new("ID", navaid.NavId),
		new("Name", navaid.Name),
		new("Type", navaid.NavType),
		new("Frequency", navaid.Freq is { } freq ? freq.ToString("0.0#", CultureInfo.InvariantCulture) : string.Empty),
		new("Low ARTCC", navaid.LowAltArtccId),
		new("High ARTCC", navaid.HighAltArtccId),
	]);
}
