namespace FeBuddy.Core.Application.Launch.Models;

/// <summary>
/// The launch steps a dev launch can leave out (see <see cref="LaunchOptions"/>); a normal launch runs
/// every one. Carrying out a reset asked for in Settings, the log, clearing the temporary folder and
/// reading the settings always run, so they aren't here.
/// </summary>
[Flags]
public enum OptionalLaunchStep
{
	/// <summary>No step.</summary>
	None = 0,

	/// <summary>Check the clock and internet connection. Left out, the PC's clock is used and the PC is taken to be online.</summary>
	CheckClockAndInternet = 1 << 0,

	/// <summary>
	/// Download AIRAC data: the check of whether the FAA has published each cycle, and each cycle's NASR
	/// data and d-TPP Metafile when they aren't on disk. Left out, nothing AIRAC-related uses the
	/// network at launch.
	/// </summary>
	DownloadAiracData = 1 << 1,

	/// <summary>Parse AIRAC data: every cycle on disk, once downloaded. Left out, nothing is parsed at launch.</summary>
	ParseAiracData = 1 << 2,

	/// <summary>Remove the AIRAC cycles on disk other than the previous, current and next.</summary>
	RemoveOldAiracCycles = 1 << 3,

	/// <summary>Check for a newer version of FE-Buddy (the update badge).</summary>
	CheckForNewerVersion = 1 << 4,

	/// <summary>Download News. Left out, the copy built into FE-Buddy is used.</summary>
	DownloadNews = 1 << 5,

	/// <summary>Check whether the saved GitHub tokens still work or expire soon.</summary>
	CheckGitHubTokens = 1 << 6,

	/// <summary>The one-time notice about FE-Buddy 2.x's GitHub token variable.</summary>
	LegacyGitHubTokenNotice = 1 << 7,

	/// <summary>The one-time welcome to FE-Buddy 3.</summary>
	WelcomeWindow = 1 << 8,

	/// <summary>Remove FE-Buddy 2.x's old install and its dead shortcuts.</summary>
	RemoveLegacyInstall = 1 << 9,

	/// <summary>Every step.</summary>
	All = (1 << 10) - 1,
}
