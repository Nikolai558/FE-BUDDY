using FeBuddy.Versioning.Models;

namespace FeBuddy.Core.Application.Updates.Models;

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
	/// When <see cref="IsAheadOfLatestRelease"/> because the running version is a published
	/// pre-release (an alpha, beta or release candidate) from a less stable channel than
	/// <see cref="Channel"/> - the user moved to a more stable channel while running one - that
	/// pre-release's channel. <see langword="null"/> otherwise, including for a development build.
	/// </summary>
	public ReleaseChannel? RunningPreReleaseChannel { get; init; }

	/// <summary>
	/// The <c>.msi</c> attached to the <see cref="LatestVersion"/> release, for the update window's
	/// "Update now"; <see langword="null"/> when that release has none (older releases predate the
	/// MSI) or the check did not complete.
	/// </summary>
	public ReleaseInstaller? LatestInstaller { get; init; }

	/// <summary>
	/// The newest release on <see cref="Channel"/> - the one <see cref="LatestVersion"/> names - for the
	/// update window when there are no <see cref="NewerReleases"/> (going back to it). <see langword="null"/>
	/// when the check did not complete or found none.
	/// </summary>
	public ReleaseSummary? LatestRelease { get; init; }

	/// <summary>
	/// Whether the user can go back to <see cref="LatestVersion"/>: they are running a published
	/// pre-release from a less stable channel than <see cref="Channel"/> (see
	/// <see cref="RunningPreReleaseChannel"/>), and that channel's newest release is older. The
	/// installer allows it, since the installed version is a pre-release.
	/// </summary>
	public bool CanGoBack => RunningPreReleaseChannel is not null && LatestVersion is not null;

	/// <summary>
	/// Parses the user's update channel from its stored name (<c>General.UpdateChannel</c>:
	/// <c>Stable</c>, <c>ReleaseCandidate</c>, <c>Beta</c> or <c>Alpha</c>), falling back to
	/// <see cref="ReleaseChannel.Stable"/> for a missing or unrecognized value.
	/// </summary>
	/// <param name="value">The stored channel name (case-insensitive).</param>
	/// <returns>The parsed channel, or <see cref="ReleaseChannel.Stable"/>.</returns>
	public static ReleaseChannel ParseChannel(string? value) => ParseChannel(value, ReleaseChannel.Stable);

	/// <summary>
	/// Parses the user's update channel from its stored name, falling back to <paramref name="fallback"/>
	/// for a missing or unrecognized value.
	/// </summary>
	/// <param name="value">The stored channel name (case-insensitive).</param>
	/// <param name="fallback">The channel when none is stored (<see cref="UpdateChannelSetting.DefaultFor"/>).</param>
	/// <returns>The parsed channel, or <paramref name="fallback"/>.</returns>
	public static ReleaseChannel ParseChannel(string? value, ReleaseChannel fallback) =>
		TryParseChannel(value, out ReleaseChannel channel) ? channel : fallback;

	/// <summary>Parses the user's update channel from its stored name, if it is one.</summary>
	/// <param name="value">The stored channel name (case-insensitive).</param>
	/// <param name="channel">The parsed channel, or <see cref="ReleaseChannel.Stable"/> when <paramref name="value"/> isn't one.</param>
	/// <returns><see langword="true"/> when <paramref name="value"/> names a channel.</returns>
	public static bool TryParseChannel(string? value, out ReleaseChannel channel)
	{
		foreach (ReleaseChannel candidate in Enum.GetValues<ReleaseChannel>())
		{
			if (string.Equals(candidate.ToString(), value?.Trim(), StringComparison.OrdinalIgnoreCase))
			{
				channel = candidate;
				return true;
			}
		}

		channel = ReleaseChannel.Stable;
		return false;
	}
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

/// <summary>A release's <c>.msi</c> asset on GitHub.</summary>
/// <param name="FileName">The asset's file name, e.g. <c>FE-BUDDY-Setup.msi</c>.</param>
/// <param name="DownloadUrl">The public download URL (<c>browser_download_url</c>).</param>
/// <param name="AssetId">
/// GitHub's asset id. Only used when a GitHub token is chosen: the download with it goes through the
/// releases-assets API, which is addressed by id.
/// </param>
/// <param name="SizeBytes">The size GitHub reports, or 0 when it reports none.</param>
public sealed record ReleaseInstaller(string FileName, string DownloadUrl, long AssetId, long SizeBytes);

/// <summary>Progress of an installer download.</summary>
/// <param name="BytesReceived">Bytes written so far.</param>
/// <param name="TotalBytes">The expected total, or <see langword="null"/> when unknown.</param>
public readonly record struct DownloadProgress(long BytesReceived, long? TotalBytes)
{
	/// <summary>Whole-number percent complete, or <see langword="null"/> when the total is unknown.</summary>
	public int? Percent => TotalBytes is > 0 ? (int)Math.Min(100, BytesReceived * 100 / TotalBytes.Value) : null;
}
