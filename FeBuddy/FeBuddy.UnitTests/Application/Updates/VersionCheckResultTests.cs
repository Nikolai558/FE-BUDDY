using FeBuddy.Core.Application.Updates.Models;
using FeBuddy.Versioning.Models;

namespace FeBuddy.UnitTests.Application.Updates;

/// <summary>
/// Covers <see cref="VersionCheckResult.Status"/> and <see cref="VersionCheckResult.DisplayVersion"/>: the one
/// decision the title bar, System health and Settings' "Check now" each word, in its order, and the one "dev" rule.
/// </summary>
public sealed class VersionCheckResultTests
{
	private static VersionCheckResult Checked(string? latest = "3.0.0", bool update = false, bool ahead = false) =>
		new("3.0.0", latest, update, ReleaseChannel.Stable, CheckSucceeded: true, Message: null, IsAheadOfLatestRelease: ahead);

	/// <summary>Each state, and the earlier state winning where two would apply.</summary>
	[Fact]
	public void the_status_is_decided_in_order()
	{
		Assert.Equal(VersionCheckStatus.Skipped, (Checked() with { CheckSucceeded = false, WasSkipped = true }).Status);
		Assert.Equal(VersionCheckStatus.Failed, (Checked(update: true) with { CheckSucceeded = false }).Status);
		Assert.Equal(VersionCheckStatus.UpdateAvailable, (Checked("3.1.0", update: true) with { RunningPreReleaseChannel = ReleaseChannel.Beta }).Status);
		Assert.Equal(VersionCheckStatus.RunningPreRelease, (Checked("2.9.0", ahead: true) with { RunningPreReleaseChannel = ReleaseChannel.Beta }).Status);
		Assert.Equal(VersionCheckStatus.AheadOfLatest, Checked("2.9.0", ahead: true).Status);
		Assert.Equal(VersionCheckStatus.NoReleases, Checked(latest: null).Status);
		Assert.Equal(VersionCheckStatus.Latest, Checked().Status);
	}

	/// <summary>An update with no version to name isn't one: the channel has no release.</summary>
	[Fact]
	public void an_update_with_no_latest_version_is_no_releases() =>
		Assert.Equal(VersionCheckStatus.NoReleases, Checked(latest: null, update: true).Status);

	/// <summary>The running version without a leading v, or <c>dev</c> for a build with none.</summary>
	[Theory]
	[InlineData("3.0.0", "3.0.0")]
	[InlineData(" v3.0.0-beta.4 ", "3.0.0-beta.4")]
	[InlineData("V2.9.1", "2.9.1")]
	[InlineData("0.0.0", "dev")]
	[InlineData("v0.0.0", "dev")]
	[InlineData("", "dev")]
	[InlineData("  ", "dev")]
	public void the_display_version_drops_the_v_and_calls_no_version_dev(string current, string expected) =>
		Assert.Equal(expected, (Checked() with { CurrentVersion = current }).DisplayVersion);
}
