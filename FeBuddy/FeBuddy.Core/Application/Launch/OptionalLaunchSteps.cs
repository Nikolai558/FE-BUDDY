using FeBuddy.Core.Application.Launch.Models;

namespace FeBuddy.Core.Application.Launch;

/// <summary>
/// The optional launch steps in the order the Developer card lists them, with the name and note it
/// shows for each. The dev launch's log line names them the same way.
/// </summary>
public static class OptionalLaunchSteps
{
	/// <summary>Every optional step, in the card's order.</summary>
	public static IReadOnlyList<OptionalLaunchStep> InCardOrder { get; } =
	[
		OptionalLaunchStep.CheckClockAndInternet,
		OptionalLaunchStep.DownloadAiracData,
		OptionalLaunchStep.ParseAiracData,
		OptionalLaunchStep.RemoveOldAiracCycles,
		OptionalLaunchStep.CheckForNewerVersion,
		OptionalLaunchStep.DownloadNews,
		OptionalLaunchStep.CheckGitHubTokens,
		OptionalLaunchStep.LegacyGitHubTokenNotice,
		OptionalLaunchStep.WelcomeWindow,
		OptionalLaunchStep.RemoveLegacyInstall,
	];

	/// <summary>A step's name, e.g. <c>Download AIRAC data</c>.</summary>
	/// <param name="step">One step.</param>
	/// <returns>The name.</returns>
	public static string Name(OptionalLaunchStep step) => step switch
	{
		OptionalLaunchStep.CheckClockAndInternet => "Check the clock and internet connection",
		OptionalLaunchStep.DownloadAiracData => "Download AIRAC data",
		OptionalLaunchStep.ParseAiracData => "Parse AIRAC data",
		OptionalLaunchStep.RemoveOldAiracCycles => "Remove old AIRAC cycles from disk",
		OptionalLaunchStep.CheckForNewerVersion => "Check for a newer version",
		OptionalLaunchStep.DownloadNews => "Download News",
		OptionalLaunchStep.CheckGitHubTokens => "Check GitHub tokens",
		OptionalLaunchStep.LegacyGitHubTokenNotice => "FE-Buddy 2.x GitHub token notice",
		OptionalLaunchStep.WelcomeWindow => "First-launch welcome window",
		OptionalLaunchStep.RemoveLegacyInstall => "Remove FE-Buddy 2.x's old install and dead shortcuts",
		_ => throw new ArgumentOutOfRangeException(nameof(step), step, "Not one optional launch step."),
	};

	/// <summary>What a step does, and what leaving it out means where that isn't plain.</summary>
	/// <param name="step">One step.</param>
	/// <returns>The note.</returns>
	public static string Description(OptionalLaunchStep step) => step switch
	{
		OptionalLaunchStep.CheckClockAndInternet => "Off: uses this PC's clock and takes the PC to be online.",
		OptionalLaunchStep.DownloadAiracData =>
			"Checks whether the FAA has published each cycle, and downloads its NASR data and d-TPP Metafile when they aren't on disk. Off: nothing AIRAC-related uses the network at launch.",
		OptionalLaunchStep.ParseAiracData =>
			"Parses every cycle on disk. Off: nothing is parsed at launch, and a cycle loads when a run, the Map, the AIRAC Service or Settings needs it.",
		OptionalLaunchStep.RemoveOldAiracCycles => "Deletes every cycle on disk but the previous, current and next.",
		OptionalLaunchStep.CheckForNewerVersion => "Asks GitHub for a newer release, for the update badge.",
		OptionalLaunchStep.DownloadNews => "Off: shows the copy of News built into FE-Buddy.",
		OptionalLaunchStep.CheckGitHubTokens => "Checks whether the saved GitHub tokens still work or expire soon.",
		OptionalLaunchStep.LegacyGitHubTokenNotice => "The one-time notice that FEBUDDY_GITHUB_TOKEN is still set.",
		OptionalLaunchStep.WelcomeWindow => "The one-time welcome when FE-Buddy 3 finds no settings.",
		OptionalLaunchStep.RemoveLegacyInstall => "Both change files on this PC.",
		_ => throw new ArgumentOutOfRangeException(nameof(step), step, "Not one optional launch step."),
	};

	/// <summary>
	/// The dev launch's log line: which optional steps run and which are left out, e.g.
	/// <c>Dev launch. Running: Download AIRAC data. Skipping: ...</c>.
	/// </summary>
	/// <param name="steps">The steps that run.</param>
	/// <returns>The line.</returns>
	public static string DescribeDevLaunch(OptionalLaunchStep steps)
	{
		static string List(IEnumerable<OptionalLaunchStep> chosen) =>
			chosen.Any() ? string.Join(", ", chosen.Select(Name)) : "none";

		IEnumerable<OptionalLaunchStep> running = InCardOrder.Where(step => (steps & step) == step);
		IEnumerable<OptionalLaunchStep> skipping = InCardOrder.Where(step => (steps & step) != step);

		return $"Dev launch. Running: {List(running)}. Skipping: {List(skipping)}.";
	}
}
