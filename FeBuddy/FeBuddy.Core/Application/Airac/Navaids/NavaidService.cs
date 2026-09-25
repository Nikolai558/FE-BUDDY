using System.Diagnostics;

using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Navaids.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Application.Airac.Navaids;

/// <summary>
/// Public entry point for the NAVAIDs sub-service: parses settings, builds every NAVAID, and
/// generates the requested GeoJSON and alias output.
/// </summary>
/// <remarks>
/// The only NAVAIDs type <c>FeBuddy.Harness</c>, the GUI, or <c>AiracService</c> calls directly.
/// Everything else in <c>Application.Airac.Navaids</c> is an implementation detail of this
/// pipeline.
/// </remarks>
public static class NavaidService
{
	private const string LogSource = "NavaidService";

	/// <summary>
	/// Runs the full NAVAIDs pipeline: parse settings, build NAVAIDs, filter, generate GeoJSON,
	/// and (if enabled) generate the alias file.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data. <c>Nav</c> must not be null.</param>
	/// <param name="navaidSettings">The raw NAVAIDs settings dictionary.</param>
	/// <returns>What was built and written, plus timing and every message collected along the way.</returns>
	/// <exception cref="ArgumentException">Thrown when a required setting is missing or invalid.</exception>
	/// <exception cref="InvalidOperationException">Thrown when <paramref name="allNasrCsvData"/>.Nav has not been parsed.</exception>
	public static NavaidServiceResult Run(NasrCsvDataCollection allNasrCsvData, IReadOnlyDictionary<string, string> navaidSettings)
	{
		ArgumentNullException.ThrowIfNull(allNasrCsvData);
		ArgumentNullException.ThrowIfNull(navaidSettings);

		Stopwatch stopwatch = Stopwatch.StartNew();
		List<ServiceMessage> messages = [];

		NavaidSettingsParseResult parseResult = NavaidSettingsParser.Parse(navaidSettings);
		messages.AddRange(parseResult.Messages);

		NavaidBuildAllResult buildResult = NavaidBuilder.BuildAll(allNasrCsvData);
		messages.AddRange(buildResult.Messages);

		// ExcludedTypes leaves a type out of everything; the ROI narrows the GeoJSON output only.
		IReadOnlyList<Navaid> includedNavaids = NavaidFilter.ExcludeTypes(buildResult.Navaids, parseResult.Settings.ExcludedTypes);
		IReadOnlyList<Navaid> navaidsInRoi = NavaidGeojsonWriter.FilterToRoi(includedNavaids, parseResult.Settings.Roi);

		NavaidGeojsonGenerateResult geojsonResult = NavaidGeojsonWriter.Generate(navaidsInRoi, parseResult.Settings);
		messages.AddRange(geojsonResult.Messages);

		NavaidAliasGenerateResult? aliasResult = parseResult.Settings.GenerateAliasFile
			? NavaidAliasWriter.Generate(includedNavaids, parseResult.Settings)
			: null;

		if (aliasResult is not null)
		{
			messages.AddRange(aliasResult.Messages);
		}

		// The ROI limits the GeoJSON only; a region with no NAVAIDs in it would otherwise end in
		// a clean-looking run with no GeoJSON at all, so say why.
		if (parseResult.Settings.GenerateGeojson && navaidsInRoi.Count == 0)
		{
			string text = includedNavaids.Count == 0
				? "No NAVAIDs matched the configured filters, so no NAVAIDs GeoJSON files were written."
				: parseResult.Settings.Roi is null
					? "No NAVAIDs were found, so no NAVAIDs GeoJSON files were written."
					: "No NAVAIDs are inside the region of interest, so no NAVAIDs GeoJSON files were written.";

			if (aliasResult?.FilePath is not null)
			{
				text += " The alias file still covers every included NAVAID.";
			}

			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource, text) { IsAdvisory = true });
		}

		stopwatch.Stop();

		// Every message also flows to the shared application log, so the Dashboard activity log
		// narrates the run.
		foreach (ServiceMessage message in messages)
		{
			AppLog.Write(message.Level, message.Source, message.Text);
		}

		return new NavaidServiceResult
		{
			Messages = messages,
			Elapsed = stopwatch.Elapsed,
			NavaidCount = includedNavaids.Count,
			GeojsonNavaidCount = navaidsInRoi.Count,
			GeojsonFilesWritten = geojsonResult.Files.FilesWritten,
			GeojsonFeatureCountsByFile = geojsonResult.Files.RenderedFeatureCountsByFile,
			AliasFilePath = aliasResult?.FilePath,
			AliasCommandCount = aliasResult?.CommandCount ?? 0
		};
	}
}
