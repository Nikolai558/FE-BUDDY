using FeBuddy.Core.Application.Airac.WxStations.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Core.Domain.WxStations;
using FeBuddy.Core.Domain.WxStations.Models;
using FeBuddy.Core.Infrastructure.Geojson;

using NetTopologySuite.Features;

namespace FeBuddy.Core.Application.Airac.WxStations;

/// <summary>
/// Generates the Wx Stations GeoJSON output: Symbols and Text only, one Point per station.
/// </summary>
/// <remarks>
/// <para>
/// There is no grouping (no <c>OutputBy</c>) and no <c>feb.*</c> properties: a Symbol Feature
/// carries no attributes at all, and a Text Feature carries only its <c>text</c> array.
/// </para>
/// <para>
/// The ROI limits this output only: the caller filters with <see cref="FilterToRoi"/> before
/// calling <see cref="Generate"/>. Each file goes in the GeoJSON folder, or the vNAS one when the
/// user marked it for vNAS. Only a file chosen for CRC-ERAM defaults gets an isDefaults Feature.
/// </para>
/// </remarks>
public static class WxStationGeojsonWriter
{
	/// <summary>
	/// Keeps the stations whose own coordinates fall inside the ROI.
	/// </summary>
	/// <param name="stations">Every included station.</param>
	/// <param name="roi">The ROI, or <see langword="null"/> for no filtering.</param>
	/// <returns>The stations the GeoJSON output covers.</returns>
	public static IReadOnlyList<WxStation> FilterToRoi(IReadOnlyList<WxStation> stations, RegionOfInterest? roi) =>
		roi is null
			? stations
			: [.. stations.Where(station => RoiFilter.Contains(roi, station.Latitude, station.Longitude))];

	/// <summary>
	/// Generates every GeoJSON file called for by <paramref name="settings"/>.
	/// </summary>
	/// <param name="stations">The stations in scope - already ROI-filtered by the caller.</param>
	/// <param name="settings">The parsed Wx Stations settings.</param>
	/// <returns>The files written and how many rendered Features each holds.</returns>
	public static WxStationGeojsonGenerateResult Generate(IReadOnlyList<WxStation> stations, WxStationSettings settings)
	{
		ArgumentNullException.ThrowIfNull(stations);
		ArgumentNullException.ThrowIfNull(settings);

		GeojsonFileSet files = new(settings.CoordinatePrecision);

		if (stations.Count == 0)
		{
			return new WxStationGeojsonGenerateResult(files);
		}

		if (settings.EmitSymbols)
		{
			GenerateSymbols(stations, settings, files);
		}

		if (settings.EmitText)
		{
			GenerateText(stations, settings, files);
		}

		return new WxStationGeojsonGenerateResult(files);
	}

	private static void GenerateSymbols(IReadOnlyList<WxStation> stations, WxStationSettings settings, GeojsonFileSet files)
	{
		FeatureCollection collection = [];

		if (settings.Vnas.HasCrcDefaults(WxStationOutputFiles.Symbols))
		{
			collection.Add(CrcFeatureFactory.CreateDefaultsFeature(settings.SymbolDefaults[WxStationOutputFiles.AllClass]));
		}

		foreach (WxStation station in stations)
		{
			collection.Add(new Feature(Wgs84.Point(station.Latitude, station.Longitude), new AttributesTable()));
		}

		WriteFile(collection, stations.Count, settings, WxStationOutputFiles.Symbols, files);
	}

	private static void GenerateText(IReadOnlyList<WxStation> stations, WxStationSettings settings, GeojsonFileSet files)
	{
		FeatureCollection collection = [];

		if (settings.Vnas.HasCrcDefaults(WxStationOutputFiles.Text))
		{
			collection.Add(CrcFeatureFactory.CreateDefaultsFeature(settings.TextDefaults[WxStationOutputFiles.AllClass]));
		}

		foreach (WxStation station in stations)
		{
			AttributesTable attributes = new()
			{
				{ "text", new[] { station.IcaoId, WxStationLabels.SecondLine(station) } }
			};

			collection.Add(new Feature(Wgs84.Point(station.Latitude, station.Longitude), attributes));
		}

		WriteFile(collection, stations.Count, settings, WxStationOutputFiles.Text, files);
	}

	/// <summary>Writes one file, into the GeoJSON or vNAS folder as the user chose.</summary>
	private static void WriteFile(FeatureCollection collection, int renderedCount, WxStationSettings settings, string fileKey, GeojsonFileSet files)
	{
		string directory = AiracOutputPaths.FileDirectory(settings.OutputDirectory, isGeojson: true, settings.Vnas.IsUploaded(fileKey));
		files.Write(collection, renderedCount, directory, $"{fileKey}.geojson");
	}
}
