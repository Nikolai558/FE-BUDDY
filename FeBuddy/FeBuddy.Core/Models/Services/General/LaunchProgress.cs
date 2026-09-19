namespace FeBuddy.Core.Models.Services.General;

/// <summary>
/// The ordered steps of the application launch sequence (see <c>Developer_Notes.md</c> -&gt;
/// LAUNCH PROCESSES). Steps run off the UI thread; a failure in any one degrades the
/// dependent feature but never blocks launch.
/// </summary>
public enum LaunchStep
{
	/// <summary>Wipe <c>%TEMP%\FE-Buddy</c>.</summary>
	ClearTempWorkspace = 0,

	/// <summary>Read <c>UserConfig.json</c> into memory.</summary>
	ReadUserConfig = 1,

	/// <summary>Establish UTC "now" from the network and decide whether the machine is online.</summary>
	CheckUtcTimeAndInternet = 2,

	/// <summary>Ask GitHub whether a newer release exists on the user's channel.</summary>
	CheckVersion = 3,

	/// <summary>Ensure previous/current/next AIRAC cycles are downloaded and parsed (Phase 2 fills this in).</summary>
	PrepareAiracData = 4,

	/// <summary>Check for a newer News post (Phase 6.1 fills this in).</summary>
	CheckNews = 5,

	/// <summary>The sequence has finished.</summary>
	Complete = 6,
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
