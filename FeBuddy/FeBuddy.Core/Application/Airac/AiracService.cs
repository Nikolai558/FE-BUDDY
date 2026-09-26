using System.Diagnostics;

using FeBuddy.Core.Application.Airac.Airports;
using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Application.Airac.Airways;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Airac.Arrivals;
using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Application.Airac.ArtccBoundaries;
using FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;
using FeBuddy.Core.Application.Airac.Departures;
using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Application.Airac.Fixes;
using FeBuddy.Core.Application.Airac.Fixes.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Navaids;
using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Application.Airac;

/// <summary>
/// The AIRAC Service: the GUI calls this once per "Run AIRAC Service". It runs each selected
/// sub-service (Airways, Airports, Departures, Arrivals, NAVAIDs, ARTCC Boundaries, Fixes) against
/// one cycle's NASR data and gathers the results.
/// </summary>
/// <remarks>
/// <para>
/// It never parses NASR data itself. The parsed cycle comes from
/// <see cref="AiracCycleDataCache"/>, which the launch sequence has usually filled already.
/// </para>
/// <para>
/// Every sub-service writes into the same cycle folder
/// (<see cref="AiracServiceSettings.CycleOutputDirectory"/>): the service sets it as each
/// block's <c>OutputDirectory</c>, so the GeoJSON of every sub-service lands in one
/// <c>Geojson</c> folder (see <see cref="AiracOutputPaths"/>).
/// </para>
/// </remarks>
public static class AiracService
{
	private const string LogSource = "AiracService";

	/// <summary>
	/// Whether the run's cycle folder already holds anything - an earlier run of the same cycle
	/// wrote there. The GUI asks this before a run, to offer to overwrite or delete those files
	/// (<see cref="AiracServiceSettings.ExistingOutput"/>).
	/// </summary>
	/// <param name="settings">The run's settings.</param>
	/// <returns><see langword="true"/> when the cycle folder exists and is not empty.</returns>
	public static bool HasExistingOutput(AiracServiceSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		string directory = settings.CycleOutputDirectory;
		return Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any();
	}

