namespace FeBuddy.Core.Application.Launch.Models;

/// <summary>
/// The steps of the launch sequence, in the order they start. Steps run off the UI thread; a
/// failure in any one degrades the dependent feature but never blocks launch.
/// </summary>
public enum LaunchStep
{
	/// <summary>Wipe <c>%TEMP%\FE-Buddy</c>.</summary>
	ClearTempWorkspace = 0,

	/// <summary>Read <c>UserConfig.json</c> into memory.</summary>
	ReadUserConfig = 1,

	/// <summary>
	/// See whether FE-Buddy 2.x's <c>FEBUDDY_GITHUB_TOKEN</c> environment variable is still set,
	/// for the one-time notice about it (<see cref="LegacyGitHubTokenNotice"/>).
	/// </summary>
	CheckLegacyGitHubToken = 2,

	/// <summary>
	/// Delete the Desktop and Start menu shortcuts FE-Buddy 2.8.x left that no longer open anything
	/// (<see cref="Infrastructure.Platform.LegacySquirrelShortcuts"/>).
	/// </summary>
	RemoveLegacyShortcuts = 3,

	/// <summary>Establish UTC "now" from the network and decide whether the machine is online.</summary>
	CheckUtcTimeAndInternet = 4,

	/// <summary>Ask GitHub whether a newer release exists on the user's channel.</summary>
	CheckVersion = 5,

	/// <summary>Ensure the previous, current and next AIRAC cycles are downloaded and parsed.</summary>
	PrepareAiracData = 6,

	/// <summary>Check for a newer News post.</summary>
	CheckNews = 7,

	/// <summary>The sequence has finished.</summary>
	Complete = 8,
}

/// <summary>The state of a <see cref="LaunchStep"/> as it is reported.</summary>
public enum LaunchStepStatus
{
	/// <summary>The step has just begun.</summary>
	Started = 0,

	/// <summary>The step finished successfully.</summary>
	Succeeded = 1,

	/// <summary>The step failed; its dependent feature is degraded.</summary>
	Failed = 2,

	/// <summary>The step was skipped (e.g. offline), which is not a failure.</summary>
	Skipped = 3,
}

/// <summary>
/// A single progress notification from <c>LaunchSequence.RunAsync</c>.
/// </summary>
/// <param name="Step">Which launch step this is about.</param>
/// <param name="Status">Where that step is.</param>
/// <param name="Message">A short human-readable description for the shell and the log.</param>
public record LaunchProgress(LaunchStep Step, LaunchStepStatus Status, string Message);
