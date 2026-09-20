using System.Diagnostics;

using FeBuddy.Core.Models.NASR.CSV;
using FeBuddy.Core.Models.Services.Airac.Airports;
using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Services.General;

namespace FeBuddy.Core.Services.Airac.Airports;

/// <summary>
/// Public entry point for the Airports sub-service: parses settings, builds every airport, and
/// generates the requested GeoJSON and alias output.
/// </summary>
/// <remarks>
/// The only Airports type <c>FeBuddy.Harness</c>, the GUI, or <c>AiracService</c> calls
/// directly. Everything else in <c>Services.Airac.Airports</c> is an implementation detail of
/// this pipeline.
/// </remarks>
public static class AirportService
{
	/// <summary>
	/// Runs the full Airports pipeline: parse settings, build airports, generate GeoJSON, and
	/// (if enabled) generate the alias file.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data. <c>Apt</c> must not be null.</param>
	/// <param name="airportSettings">The raw Airports settings dictionary.</param>
	/// <returns>What was built and written, plus timing and every message collected along the way.</returns>
	/// <exception cref="ArgumentException">Thrown when a required setting is missing or invalid.</exception>
	/// <exception cref="InvalidOperationException">Thrown when <paramref name="allNasrCsvData"/>.Apt has not been parsed.</exception>
	public static AirportServiceResult Run(NasrCsvDataCollection allNasrCsvData, Dictionary<string, string> airportSettings)
	{
		ArgumentNullException.ThrowIfNull(allNasrCsvData);
		ArgumentNullException.ThrowIfNull(airportSettings);

		Stopwatch stopwatch = Stopwatch.StartNew();
		List<ServiceMessage> messages = new();

		AirportSettingsParseResult parseResult = AirportSettingsParser.Parse(airportSettings);
		messages.AddRange(parseResult.Messages);

		AirportBuildAllResult buildResult = AirportBuilder.BuildAll(allNasrCsvData);
		messages.AddRange(buildResult.Messages);

		// Filtered once here rather than inside each consumer: the GeoJSON output covers the
		// airports inside the ROI, the alias file deliberately covers all of them.
		IReadOnlyList<Airport> airportsInRoi =
			AirportGeojsonService.FilterToRoi(buildResult.Airports, parseResult.Settings.Roi);

		AirportGeojsonGenerateResult geojsonResult =
			AirportGeojsonService.Generate(airportsInRoi, parseResult.Settings);
		messages.AddRange(geojsonResult.Messages);

		AirportAliasGenerateResult? aliasResult = parseResult.Settings.GenerateAliasFile
			? AirportAliasService.Generate(buildResult.Airports, parseResult.Settings)
			: null;

		if (aliasResult is not null)
		{
			messages.AddRange(aliasResult.Messages);
		}

		stopwatch.Stop();

		// Every message also flows to the shared application log, so the Dashboard activity log
		// narrates the run.
		foreach (ServiceMessage message in messages)
		{
			AppLog.Write(message.Level, message.Source, message.Text);
		}

		return new AirportServiceResult
		{
			Messages = messages,
			Elapsed = stopwatch.Elapsed,
			AirportCount = buildResult.Airports.Count,
			AirportsInRoiCount = airportsInRoi.Count,
			GeojsonFilesWritten = geojsonResult.FilesWritten,
			GeojsonFeatureCountsByFile = geojsonResult.RenderedFeatureCountsByFile,
			AliasFilePath = aliasResult?.FilePath,
			AliasCommandCount = aliasResult?.CommandCount ?? 0
		};
	}
}
