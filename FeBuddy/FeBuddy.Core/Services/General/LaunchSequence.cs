using FeBuddy.Core.Helpers;
using FeBuddy.Core.Models.Services.Airac;
using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Services.Airac;

namespace FeBuddy.Core.Services.General;

/// <summary>
/// The result of <see cref="LaunchSequence.RunAsync"/>.
/// </summary>
/// <param name="Time">The UTC clock / connectivity check outcome.</param>
/// <param name="Version">The version-check outcome.</param>
/// <param name="TempClearFailures">How many temp entries could not be deleted (0 is normal).</param>
public record LaunchResult(UtcTimeCheckResult Time, VersionCheckResult Version, int TempClearFailures);

/// <summary>
/// Runs FE-Buddy's launch sequence off the UI thread, in the order set out in
/// <c>Developer_Notes.md</c> -&gt; LAUNCH PROCESSES. Every step logs its start and outcome
/// through <see cref="AppLog"/>; a step that fails degrades the feature that depends on it and
/// is never allowed to block launch.
/// </summary>
/// <remarks>
/// Every step is wired to its real service: the temp clear, the saved-settings read, the UTC
/// clock / internet check, the version check, the AIRAC download-and-parse pipeline
/// (<see cref="AiracCycleDataCache"/>), and the News check (<see cref="NewsService"/>).
/// </remarks>
public static class LaunchSequence
{
	private const string LogSource = "Launch";

	/// <summary>
	/// Executes the launch steps. Never throws; a failing step is caught, logged, and the
	/// sequence continues.
	/// </summary>
	/// <param name="currentVersion">The running application's version string (from the entry assembly).</param>
	/// <param name="progress">Optional per-step progress for the shell.</param>
	/// <param name="cancellationToken">Cancels remaining steps.</param>
	/// <returns>The aggregated launch result; also published to <see cref="AppEnvironment"/>.</returns>
	public static async Task<LaunchResult> RunAsync(
		string currentVersion,
		IProgress<LaunchProgress>? progress = null,
		CancellationToken cancellationToken = default)
	{
		AppLog.Info(LogSource, "Launch sequence started.");

		int tempClearFailures = RunStep(
			progress, LaunchStep.ClearTempWorkspace, "Clearing the temporary workspace",
			() => TempWorkspace.ClearOnLaunch(),
			defaultValue: 0);

		RunStep(
			progress, LaunchStep.ReadUserConfig, "Reading saved settings",
			() => { UserConfigFile.ReadAll(); return true; },
			defaultValue: false);

		UtcTimeCheckResult time = await RunStepAsync(
			progress, LaunchStep.CheckUtcTimeAndInternet, "Checking the clock and internet connection",
			() => UtcTimeCheck.RunAsync(cancellationToken: cancellationToken),
			defaultValue: () => new UtcTimeCheckResult(DateTime.UtcNow, UtcTimeSource.LocalClock, HasInternetConnection: false))
			.ConfigureAwait(false);

		AppEnvironment.HasInternetConnection = time.HasInternetConnection;
		AppEnvironment.LaunchUtcNow = time.UtcNow;
		AppEnvironment.LaunchUtcSource = time.Source;
		AppEnvironment.RaiseChanged();

		UpdateChannel channel = VersionCheckResult.ParseChannel(UserConfigFile.GetValue("General.UpdateChannel"));

		VersionCheckResult version = await RunStepAsync(
			progress, LaunchStep.CheckVersion, "Checking for a newer version",
			() => VersionCheck.RunAsync(currentVersion, channel, time.HasInternetConnection, cancellationToken: cancellationToken),
			defaultValue: () => new VersionCheckResult(currentVersion, null, false, channel, CheckSucceeded: false, "Version check did not run."))
			.ConfigureAwait(false);

		AppEnvironment.Version = version;
		AppEnvironment.RaiseChanged();

		// Step 5 - AIRAC data pipeline: probe, download and parse previous/current/next.
		await RunStepAsync(
			progress, LaunchStep.PrepareAiracData, "Preparing AIRAC data (download + parse)",
			async () =>
			{
				DateOnly asOfUtc = DateOnly.FromDateTime(time.UtcNow);
				AiracCycleInfo previous = AiracCycleResolver.GetCycle(AiracCyclePosition.Previous, asOfUtc);
				AiracCycleInfo current = AiracCycleResolver.GetCycle(AiracCyclePosition.Current, asOfUtc);
				AiracCycleInfo next = AiracCycleResolver.GetCycle(AiracCyclePosition.Next, asOfUtc);

				await AiracCycleDataCache.Instance
					.PrepareCyclesAsync(previous, current, next, cancellationToken)
					.ConfigureAwait(false);

				return AiracCycleDataCache.Instance.ComputeReadiness();
			},
			defaultValue: () => AiracCycleReadiness.Waiting)
			.ConfigureAwait(false);

		// Step 6 - News: fetch (GitHub raw when online, bundled copy otherwise), parse, and
		// count posts newer than General.NewsLastOpen.
		NewsCheckResult news = await RunStepAsync(
			progress, LaunchStep.CheckNews, "Checking for news",
			() => NewsService.CheckAsync(
				UserConfigFile.GetValue("General.NewsLastOpen"),
				time.HasInternetConnection,
				cancellationToken: cancellationToken),
			defaultValue: () => new NewsCheckResult(Array.Empty<NewsPost>(), null, 0, ParseSucceeded: false, FromNetwork: false))
			.ConfigureAwait(false);

		AppEnvironment.News = news;
		AppEnvironment.RaiseChanged();

		AppEnvironment.LaunchCompleted = true;
		AppEnvironment.RaiseChanged();

		progress?.Report(new LaunchProgress(LaunchStep.Complete, LaunchStepStatus.Succeeded, "Launch complete"));
		AppLog.Success(LogSource, "Launch sequence complete.");

		return new LaunchResult(time, version, tempClearFailures);
	}

	private static T RunStep<T>(
		IProgress<LaunchProgress>? progress,
		LaunchStep step,
		string description,
		Func<T> action,
		T defaultValue)
	{
		progress?.Report(new LaunchProgress(step, LaunchStepStatus.Started, description));
		AppLog.Info(LogSource, $"{description}...");

		try
		{
			T result = action();
			progress?.Report(new LaunchProgress(step, LaunchStepStatus.Succeeded, $"{description} - done"));
			return result;
		}
		catch (Exception ex)
		{
			AppLog.Warning(LogSource, $"{description} failed: {ex.Message}. Continuing launch.");
			progress?.Report(new LaunchProgress(step, LaunchStepStatus.Failed, $"{description} failed: {ex.Message}"));
			return defaultValue;
		}
	}

	private static async Task<T> RunStepAsync<T>(
		IProgress<LaunchProgress>? progress,
		LaunchStep step,
		string description,
		Func<Task<T>> action,
		Func<T> defaultValue)
	{
		progress?.Report(new LaunchProgress(step, LaunchStepStatus.Started, description));
		AppLog.Info(LogSource, $"{description}...");

		try
		{
			T result = await action().ConfigureAwait(false);
			progress?.Report(new LaunchProgress(step, LaunchStepStatus.Succeeded, $"{description} - done"));
			return result;
		}
		catch (Exception ex)
		{
			AppLog.Warning(LogSource, $"{description} failed: {ex.Message}. Continuing launch.");
			progress?.Report(new LaunchProgress(step, LaunchStepStatus.Failed, $"{description} failed: {ex.Message}"));
			return defaultValue();
		}
	}
}
