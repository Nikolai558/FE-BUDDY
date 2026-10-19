namespace FeBuddy.Core.Application.Updates.Models;

/// <summary>
/// Where a version check left FE-Buddy (<see cref="VersionCheckResult.Status"/>): what the title bar, System
/// health and Settings' "Check now" each say, in their own words, from one decision.
/// </summary>
public enum VersionCheckStatus
{
	/// <summary>No check was made: a dev launch left it out.</summary>
	Skipped,

	/// <summary>The check didn't finish: offline, or GitHub couldn't be reached or read.</summary>
	Failed,

	/// <summary>A newer release is on the channel.</summary>
	UpdateAvailable,

	/// <summary>A published pre-release from a less stable channel, newer than the channel's latest.</summary>
	RunningPreRelease,

	/// <summary>A build newer than every release on the channel: one in development.</summary>
	AheadOfLatest,

	/// <summary>The channel has no release yet.</summary>
	NoReleases,

	/// <summary>The latest release on the channel.</summary>
	Latest,
}
