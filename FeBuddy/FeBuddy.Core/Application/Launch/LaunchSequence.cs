using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Launch.Models;
using FeBuddy.Core.Application.News;
using FeBuddy.Core.Application.News.Models;
using FeBuddy.Core.Application.Updates;
using FeBuddy.Core.Application.Updates.Models;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Configuration.Models;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Platform;
using FeBuddy.Core.Infrastructure.Platform.Models;
using FeBuddy.Versioning.Models;

namespace FeBuddy.Core.Application.Launch;

/// <summary>
/// Runs FE-Buddy's launch steps off the UI thread. Every step logs its start and outcome
/// through <see cref="AppLog"/>; a step that fails degrades the feature that depends on it and
/// never blocks launch.
/// </summary>
/// <remarks>
/// <para>
/// Steps run in dependency order, not list order: temp clear and config read first (the
/// version check and News need the config; the AIRAC download uses the temp folder), then the
/// check for FE-Buddy 2.x's GitHub token variable (it needs the config, nothing needs it), the
/// removal of 2.x's old Squirrel install and then of 2.x's dead shortcuts (the uninstall leaves its
/// shortcuts behind; nothing needs either), then the UTC time / internet check (AIRAC needs the time, and all three network steps use the
/// internet flag), then version, AIRAC and News concurrently since none depends on another.
/// </para>
/// <para>
/// A dev launch (<see cref="LaunchOptions.IsDevLaunch"/>) runs only the <see cref="OptionalLaunchStep"/>s
/// it was given, and logs the ones it leaves out; the AIRAC cycles are always tracked, so a cycle it
/// doesn't load is loaded when something asks for it. Clearing the temporary folder and reading the
/// settings always run.
/// </para>
/// <para>
/// The app reads the settings itself, on the UI thread, before it builds any page
/// (<see cref="ReadSettings"/>, then <see cref="LaunchOptions.SettingsRead"/>): a page that reads its
/// settings while it is built would otherwise show defaults, and saving it would write them over the
/// user's. It is a few small files.
/// </para>
/// </remarks>
public static class LaunchSequence
{
	private const string LogSource = "Launch";

	/// <summary>
	/// Publishes how FE-Buddy was started (<see cref="AppEnvironment.LaunchOptions"/>). The app calls
	/// this before it builds its window, so everything built sees it; <see cref="RunAsync"/> calls it too.
	/// </summary>
	/// <param name="options">The options.</param>
	public static void UseOptions(LaunchOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		AppEnvironment.LaunchOptions = options;
	}

