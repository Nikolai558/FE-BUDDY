using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the File Conversions screen: its picker (Source, File, Output, each picking its top
/// choice until the user picks another), Continue opening the conversion's page, the back arrow,
/// and the start it goes back to from the side nav (<see cref="IOpensAtStart"/>) - against a
/// throwaway config.
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
	public void the_picker_starts_with_nothing_picked_and_nothing_to_continue_to()
	{
		FileConversionsViewModel screen = new();

		Assert.Equal(
			["FAA Radar Video Map .dat files", "Legacy Sector File (.sct2)", "FAA ERAM Adaptation Files"],
			screen.Sources.Select(s => s.Name));
		Assert.Null(screen.SelectedSource);
		Assert.False(screen.HasFiles);
		Assert.False(screen.HasOutputs);
		Assert.Null(screen.PickSummary);
		Assert.False(screen.ContinueCommand.CanExecute(null));
		Assert.Null(screen.Page);
	}

	[Fact]
	public void a_source_with_no_files_skips_to_its_outputs_with_the_top_one_picked()
	{
		FileConversionsViewModel screen = new();

		Source(screen, "FAA Radar Video Map .dat files").IsSelected = true;

		Assert.False(screen.HasFiles);
		Assert.Null(screen.SelectedFile);
		Assert.Same(screen.Outputs[0], screen.SelectedOutput);
		Assert.Equal("FAA Radar Video Map .dat files → GeoJSON for CRC", screen.PickSummary);
		Assert.True(screen.ContinueCommand.CanExecute(null));
	}

	[Fact]
	public void the_eram_source_asks_for_its_file_and_picks_geomaps_and_its_output()
	{
		FileConversionsViewModel screen = new();

		Source(screen, "FAA ERAM Adaptation Files").IsSelected = true;

		Assert.True(screen.HasFiles);
		Assert.Equal(["Geomaps.xml"], screen.Files.Select(f => f.Name));
		Assert.Same(screen.Files[0], screen.SelectedFile);
		Assert.Same(screen.Outputs[0], screen.SelectedOutput);
		Assert.Equal("FAA ERAM Adaptation Files ▸ Geomaps.xml → GeoJSON for CRC", screen.PickSummary);
	}

	[Fact]
	public void picking_a_source_unpicks_the_one_before_and_shows_its_own_outputs()
	{
		FileConversionsViewModel screen = new();
		ConversionChoice dat = Source(screen, "FAA Radar Video Map .dat files");
		ConversionChoice eram = Source(screen, "FAA ERAM Adaptation Files");
		dat.IsSelected = true;

		eram.IsSelected = true;

		Assert.False(dat.IsSelected);
		Assert.Same(eram, screen.SelectedSource);
		Assert.IsType<EramToGeojsonViewModel>(screen.SelectedOutput?.Conversion);
	}

	[Fact]
	public void every_conversion_can_be_picked()
	{
		FileConversionsViewModel screen = new();

		IEnumerable<ConversionTabViewModel?> reachable = screen.Sources
			.SelectMany(source => source.Files.Count > 0 ? source.Files.SelectMany(file => file.Outputs) : source.Outputs)
			.Select(output => output.Conversion);

		Assert.Equal<ConversionTabViewModel?>(screen.Conversions, reachable);
	}

	[Fact]
	public void continue_opens_the_picked_conversion_and_back_returns_to_the_picker_keeping_the_pick()
	{
		FileConversionsViewModel screen = new();
		ConversionChoice sct = Source(screen, "Legacy Sector File (.sct2)");
		sct.IsSelected = true;

		screen.ContinueCommand.Execute(null);

		Assert.IsType<SctToGeojsonViewModel>(screen.Page);
		Assert.Null(screen.Results);

		screen.BackCommand.Execute(null);

		Assert.Null(screen.Page);
		Assert.Same(sct, screen.SelectedSource);
		Assert.True(screen.ContinueCommand.CanExecute(null));
	}

	[Fact]
	public void returning_to_the_start_shows_the_picker_and_keeps_the_pages_edits()
	{
		FileConversionsViewModel screen = new();
		Source(screen, "FAA ERAM Adaptation Files").IsSelected = true;
		screen.ContinueCommand.Execute(null);
		EramToGeojsonViewModel eram = Assert.IsType<EramToGeojsonViewModel>(screen.Page);
		eram.LayoutRaw = true;

		screen.ReturnToStart();

		Assert.Null(screen.Page);
		Assert.False(screen.IsRunning);
		Assert.True(eram.LayoutRaw);
		Assert.True(eram.IsDirty);
	}

	private static ConversionChoice Source(FileConversionsViewModel screen, string name) =>
		screen.Sources.Single(s => s.Name == name);
}
