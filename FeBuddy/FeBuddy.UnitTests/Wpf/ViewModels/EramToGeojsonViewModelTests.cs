using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers <see cref="EramToGeojsonViewModel"/>: a layout saved before the three loads as its
/// nearest, one Geomaps file per run, what is sent to a run, and the <c>feb.*</c> check - against
/// a throwaway config and folder.
/// </summary>
[Collection("AppLog")]
public sealed class EramToGeojsonViewModelTests : IDisposable
{
	private const string Node = "Services.FileConversions.EramToGeojson";

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_EramTab_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public EramToGeojsonViewModelTests()
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

	[Theory]
	[InlineData("ByObject", true, false)]
	[InlineData("ByFilter", false, true)]
	[InlineData(null, true, false)]
	public void a_layout_saved_before_the_three_loads_as_its_nearest(string? saved, bool byAttributes, bool byFilters)
	{
		if (saved is not null)
		{
			UserConfigFile.TrySetValue($"{Node}.OutputLayout", saved);
		}

		EramToGeojsonViewModel tab = new();

		Assert.Equal(byAttributes, tab.LayoutByAttributes);
		Assert.Equal(byFilters, tab.LayoutByFilters);
		Assert.False(tab.LayoutRaw);
		Assert.False(tab.IsDirty);
	}

	/// <summary>Two Geomaps files in the folder would land on each other's names, so the run waits for one to be picked.</summary>
	[Fact]
	public void a_folder_with_two_geomaps_files_blocks_the_run()
	{
		string folder = Path.Combine(_root, "Export");
		Directory.CreateDirectory(folder);
		File.WriteAllText(Path.Combine(folder, "A.xml"), "<Geomaps_Records />");
		File.WriteAllText(Path.Combine(folder, "Airport.xml"), "<Airport_Records />");

		EramToGeojsonViewModel tab = new() { SourceFolder = folder };
		Assert.True(tab.OneSourceFileOnly);
		Assert.Null(tab.RunBlocker);

		File.WriteAllText(Path.Combine(folder, "B.xml"), "<Geomaps_Records />");
		tab.SourceFolder = string.Empty;
		tab.SourceFolder = folder;

		Assert.StartsWith("The source folder has 2 Geomaps files, and this conversion takes one per run.", tab.RunBlocker, StringComparison.Ordinal);
	}

	[Fact]
	public void the_run_gets_the_layout_and_the_chosen_feb_properties()
	{
		EramToGeojsonViewModel tab = new()
		{
			LayoutRaw = true,
			IncludeFebCustomProperties = true,
		};
		tab.FebProperties.Single(p => p.Name == "lineObjectId").IsSelected = true;
		tab.FebProperties.Single(p => p.Name == "mapObjectType").IsSelected = true;

		IReadOnlyDictionary<string, string> block = tab.BuildSettingsBlock(Path.Combine(_root, "Out"), addFeBuddyOutputFolder: true);

		Assert.Equal("Raw", block["OutputLayout"]);
		Assert.Equal("Y", block["IncludeFebCustomProperties"]);
		Assert.Equal("mapObjectType,lineObjectId", block["FebProperties"]);
		Assert.Null(tab.ValidationError);
	}

	[Fact]
	public void feb_properties_on_with_none_picked_needs_fixing()
	{
		EramToGeojsonViewModel tab = new() { IncludeFebCustomProperties = true };

		Assert.Equal("FE-Buddy properties are on but none are selected. Pick at least one, or switch them off.", tab.ValidationError);
	}

	/// <summary>An empty or missing folder has nothing to lose, so the run starts without asking.</summary>
	[Fact]
	public void a_run_with_nothing_to_empty_asks_nothing()
	{
		EramToGeojsonViewModel tab = new();

		Assert.True(tab.ConfirmRun(tab.BuildSettingsBlock(Path.Combine(_root, "Out"), addFeBuddyOutputFolder: false)));

		Directory.CreateDirectory(Path.Combine(_root, "Out", "ERAM_TO_GEOJSON"));
		Assert.True(tab.ConfirmRun(tab.BuildSettingsBlock(Path.Combine(_root, "Out"), addFeBuddyOutputFolder: false)));
	}

	[Fact]
	public void saved_choices_come_back()
	{
		EramToGeojsonViewModel tab = new() { LayoutByFilters = true, DefaultsFromCard = true, IncludeFebCustomProperties = true };
		tab.FebProperties.Single(p => p.Name == "saaId").IsSelected = true;
		tab.LineDefaults[0].Bcg = "1";
		tab.LineDefaults[0].Filters = "1";
		tab.LineDefaults[0].Style = "solid";
		tab.LineDefaults[0].Thickness = "1";
		tab.IncludeCrcSymbolDefaults = false;
		tab.IncludeCrcTextDefaults = false;
		Assert.True(tab.Save());

		EramToGeojsonViewModel reloaded = new();

		Assert.True(reloaded.LayoutByFilters);
		Assert.True(reloaded.DefaultsFromCard);
		Assert.True(reloaded.IncludeFebCustomProperties);
		Assert.Equal(["saaId"], reloaded.FebProperties.Where(p => p.IsSelected).Select(p => p.Name));
	}
}
