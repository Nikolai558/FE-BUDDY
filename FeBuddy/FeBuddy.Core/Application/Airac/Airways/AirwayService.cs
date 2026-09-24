using System.Diagnostics;

using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Airways.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Application.Airac.Airways;

/// <summary>
/// Public entry point for the Airways services: parses settings, builds every airway, and
/// generates the requested GeoJSON and alias output.
/// </summary>
/// <remarks>
/// This is the only Airways type <c>FeBuddy.Harness</c> (and later the GUI) needs to call
/// directly. Every other type in <c>Services.Airac.Airways</c> is an implementation detail of this
/// pipeline.
/// </remarks>
public static class AirwayService
{
	/// <summary>
	/// Runs the full Airways pipeline: parse settings, build airways, generate GeoJSON, and
	/// (if enabled) generate the alias file.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data. <c>Awy</c> must not be null.</param>
	/// <param name="airwaySettings">The raw Airways settings dictionary (see the build plan's Settings Contract).</param>
	/// <returns>What was built and written, plus timing and every warning collected along the way.</returns>
	/// <exception cref="ArgumentException">Thrown when a required setting is missing or invalid.</exception>
	/// <exception cref="InvalidOperationException">Thrown when <paramref name="allNasrCsvData"/>.Awy has not been parsed.</exception>
	public static AirwayServiceResult Run(NasrCsvDataCollection allNasrCsvData, Dictionary<string, string> airwaySettings)
	{
		ArgumentNullException.ThrowIfNull(allNasrCsvData);
		ArgumentNullException.ThrowIfNull(airwaySettings);

		Stopwatch stopwatch = Stopwatch.StartNew();
		List<ServiceMessage> messages = new();

		AirwaySettingsParseResult parseResult = AirwaySettingsParser.Parse(airwaySettings);
		messages.AddRange(parseResult.Messages);

		AirwayBuildAllResult buildResult = AirwayBuilder.BuildAll(allNasrCsvData, parseResult.Settings);
		messages.AddRange(buildResult.Messages);

		// The ROI limits the GeoJSON only. The alias file gets every built airway and applies its
		// own AliasRoiScope - "All" really is all, "ROI airways only" narrows it.
		IReadOnlyList<Airway> airwaysInRoi = buildResult.Airways.Where(a => a.CrossesRoi).ToList();

		AirwayGeojsonGenerateResult geojsonResult =
			AirwayGeojsonWriter.Generate(airwaysInRoi, parseResult.Settings);
		messages.AddRange(geojsonResult.Messages);

		AirwayAliasGenerateResult? aliasResult = parseResult.Settings.GenerateAliasFile
			? AirwayAliasWriter.Generate(buildResult.Airways, parseResult.Settings)
			: null;

		// Filters that leave nothing to write would otherwise end in a clean-looking run, so say
		// which requested output came out empty and why.
		bool noGeojson = parseResult.Settings.OutputBy != AirwayGeojsonOutputBy.None && airwaysInRoi.Count == 0;
		bool noAlias = parseResult.Settings.GenerateAliasFile && aliasResult?.FilePath is null;

		if (noGeojson || noAlias)
		{
			string what = (noGeojson, noAlias) switch
			{
				(true, true) => "no Airways GeoJSON or alias files were written",
				(true, false) => "no Airways GeoJSON files were written",
				_ => "no Airways alias file was written",
			};

			messages.Add(new ServiceMessage(LogLevel.Warning, "AirwayService",
				$"No airways matched your designation and region filters, so {what}.")
			{
				IsAdvisory = true
			});
		}

		stopwatch.Stop();

		// Every message the run produced also flows to the shared application log, so the
		// Dashboard activity log narrates the run (remediation plan 0.3 / 3.8).
		foreach (ServiceMessage message in messages)
		{
			AppLog.Write(message.Level, message.Source, message.Text);
		}

		return new AirwayServiceResult
		{
			Messages = messages,
			Elapsed = stopwatch.Elapsed,
			AirwayCount = airwaysInRoi.Count,
			GeojsonFilesWritten = geojsonResult.FilesWritten,
			GeojsonFeatureCountsByFile = geojsonResult.RenderedFeatureCountsByFile,
			AliasFilePath = aliasResult?.FilePath,
			AliasAirwayLineCount = aliasResult?.AirwayLineCount ?? 0,
			ExcludedAirwayIds = buildResult.ExcludedAirwayIds
		};
	}
}