	/// <summary>
	/// Runs every selected sub-service, resolving the parsed cycle data for
	/// <see cref="AiracServiceSettings.SelectedCycle"/> from <see cref="AiracCycleDataCache.Instance"/>
	/// (awaiting an in-flight parse rather than starting a second one).
	/// </summary>
	/// <param name="settings">The run's cross-cutting choices and per-sub-service settings blocks.</param>
	/// <param name="progress">Optional per-sub-service progress for the run panel.</param>
	/// <param name="cancellationToken">Cancels the run.</param>
	/// <returns>The aggregated result.</returns>
	public static async Task<AiracServiceResult> RunAsync(
		AiracServiceSettings settings,
		IProgress<AiracServiceProgress>? progress = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(settings);

		progress?.Report(new AiracServiceProgress("AIRAC", $"Loading parsed data for cycle {settings.SelectedCycle.AiracCycleId}"));
		NasrCsvDataCollection nasrData = await AiracCycleDataCache.Instance
			.GetAsync(settings.SelectedCycle.AiracCycleId, cancellationToken)
			.ConfigureAwait(false);

		return await RunAsync(settings, nasrData, progress, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Runs every selected sub-service against the supplied parsed cycle data.
	/// </summary>
	/// <param name="settings">The run's cross-cutting choices and per-sub-service settings blocks.</param>
	/// <param name="nasrData">
	/// The parsed NASR CSV data for <see cref="AiracServiceSettings.SelectedCycle"/>.
	/// </param>
	/// <param name="progress">Optional per-sub-service progress for the run panel.</param>
	/// <param name="cancellationToken">Cancels before the next sub-service starts.</param>
	/// <returns>The aggregated result: each sub-service's result plus a combined warning list.</returns>
	/// <exception cref="ArgumentException">Thrown when <see cref="AiracServiceSettings.OutputDirectory"/> is blank.</exception>
	/// <exception cref="IOException">
	/// Thrown when <see cref="ExistingOutputAction.DeleteExisting"/> cannot delete the cycle
	/// folder (a file in it is open elsewhere, say). Nothing is written in that case, but the
	/// files deleted before the failure stay deleted.
	/// </exception>
	public static async Task<AiracServiceResult> RunAsync(
		AiracServiceSettings settings,
		NasrCsvDataCollection nasrData,
		IProgress<AiracServiceProgress>? progress = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(settings);
		ArgumentNullException.ThrowIfNull(nasrData);
		ArgumentException.ThrowIfNullOrWhiteSpace(settings.OutputDirectory, nameof(settings));

		Stopwatch stopwatch = Stopwatch.StartNew();
		List<ServiceMessage> messages = [];
		string outputDirectory = settings.CycleOutputDirectory;
		bool anySelected = settings.Airways is not null || settings.Airports is not null
			|| settings.Departures is not null || settings.Arrivals is not null || settings.Navaids is not null
			|| settings.ArtccBoundaries is not null || settings.Fixes is not null;

		// Only when there is something to write: a run with nothing selected must not empty the
		// folder and leave it that way.
		if (anySelected && settings.ExistingOutput == ExistingOutputAction.DeleteExisting && Directory.Exists(outputDirectory))
		{
			progress?.Report(new AiracServiceProgress("AIRAC", $"Deleting the files already in {outputDirectory}"));

			// Off the caller's thread: a Departures or Arrivals run leaves thousands of files, and
			// the GUI awaits this from its UI thread.
			await Task.Run(() => Directory.Delete(outputDirectory, recursive: true), cancellationToken).ConfigureAwait(false);
			AppLog.Info(LogSource, $"Deleted the earlier output in '{outputDirectory}' before the run.");
		}

		AirwayServiceResult? airwaysResult = await RunSubServiceAsync(
			settings.Airways, "Airways", "Building airway GeoJSON and alias output",
			block => AirwayService.Run(nasrData, block),
			result => $"{result.AirwayCount} airway(s), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		AirportServiceResult? airportsResult = await RunSubServiceAsync(
			settings.Airports, "Airports", "Building airport GeoJSON and alias output",
			block => AirportService.Run(nasrData, block),
			result => $"{result.AirportCount} airport(s), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		DepartureServiceResult? departuresResult = await RunSubServiceAsync(
			settings.Departures, "Departures", "Building departure procedure GeoJSON and alias output",
			block => DepartureService.Run(nasrData, block),
			result => $"{result.AirportProcedureCount} airport procedure(s), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		ArrivalServiceResult? arrivalsResult = await RunSubServiceAsync(
			settings.Arrivals, "Arrivals", "Building arrival procedure GeoJSON and alias output",
			block => ArrivalService.Run(nasrData, block),
			result => $"{result.AirportProcedureCount} airport procedure(s), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		NavaidServiceResult? navaidsResult = await RunSubServiceAsync(
			settings.Navaids, "NAVAIDs", "Building NAVAID GeoJSON and alias output",
			block => NavaidService.Run(nasrData, block),
			result => $"{result.NavaidCount} NAVAID(s), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		ArtccBoundaryServiceResult? artccBoundariesResult = await RunSubServiceAsync(
			settings.ArtccBoundaries, "ARTCC Boundaries", "Building ARTCC boundary GeoJSON output",
			block => ArtccBoundaryService.Run(nasrData, block),
			result => $"{result.LocationCount} ARTCC(s), {result.RingCount} boundary line(s), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		FixServiceResult? fixesResult = await RunSubServiceAsync(
			settings.Fixes, "Fixes", "Building fix GeoJSON output",
			block => FixService.Run(nasrData, block),
			result => $"{result.FixCount} fix(es), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		if (!anySelected)
		{
			const string message = "AIRAC Service run requested with no sub-service selected; nothing to do.";
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource, message));
			AppLog.Warning(LogSource, message);
		}

		stopwatch.Stop();

		return new AiracServiceResult
		{
			Messages = messages,
			Elapsed = stopwatch.Elapsed,
			OutputDirectory = outputDirectory,
			Airways = airwaysResult,
			Airports = airportsResult,
			Departures = departuresResult,
			Arrivals = arrivalsResult,
			Navaids = navaidsResult,
			ArtccBoundaries = artccBoundariesResult,
			Fixes = fixesResult,
		};

		// Runs one sub-service if it was selected (its settings block is not null), reporting
		// start and finish to the run panel and the log. Null when it was not selected.
		async Task<TResult?> RunSubServiceAsync<TResult>(
			IReadOnlyDictionary<string, string>? block,
			string name,
			string startMessage,
			Func<IReadOnlyDictionary<string, string>, TResult> run,
			Func<TResult, string> summarize)
			where TResult : ServiceResult
		{
			if (block is null)
			{
				return null;
			}

			cancellationToken.ThrowIfCancellationRequested();

			AppLog.Info(LogSource, $"AIRAC Service: running {name} for cycle {settings.SelectedCycle.AiracCycleId}.");
			progress?.Report(new AiracServiceProgress(name, startMessage));

			// Keys match ignoring case whatever dictionary the caller built. Every sub-service
			// writes into the one cycle folder, whatever the block said.
			Dictionary<string, string> caseInsensitive = new(block, StringComparer.OrdinalIgnoreCase)
			{
				["OutputDirectory"] = outputDirectory,
			};

			TResult result = await Task.Run(() => run(caseInsensitive), cancellationToken).ConfigureAwait(false);

			messages.AddRange(result.Messages);

			string summary = $"{name} complete: {summarize(result)}.";
			progress?.Report(new AiracServiceProgress(name, summary, 100));
			AppLog.Success(LogSource, summary);

			return result;
		}
	}
}
