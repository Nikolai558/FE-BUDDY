using System.Diagnostics;

using FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.ArtccBoundaries.Models;
using FeBuddy.Core.Infrastructure.Geojson;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Application.Airac.ArtccBoundaries;

/// <summary>
/// Public entry point for the ARTCC Boundaries sub-service: parses settings, reads every
/// boundary ring, and generates the GeoJSON output.
/// </summary>
/// <remarks>
/// The only ARTCC Boundaries type <c>AiracService</c> and <c>FeBuddy.Harness</c> call directly;
/// every other type in this folder is a step of this pipeline. Unlike every other AIRAC
/// sub-service, there is no alias file.
/// </remarks>
public static class ArtccBoundaryService
{
	private const string LogSource = "ArtccBoundaryService";

	/// <summary>
	/// Runs the full ARTCC Boundaries pipeline: parse settings, read every boundary ring, filter
	/// by location, and generate GeoJSON.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data. <c>Arb</c> must not be null.</param>
	/// <param name="artccBoundarySettings">The raw ARTCC Boundaries settings block (see <see cref="ArtccBoundarySettingsParser"/> for the keys).</param>
	/// <returns>What was built and written, plus timing and every message collected along the way.</returns>
	/// <exception cref="ArgumentException">Thrown when a required setting is missing or invalid.</exception>
	/// <exception cref="InvalidOperationException">Thrown when <paramref name="allNasrCsvData"/>.Arb has not been parsed.</exception>
	public static ArtccBoundaryServiceResult Run(NasrCsvDataCollection allNasrCsvData, IReadOnlyDictionary<string, string> artccBoundarySettings)
	{
		ArgumentNullException.ThrowIfNull(allNasrCsvData);
		ArgumentNullException.ThrowIfNull(artccBoundarySettings);

		Stopwatch stopwatch = Stopwatch.StartNew();
		List<ServiceMessage> messages = [];

		ArtccBoundarySettingsParseResult parseResult = ArtccBoundarySettingsParser.Parse(artccBoundarySettings);
		messages.AddRange(parseResult.Messages);

		ArtccBoundaryBuildAllResult buildResult = ArtccBoundaryBuilder.Read(allNasrCsvData);
		messages.AddRange(buildResult.Messages);

		IReadOnlyList<ArtccBoundaryRing> filteredRings =
			ArtccBoundaryFilter.ByLocation(buildResult.Rings, parseResult.Settings.LocationFilter);

		int locationCount = filteredRings
			.Select(ring => ring.Location.LocationId)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Count();

		GeojsonFileSet geojsonFiles = ArtccBoundaryGeojsonWriter.Generate(filteredRings, parseResult.Settings);
		int ringCount = geojsonFiles.RenderedFeatureCountsByFile.Values.Sum();

		if (geojsonFiles.FilesWritten.Count == 0)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				"No ARTCC boundaries matched your filters, so no ARTCC Boundaries files were written.")
			{
				IsAdvisory = true
			});
		}

		stopwatch.Stop();

		// Every message the run produced also flows to the shared application log, so the
		// Dashboard activity log narrates the run.
		foreach (ServiceMessage message in messages)
		{
			AppLog.Write(message.Level, message.Source, message.Text);
		}

		return new ArtccBoundaryServiceResult
		{
			Messages = messages,
			Elapsed = stopwatch.Elapsed,
			LocationCount = locationCount,
			RingCount = ringCount,
			GeojsonFilesWritten = geojsonFiles.FilesWritten,
			GeojsonFeatureCountsByFile = geojsonFiles.RenderedFeatureCountsByFile,
		};
	}
}
