using System.Diagnostics;

using FEBuddyLibrary.Models.NASR.CSV;
using FEBuddyLibrary.Models.Services.Airac.Airways;

namespace FEBuddyLibrary.Services.Airac.Airways;

/// <summary>
/// Public entry point for the Airways services: parses settings, builds every airway, and
/// generates the requested GeoJSON and alias output.
/// </summary>
/// <remarks>
/// This is the only Airways type <c>FEBuddyTest</c> (and later the GUI) needs to call
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
		List<string> warnings = new();

		AirwaySettingsParseResult parseResult = AirwaySettingsParser.Parse(airwaySettings);
		warnings.AddRange(parseResult.Warnings);

		AirwayBuildAllResult buildResult = AirwayBuilder.BuildAll(allNasrCsvData, parseResult.Settings);
		warnings.AddRange(buildResult.Warnings);

		AirwayGeojsonGenerateResult geojsonResult =
			AirwayGeojsonService.Generate(buildResult.Airways, parseResult.Settings);
		warnings.AddRange(geojsonResult.Warnings);

		AirwayAliasGenerateResult? aliasResult = parseResult.Settings.GenerateAliasFile
			? AirwayAliasService.Generate(buildResult.Airways, parseResult.Settings)
			: null;

		stopwatch.Stop();

		return new AirwayServiceResult
		{
			Warnings = warnings,
			Elapsed = stopwatch.Elapsed,
			AirwayCount = buildResult.Airways.Count,
			GeojsonFilesWritten = geojsonResult.FilesWritten,
			GeojsonFeatureCountsByFile = geojsonResult.RenderedFeatureCountsByFile,
			AliasFilePath = aliasResult?.FilePath,
			AliasAirwayLineCount = aliasResult?.AirwayLineCount ?? 0,
			ExcludedAirwayIds = buildResult.ExcludedAirwayIds
		};
	}
}
