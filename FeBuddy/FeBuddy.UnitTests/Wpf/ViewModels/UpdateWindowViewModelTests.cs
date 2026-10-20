using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Application.Updates.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Versioning.Models;
using FeBuddy.Wpf.Views.Models;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers <see cref="UpdateWindowViewModel"/>'s two modes: an update, and going back to the latest
/// release on the more stable channel chosen while running a pre-release - first asking about the
/// settings that release can't read.
/// </summary>
[Collection("AppLog")]
public sealed class UpdateWindowViewModelTests : IDisposable
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

	private readonly string _logs = Path.Combine(Path.GetTempPath(), "FeBuddyTests_UpdateWindow_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points <see cref="AppLog"/> at a throwaway directory.</summary>
	public UpdateWindowViewModelTests() => AppLog.ConfigureForTesting(_logs);

	/// <summary>Restores the log and deletes the throwaway directory.</summary>
	public void Dispose()
	{
		AppLog.ConfigureForTesting(null);

		try
		{
			Directory.Delete(_logs, recursive: true);
		}
		catch (IOException)
		{
			// Best-effort cleanup; it may not exist.
		}
	}

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

	// ---- going back: the settings the release can't read ----

	/// <summary>With no profile the release can't read, nothing is asked.</summary>
	[Fact]
	public async Task going_back_asks_nothing_when_the_release_reads_every_profile()
	{
		string? lookedFor = null;
		UpdateWindowViewModel vm = GoingBack([], _ => throw new InvalidOperationException("Not asked."));
		vm.FindUnreadableProfiles = version =>
		{
			lookedFor = version;
			return [];
		};

		Assert.True(await vm.SettingsReadyToGoBackAsync());
		Assert.Equal("2.9.3", lookedFor);
	}

	[Theory]
	[InlineData(ConfirmChoice.Alternative, true)]
	[InlineData(ConfirmChoice.Cancel, false)]
	public async Task going_back_without_an_export_goes_on_or_stops_as_chosen(ConfirmChoice choice, bool goesOn)
	{
		IReadOnlyList<string>? asked = null;
		UpdateWindowViewModel vm = GoingBack(["ZNY", "ZOB"], profiles =>
		{
			asked = profiles;
			return choice;
		});
		vm.ExportProfiles = (_, _) => throw new InvalidOperationException("Not exported.");

		Assert.Equal(goesOn, await vm.SettingsReadyToGoBackAsync());
		Assert.Equal(["ZNY", "ZOB"], asked);
	}

	/// <summary>Export… asks for a folder and exports the profiles there; cancelling the folder stops going back.</summary>
	[Fact]
	public async Task export_exports_the_profiles_to_the_folder_chosen_or_stops_without_one()
	{
		(string Folder, IReadOnlyList<string> Profiles)? exported = null;
		UpdateWindowViewModel vm = GoingBack(["ZNY"], _ => ConfirmChoice.Confirm);
		vm.ExportProfiles = (folder, profiles) => exported = (folder, profiles);
		vm.PickExportFolder = () => @"C:\Exports";

		Assert.True(await vm.SettingsReadyToGoBackAsync());
		Assert.Equal(@"C:\Exports", exported!.Value.Folder);
		Assert.Equal(["ZNY"], exported.Value.Profiles);

		exported = null;
		vm.PickExportFolder = () => null;

		Assert.False(await vm.SettingsReadyToGoBackAsync());
		Assert.Null(exported);
	}

	/// <summary>An export that fails says so, and nothing is downloaded.</summary>
	[Fact]
	public async Task an_export_that_fails_says_so_and_stops_going_back()
	{
		UpdateWindowViewModel vm = GoingBack(["ZNY"], _ => ConfirmChoice.Confirm);
		vm.PickExportFolder = () => @"C:\Exports";
		vm.ExportProfiles = (_, _) => throw new IOException("The disk is full.");

		Assert.False(await vm.SettingsReadyToGoBackAsync());
		Assert.Equal("The settings could not be exported (The disk is full.), so nothing was downloaded.", vm.ErrorText);
	}

	/// <summary>With no window to ask, going back goes ahead.</summary>
	[Fact]
	public async Task with_no_window_to_ask_going_back_goes_on()
	{
		UpdateWindowViewModel vm = new(GoBack(Installer), isMsiInstalled: true, () => []) { FindUnreadableProfiles = _ => ["ZNY"] };

		Assert.True(await vm.SettingsReadyToGoBackAsync());
	}

	/// <summary>Go back now asks about the settings before anything else - here going on to the unfinished-work question.</summary>
	[Fact]
	public async Task go_back_now_asks_about_the_settings_first()
	{
		List<string> asked = [];
		TaskCompletionSource reachedUnfinishedWork = new(TaskCreationOptions.RunContinuationsAsynchronously);
		UpdateWindowViewModel vm = new(GoBack(Installer), isMsiInstalled: true, () => ["A job is running."])
		{
			FindUnreadableProfiles = _ => ["ZNY"],
			AskAboutUnreadableSettings = _ =>
			{
				asked.Add("settings");
				return ConfirmChoice.Alternative;
			},
			ConfirmCloseWithUnfinishedWork = _ =>
			{
				asked.Add("unfinished work");
				reachedUnfinishedWork.SetResult();
				return false;
			},
		};

		vm.UpdateCommand.Execute(null);
		await reachedUnfinishedWork.Task.WaitAsync(TimeSpan.FromSeconds(10));

		Assert.Equal(["settings", "unfinished work"], asked);
	}

	/// <summary>An update never asks: only going back to an older release can meet settings it can't read.</summary>
	[Fact]
	public async Task an_update_never_asks_about_the_settings()
	{
		TaskCompletionSource reachedUnfinishedWork = new(TaskCreationOptions.RunContinuationsAsynchronously);
		VersionCheckResult update = new("3.0.0", "3.1.0", UpdateAvailable: true, ReleaseChannel.Stable, CheckSucceeded: true, "update") { LatestInstaller = Installer };
		UpdateWindowViewModel vm = new(update, isMsiInstalled: true, () => ["A job is running."])
		{
			FindUnreadableProfiles = _ => throw new InvalidOperationException("Not looked for."),
			ConfirmCloseWithUnfinishedWork = _ =>
			{
				reachedUnfinishedWork.SetResult();
				return false;
			},
		};

		vm.UpdateCommand.Execute(null);

		await reachedUnfinishedWork.Task.WaitAsync(TimeSpan.FromSeconds(10));
	}

	[Fact]
	public void the_question_names_the_release_the_profiles_and_where_they_go()
	{
		UpdateWindowViewModel vm = new(GoBack(Installer), isMsiInstalled: true, () => []);

		Assert.Equal("v2.9.3 can't read your settings", vm.UnreadableSettingsHeading);
		Assert.Equal(
			"Settings saved in this version can't be read by v2.9.3: the profile ZNY. When v2.9.3 opens, it sets them aside in " +
			@"`%APPDATA%\FE-Buddy\Reverted` and goes back to the last copy of each it can read, or to its defaults." +
			"\n\nExport them first to import them after you update again.",
			vm.DescribeUnreadableSettings(["ZNY"]));
		Assert.Contains("the profiles ZDC, ZNY and ZOB.", vm.DescribeUnreadableSettings(["ZDC", "ZNY", "ZOB"]), StringComparison.Ordinal);
		Assert.Throws<ArgumentNullException>(() => vm.DescribeUnreadableSettings(null!));
	}

	/// <summary>The real lookup: a release from before these settings files (2.x) never reads them, so it names none.</summary>
	[Fact]
	public void the_real_lookup_names_no_profile_for_a_release_before_userconfig()
	{
		UpdateWindowViewModel vm = new(GoBack(Installer), isMsiInstalled: true, () => []);

		Assert.Empty(vm.FindUnreadableProfiles("2.9.3"));
	}

	private static UpdateWindowViewModel GoingBack(IReadOnlyList<string> unreadable, Func<IReadOnlyList<string>, ConfirmChoice> ask) =>
		new(GoBack(Installer), isMsiInstalled: true, () => [])
		{
			FindUnreadableProfiles = _ => unreadable,
			AskAboutUnreadableSettings = ask,
		};
}