	/// <summary>
	/// Executes the launch steps <paramref name="options"/> asks for: every one in a normal launch.
	/// Never throws; a failing step is caught, logged, and the sequence continues.
	/// </summary>
	/// <param name="currentVersion">The running application's version string (from the entry assembly).</param>
	/// <param name="options">A normal launch, or a dev launch and the steps it runs.</param>
	/// <param name="cancellationToken">Cancels remaining steps.</param>
	/// <returns>When every step has finished; each publishes its outcome to <see cref="AppEnvironment"/>.</returns>
	public static async Task RunAsync(string currentVersion, LaunchOptions options, CancellationToken cancellationToken = default)
	{
		UseOptions(options);
		AppLog.Info(LogSource, "Launch sequence started.");

		if (options.IsDevLaunch)
		{
			AppLog.Info(LogSource, OptionalLaunchSteps.DescribeDevLaunch(options.Steps));
		}

		RunStep(
			"Clearing the temporary workspace",
			() => TempWorkspace.ClearOnLaunch(),
			defaultValue: 0);

		if (!options.SettingsRead)
		{
			ReadSettings(currentVersion, options);
		}

		// FE-Buddy 2.x kept a GitHub token in plain text in an environment variable. This needs the
		// settings just read (whether the user has been told already); the shell tells them as soon
		// as it can.
		if (options.Runs(OptionalLaunchStep.LegacyGitHubTokenNotice))
		{
			RunStep(
				"Checking for FE-Buddy 2.x's GitHub token variable",
				() => LegacyGitHubTokenNotice.Check(),
				defaultValue: []);
		}
		else
		{
			Skip("Checking for FE-Buddy 2.x's GitHub token variable");
		}

		if (options.Runs(OptionalLaunchStep.RemoveLegacyInstall))
		{
			// A copy of FE-Buddy 2.x that Squirrel installed can still be here when 3.x arrived without
			// 2.9.x's MSI - two FE-Buddys on the PC. Usually just a File.Exists; nothing needs this.
			RunStep(
				"Removing FE-Buddy 2.x's old install",
				() => LegacySquirrelInstall.Remove(),
				defaultValue: LegacySquirrelCleanupResult.NotFound);

			// FE-Buddy 2.x's Desktop and Start menu shortcuts outlive the copy they open - including the
			// ones Squirrel's uninstaller just left. Nothing needs this.
			RunStep(
				"Removing FE-Buddy 2.x's dead shortcuts",
				() => LegacySquirrelShortcuts.RemoveDead(),
				defaultValue: 0);
		}
		else
		{
			Skip("Removing FE-Buddy 2.x's old install");
			Skip("Removing FE-Buddy 2.x's dead shortcuts");
		}

		AppEnvironment.RaiseChanged();

		UtcTimeCheckResult time;

		if (options.Runs(OptionalLaunchStep.CheckClockAndInternet))
		{
			time = await RunStepAsync(
				"Checking the clock and internet connection",
				() => UtcTimeCheck.RunAsync(AppEnvironment.HttpClientForTesting, cancellationToken),
				defaultValue: () => new UtcTimeCheckResult(DateTime.UtcNow, UtcTimeSource.LocalClock, HasInternetConnection: false))
				.ConfigureAwait(false);

			AppEnvironment.LaunchUtcNow = time.UtcNow;
			AppEnvironment.LaunchUtcSource = time.Source;
		}
		else
		{
			// The PC's clock, and online until something finds otherwise. LaunchUtcNow stays unset, so
			// the date follows the PC's clock, and so the shell can say the check was left out.
			Skip("Checking the clock and internet connection");
			time = new UtcTimeCheckResult(DateTime.UtcNow, UtcTimeSource.LocalClock, HasInternetConnection: true);
		}

		AppEnvironment.HasInternetConnection = time.HasInternetConnection;
		AppEnvironment.RaiseChanged();

		// Everything below needs the time / internet result above, but none of it needs each
		// other, so it all starts together. Each task publishes its own result to
		// AppEnvironment the moment it finishes - a fast check is never held up behind the slow
		// AIRAC pipeline. RunStepAsync catches every exception, so WhenAll cannot fault.
		Task versionTask = options.Runs(OptionalLaunchStep.CheckForNewerVersion)
			? CheckVersionAsync(currentVersion, time, cancellationToken)
			: SkipVersionCheck(currentVersion);
		Task airacTask = PrepareAiracAsync(AiracPlanFor(options), cancellationToken);

		if (!options.Runs(OptionalLaunchStep.DownloadNews))
		{
			AppLog.Info(LogSource, "Downloading News: skipped (dev launch). The copy built into FE-Buddy is shown.");
		}

		Task newsTask = CheckNewsAsync(
			online: time.HasInternetConnection && options.Runs(OptionalLaunchStep.DownloadNews), cancellationToken);

		await Task.WhenAll(versionTask, airacTask, newsTask).ConfigureAwait(false);

		AppEnvironment.LaunchCompleted = true;
		AppEnvironment.RaiseChanged();

		AppLog.Success(LogSource, "Launch sequence complete.");
	}

	/// <summary>
	/// Reads the saved settings, and applies what launch needs from them: whether to welcome the user,
	/// the output preferences, and the update channel a first launch keeps. The app calls this on the
	/// UI thread before it builds any page, then passes <see cref="LaunchOptions.SettingsRead"/> so
	/// <see cref="RunAsync"/> leaves it out. Never throws.
	/// </summary>
	/// <param name="currentVersion">The running application's version string.</param>
	/// <param name="options">How FE-Buddy was started.</param>
	/// <returns><see langword="true"/> when the step ran to the end.</returns>
	public static bool ReadSettings(string currentVersion, LaunchOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		return RunStep(
			"Reading saved settings",
			() =>
			{
				// Every profile and Shared.json: a file a newer FE-Buddy saved is set aside, and older ones are
				// brought forward together, before the profile in use is read.
				UserConfigFile.CheckLayouts();
				UserConfigReadResult read = UserConfigFile.ReadAll();

				// No settings to read is FE-Buddy 3's first launch on this PC. Worked out before the
				// update channel below is saved, since that writes the settings file. A dev launch
				// that leaves the welcome out doesn't look, so it doesn't count as shown either.
				if (options.Runs(OptionalLaunchStep.WelcomeWindow))
				{
					WelcomeNotice.Check(read);
				}

				// App-wide output preferences are applied as soon as they are readable, so any
				// file written this session follows them.
				OutputFormatting.LoadFromUserConfig();

				// The first launch keeps the running build's update channel, so no later update
				// can move the user to another one; only Settings does.
				UpdateChannelSetting.SaveDefaultIfUnset(currentVersion, read);
				return true;
			},
			defaultValue: false);
	}

