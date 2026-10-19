using System.Text.Json.Nodes;

using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels;
using FeBuddy.Core.Application.News.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Wpf.Shell;

/// <summary>
/// Covers <see cref="SettingsWrites"/> through the pages that save with it: a settings file that can't
/// be written (here, read-only) is reported, never thrown, and the change stays unsaved on the page
/// but not in memory, so saving again once the file can be written saves it; and the News marked read saved
/// to <c>Shared.json</c> alone. Against a throwaway config.
/// </summary>
[Collection("AppLog")]
public sealed class SettingsWritesTests : IDisposable
{
	private const string SelectedKey = "Services.AiracService.General.SelectedSubServices";

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_SettingsWrites_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public SettingsWritesTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		UserConfigFile.ConfigureForTesting(_root);
		UserConfigFile.Write();
	}

	/// <summary>Restores the real config and log, and deletes the folder.</summary>
	public void Dispose()
	{
		ReadOnly(false);
		UserConfigFile.ConfigureForTesting(null);
		AppLog.ConfigureForTesting(null);

		try
		{
			Directory.Delete(_root, recursive: true);
		}
		catch (IOException)
		{
			// Best-effort cleanup.
		}
	}

	private static void ReadOnly(bool on)
	{
		if (File.Exists(UserConfigFile.ConfigFilePath))
		{
			File.SetAttributes(UserConfigFile.ConfigFilePath, on ? FileAttributes.ReadOnly : FileAttributes.Normal);
		}
	}

	[Fact]
	public void a_tab_that_cannot_save_says_so_keeps_its_changes_and_saves_once_it_can()
	{
		AiracGeneralTabViewModel general = new();
		Assert.True(general.Save());
		general.RowFor(AiracSubServices.FixesKey)!.IsIncluded = false;
		ReadOnly(true);

		Assert.False(general.Save());

		Assert.True(general.IsDirty);
		Assert.Contains("Fixes", UserConfigFile.GetValue(SelectedKey)!, StringComparison.Ordinal);
		Assert.Contains(AppLog.Entries, entry => entry.Level == LogLevel.Error
			&& entry.Message.StartsWith("Could not save the General settings", StringComparison.Ordinal));

		ReadOnly(false);

		Assert.True(general.Save());
		Assert.False(general.IsDirty);
		Assert.DoesNotContain("Fixes", UserConfigFile.GetValue(SelectedKey)!, StringComparison.Ordinal);
	}

	[Fact]
	public void a_default_roi_that_cannot_be_saved_is_not_announced_or_kept()
	{
		int changes = 0;
		void Count(object? sender, EventArgs e) => changes++;
		ReadOnly(true);
		DefaultRoiStore.Changed += Count;

		try
		{
			Assert.False(DefaultRoiStore.Set(new RegionOfInterest(40, -85, 42, -80)));
		}
		finally
		{
			DefaultRoiStore.Changed -= Count;
		}

		Assert.Equal(0, changes);
		Assert.Null(DefaultRoiStore.Load());
	}

	/// <summary>
	/// Marking the News read saves that one setting, to <c>Shared.json</c>, where it is kept: the profile's undo
	/// snapshot - here the default ROI's - is left as it is.
	/// </summary>
	[Fact]
	public void marking_the_news_read_saves_only_that_setting()
	{
		DefaultRoiStore.Set(new RegionOfInterest(40, -85, 42, -80));
		DefaultRoiStore.Set(new RegionOfInterest(39, -86, 43, -79));
		string snapshot = File.ReadAllText(UserConfigFile.PreviousFilePath);

		DashboardViewModel.MarkNewsRead(new NewsPostId(new DateOnly(2026, 10, 19), 2));

		Assert.Equal(snapshot, File.ReadAllText(UserConfigFile.PreviousFilePath));
		Assert.Equal("2026-10-19.2", JsonNode.Parse(File.ReadAllText(UserConfigFile.SharedFilePath))!["General"]!["NewsLastOpen"]!.GetValue<string>());
		Assert.True(UserConfigFile.CanUndo(DefaultRoiStore.Node));
	}

	/// <summary>While a profile change or an import runs, a page's save is refused, saying to wait for it, and nothing is written.</summary>
	[Fact]
	public void a_save_while_a_profile_change_runs_is_refused()
	{
		UserConfigFile.TrySetValue("Services.AiracService.Airways.OutputBy", "HighLow");
		SettingsBusy.Start();

		try
		{
			Assert.False(SettingsWrites.TrySave("Services.AiracService.Airways", "Airways settings"));
		}
		finally
		{
			SettingsBusy.Finish();
		}

		Assert.Contains(Toast.Items, t => t.Title == "Could not save the Airways settings" && t.Message!.StartsWith("A settings profile change or import is running.", StringComparison.Ordinal));
		Assert.DoesNotContain("HighLow", File.ReadAllText(UserConfigFile.ConfigFilePath), StringComparison.Ordinal);
	}
}
