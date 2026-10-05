using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the CRC ERAM Defaults card's choice of files on a GeoJSON sub-service tab
/// (<see cref="GeojsonSubServiceViewModel"/>, through the Airports tab): none to start, any GeoJSON
/// file the tab writes, what the run is sent, and where the choice is saved - against a throwaway
/// config.
/// </summary>
[Collection("AppLog")]
public sealed class CrcDefaultsChoiceTests : IDisposable
{
	private const string Node = "Services.AiracService.Airports";

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_CrcChoice_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public CrcDefaultsChoiceTests()
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

	private static CrcFileToggle File(GeojsonSubServiceViewModel tab, string key) =>
		tab.CrcFileRows.SelectMany(row => row.Files).Single(file => file.Key == key);

	[Fact]
	public void no_file_gets_crc_eram_defaults_to_start_and_every_geojson_file_is_offered()
	{
		AirportsViewModel tab = new();

		Assert.Equal(CrcDefaultsScope.None, tab.CrcDefaultsScope);
		Assert.True(tab.ShowsCrcDefaultsCard);
		Assert.False(tab.HasCrcDefaultsInUse);
		Assert.Equal(["Runways_Lines", "Airports_Symbols", "Airports_Text"], tab.CrcFileRows.SelectMany(row => row.Files).Select(file => file.Key));

		Assert.Equal(string.Empty, tab.BuildSettingsBlock()["CrcDefaultsFor"]);
	}

	[Fact]
	public void every_geojson_file_gets_them_when_chosen()
	{
		AirportsViewModel tab = new() { CrcDefaultsScope = CrcDefaultsScope.AllGeojsonFiles };

		Assert.True(tab.HasCrcDefaultsInUse);
		Assert.Equal("Runways_Lines,Airports_Symbols,Airports_Text", tab.BuildSettingsBlock()["CrcDefaultsFor"]);
		Assert.Single(tab.SymbolDefaultsInUse);
	}

	[Fact]
	public void specific_files_need_one_ticked()
	{
		AirportsViewModel tab = new() { CrcDefaultsScope = CrcDefaultsScope.SpecificFiles };

		Assert.True(tab.IsCrcDefaultsSpecific);
		Assert.StartsWith("CRC-ERAM defaults are set to go on specific files, but none is ticked.", tab.ValidationError, StringComparison.Ordinal);

		File(tab, "Airports_Symbols").HasCrcDefaults = true;

		Assert.Equal("Airports_Symbols", tab.BuildSettingsBlock()["CrcDefaultsFor"]);
		Assert.True(tab.IsDirty);

		File(tab, "Airports_Symbols").HasCrcDefaults = false;

		Assert.Equal(string.Empty, tab.BuildSettingsBlock()["CrcDefaultsFor"]);
	}

	/// <summary>With GeoJSON off on the General tab there is nothing to put CRC-ERAM defaults on, so the card hides.</summary>
	[Fact]
	public void with_geojson_off_the_card_hides_and_nothing_gets_them()
	{
		AirportsViewModel tab = new() { CrcDefaultsScope = CrcDefaultsScope.AllGeojsonFiles };
		SubServiceRow row = new(AiracSubServices.All.Single(d => d.Key == AiracSubServices.AirportsKey), () => { });
		row.Load(included: true, SubServiceOutputKinds.Alias);

		tab.AttachOutputs(row);

		Assert.False(tab.ShowsCrcDefaultsCard);
		Assert.False(tab.HasCrcDefaultsInUse);
		Assert.Equal(string.Empty, tab.BuildSettingsBlock()["CrcDefaultsFor"]);
	}

	[Fact]
	public void the_choice_is_saved_under_the_tab_and_read_back()
	{
		AirportsViewModel tab = new() { CrcDefaultsScope = CrcDefaultsScope.SpecificFiles };
		File(tab, "Airports_Text").HasCrcDefaults = true;
		File(tab, "Airports_Symbols").HasCrcDefaults = true;
		tab.CrcDefaultsScope = CrcDefaultsScope.None;

		Assert.True(tab.Save());

		Assert.Equal("None", UserConfigFile.GetValue($"{Node}.CrcDefaultsScope"));
		Assert.Equal("Airports_Symbols,Airports_Text", UserConfigFile.GetValue($"{Node}.CrcDefaultsFiles"));

		AirportsViewModel reloaded = new();
		Assert.Equal(CrcDefaultsScope.None, reloaded.CrcDefaultsScope);
		reloaded.CrcDefaultsScope = CrcDefaultsScope.SpecificFiles;
		Assert.Equal("Airports_Symbols,Airports_Text", reloaded.BuildSettingsBlock()["CrcDefaultsFor"]);
	}
}
