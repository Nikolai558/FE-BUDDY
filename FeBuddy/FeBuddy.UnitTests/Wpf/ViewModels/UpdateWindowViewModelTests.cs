using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Application.Updates.Models;
using FeBuddy.Versioning.Models;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers <see cref="UpdateWindowViewModel"/>'s two modes: an update, and going back to the latest
/// release on the more stable channel chosen while running a pre-release.
/// </summary>
public sealed class UpdateWindowViewModelTests
{
	private static readonly ReleaseInstaller Installer = new("FE-BUDDY-2.9.3.msi", "https://example.test/293.msi", 8, 9);

	private static VersionCheckResult GoBack(ReleaseInstaller? installer = null) =>
		new("3.0.0-alpha.2", "2.9.3", UpdateAvailable: false, ReleaseChannel.Stable, CheckSucceeded: true, "ahead",
			LatestReleaseUrl: "https://example.test/293", IsAheadOfLatestRelease: true)
		{
			RunningPreReleaseChannel = ReleaseChannel.Alpha,
			LatestRelease = new ReleaseSummary("2.9.3", null, false, "## Change log:\n- Fixes", "https://example.test/293"),
			LatestInstaller = installer,
		};

	[Fact]
	public void going_back_names_the_release_and_warns_about_the_pre_release()
	{
		UpdateWindowViewModel vm = new(GoBack(Installer), isMsiInstalled: true, () => []);

		Assert.True(vm.IsGoingBack);
		Assert.Equal("Go back to the latest Stable release", vm.Heading);
		Assert.Equal("Go back to", vm.LatestLabel);
		Assert.Equal("2.9.3", vm.LatestVersion);
		Assert.Equal("Go back now", vm.UpdateButtonText);
		Assert.Contains("v2.9.3", vm.FooterText, StringComparison.Ordinal);
		Assert.Contains("may not carry over", vm.FooterText, StringComparison.Ordinal);
		Assert.Equal("v2.9.3", Assert.Single(vm.Releases).Version);
		Assert.Null(vm.ReleasesBehind);
	}

	/// <summary>With no installer, or in a dev build, going back opens that release's page.</summary>
	[Theory]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public void going_back_without_an_installer_opens_the_release_page(bool isMsiInstalled, bool hasInstaller)
	{
		UpdateWindowViewModel vm = new(GoBack(hasInstaller ? Installer : null), isMsiInstalled, () => []);

		Assert.False(vm.CanInstall);
		Assert.Equal("Open release page", vm.UpdateButtonText);
		Assert.Equal("https://example.test/293", vm.ReleaseUrl);
	}

	[Fact]
	public void an_update_reads_as_one()
	{
		VersionCheckResult update = new("3.0.0", "3.1.0", UpdateAvailable: true, ReleaseChannel.Stable, CheckSucceeded: true, "update")
		{
			NewerReleases = [new ReleaseSummary("3.1.0", null, false, null, null)],
			LatestInstaller = Installer,
			RunningPreReleaseChannel = null,
		};

		UpdateWindowViewModel vm = new(update, isMsiInstalled: true, () => []);

		Assert.False(vm.IsGoingBack);
		Assert.Equal("Update available", vm.Heading);
		Assert.Equal("Latest", vm.LatestLabel);
		Assert.Equal("Update now", vm.UpdateButtonText);
		Assert.Contains("your settings are kept", vm.FooterText, StringComparison.Ordinal);
	}

	/// <summary>With no notes for the release, a placeholder entry still names it.</summary>
	[Fact]
	public void a_release_with_no_summary_still_gets_an_entry()
	{
		VersionCheckResult update = new("3.0.0", "3.1.0", UpdateAvailable: true, ReleaseChannel.Stable, CheckSucceeded: true, "update");

		UpdateWindowViewModel vm = new(update, isMsiInstalled: true, () => []);

		Assert.Equal("v3.1.0", Assert.Single(vm.Releases).Version);
	}
}
