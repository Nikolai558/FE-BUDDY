using System.Diagnostics;

using FeBuddy.Core.Models.NASR.CSV;
using FeBuddy.Core.Models.Services.Airac.Departures;
using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Services.General;

namespace FeBuddy.Core.Services.Airac.Departures;

/// <summary>
/// Public entry point for the Departures sub-service: parses settings, reads and filters every
/// departure procedure, locates each airport's copy of it, and generates the requested GeoJSON
/// and alias output.
/// </summary>
/// <remarks>
/// The only Departures type <c>FeBuddy.Harness</c>, the GUI, or <c>AiracService</c> calls
/// directly. Everything else in <c>Services.Airac.Departures</c> is an implementation detail of
/// this pipeline.
/// </remarks>
public static class DepartureService
{
	/// <summary>
	/// Runs the full Departures pipeline.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data. <c>Dp</c> must not be null; FIX, NAV and APT are read for locations.</param>
	/// <param name="departureSettings">The raw Departures settings dictionary.</param>
	/// <returns>What was built and written, plus timing and every message collected along the way.</returns>
	/// <exception cref="ArgumentException">Thrown when a required setting is missing or invalid.</exception>
	/// <exception cref="InvalidOperationException">Thrown when the DP data has not been parsed.</exception>
	public static DepartureServiceResult Run(NasrCsvDataCollection allNasrCsvData, Dictionary<string, string> departureSettings)
	{
		ArgumentNullException.ThrowIfNull(allNasrCsvData);
		ArgumentNullException.ThrowIfNull(departureSettings);

		Stopwatch stopwatch = Stopwatch.StartNew();
		List<ServiceMessage> messages = new();

		DepartureSettingsParseResult parseResult = DepartureSettingsParser.Parse(departureSettings);
		messages.AddRange(parseResult.Messages);
		DepartureSettings settings = parseResult.Settings;

		DepartureProcedureReadResult readResult = DepartureBuilder.ReadProcedures(allNasrCsvData);
		messages.AddRange(readResult.Messages);

		// Type, ARTCC and amendment filters run before anything is located, so a procedure the
		// user filtered out never costs a lookup or raises a "point not found" warning.
		IReadOnlyList<DepartureProcedure> inScope = DepartureFilter.ByProcedure(readResult.Procedures, settings, messages);

		DepartureLocateResult locateResult = DepartureBuilder.Locate(inScope, allNasrCsvData);
		messages.AddRange(locateResult.Messages);

		// Unlike Airports, the ROI limits the alias file too: both outputs get the same set.
		IReadOnlyList<DepartureAirportProcedure> output =
			DepartureFilter.ByRoi(locateResult.AirportProcedures, settings, allNasrCsvData, messages);

		DepartureGeojsonGenerateResult geojsonResult = DepartureGeojsonService.Generate(output, settings);
		messages.AddRange(geojsonResult.Messages);

		DepartureAliasGenerateResult? aliasResult = settings.GenerateAliasFile
			? DepartureAliasService.Generate(output, settings)
			: null;

		if (aliasResult is not null)
		{
			messages.AddRange(aliasResult.Messages);
		}

		// Filters that leave nothing to write would otherwise end in a clean-looking run with no
		// output folder at all, so say why.
		if (output.Count == 0 && (settings.GenerateGeojson || settings.GenerateAliasFile))
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, "DepartureService",
				"No departure procedures matched your filters, so no Departures files were written.")
			{
				IsAdvisory = true
			});
		}

		stopwatch.Stop();

		// Every message also flows to the shared application log, so the Dashboard activity log
		// narrates the run.
		foreach (ServiceMessage message in messages)
		{
			AppLog.Write(message.Level, message.Source, message.Text);
		}

		return new DepartureServiceResult
		{
			Messages = messages,
			Elapsed = stopwatch.Elapsed,
			ProcedureCount = readResult.Procedures.Count,
			ProceduresInScopeCount = inScope.Count,
			AirportProcedureCount = output.Count,
			SkippedForMissingPointsCount = locateResult.SkippedCount,
			GeojsonFilesWritten = geojsonResult.FilesWritten,
			GeojsonFeatureCountsByFile = geojsonResult.RenderedFeatureCountsByFile,
			AliasFilePath = aliasResult?.FilePath,
			AliasCommandCount = aliasResult?.CommandCount ?? 0
		};
	}
}
