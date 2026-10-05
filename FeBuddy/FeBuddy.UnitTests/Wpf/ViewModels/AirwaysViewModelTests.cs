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
/// Covers the High and Low Files card of <see cref="AirwaysViewModel"/>: the file each designation
/// goes in, its defaults, the designations that still need one, and what is saved and sent - against
/// a throwaway config and a stand-in cycle.
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
		Assert.Equal("Choose High, Low or Both.", Designation(tab, "Y").StratumError);
		Assert.Null(Designation(tab, "J").StratumError);

		Assert.True(tab.ShowsStrata);
		Assert.False(tab.IsDirty);
		Assert.Equal(
			"Choose High, Low or Both for Y, ZK on the High and Low Files card, or untick them under Designations to Include.",
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
		Assert.Equal("Q", UserConfigFile.GetValue("Services.AiracService.Geojson.Airways.HighDesignations"));
		Assert.Equal("T,V,Y", UserConfigFile.GetValue("Services.AiracService.Geojson.Airways.LowDesignations"));
		Assert.Equal("J", UserConfigFile.GetValue("Services.AiracService.Geojson.Airways.BothDesignations"));

		AirwaysViewModel reloaded = NewTab("J", "Q", "V", "Y", "ZK");

		Assert.Equal(AirwayStratum.Both, Designation(reloaded, "J").Stratum);
		Assert.Equal(AirwayStratum.High, Designation(reloaded, "Q").Stratum);
		Assert.Equal(AirwayStratum.Low, Designation(reloaded, "Y").Stratum);
		Assert.Null(Designation(reloaded, "ZK").Stratum);
		Assert.StartsWith("Choose High, Low or Both for ZK ", reloaded.ValidationError, StringComparison.Ordinal);
		Assert.False(reloaded.IsDirty);
	}

	/// <summary>Once anything is saved, the defaults no longer fill gaps: a J the user never had stays unchosen.</summary>
	[Fact]
	public void after_a_save_the_defaults_no_longer_apply()
	{
		UserConfigFile.TrySetValue("Services.AiracService.Geojson.Airways.LowDesignations", "V");

		AirwaysViewModel tab = NewTab("J", "V");

		Assert.Null(Designation(tab, "J").Stratum);
		Assert.Equal(AirwayStratum.Low, Designation(tab, "V").Stratum);
	}

	/// <summary>
	/// "None" was the GeoJSON switch before the General tab had one: it loads as High and Low files,
	/// and while the General tab has GeoJSON off the run is sent None, with the strata card hidden.
	/// </summary>
	[Fact]
	public void geojson_off_on_the_general_tab_sends_none_and_an_old_none_loads_as_high_and_low()
	{
		UserConfigFile.TrySetValue("Services.AiracService.Geojson.Airways.OutputBy", "None");
		AirwaysViewModel tab = NewTab("J");
		Assert.Equal(AirwayGeojsonOutputBy.HighLow, tab.OutputBy);
		Assert.True(tab.ShowsStrata);

		SubServiceRow row = new(AiracSubServices.All.Single(d => d.Key == AiracSubServices.AirwaysKey), () => { });
		row.Load(included: true, SubServiceOutputKinds.Alias);
		tab.AttachOutputs(row);

		Assert.False(tab.GenerateGeojson);
		Assert.False(tab.ShowsStrata);
		Assert.Equal("None", tab.BuildSettingsBlock()["OutputBy"]);
		Assert.Equal(["Airways.txt"], tab.OutputFileEntries().Select(file => file.Key));
		Assert.False(tab.IsDirty);
	}

	/// <summary>None is never one of the split choices: GeoJSON is turned off on the General tab.</summary>
	[Fact]
	public void none_is_not_a_split_choice()
	{
		Assert.DoesNotContain(AirwayGeojsonOutputBy.None, AirwaysViewModel.OutputByValues);

		AirwaysViewModel tab = NewTab("J");
		tab.OutputBy = AirwayGeojsonOutputBy.None;

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
