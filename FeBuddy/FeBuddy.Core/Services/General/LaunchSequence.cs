using FeBuddy.Core.Configuration;
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
/// Runs FE-Buddy's launch sequence off the UI thread (see <c>Developer_Notes.md</c> -&gt;
/// LAUNCH PROCESSES). Every step logs its start and outcome through <see cref="AppLog"/>; a
/// step that fails degrades the feature that depends on it and is never allowed to block launch.
/// </summary>
/// <remarks>

/// Steps run in dependency order, not list order: temp clear and config read first (the
/// version check and News need the config; the AIRAC download uses the temp folder), then the
/// UTC time / internet check (AIRAC needs the time, and all three network steps use the
/// internet flag), then version, AIRAC and News concurrently since none depends on another.
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
			() =>
			{
				UserConfigFile.ReadAll();

				// App-wide output preferences are applied as soon as they are readable, so any
				// file written this session follows them.
				OutputFormatting.LoadFromUserConfig();
				return true;
			},
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

		// Everything below needs the time / internet result above, but none of it needs each
		// other, so it all starts together. Each task publishes its own result to
		// AppEnvironment the moment it finishes - a fast check is never held up behind the slow
		// AIRAC pipeline. RunStepAsync catches every exception, so WhenAll cannot fault.
		Task<VersionCheckResult> versionTask = CheckVersionAsync(currentVersion, time, progress, cancellationToken);
		Task airacTask = PrepareAiracAsync(time, progress, cancellationToken);
		Task<NewsCheckResult> newsTask = CheckNewsAsync(time, progress, cancellationToken);

		await Task.WhenAll(versionTask, airacTask, newsTask).ConfigureAwait(false);

		VersionCheckResult version = versionTask.Result;

		AppEnvironment.LaunchCompleted = true;
		AppEnvironment.RaiseChanged();

		progress?.Report(new LaunchProgress(LaunchStep.Complete, LaunchStepStatus.Succeeded, "Launch complete"));
		AppLog.Success(LogSource, "Launch sequence complete.");

		return new LaunchResult(time, version, tempClearFailures);
	}

	// Step 3 - version: ask GitHub whether a newer release exists on the user's channel.
	private static async Task<VersionCheckResult> CheckVersionAsync(
		string currentVersion,
		UtcTimeCheckResult time,
		IProgress<LaunchProgress>? progress,
		CancellationToken cancellationToken)
	{
		UpdateChannel channel = VersionCheckResult.ParseChannel(UserConfigFile.GetValue("General.UpdateChannel"));

		VersionCheckResult version = await RunStepAsync(
			progress, LaunchStep.CheckVersion, "Checking for a newer version",
			() => VersionCheck.RunAsync(currentVersion, channel, time.HasInternetConnection, cancellationToken: cancellationToken),
			defaultValue: () => new VersionCheckResult(currentVersion, null, false, channel, CheckSucceeded: false, "Version check did not run."))
			.ConfigureAwait(false);

		AppEnvironment.Version = version;
		AppEnvironment.RaiseChanged();
		return version;
	}

	// Step 5 - AIRAC data pipeline: probe, download and parse previous/current/next.
	private static Task PrepareAiracAsync(
		UtcTimeCheckResult time,
		IProgress<LaunchProgress>? progress,
		CancellationToken cancellationToken) =>
		RunStepAsync(
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
			defaultValue: () => AiracCycleReadiness.Waiting);

	// Step 6 - News: fetch (GitHub raw when online, bundled copy otherwise), parse, and count
	// posts newer than General.NewsLastOpen.
	private static async Task<NewsCheckResult> CheckNewsAsync(
		UtcTimeCheckResult time,
		IProgress<LaunchProgress>? progress,
		CancellationToken cancellationToken)
	{
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
		return news;
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
