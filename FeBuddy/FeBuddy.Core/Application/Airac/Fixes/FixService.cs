using System.Diagnostics;

using FeBuddy.Core.Application.Airac.Fixes.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Fixes;
using FeBuddy.Core.Domain.Fixes.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Application.Airac.Fixes;

/// <summary>
/// Public entry point for the Fixes sub-service: parses settings, builds every fix, and generates
/// the requested GeoJSON output.
/// </summary>
/// <remarks>
/// The only Fixes type <c>AiracService</c> and <c>FeBuddy.Harness</c> call directly; every other
/// type in this folder is a step of this pipeline. Unlike most other AIRAC sub-services, there is
/// no alias file.
/// </remarks>
public static class FixService
{
	private const string LogSource = "FixService";

	/// <summary>
	/// Runs the full Fixes pipeline: parse settings, build every fix, filter by ROI, and generate
	/// GeoJSON.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data. <c>Fix</c> must not be null.</param>
	/// <param name="fixSettings">The raw Fixes settings dictionary.</param>
	/// <returns>What was built and written, plus timing and every message collected along the way.</returns>
	/// <exception cref="ArgumentException">Thrown when a required setting is missing or invalid.</exception>
	/// <exception cref="InvalidOperationException">Thrown when <paramref name="allNasrCsvData"/>.Fix has not been parsed.</exception>
	public static FixServiceResult Run(NasrCsvDataCollection allNasrCsvData, IReadOnlyDictionary<string, string> fixSettings)
	{
		ArgumentNullException.ThrowIfNull(allNasrCsvData);
		ArgumentNullException.ThrowIfNull(fixSettings);

		Stopwatch stopwatch = Stopwatch.StartNew();
		List<ServiceMessage> messages = [];

		FixSettingsParseResult parseResult = FixSettingsParser.Parse(fixSettings);
		messages.AddRange(parseResult.Messages);

		FixBuildAllResult buildResult = FixBuilder.Read(allNasrCsvData);
		messages.AddRange(buildResult.Messages);

		if (parseResult.Settings.OutputBy == FixOutputBy.ChartAndFixUse)
		{
			// Checked against every built fix, not just what survives the ROI: a combination that
			// exists nowhere in the cycle is worth flagging on its own, separately from "nothing
			// in this region".
			foreach (FixCombination combination in parseResult.Settings.Combinations)
			{
				bool matchesAny = buildResult.Fixes.Any(fix =>
					FixCharts.TokensFor(fix.Charts).Contains(combination.Chart, StringComparer.OrdinalIgnoreCase)
					&& FixUses.Token(fix.FixUse).Equals(combination.FixUse, StringComparison.OrdinalIgnoreCase));

				if (!matchesAny)
				{
					messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
						$"Combination {combination.Chart} + {combination.FixUse} matches no fix in this cycle, " +
						"so its files were not written."));
				}
			}
		}

		IReadOnlyList<Fix> fixesInRoi = FixGeojsonWriter.FilterToRoi(buildResult.Fixes, parseResult.Settings.Roi);

		FixGeojsonGenerateResult geojsonResult = FixGeojsonWriter.Generate(fixesInRoi, parseResult.Settings);

		// Covers every reason the run could end with nothing written: no fixes at all, an empty
		// ROI, or (in the Chart file layout) every chart present having been excluded.
		if (geojsonResult.Files.FilesWritten.Count == 0)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				"No fixes matched your filters, so no Fixes files were written.")
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

		return new FixServiceResult
		{
			Messages = messages,
			Elapsed = stopwatch.Elapsed,
			FixCount = buildResult.Fixes.Count,
			GeojsonFixCount = fixesInRoi.Count,
			GeojsonFilesWritten = geojsonResult.Files.FilesWritten,
			GeojsonFeatureCountsByFile = geojsonResult.Files.RenderedFeatureCountsByFile
		};
	}
}
