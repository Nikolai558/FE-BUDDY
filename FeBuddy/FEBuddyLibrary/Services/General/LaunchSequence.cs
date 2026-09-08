using FEBuddyLibrary.Helpers;
using FEBuddyLibrary.Models.Services.General;

namespace FEBuddyLibrary.Services.General;

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
/// This is the skeleton. Step 5 (AIRAC data pipeline) and step 6 (News) are wired up in
/// Phase 2 and Phase 6.1 respectively - here they are logged placeholders so the sequence and
/// its logging exist end to end first.
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

		// Step 5 - AIRAC data pipeline. Wired up in Phase 2 (AiracCycleDataCache); logged here
		// so the launch narration is complete from the start.
		progress?.Report(new LaunchProgress(LaunchStep.PrepareAiracData, LaunchStepStatus.Started, "Preparing AIRAC data"));
		AppLog.Info(LogSource, "AIRAC data pipeline: not yet wired up (Phase 2). Skipping.");
		progress?.Report(new LaunchProgress(LaunchStep.PrepareAiracData, LaunchStepStatus.Skipped, "AIRAC data pipeline not yet implemented"));

		// Step 6 - News. Wired up in Phase 6.1 (NewsService).
		progress?.Report(new LaunchProgress(LaunchStep.CheckNews, LaunchStepStatus.Started, "Checking for news"));
		AppLog.Info(LogSource, "News check: not yet wired up (Phase 6.1). Skipping.");
		progress?.Report(new LaunchProgress(LaunchStep.CheckNews, LaunchStepStatus.Skipped, "News check not yet implemented"));

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
