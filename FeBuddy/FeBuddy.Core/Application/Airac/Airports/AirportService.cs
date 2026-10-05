using System.Diagnostics;

using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Airports.Models;
using FeBuddy.Core.Infrastructure.Geojson;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Application.Airac.Airports;

/// <summary>
/// Public entry point for the Airports sub-service: parses settings, builds every airport, and
/// generates the requested GeoJSON and alias output.
/// </summary>
/// <remarks>
/// The only Airports type <c>AiracService</c> and <c>FeBuddy.Harness</c> call directly; every
/// other type in this folder is a step of this pipeline.
/// </remarks>
public static class AirportService
{
	/// <summary>
	/// Runs the full Airports pipeline: parse settings, build airports, generate GeoJSON, and
	/// (if enabled) generate the alias file.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data. <c>Apt</c> must not be null.</param>
	/// <param name="airportSettings">The raw Airports settings dictionary.</param>
	/// <param name="fileNames">The names the user gave files in place of FE-Buddy's, or <see langword="null"/> for none.</param>
	/// <returns>What was built and written, plus timing and every message collected along the way.</returns>
	/// <exception cref="ArgumentException">Thrown when a required setting is missing or invalid.</exception>
	/// <exception cref="InvalidOperationException">Thrown when <paramref name="allNasrCsvData"/>.Apt has not been parsed.</exception>
	public static AirportServiceResult Run(
		NasrCsvDataCollection allNasrCsvData,
		IReadOnlyDictionary<string, string> airportSettings,
		OutputFileNames? fileNames = null)
	{
		ArgumentNullException.ThrowIfNull(allNasrCsvData);
		ArgumentNullException.ThrowIfNull(airportSettings);

		Stopwatch stopwatch = Stopwatch.StartNew();
		List<ServiceMessage> messages = [];

		AirportSettingsParseResult parseResult = AirportSettingsParser.Parse(airportSettings);
		messages.AddRange(parseResult.Messages);

		AirportSettings settings = parseResult.Settings with { FileNames = fileNames ?? OutputFileNames.None };

		AirportBuildAllResult buildResult = AirportBuilder.BuildAll(allNasrCsvData);
		messages.AddRange(buildResult.Messages);

		// Filtered once here rather than inside each consumer: the GeoJSON output covers the
		// airports inside the ROI, the alias file deliberately covers all of them.
		IReadOnlyList<Airport> airportsInRoi =
			AirportGeojsonWriter.FilterToRoi(buildResult.Airports, settings.Roi);

		GeojsonFileSet geojsonFiles = AirportGeojsonWriter.Generate(airportsInRoi, settings);

		AirportAliasGenerateResult? aliasResult = settings.GenerateAliasFile
			? AirportAliasWriter.Generate(buildResult.Airports, settings)
			: null;

		if (aliasResult is not null)
		{
			messages.AddRange(aliasResult.Messages);
		}

		// The ROI limits the GeoJSON only; a region with no airports in it would otherwise end in
		// a clean-looking run with no GeoJSON at all, so say why.
		if (settings.GenerateGeojson && airportsInRoi.Count == 0)
		{
			string text = settings.Roi is null
				? "No airports were found, so no Airports GeoJSON files were written."
				: "No airports are inside the region of interest, so no Airports GeoJSON files were written.";

			if (aliasResult?.FilePath is not null)
			{
				text += " The alias file still covers every airport.";
			}

			messages.Add(new ServiceMessage(LogLevel.Warning, "AirportService", text) { IsAdvisory = true });
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
			GeojsonFilesWritten = geojsonFiles.FilesWritten,
			GeojsonFeatureCountsByFile = geojsonFiles.RenderedFeatureCountsByFile,
			AliasFilePath = aliasResult?.FilePath,
			AliasCommandCount = aliasResult?.CommandCount ?? 0
		};
	}
}
