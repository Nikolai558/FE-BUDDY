using System.Diagnostics;

using FeBuddy.Core.Models.NASR.CSV;
using FeBuddy.Core.Models.Services.Airac;
using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Models.Services.Airac.Airports;
using FeBuddy.Core.Models.Services.Airac.Airways;
using FeBuddy.Core.Models.Services.Airac.Departures;
using FeBuddy.Core.Services.Airac.Airports;
using FeBuddy.Core.Services.Airac.Airways;
using FeBuddy.Core.Services.Airac.Departures;
using FeBuddy.Core.Services.General;

namespace FeBuddy.Core.Services.Airac;

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
		List<ServiceMessage> messages = new();
		AirwayServiceResult? airwaysResult = null;
		AirportServiceResult? airportsResult = null;
		DepartureServiceResult? departuresResult = null;

		if (settings.Airways is { } airwayBlock)
		{
			cancellationToken.ThrowIfCancellationRequested();

			AppLog.Info(LogSource, $"AIRAC Service: running Airways for cycle {settings.SelectedCycle.AiracCycleId}, facility {settings.ArtccId}.");
			progress?.Report(new AiracServiceProgress("Airways", "Building airway GeoJSON and alias output"));

			// The dictionary contract is preserved end to end (Phase 1.3): AirwayService.Run
			// parses the block itself, so "what was saved" and "what runs" cannot diverge.
			Dictionary<string, string> block = new(airwayBlock, StringComparer.OrdinalIgnoreCase);

			airwaysResult = await Task.Run(() => AirwayService.Run(nasrData, block), cancellationToken).ConfigureAwait(false);

			messages.AddRange(airwaysResult.Messages);
			progress?.Report(new AiracServiceProgress(
				"Airways",
				$"Airways complete: {airwaysResult.AirwayCount} airway(s), {airwaysResult.GeojsonFilesWritten.Count} GeoJSON file(s).",
				100));
			AppLog.Success(LogSource, $"Airways complete: {airwaysResult.AirwayCount} airway(s), {airwaysResult.GeojsonFilesWritten.Count} file(s).");
		}

		if (settings.Airports is { } airportBlock)
		{
			cancellationToken.ThrowIfCancellationRequested();

			AppLog.Info(LogSource, $"AIRAC Service: running Airports for cycle {settings.SelectedCycle.AiracCycleId}, facility {settings.ArtccId}.");
			progress?.Report(new AiracServiceProgress("Airports", "Building airport GeoJSON and alias output"));

			Dictionary<string, string> block = new(airportBlock, StringComparer.OrdinalIgnoreCase);

			airportsResult = await Task.Run(() => AirportService.Run(nasrData, block), cancellationToken).ConfigureAwait(false);

			messages.AddRange(airportsResult.Messages);
			progress?.Report(new AiracServiceProgress(
				"Airports",
				$"Airports complete: {airportsResult.AirportCount} airport(s), {airportsResult.GeojsonFilesWritten.Count} GeoJSON file(s).",
				100));
			AppLog.Success(LogSource, $"Airports complete: {airportsResult.AirportCount} airport(s), {airportsResult.GeojsonFilesWritten.Count} file(s).");
		}

		if (settings.Departures is { } departureBlock)
		{
			cancellationToken.ThrowIfCancellationRequested();

			AppLog.Info(LogSource, $"AIRAC Service: running Departures for cycle {settings.SelectedCycle.AiracCycleId}, facility {settings.ArtccId}.");
			progress?.Report(new AiracServiceProgress("Departures", "Building departure procedure GeoJSON and alias output"));

			Dictionary<string, string> block = new(departureBlock, StringComparer.OrdinalIgnoreCase);

			departuresResult = await Task.Run(() => DepartureService.Run(nasrData, block), cancellationToken).ConfigureAwait(false);

			messages.AddRange(departuresResult.Messages);
			progress?.Report(new AiracServiceProgress(
				"Departures",
				$"Departures complete: {departuresResult.AirportProcedureCount} airport procedure(s), {departuresResult.GeojsonFilesWritten.Count} GeoJSON file(s).",
				100));
			AppLog.Success(LogSource, $"Departures complete: {departuresResult.AirportProcedureCount} airport procedure(s), {departuresResult.GeojsonFilesWritten.Count} file(s).");
		}

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
			ExcludedAirwayIds = airwaysResult?.ExcludedAirwayIds ?? Array.Empty<string>(),
		};
	}
}
