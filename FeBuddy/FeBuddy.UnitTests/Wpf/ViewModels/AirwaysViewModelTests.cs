using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the High and Low Airway Classification card of <see cref="AirwaysViewModel"/>: the file each designation
/// goes in, its defaults, the designations that still need one, and what is saved and sent - plus
/// what the run gets with GeoJSON off on the General tab, and an unknown saved split - against a
/// throwaway config and a stand-in cycle.
/// </summary>
[Collection("AppLog")]
public sealed class AirwaysViewModelTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_AirwaysTab_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public AirwaysViewModelTests()
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

	/// <summary>A cycle with one airway of each designation named.</summary>
	private static NasrCsvDataCollection Cycle(params string[] designations)
	{
		AwyCsvDataCollection airways = new();

		foreach (string designation in designations)
		{
			airways.AwyBase.Add(new AwyCsvDataModel.AwyBase { AwyId = designation + "1", AwyDesignation = designation, AwyLocation = "C" });
		}

		return new NasrCsvDataCollection { Awy = airways };
	}

	private static AirwaysViewModel NewTab(params string[] designations)
	{
		AirwaysViewModel tab = new();
		tab.LoadCycleDependentLists(Cycle(designations));
		return tab;
	}

	private static DesignationToggle Designation(AirwaysViewModel tab, string designation) =>
		tab.Designations.Single(d => d.Designation == designation);

	/// <summary>With nothing saved, J and Q are High and V and T Low; any other type waits for the user.</summary>
	[Fact]
	public void j_q_start_high_v_t_start_low_and_every_other_type_needs_a_choice()
	{
		AirwaysViewModel tab = NewTab("J", "Q", "T", "V", "Y", "ZK");

		Assert.Equal(AirwayStratum.High, Designation(tab, "J").Stratum);
		Assert.Equal(AirwayStratum.High, Designation(tab, "Q").Stratum);
		Assert.Equal(AirwayStratum.Low, Designation(tab, "T").Stratum);
		Assert.Equal(AirwayStratum.Low, Designation(tab, "V").Stratum);
		Assert.Null(Designation(tab, "Y").Stratum);
		Assert.Equal("Choose High, Low, or Both.", Designation(tab, "Y").StratumError);
		Assert.Null(Designation(tab, "J").StratumError);

		Assert.True(tab.ShowsStrata);
		Assert.False(tab.IsDirty);
		Assert.Equal(
			"Choose High, Low, or Both for Y, ZK on the High and Low Airway Classification card, or untick them under Airway Types to Include.",
			tab.ValidationError);
	}

	[Fact]
	public void choosing_a_file_or_excluding_the_type_clears_it()
	{
		AirwaysViewModel tab = NewTab("J", "Y", "ZK");

		Designation(tab, "Y").Stratum = AirwayStratum.Both;
		Designation(tab, "ZK").Included = false;

		Assert.Null(Designation(tab, "Y").StratumError);
		Assert.Null(Designation(tab, "ZK").StratumError);
		Assert.Null(tab.ValidationError);
		Assert.True(tab.IsDirty);
	}

	/// <summary>Only High and Low output asks: by designation, a file names its type anyway.</summary>
	[Fact]
	public void designation_output_needs_no_high_or_low_file()
	{
		AirwaysViewModel tab = NewTab("J", "Y");

		tab.OutputBy = AirwayGeojsonOutputBy.Designation;

		Assert.False(tab.ShowsStrata);
		Assert.Null(Designation(tab, "Y").StratumError);
		Assert.Null(tab.ValidationError);
	}

	[Fact]
	public void the_card_hides_once_no_type_is_included()
	{
		AirwaysViewModel tab = NewTab("J");

		Designation(tab, "J").Included = false;

		Assert.False(tab.ShowsStrata);
	}

	/// <summary>Every choice goes to the run, excluded types' included, as one list per file.</summary>
	[Fact]
	public void the_run_gets_the_designations_of_each_file()
	{
		AirwaysViewModel tab = NewTab("J", "Q", "T", "V", "Y", "ZK");
		Designation(tab, "Y").Stratum = AirwayStratum.High;
		Designation(tab, "ZK").Stratum = AirwayStratum.Both;
		Designation(tab, "ZK").Included = false;

		IReadOnlyDictionary<string, string> block = tab.BuildSettingsBlock();

		Assert.Equal("J,Q,Y", block["HighDesignations"]);
		Assert.Equal("T,V", block["LowDesignations"]);
		Assert.Equal("ZK", block["BothDesignations"]);
		Assert.Equal("ZK", block["ExcludedDesignations"]);
	}

	/// <summary>A High or Low file is listed - for CRC-ERAM defaults, and for renaming - only while an included type goes in it.</summary>
	[Fact]
	public void only_the_files_an_included_type_goes_in_are_listed()
	{
		AirwaysViewModel tab = NewTab("J", "V");

		Assert.Equal(["High", "Low"], tab.CrcFileRows.Select(row => row.Label));

		Designation(tab, "J").Included = false;

		Assert.Equal(["Low"], tab.CrcFileRows.Select(row => row.Label));
		Assert.DoesNotContain(tab.OutputFileEntries(), file => file.Key.StartsWith("Airways_High", StringComparison.Ordinal));

		Designation(tab, "V").Stratum = AirwayStratum.Both;

		Assert.Equal(["High", "Low"], tab.CrcFileRows.Select(row => row.Label));
	}

	/// <summary>The choices are saved, and a type a later cycle adds is flagged until the user chooses.</summary>
	[Fact]
	public void saved_choices_come_back_and_a_new_type_needs_one()
	{
		AirwaysViewModel tab = NewTab("J", "V", "Y");
		Designation(tab, "J").Stratum = AirwayStratum.Both;
		Designation(tab, "Y").Stratum = AirwayStratum.Low;
		Assert.True(tab.Save());

		// The defaults for Q and T are kept too, though this cycle has neither.
		UserConfigFile.ReadAll();
		Assert.Equal("Q", UserConfigFile.GetValue("Services.AiracService.Airways.HighDesignations"));
		Assert.Equal("T,V,Y", UserConfigFile.GetValue("Services.AiracService.Airways.LowDesignations"));
		Assert.Equal("J", UserConfigFile.GetValue("Services.AiracService.Airways.BothDesignations"));

		AirwaysViewModel reloaded = NewTab("J", "Q", "V", "Y", "ZK");

		Assert.Equal(AirwayStratum.Both, Designation(reloaded, "J").Stratum);
		Assert.Equal(AirwayStratum.High, Designation(reloaded, "Q").Stratum);
		Assert.Equal(AirwayStratum.Low, Designation(reloaded, "Y").Stratum);
		Assert.Null(Designation(reloaded, "ZK").Stratum);
		Assert.StartsWith("Choose High, Low, or Both for ZK ", reloaded.ValidationError, StringComparison.Ordinal);
		Assert.False(reloaded.IsDirty);
	}

	/// <summary>Once anything is saved, the defaults no longer fill gaps: a J the user never had stays unchosen.</summary>
	[Fact]
	public void after_a_save_the_defaults_no_longer_apply()
	{
		UserConfigFile.TrySetValue("Services.AiracService.Airways.LowDesignations", "V");

		AirwaysViewModel tab = NewTab("J", "V");

		Assert.Null(Designation(tab, "J").Stratum);
		Assert.Equal(AirwayStratum.Low, Designation(tab, "V").Stratum);
	}

	/// <summary>With GeoJSON off on the General tab, the run is told so and the strata card hides.</summary>
	[Fact]
	public void geojson_off_on_the_general_tab_sends_generate_geojson_n()
	{
		AirwaysViewModel tab = NewTab("J");
		Assert.Equal("Y", tab.BuildSettingsBlock()["GenerateGeojson"]);
		Assert.True(tab.ShowsStrata);

		SubServiceRow row = new(AiracSubServices.All.Single(d => d.Key == AiracSubServices.AirwaysKey), () => { });
		row.Load(included: true, SubServiceOutputKinds.Alias);
		tab.AttachOutputs(row);

		Assert.False(tab.GenerateGeojson);
		Assert.False(tab.ShowsStrata);
		Assert.Equal("N", tab.BuildSettingsBlock()["GenerateGeojson"]);
		Assert.Equal("HighLow", tab.BuildSettingsBlock()["OutputBy"]);
		Assert.Equal(["Airways.txt"], tab.OutputFileEntries().Select(file => file.Key));
		Assert.False(tab.IsDirty);
	}

	/// <summary>The cards' output tags follow the General tab: the tab says which outputs are on, and says so again when one changes.</summary>
	[Fact]
	public void the_outputs_on_follow_the_general_tab()
	{
		AirwaysViewModel tab = NewTab("J");
		Assert.Equal(OutputKinds.Every, tab.OutputsOn);

		SubServiceRow row = new(AiracSubServices.All.Single(d => d.Key == AiracSubServices.AirwaysKey), () => { });
		row.Load(included: true, SubServiceOutputKinds.Alias | SubServiceOutputKinds.Geojson);
		tab.AttachOutputs(row);
		List<string?> changed = [];
		tab.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		Assert.Equal(SubServiceOutputKinds.Alias | SubServiceOutputKinds.Geojson, tab.OutputsOn);

		row.Geojson = false;

		Assert.Equal(SubServiceOutputKinds.Alias, tab.OutputsOn);
		Assert.Contains(nameof(AirwaysViewModel.OutputsOn), changed);
	}

	/// <summary>Which airways the alias file gets is chosen on the Area card, so the card tags it whatever is picked.</summary>
	[Fact]
	public void the_area_card_tags_the_alias_file_too()
	{
		AirwaysViewModel tab = NewTab("J");

		Assert.Equal(SubServiceOutputKinds.Geojson | SubServiceOutputKinds.Alias, tab.AreaOutputs);

		tab.AliasRoiAirwaysOnly = true;

		Assert.Equal(SubServiceOutputKinds.Geojson | SubServiceOutputKinds.Alias, tab.AreaOutputs);
		Assert.True(tab.IsDirty);
	}

	/// <summary>An unknown saved split (a typo, or a number) falls back to High and Low files.</summary>
	[Fact]
	public void an_unknown_saved_split_loads_as_high_and_low()
	{
		UserConfigFile.TrySetValue("Services.AiracService.Airways.OutputBy", "7");

		Assert.Equal(AirwayGeojsonOutputBy.HighLow, NewTab("J").OutputBy);
	}

	/// <summary>The File Layout card's two radios set the split, and each follows a change made the other way.</summary>
	[Fact]
	public void the_file_layout_radios_set_the_split()
	{
		AirwaysViewModel tab = NewTab("J");
		List<string?> changed = [];
		tab.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
		Assert.True(tab.OutputHighLow);
		Assert.False(tab.OutputByDesignation);

		tab.OutputByDesignation = true;

		Assert.Equal(AirwayGeojsonOutputBy.Designation, tab.OutputBy);
		Assert.False(tab.OutputHighLow);
		Assert.Contains(nameof(AirwaysViewModel.OutputHighLow), changed);
		Assert.False(tab.ShowsStrata);

		tab.OutputByDesignation = false;
		Assert.Equal(AirwayGeojsonOutputBy.Designation, tab.OutputBy);

		tab.OutputHighLow = true;

		Assert.Equal(AirwayGeojsonOutputBy.HighLow, tab.OutputBy);
		Assert.True(tab.ShowsStrata);
		tab.OutputHighLow = false;
		Assert.Equal(AirwayGeojsonOutputBy.HighLow, tab.OutputBy);
	}

	[Fact]
	public void the_preview_says_which_types_go_in_each_file()
	{
		AirwaysViewModel tab = NewTab("J", "T", "Y", "ZK");
		Designation(tab, "ZK").Stratum = AirwayStratum.Both;

		string strata = tab.BuildPreviewSummary()[0].Rows.Single(row => row.Label == "High and Low files").Value;

		Assert.Equal("High: J · Low: T · Both: ZK · not chosen yet: Y", strata);
	}
}
