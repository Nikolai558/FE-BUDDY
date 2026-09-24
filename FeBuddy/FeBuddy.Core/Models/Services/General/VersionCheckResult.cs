using FeBuddy.Versioning;

namespace FeBuddy.Core.Models.Services.General;

/// <summary>
/// The outcome of the launch-time application version check.
/// </summary>
/// <param name="CurrentVersion">The running application's version string.</param>
/// <param name="LatestVersion">
/// The newest version found on <paramref name="Channel"/>, or <see langword="null"/> when the
/// check did not complete.
/// </param>
/// <param name="UpdateAvailable"><see langword="true"/> if <paramref name="LatestVersion"/> is newer than <paramref name="CurrentVersion"/>.</param>
/// <param name="Channel">The channel that was queried.</param>
/// <param name="CheckSucceeded">
/// <see langword="false"/> when offline or the GitHub API could not be reached/parsed - the
/// GUI shows update state as "unknown" rather than "up to date".
/// </param>
/// <param name="Message">A short human-readable note about the result or the failure.</param>
/// <param name="LatestReleaseUrl">The GitHub release page URL for <paramref name="LatestVersion"/>, when available.</param>
/// <param name="IsAheadOfLatestRelease">
/// <see langword="true"/> when <paramref name="CurrentVersion"/> is newer than every comparable
/// release found (e.g. an in-development build ahead of the latest public release). Distinct
/// from the ordinary "up to date" case (<paramref name="CurrentVersion"/> exactly matches
/// <paramref name="LatestVersion"/>) so the GUI can say so rather than implying the two match.
/// </param>
public record VersionCheckResult(
	string CurrentVersion,
	string? LatestVersion,
	bool UpdateAvailable,
	ReleaseChannel Channel,
	bool CheckSucceeded,
	string? Message,
	string? LatestReleaseUrl = null,
	bool IsAheadOfLatestRelease = false)
{
	/// <summary>
	/// Every release between <see cref="CurrentVersion"/> (exclusive) and <see cref="LatestVersion"/>
	/// (inclusive) on <see cref="Channel"/>, newest first - so a user several versions behind sees
	/// what changed in each. Empty unless <see cref="UpdateAvailable"/>.
	/// </summary>
	public IReadOnlyList<ReleaseSummary> NewerReleases { get; init; } = [];

	/// <summary>
	/// Parses the user's update channel from its stored name (<c>General.UpdateChannel</c>:
	/// <c>Stable</c>, <c>ReleaseCandidate</c>, <c>Beta</c> or <c>Alpha</c>), falling back to
	/// <see cref="ReleaseChannel.Stable"/> for a missing or unrecognized value.
	/// </summary>
	/// <param name="value">The stored channel name (case-insensitive).</param>
	/// <returns>The parsed channel, or <see cref="ReleaseChannel.Stable"/>.</returns>
	public static ReleaseChannel ParseChannel(string? value) =>
		Enum.GetValues<ReleaseChannel>()
			.Where(channel => string.Equals(channel.ToString(), value?.Trim(), StringComparison.OrdinalIgnoreCase))
			.DefaultIfEmpty(ReleaseChannel.Stable)
			.First();
}

/// <summary>One GitHub release, as shown in the update window.</summary>
/// <param name="Version">The release's version, without a leading <c>v</c> (e.g. <c>2.9.0</c> or <c>2.9.1-alpha.1</c>).</param>
/// <param name="PublishedAt">When the release was published, when GitHub reported it.</param>
/// <param name="IsPrerelease"><see langword="true"/> for an alpha or beta release.</param>
/// <param name="Notes">
/// The release body (Markdown) with its "Instructions to install" section removed - the user is
/// already running FE-Buddy when they see it. <see langword="null"/> when the release has no body.
/// </param>
/// <param name="Url">The release page URL, when available.</param>
public sealed record ReleaseSummary(
	string Version,
	DateTimeOffset? PublishedAt,
	bool IsPrerelease,
	string? Notes,
	string? Url);
