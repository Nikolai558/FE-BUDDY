using System.Diagnostics;

using FeBuddy.Core.Application.Airac.WxStations.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.WxStations.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.WxStations.Models;

namespace FeBuddy.Core.Application.Airac.WxStations;

/// <summary>
/// Public entry point for the Wx Stations sub-service: parses settings, builds every included
/// station, and generates the requested GeoJSON output.
/// </summary>
/// <remarks>
/// The only Wx Stations type <c>AiracService</c> and <c>FeBuddy.Harness</c> call directly; every
/// other type in this folder is a step of this pipeline. Unlike every other AIRAC sub-service,
/// its data does not come from a NASR CSV cycle - it comes from aviationweather.gov's live station
/// list, which every AIRAC Service run downloads into one kept copy (see
/// <see cref="Infrastructure.WxStations.WxStationDownloader"/>), supplied here already parsed.
/// There is no alias file.
/// </remarks>
public static class WxStationService
{
	private const string LogSource = "WxStationService";

	/// <summary>
	/// Runs the full Wx Stations pipeline: parse settings, build every included station, filter by
	/// ROI, and generate GeoJSON.
	/// </summary>
	/// <param name="wxStationData">
	/// The parsed <c>stations.cache.xml</c> data, or <see langword="null"/> when FE-Buddy has no copy
	/// of it - the run then writes nothing and says so; the AIRAC Service reports why the download
	/// failed.
	/// </param>
	/// <param name="wxStationSettings">The raw Wx Stations settings dictionary.</param>
	/// <returns>What was built and written, plus timing and every message collected along the way.</returns>
	/// <exception cref="ArgumentException">Thrown when a required setting is missing or invalid.</exception>
	public static WxStationServiceResult Run(WxStationDataCollection? wxStationData, IReadOnlyDictionary<string, string> wxStationSettings)
	{
		ArgumentNullException.ThrowIfNull(wxStationSettings);

		Stopwatch stopwatch = Stopwatch.StartNew();
		List<ServiceMessage> messages = [];

		WxStationSettingsParseResult parseResult = WxStationSettingsParser.Parse(wxStationSettings);
		messages.AddRange(parseResult.Messages);

		if (wxStationData is null)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				"There was no weather station data to build from, so no Wx Stations files were written."));

			stopwatch.Stop();
			LogAll(messages);

			return new WxStationServiceResult
			{
				Messages = messages,
				Elapsed = stopwatch.Elapsed,
				StationCount = 0,
				TotalStationCount = 0,
				GeojsonStationCount = 0,
				GeojsonFilesWritten = [],
				GeojsonFeatureCountsByFile = new Dictionary<string, int>(),
			};
		}

		WxStationBuildResult buildResult = WxStationBuilder.Read(wxStationData);
		messages.AddRange(buildResult.Messages);

		IReadOnlyList<WxStation> stationsInRoi =
			WxStationGeojsonWriter.FilterToRoi(buildResult.Stations, parseResult.Settings.Roi);

		WxStationGeojsonGenerateResult geojsonResult = WxStationGeojsonWriter.Generate(stationsInRoi, parseResult.Settings);

		// Covers every reason the run could end with nothing written: no stations at all, or an
		// empty ROI.
		if (geojsonResult.Files.FilesWritten.Count == 0)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				"No weather stations matched your filters, so no Wx Stations files were written.")
			{
				IsAdvisory = true
			});
		}

		stopwatch.Stop();
		LogAll(messages);

		return new WxStationServiceResult
		{
			Messages = messages,
			Elapsed = stopwatch.Elapsed,
			StationCount = buildResult.Stations.Count,
			TotalStationCount = buildResult.TotalStationCount,
			GeojsonStationCount = stationsInRoi.Count,
			GeojsonFilesWritten = geojsonResult.Files.FilesWritten,
			GeojsonFeatureCountsByFile = geojsonResult.Files.RenderedFeatureCountsByFile
		};
	}

	/// <summary>
	/// Copies every message to the shared application log, so the Dashboard activity log narrates
	/// the run.
	/// </summary>
	private static void LogAll(List<ServiceMessage> messages)
	{
		foreach (ServiceMessage message in messages)
		{
			AppLog.Write(message.Level, message.Source, message.Text);
		}
	}
}
