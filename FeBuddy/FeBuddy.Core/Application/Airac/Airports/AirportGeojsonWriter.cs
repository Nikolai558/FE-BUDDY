using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Domain.Airports.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Core.Infrastructure.Geojson;

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
	/// <summary>
	/// Generates every GeoJSON file called for by <paramref name="settings"/>.
	/// </summary>
	/// <param name="airports">The airports in scope - already ROI-filtered by the caller.</param>
	/// <param name="settings">The parsed Airports settings.</param>
	/// <returns>The files written and how many rendered Features each holds.</returns>
	public static GeojsonFileSet Generate(IReadOnlyList<Airport> airports, AirportSettings settings)
	{
		ArgumentNullException.ThrowIfNull(airports);
		ArgumentNullException.ThrowIfNull(settings);

		GeojsonFileSet files = new(settings.CoordinatePrecision);

		if (!settings.GenerateGeojson || airports.Count == 0)
		{
			return files;
		}

		string directory = SubServiceOutputPaths.Resolve(settings.OutputDirectory, settings.AddFeBuddyOutputFolder, "Airports", "Geojson");

		if (settings.EmitAirportSymbols)
		{
			GenerateSymbols(airports, settings, directory, files);
		}

		if (settings.EmitAirportText)
		{
			GenerateText(airports, settings, directory, files);
		}

		if (settings.EmitRunwayLines)
		{
			GenerateRunways(airports, settings, directory, files);
		}

		return files;
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
		GeojsonFileSet files)
	{
		FeatureCollection collection = new();

		if (settings.IncludeCrcSymbolDefaults)
		{
			collection.Add(CrcFeatureFactory.CreateDefaultsFeature(settings.SymbolDefaults[AirportCrcClass.Airports]));
		}

		foreach (Airport airport in airports)
		{
			AttributesTable attributes = new();
			AddFebProperties(attributes, airport, settings, forTextFile: false);

			collection.Add(new Feature(CreatePoint(airport), attributes));
		}

		files.Write(collection, airports.Count, directory, "Airports_Symbols.geojson");
	}

	private static void GenerateText(
		IReadOnlyList<Airport> airports,
		AirportSettings settings,
		string directory,
		GeojsonFileSet files)
	{
		FeatureCollection collection = new();

		if (settings.IncludeCrcTextDefaults)
		{
			collection.Add(CrcFeatureFactory.CreateDefaultsFeature(settings.TextDefaults[AirportCrcClass.Airports]));
		}

		foreach (Airport airport in airports)
		{
			AttributesTable attributes = new();

			// Two rendered lines: the identifier, then the airport's name.
			attributes.Add("text", new[] { airport.FaaId, airport.Name });

			AddFebProperties(attributes, airport, settings, forTextFile: true);

			collection.Add(new Feature(CreatePoint(airport), attributes));
		}

		files.Write(collection, airports.Count, directory, "Airports_Text.geojson");
	}

	private static void GenerateRunways(
		IReadOnlyList<Airport> airports,
		AirportSettings settings,
		string directory,
		GeojsonFileSet files)
	{
		FeatureCollection collection = new();

		if (settings.IncludeCrcLineDefaults)
		{
			collection.Add(CrcFeatureFactory.CreateDefaultsFeature(settings.LineDefaults[AirportCrcClass.Runways]));
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
			FebProperties.Add(attributes, settings.IncludeFebCustomProperties, settings.FebProperties, property =>
				property == AirportFebProperty.RwyId ? drawable.Select(r => r.RunwayId).ToArray() : null);

			collection.Add(new Feature(Wgs84.Factory.CreateMultiLineString(runwayLines), attributes));
			rendered++;
		}

		files.Write(collection, rendered, directory, "Runways_Lines.geojson");
	}

	private static LineString ToLineString(AirportRunway runway) =>
		Wgs84.Factory.CreateLineString(new[]
		{
			new Coordinate(runway.FirstEnd!.Longitude, runway.FirstEnd.Latitude),
			new Coordinate(runway.SecondEnd!.Longitude, runway.SecondEnd.Latitude),
		});

	private static Point CreatePoint(Airport airport) =>
		Wgs84.Point(airport.Latitude, airport.Longitude);

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
		FebProperties.Add(attributes, settings.IncludeFebCustomProperties, settings.FebProperties, property =>
			forTextFile && property is AirportFebProperty.FaaId or AirportFebProperty.Name
				? null
				: ValueFor(airport, property));
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
}
