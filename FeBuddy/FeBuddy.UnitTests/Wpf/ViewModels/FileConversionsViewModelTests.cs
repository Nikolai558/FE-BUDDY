using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the start a tabbed screen goes back to when it is opened from the side nav
/// (<see cref="IOpensAtStart"/>), on File Conversions: its first tab, with the tab left keeping its
/// edits - against a throwaway config.
/// </summary>
[Collection("AppLog")]
public sealed class FileConversionsViewModelTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_Conversions_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public FileConversionsViewModelTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		UserConfigFile.ConfigureForTesting(Path.Combine(_root, "config"));
	}

	/// <summary>Restores the real config and log, and deletes the folder.</summary>
	public void Dispose()
	{
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

	[Fact]
	public void returning_to_the_start_selects_the_first_tab_and_keeps_the_last_ones_edits()
	{
		FileConversionsViewModel screen = new();
		DatToGeojsonViewModel dat = Assert.IsType<DatToGeojsonViewModel>(screen.Tabs[0]);
		EramToGeojsonViewModel eram = Assert.IsType<EramToGeojsonViewModel>(screen.Tabs.Last());
		screen.SelectedTab = eram;
		eram.LayoutRaw = true;

		screen.ReturnToStart();

		Assert.Same(dat, screen.SelectedTab);
		Assert.True(eram.LayoutRaw);
		Assert.True(eram.IsDirty);
	}
}
