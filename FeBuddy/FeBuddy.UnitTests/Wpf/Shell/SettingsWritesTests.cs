using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Wpf.Shell;

/// <summary>
/// Covers <see cref="SettingsWrites"/> through the pages that save with it: a settings file that can't
/// be written (here, read-only) is reported, never thrown, and the change stays unsaved on the page
/// but not in memory, so saving again once the file can be written saves it. Against a throwaway config.
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
}
