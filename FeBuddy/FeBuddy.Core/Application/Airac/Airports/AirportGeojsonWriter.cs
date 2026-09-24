using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Airports.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Core.Infrastructure.Geojson;

using NetTopologySuite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Application.Airac.Airports;

/// <summary>
/// Generates the Airports GeoJSON output: <c>Airports_Symbols.geojson</c>,
/// <c>Airports_Text.geojson</c> (one Point per airport) and <c>Runways_Lines.geojson</c> (one
/// MultiLineString per airport that has drawable runways).
/// </summary>
/// <remarks>
/// ROI filtering is whole-airport and is tested on the airport reference point, so an airport's
/// symbol, its label and its runways are always in or out together - a runway never survives
/// into a file whose airport did not.
/// </remarks>
public static class AirportGeojsonWriter
{
	private static readonly GeometryFactory GeometryFactory =
		NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

	/// <summary>
	/// Generates every GeoJSON file called for by <paramref name="settings"/>.
	/// </summary>
	/// <param name="airports">The airports in scope - already ROI-filtered by the caller.</param>
	/// <param name="settings">The parsed Airports settings.</param>
	/// <returns>Which files were written, their rendered feature counts, and any messages.</returns>
	public static AirportGeojsonGenerateResult Generate(IReadOnlyList<Airport> airports, AirportSettings settings)
	{
		ArgumentNullException.ThrowIfNull(airports);
		ArgumentNullException.ThrowIfNull(settings);

		List<string> filesWritten = new();
		Dictionary<string, int> renderedCounts = new();
		List<ServiceMessage> messages = new();

		if (!settings.GenerateGeojson)
		{
			return new AirportGeojsonGenerateResult(filesWritten, renderedCounts, messages);
		}

		IReadOnlyList<Airport> inScope = airports;

		if (inScope.Count == 0)
		{
			return new AirportGeojsonGenerateResult(filesWritten, renderedCounts, messages);
		}

		string directory = AirportOutputPaths.Resolve(settings, "Geojson");

		if (settings.EmitAirportSymbols)
		{
			GenerateSymbols(inScope, settings, directory, filesWritten, renderedCounts);
		}

		if (settings.EmitAirportText)
		{
			GenerateText(inScope, settings, directory, filesWritten, renderedCounts);
		}

		if (settings.EmitRunwayLines)
		{
			GenerateRunways(inScope, settings, directory, filesWritten, renderedCounts);
		}

		return new AirportGeojsonGenerateResult(filesWritten, renderedCounts, messages);
	}

	/// <summary>
	/// Keeps the airports whose reference point falls inside the ROI.
	/// </summary>
	/// <param name="airports">Every built airport.</param>
	/// <param name="roi">The ROI, or <see langword="null"/> for no filtering.</param>
	/// <returns>The airports the GeoJSON output covers.</returns>
	public static IReadOnlyList<Airport> FilterToRoi(IReadOnlyList<Airport> airports, RegionOfInterest? roi) =>
		roi is null
			? airports
			: airports.Where(a => RoiFilter.Contains(roi, a.Latitude, a.Longitude)).ToList();

	private static void GenerateSymbols(
		IReadOnlyList<Airport> airports,
		AirportSettings settings,
		string directory,
		List<string> filesWritten,
		Dictionary<string, int> renderedCounts)
	{
		FeatureCollection collection = new();

		if (settings.IncludeCrcSymbolDefaults)
		{
			collection.Add(CrcFeatureFactory.CreateDefault(settings.SymbolDefaults[AirportCrcClass.Airports]));
		}

		foreach (Airport airport in airports)
		{
			AttributesTable attributes = new();
			AddFebProperties(attributes, airport, settings, forTextFile: false);

			collection.Add(new Feature(CreatePoint(airport), attributes));
		}

		Write(collection, airports.Count, directory, "Airports_Symbols.geojson", settings, filesWritten, renderedCounts);
	}

	private static void GenerateText(
		IReadOnlyList<Airport> airports,
		AirportSettings settings,
		string directory,
		List<string> filesWritten,
		Dictionary<string, int> renderedCounts)
	{
		FeatureCollection collection = new();

		if (settings.IncludeCrcTextDefaults)
		{
			collection.Add(CrcFeatureFactory.CreateDefault(settings.TextDefaults[AirportCrcClass.Airports]));
		}

		foreach (Airport airport in airports)
		{
			AttributesTable attributes = new();

			// Two rendered lines: the identifier, then the airport's name.
			attributes.Add("text", new[] { airport.FaaId, airport.Name });

			AddFebProperties(attributes, airport, settings, forTextFile: true);

			collection.Add(new Feature(CreatePoint(airport), attributes));
		}

		Write(collection, airports.Count, directory, "Airports_Text.geojson", settings, filesWritten, renderedCounts);
	}

