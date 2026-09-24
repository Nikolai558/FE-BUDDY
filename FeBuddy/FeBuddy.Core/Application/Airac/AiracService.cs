using System.Diagnostics;

using FeBuddy.Core.Application.Airac.Airports;
using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Application.Airac.Airways;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Airac.Departures;
using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Application.Airac;

/// <summary>
/// The AIRAC Service orchestrator. The GUI calls this once per "Run AIRAC Service"; it
/// dispatches to each selected sub-service (Airways is the only one with a backend today) and
/// aggregates the results.
/// </summary>
/// <remarks>
/// <para>
/// The folder is <c>Services/Airac/</c> and this type is <c>AiracService</c> - deliberately
/// not <c>AiracService.AiracService</c> in one chain. The user-facing name stays
/// "AIRAC Service".
/// </para>
/// <para>
/// This orchestrator does not parse NASR data. The caller supplies the already-parsed
/// <see cref="NasrCsvDataCollection"/> for <see cref="AiracServiceSettings.SelectedCycle"/>.
/// Phase 2 adds the cycle cache (<c>AiracCycleDataCache</c>) that will resolve it from the
/// cycle ID and await an in-flight parse rather than starting a second one.
/// </para>
/// </remarks>
public static class AiracService
{
	private const string LogSource = "AiracService";

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
	/// The parsed NASR CSV data for <see cref="AiracServiceSettings.SelectedCycle"/>. Never
	/// parsed here - it comes from the cycle cache (Phase 2).
	/// </param>
	/// <param name="progress">Optional per-sub-service progress for the run panel.</param>
	/// <param name="cancellationToken">Cancels before the next sub-service starts.</param>
	/// <returns>The aggregated result: each sub-service's result plus a combined warning list.</returns>
	public static async Task<AiracServiceResult> RunAsync(
		AiracServiceSettings settings,
		NasrCsvDataCollection nasrData,
		IProgress<AiracServiceProgress>? progress = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(settings);
		ArgumentNullException.ThrowIfNull(nasrData);

		Stopwatch stopwatch = Stopwatch.StartNew();
		List<ServiceMessage> messages = [];
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

		if (airwaysResult is null && airportsResult is null && departuresResult is null)
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
			Airways = airwaysResult,
			Airports = airportsResult,
			Departures = departuresResult,
			ExcludedAirwayIds = airwaysResult?.ExcludedAirwayIds ?? [],
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

			// Keys match ignoring case whatever dictionary the caller built.
			Dictionary<string, string> caseInsensitive = new(block, StringComparer.OrdinalIgnoreCase);

			TResult result = await Task.Run(() => run(caseInsensitive), cancellationToken).ConfigureAwait(false);

			messages.AddRange(result.Messages);

			string summary = $"{name} complete: {summarize(result)}.";
			progress?.Report(new AiracServiceProgress(name, summary, 100));
			AppLog.Success(LogSource, summary);

			return result;
		}
	}
}
