using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the AIRAC Service screen's rail (<see cref="AiracServiceViewModel"/>): every sub-service
/// keeps its tab, greyed out while it is left out on the General tab, with a tooltip saying what it
/// is and how to bring it in; Next steps over the greyed ones; and Concatenate Aliases comes in by
/// itself while an included sub-service makes an alias file - against a throwaway config.
/// </summary>
[Collection("AppLog")]
public sealed class AiracServiceViewModelTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_AiracScreen_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public AiracServiceViewModelTests()
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

	private static ServiceTabViewModel TabTitled(AiracServiceViewModel screen, string title) =>
		screen.Tabs.Single(tab => tab.Title == title);

	[Fact]
	public void a_sub_service_left_out_keeps_its_tab_greyed_out_with_a_tooltip()
	{
		UserConfigFile.TrySetValue("Services.AiracService.SelectedSubServices", "Airports");

		AiracServiceViewModel screen = new();

		Assert.True(TabTitled(screen, "Airports").IsAvailable);
		ServiceTabViewModel fixes = TabTitled(screen, "Fixes");
		Assert.False(fixes.IsAvailable);
		Assert.StartsWith("A symbol and a label for every fix", fixes.UnavailableToolTip);
		Assert.EndsWith("To include it, tick Fixes under Include on the General tab.", fixes.UnavailableToolTip);

		// Every sub-service is in the rail, in order, with File Names and Preview Settings after them.
		Assert.Equal(
			["General", .. AiracSubServices.All.Select(d => d.DisplayName), "File Names", "Preview Settings"],
			screen.Tabs.Select(tab => tab.Title));
	}

	[Fact]
	public void next_steps_over_greyed_out_tabs()
	{
		UserConfigFile.TrySetValue("Services.AiracService.SelectedSubServices", "Fixes");

		AiracServiceViewModel screen = new();
		screen.NextCommand.Execute(null);

		Assert.Equal("Fixes", screen.SelectedTab?.Title);
	}

	[Fact]
	public void including_a_sub_service_brings_its_tab_back()
	{
		UserConfigFile.TrySetValue("Services.AiracService.SelectedSubServices", "Airports");
		AiracServiceViewModel screen = new();
		AiracGeneralTabViewModel general = Assert.IsType<AiracGeneralTabViewModel>(screen.Tabs[0]);

		general.RowFor(AiracSubServices.FixesKey)!.IsIncluded = true;

		Assert.True(TabTitled(screen, "Fixes").IsAvailable);
		Assert.Null(TabTitled(screen, "Fixes").UnavailableToolTip);
	}

	/// <summary>With nothing included, there is nothing to rename or run.</summary>
	[Fact]
	public void with_nothing_included_there_is_no_file_names_or_preview_tab()
	{
		UserConfigFile.TrySetValue("Services.AiracService.SelectedSubServices", string.Empty);

		AiracServiceViewModel screen = new();

		Assert.DoesNotContain(screen.Tabs, tab => tab.Title is "File Names" or "Preview Settings");
		Assert.All(screen.Tabs.Skip(1), tab => Assert.False(tab.IsAvailable));
	}

	[Fact]
	public void concatenate_aliases_comes_in_while_an_included_sub_service_makes_an_alias_file()
	{
		UserConfigFile.TrySetValue("Services.AiracService.SelectedSubServices", "Fixes");
		AiracServiceViewModel screen = new();
		ServiceTabViewModel concatenate = TabTitled(screen, "Concatenate Aliases");
		Assert.False(concatenate.IsAvailable);
		Assert.StartsWith("Combines every alias file the run makes", concatenate.UnavailableToolTip, StringComparison.Ordinal);
		Assert.EndsWith("To make one, tick a sub-service and its Alias box on the General tab.", concatenate.UnavailableToolTip, StringComparison.Ordinal);

		AiracGeneralTabViewModel general = Assert.IsType<AiracGeneralTabViewModel>(screen.Tabs[0]);
		SubServiceRow airports = general.RowFor(AiracSubServices.AirportsKey)!;
		airports.IsIncluded = true;

		Assert.True(concatenate.IsAvailable);

		airports.Alias = false;

		Assert.False(concatenate.IsAvailable);
	}

	/// <summary>Combined_Alias.txt goes in Aliases, and is on the File Names tab while combining is on.</summary>
	[Fact]
	public void the_combined_alias_file_is_listed_for_renaming_while_combining()
	{
		UserConfigFile.TrySetValue("Services.AiracService.SelectedSubServices", "Airports");
		AiracServiceViewModel screen = new();
		FileNamesViewModel fileNames = Assert.IsType<FileNamesViewModel>(TabTitled(screen, "File Names"));
		ConcatenateAliasesViewModel concatenate = Assert.IsType<ConcatenateAliasesViewModel>(TabTitled(screen, "Concatenate Aliases"));

		fileNames.RefreshFiles();
		FileNameFolder aliases = fileNames.Folders.Single(folder => folder.Folder.EndsWith(@"\Aliases", StringComparison.Ordinal));
		Assert.Contains(aliases.Files, file => file.Key == "Combined_Alias.txt");

		concatenate.CombineAliasFiles = false;
		fileNames.RefreshFiles();

		Assert.DoesNotContain(fileNames.Folders.SelectMany(folder => folder.Files), file => file.Key == "Combined_Alias.txt");
	}
}