	private static void GenerateRunways(
		IReadOnlyList<Airport> airports,
		AirportSettings settings,
		string directory,
		List<string> filesWritten,
		Dictionary<string, int> renderedCounts)
	{
		FeatureCollection collection = new();

		if (settings.IncludeCrcLineDefaults)
		{
			collection.Add(CrcFeatureFactory.CreateDefault(settings.LineDefaults[AirportCrcClass.Runways]));
		}

		int rendered = 0;

		foreach (Airport airport in airports)
		{
			// One Feature per airport, one LineString per runway - so an airport is a single
			// object to a controller regardless of how many runways it has.
			AirportRunway[] drawable = airport.Runways.Where(r => r.HasGeometry).ToArray();

			if (drawable.Length == 0)
			{
				continue;
			}

			LineString[] runwayLines = drawable.Select(ToLineString).ToArray();
			AttributesTable attributes = new();

			// feb.rwyId is the only property that describes a runway; it lines up one-for-one with
			// the LineStrings above.
			if (settings.IncludeFebCustomProperties && settings.FebProperties.Contains(AirportFebProperty.RwyId))
			{
				attributes.Add($"feb.{Name(AirportFebProperty.RwyId)}", drawable.Select(r => r.RunwayId).ToArray());
			}

			collection.Add(new Feature(GeometryFactory.CreateMultiLineString(runwayLines), attributes));
			rendered++;
		}

		Write(collection, rendered, directory, "Runways_Lines.geojson", settings, filesWritten, renderedCounts);
	}

	private static LineString ToLineString(AirportRunway runway) =>
		GeometryFactory.CreateLineString(new[]
		{
			new Coordinate(runway.FirstEnd!.Longitude, runway.FirstEnd.Latitude),
			new Coordinate(runway.SecondEnd!.Longitude, runway.SecondEnd.Latitude),
		});

	private static Point CreatePoint(Airport airport) =>
		GeometryFactory.CreatePoint(new Coordinate(airport.Longitude, airport.Latitude));

	/// <summary>
	/// Adds the selected <c>feb.*</c> properties to a Feature.
	/// </summary>
	/// <param name="attributes">The Feature's attribute table.</param>
	/// <param name="airport">The airport being written.</param>
	/// <param name="settings">The parsed settings.</param>
	/// <param name="forTextFile">
	/// <see langword="true"/> for the Text file, which skips the FAA identifier and the name -
	/// its <c>text</c> array already carries both.
	/// </param>
	private static void AddFebProperties(
		AttributesTable attributes,
		Airport airport,
		AirportSettings settings,
		bool forTextFile)
	{
		if (!settings.IncludeFebCustomProperties)
		{
			return;
		}

		foreach (AirportFebProperty property in settings.FebProperties)
		{
			if (forTextFile && property is AirportFebProperty.FaaId or AirportFebProperty.Name)
			{
				continue;
			}

			object? value = ValueFor(airport, property);

			if (value is not null)
			{
				attributes.Add($"feb.{Name(property)}", value);
			}
		}
	}

	private static object? ValueFor(Airport airport, AirportFebProperty property) => property switch
	{
		AirportFebProperty.FaaId => airport.FaaId,
		AirportFebProperty.IcaoId => airport.IcaoId,
		AirportFebProperty.Name => airport.Name,
		AirportFebProperty.Elev => airport.Elevation,
		AirportFebProperty.RespArtcc => airport.RespArtccId,
		AirportFebProperty.TfcPtrnAlt => airport.TrafficPatternAltitude,
		AirportFebProperty.FssId => airport.FssId,
		AirportFebProperty.TwrType => airport.TowerType,
		// Runways Lines only; see GenerateRunways.
		AirportFebProperty.RwyId => null,
		_ => null,
	};

	/// <summary>
	/// The property name as it appears after the <c>feb.</c> prefix. Spelled out rather than
	/// derived from the enum so the JSON keys match the spec exactly (<c>faaId</c>, not
	/// <c>FaaId</c>).
	/// </summary>
	/// <param name="property">The property.</param>
	/// <returns>The JSON name.</returns>
	private static string Name(AirportFebProperty property) => property switch
	{
		AirportFebProperty.FaaId => "faaId",
		AirportFebProperty.IcaoId => "icaoId",
		AirportFebProperty.Name => "name",
		AirportFebProperty.Elev => "elev",
		AirportFebProperty.RespArtcc => "respArtcc",
		AirportFebProperty.TfcPtrnAlt => "tfcPtrnAlt",
		AirportFebProperty.FssId => "fssId",
		AirportFebProperty.TwrType => "twrType",
		AirportFebProperty.RwyId => "rwyId",
		_ => property.ToString(),
	};

	private static void Write(
		FeatureCollection collection,
		int renderedFeatureCount,
		string directory,
		string fileName,
		AirportSettings settings,
		List<string> filesWritten,
		Dictionary<string, int> renderedCounts)
	{
		string? path = GeojsonFileWriter.Write(
			collection, renderedFeatureCount, directory, fileName, settings.CoordinatePrecision);

		if (path is not null)
		{
			filesWritten.Add(path);
			renderedCounts[path] = renderedFeatureCount;
		}
	}
}
