using System.Diagnostics;

using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Arrivals.Models;
using FeBuddy.Core.Infrastructure.Geojson;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Application.Airac.Arrivals;

/// <summary>
/// Public entry point for the Arrivals sub-service: parses settings, reads and filters every
/// standard terminal arrival, locates each airport's copy of it, and generates the requested
/// GeoJSON and alias output.
/// </summary>
/// <remarks>
/// The only Arrivals type <c>FeBuddy.Harness</c>, the GUI, or <c>AiracService</c> calls directly.
/// Everything else in <c>Services.Airac.Arrivals</c> is an implementation detail of this pipeline.
/// </remarks>
public static class ArrivalService
{
	/// <summary>
	/// Runs the full Arrivals pipeline.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data. <c>Star</c> must not be null; FIX, NAV and APT are read for locations.</param>
	/// <param name="arrivalSettings">The raw Arrivals settings dictionary.</param>
	/// <returns>What was built and written, plus timing and every message collected along the way.</returns>
	/// <exception cref="ArgumentException">Thrown when a required setting is missing or invalid.</exception>
	/// <exception cref="InvalidOperationException">Thrown when the STAR data has not been parsed.</exception>
	public static ArrivalServiceResult Run(NasrCsvDataCollection allNasrCsvData, IReadOnlyDictionary<string, string> arrivalSettings)
	{
		ArgumentNullException.ThrowIfNull(allNasrCsvData);
		ArgumentNullException.ThrowIfNull(arrivalSettings);

		Stopwatch stopwatch = Stopwatch.StartNew();
		List<ServiceMessage> messages = [];

		ArrivalSettingsParseResult parseResult = ArrivalSettingsParser.Parse(arrivalSettings);
		messages.AddRange(parseResult.Messages);
		ArrivalSettings settings = parseResult.Settings;

		ArrivalProcedureReadResult readResult = ArrivalBuilder.ReadProcedures(allNasrCsvData);
		messages.AddRange(readResult.Messages);

		// ARTCC and amendment filters run before anything is located, so a procedure the user
		// filtered out never costs a lookup or raises a "point not found" warning.
		IReadOnlyList<ArrivalProcedure> inScope = ArrivalFilter.ByProcedure(readResult.Procedures, settings, messages);

		ArrivalLocateResult locateResult = ArrivalBuilder.Locate(inScope, allNasrCsvData);
		messages.AddRange(locateResult.Messages);

		// Unlike Airports, the ROI limits the alias file too: both outputs get the same set.
		IReadOnlyList<ArrivalAirportProcedure> output =
			ArrivalFilter.ByRoi(locateResult.AirportProcedures, settings, allNasrCsvData, messages);

		GeojsonFileSet geojsonFiles = ArrivalGeojsonWriter.Generate(output, settings);

		ArrivalAliasGenerateResult? aliasResult = settings.GenerateAliasFile
			? ArrivalAliasWriter.Generate(output, settings)
			: null;

		if (aliasResult is not null)
		{
			messages.AddRange(aliasResult.Messages);
		}

		// Filters that leave nothing to write would otherwise end in a clean-looking run with no
		// output folder at all, so say why.
		if (output.Count == 0 && (settings.GenerateGeojson || settings.GenerateAliasFile))
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, "ArrivalService",
				"No arrival procedures matched your filters, so no Arrivals files were written.")
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

		return new ArrivalServiceResult
		{
			Messages = messages,
			Elapsed = stopwatch.Elapsed,
			ProcedureCount = readResult.Procedures.Count,
			ProceduresInScopeCount = inScope.Count,
			AirportProcedureCount = output.Count,
			SkippedForMissingPointsCount = locateResult.SkippedCount,
			GeojsonFilesWritten = geojsonFiles.FilesWritten,
			GeojsonFeatureCountsByFile = geojsonFiles.RenderedFeatureCountsByFile,
			AliasFilePath = aliasResult?.FilePath,
			AliasCommandCount = aliasResult?.CommandCount ?? 0
		};
	}
}