	// Ask GitHub whether a newer release exists on the user's channel.
	private static async Task CheckVersionAsync(string currentVersion, UtcTimeCheckResult time, CancellationToken cancellationToken)
	{
		ReleaseChannel channel = UpdateChannelSetting.Read(currentVersion);

		VersionCheckResult version = await RunStepAsync(
			"Checking for a newer version",
			() => VersionCheck.RunAsync(currentVersion, channel, time.HasInternetConnection, AppEnvironment.HttpClientForTesting, cancellationToken),
			defaultValue: () => new VersionCheckResult(currentVersion, null, false, channel, CheckSucceeded: false, "Version check did not run."))
			.ConfigureAwait(false);

		AppEnvironment.Version = version;
		AppEnvironment.RaiseChanged();
	}

	// A dev launch that leaves out the version check publishes a result that says so: the version
	// still shows, and nothing claims an update state.
	private static Task SkipVersionCheck(string currentVersion)
	{
		Skip("Checking for a newer version");

		AppEnvironment.Version = VersionCheckResult.Skipped(currentVersion, UpdateChannelSetting.Read(currentVersion));
		AppEnvironment.RaiseChanged();
		return Task.CompletedTask;
	}

	// What the AIRAC pipeline does: everything, unless a dev launch leaves some of it out.
	private static AiracLaunchPlan AiracPlanFor(LaunchOptions options) => new(
		Download: options.Runs(OptionalLaunchStep.DownloadAiracData),
		Parse: options.Runs(OptionalLaunchStep.ParseAiracData),
		Prune: options.Runs(OptionalLaunchStep.RemoveOldAiracCycles));

	// Probe, download and parse the previous, current and next AIRAC cycles, as of the time
	// check just published to AppEnvironment. The three are tracked whatever the plan.
	private static Task PrepareAiracAsync(AiracLaunchPlan plan, CancellationToken cancellationToken) =>
		RunStepAsync(
			"Preparing AIRAC data (download + parse)",
			async () =>
			{
				AiracCycleInfo previous = AppEnvironment.GetAiracCycle(AiracCyclePosition.Previous);
				AiracCycleInfo current = AppEnvironment.GetAiracCycle(AiracCyclePosition.Current);
				AiracCycleInfo next = AppEnvironment.GetAiracCycle(AiracCyclePosition.Next);

				await AiracCycleDataCache.Instance
					.PrepareCyclesAsync(previous, current, next, plan, cancellationToken)
					.ConfigureAwait(false);

				return AiracCycleDataCache.Instance.ComputeReadiness();
			},
			defaultValue: () => AiracCycleReadiness.Waiting);

	// Fetch News (GitHub when online, the bundled copy otherwise - and in a dev launch that leaves
	// out downloading it), parse it, and count the posts newer than General.NewsLastOpen.
	private static async Task CheckNewsAsync(bool online, CancellationToken cancellationToken)
	{
		NewsCheckResult news = await RunStepAsync(
			"Checking for news",
			() => NewsService.CheckAsync(
				UserConfigFile.GetValue(UserConfigKeys.NewsLastOpen),
				online,
				AppEnvironment.HttpClientForTesting,
				cancellationToken),
			defaultValue: () => new NewsCheckResult([], null, 0, ParseSucceeded: false))
			.ConfigureAwait(false);

		AppEnvironment.News = news;
		AppEnvironment.RaiseChanged();
	}

	// A step a dev launch leaves out: logged, never a failure.
	private static void Skip(string description) =>
		AppLog.Info(LogSource, $"{description}: skipped (dev launch).");

	private static T RunStep<T>(string description, Func<T> action, T defaultValue)
	{
		AppLog.Info(LogSource, $"{description}...");

		try
		{
			return action();
		}
		catch (Exception ex)
		{
			AppLog.Warning(LogSource, $"{description} failed: {ex.Message}. Continuing launch.");
			return defaultValue;
		}
	}

	private static async Task<T> RunStepAsync<T>(string description, Func<Task<T>> action, Func<T> defaultValue)
	{
		AppLog.Info(LogSource, $"{description}...");

		try
		{
			return await action().ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			AppLog.Warning(LogSource, $"{description} failed: {ex.Message}. Continuing launch.");
			return defaultValue();
		}
	}
}
