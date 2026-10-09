using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the Area card's view model (issue #335, <see cref="IAreaSettings"/> on
/// <see cref="GeojsonSubServiceViewModel"/>): one area picked from those the tab offers, saved and
/// loaded (or worked out from the tab's other settings), sent to a run with only its own filter,
/// checked while it matters, and described for the Preview Settings tab - and on Procedures, the
/// cards that add to the area - against a throwaway config.
/// </summary>
[Collection("AppLog")]
public sealed class AreaTests : IDisposable
{
	private const SubServiceOutputKinds Alias = SubServiceOutputKinds.Alias;
	private const SubServiceOutputKinds Changes = SubServiceOutputKinds.ProcedureChanges;
	private const SubServiceOutputKinds Json = SubServiceOutputKinds.ProceduresJson;

	private static readonly RegionOfInterest DefaultRoi = new(32.5, -120.0, 37.0, -114.0);

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_Area_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public AreaTests()
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

	private static void Attach(GeojsonSubServiceViewModel tab, string key, SubServiceOutputKinds on)
	{
		SubServiceRow row = new(AiracSubServices.All.Single(d => d.Key == key), () => { });
		row.Load(included: true, on);
		tab.AttachOutputs(row);
	}

	private static List<string?> Changed(ServiceTabViewModel tab)
	{
		List<string?> changed = [];
		tab.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
		return changed;
	}

	// ---- picking an area ----

	[Fact]
	public void each_tab_offers_its_own_areas()
	{
		Assert.True(new ArrivalsViewModel().OffersArtccs);
		Assert.False(new ArrivalsViewModel().OffersNone);
		Assert.False(new AirportsViewModel().OffersArtccs);
		Assert.True(new ProceduresViewModel().OffersNone);
		Assert.True(new ProceduresViewModel().OffersArtccs);
	}

	/// <summary>The card's radio buttons bind two-way: true picks, false (the one letting go) does nothing.</summary>
	[Fact]
	public void a_choice_set_true_picks_it_and_one_set_false_does_nothing()
	{
		ArrivalsViewModel tab = new();
		List<string?> changed = Changed(tab);

		tab.AreaRoi = true;

		Assert.Equal(SubServiceArea.Roi, tab.Area);
		Assert.True(tab.AreaRoi);
		Assert.False(tab.AreaEverything);
		Assert.True(tab.HasRoi);
		Assert.True(tab.IsDirty);
		Assert.Contains(nameof(GeojsonSubServiceViewModel.AreaRoi), changed);
		Assert.Contains(nameof(GeojsonSubServiceViewModel.AreaEverything), changed);
		Assert.Contains(nameof(ServiceTabViewModel.WhatYoullGet), changed);

		tab.AreaEverything = false;
		Assert.Equal(SubServiceArea.Roi, tab.Area);

		tab.AreaArtccs = true;
		Assert.Equal(SubServiceArea.Artccs, tab.Area);

		tab.AreaEverything = true;
		Assert.Equal(SubServiceArea.Everything, tab.Area);
	}

	[Fact]
	public void an_area_the_tab_doesnt_offer_cant_be_picked()
	{
		AirportsViewModel airports = new();
		ArrivalsViewModel arrivals = new() { Area = SubServiceArea.Everything };

		airports.AreaArtccs = true;
		arrivals.AreaNone = true;

		Assert.Equal(SubServiceArea.Everything, airports.Area);
		Assert.Equal(SubServiceArea.Everything, arrivals.Area);
	}

	[Fact]
	public void the_roi_is_the_default_one_or_the_tabs_own()
	{
		AirportsViewModel tab = new() { Area = SubServiceArea.Roi };
		Assert.True(tab.UseDefaultRoi);

		tab.OverrideRoi = true;
		Assert.False(tab.UseDefaultRoi);

		tab.UseDefaultRoi = false;
		Assert.True(tab.OverrideRoi);

		tab.UseDefaultRoi = true;
		Assert.False(tab.OverrideRoi);
	}

	// ---- saving and loading ----

	[Fact]
	public void the_area_is_saved_and_loaded()
	{
		DefaultRoiStore.Set(DefaultRoi);
		UserConfigFile.TrySetValue("Services.AiracService.Arrivals.ArtccFilter", "ZOB");
		ArrivalsViewModel tab = new();
		Assert.Equal(SubServiceArea.Artccs, tab.Area);

		tab.Area = SubServiceArea.Roi;

		Assert.True(tab.Save());
		Assert.Equal("Roi", UserConfigFile.GetValue("Services.AiracService.Arrivals.Area"));
		Assert.Equal(SubServiceArea.Roi, new ArrivalsViewModel().Area);
	}

	/// <summary>A saved area the tab doesn't offer (a hand edit) is worked out from its other settings instead.</summary>
	[Fact]
	public void a_saved_area_the_tab_doesnt_offer_is_worked_out_instead()
	{
		UserConfigFile.TrySetValue("Services.AiracService.Airports.Area", "Artccs");
		DefaultRoiStore.Set(DefaultRoi);

		Assert.Equal(SubServiceArea.Roi, new AirportsViewModel().Area);
	}

	[Fact]
	public void a_tab_never_given_an_area_starts_on_the_one_its_settings_mean()
	{
		UserConfigFile.TrySetValue(UserConfigAreas.FacilityKey, "ZOB");

		Assert.Equal(SubServiceArea.Artccs, new DeparturesViewModel().Area);
		Assert.Equal(SubServiceArea.Everything, new AirportsViewModel().Area);
	}

	/// <summary>Telephony has no area: it saves neither an area nor an ROI, and sends neither.</summary>
	[Fact]
	public void telephony_has_no_area_and_neither_saves_nor_sends_one()
	{
		TelephonyViewModel tab = new();

		Assert.DoesNotContain(tab.CaptureCurrentValues().Keys, key => key == "Area" || key.StartsWith("Roi", StringComparison.Ordinal));
		Assert.DoesNotContain("Area", tab.BuildSettingsBlock().Keys);
		Assert.DoesNotContain("FilterByRoi", tab.BuildSettingsBlock().Keys);
	}

	// ---- what a run is sent ----

	[Fact]
	public void the_artccs_area_sends_its_artccs_and_no_roi()
	{
		DefaultRoiStore.Set(DefaultRoi);
		UserConfigFile.TrySetValue("Services.AiracService.Arrivals.ArtccFilter", "ZOB,ZNY");
		ArrivalsViewModel tab = new() { Area = SubServiceArea.Artccs };

		IReadOnlyDictionary<string, string> block = tab.BuildSettingsBlock();

		Assert.Equal("Artccs", block["Area"]);
		Assert.Equal("N", block["FilterByRoi"]);
		Assert.Contains("ZOB", block["ArtccFilter"], StringComparison.Ordinal);
		Assert.DoesNotContain("RoiMode", block.Keys);
		Assert.DoesNotContain("RoiSwLat", block.Keys);
	}

	[Fact]
	public void the_roi_area_sends_the_default_roi_or_the_tabs_own_and_no_artccs()
	{
		DefaultRoiStore.Set(DefaultRoi);
		UserConfigFile.TrySetValue("Services.AiracService.Departures.ArtccFilter", "ZOB");
		DeparturesViewModel tab = new() { Area = SubServiceArea.Roi };

		IReadOnlyDictionary<string, string> block = tab.BuildSettingsBlock();

		Assert.Equal("Roi", block["Area"]);
		Assert.Equal("Y", block["FilterByRoi"]);
		Assert.Equal("32.5", block["RoiSwLat"]);
		Assert.Equal("Airport", block["RoiMode"]);
		Assert.DoesNotContain("ArtccFilter", block.Keys);

		tab.OverrideRoi = true;
		tab.SwLat = "40";

		Assert.Equal("40", tab.BuildSettingsBlock()["RoiSwLat"]);
	}

	[Fact]
	public void everything_sends_neither_the_artccs_nor_the_roi()
	{
		DefaultRoiStore.Set(DefaultRoi);
		ArtccBoundariesViewModel tab = new() { Area = SubServiceArea.Everything };

		IReadOnlyDictionary<string, string> block = tab.BuildSettingsBlock();

		Assert.Equal("Everything", block["Area"]);
		Assert.Equal("N", block["FilterByRoi"]);
		Assert.DoesNotContain("LocationFilter", block.Keys);
		Assert.DoesNotContain("RoiSwLat", block.Keys);
	}

	// ---- what is checked ----

	[Fact]
	public void the_artccs_area_needs_an_artcc_ticked()
	{
		UserConfigFile.TrySetValue("Services.AiracService.Arrivals.ArtccFilter", string.Empty);
		ArrivalsViewModel tab = new() { Area = SubServiceArea.Artccs };

		Assert.Contains("Tick at least one ARTCC on the Area card, or pick another area.", tab.ValidationErrors);
		Assert.NotNull(tab.FieldErrors[ServiceAreas.Area]);

		tab.Area = SubServiceArea.Everything;

		Assert.Empty(tab.ValidationErrors);
	}

	[Fact]
	public void the_roi_area_needs_a_default_roi_or_the_tabs_own()
	{
		AirportsViewModel tab = new() { Area = SubServiceArea.Roi };

		Assert.Contains(
			"No default ROI is set. Set one in Settings or on the Map page, or pick \"An ROI specific to Airports\" on the Area card.",
			tab.ValidationErrors);
		Assert.False(tab.HasDefaultRoi);
		Assert.StartsWith("No default ROI is set.", tab.DefaultRoiSummary, StringComparison.Ordinal);

		DefaultRoiStore.Set(DefaultRoi);
		tab.Area = SubServiceArea.Everything;
		tab.Area = SubServiceArea.Roi;

		Assert.Empty(tab.ValidationErrors);
		Assert.True(tab.HasDefaultRoi);
		Assert.Equal("SW 32.5, -120 / NE 37, -114. Change it in Settings or on the Map page.", tab.DefaultRoiSummary);
	}

	[Fact]
	public void the_tabs_own_roi_needs_four_valid_corners_but_only_while_the_area_is_the_roi()
	{
		WxStationsViewModel tab = new() { OverrideRoi = true };
		Assert.Empty(tab.ValidationErrors);

		tab.Area = SubServiceArea.Roi;

		Assert.Contains("Enter the southwest latitude of the ROI specific to Wx Stations.", tab.ValidationErrors);

		tab.SwLat = "40";
		tab.SwLon = "-80";
		tab.NeLat = "39";
		tab.NeLon = "-79";

		Assert.Contains(tab.ValidationErrors, error => error.StartsWith("The ROI specific to Wx Stations: ", StringComparison.Ordinal));

		tab.NeLat = "41";

		Assert.Empty(tab.ValidationErrors);
	}

	/// <summary>While every output the area narrows is off, its card greys out and the area isn't checked.</summary>
	[Fact]
	public void the_area_isnt_checked_while_the_outputs_it_narrows_are_off()
	{
		AirportsViewModel tab = new() { Area = SubServiceArea.Roi };
		Assert.NotEmpty(tab.ValidationErrors);

		Attach(tab, AiracSubServices.AirportsKey, Alias);

		Assert.Empty(tab.ValidationErrors);
	}

	// ---- the Preview Settings tab ----

	[Fact]
	public void the_area_row_says_which_area_and_what_it_has()
	{
		UserConfigFile.TrySetValue("Services.AiracService.ArtccBoundaries.LocationFilter", "ZOB,ZNY");
		ArtccBoundariesViewModel tab = new() { Area = SubServiceArea.Artccs };

		string AreaRow() => tab.BuildPreviewSummary().Single().Rows.Single(row => row.Label == "Area").Value;

		Assert.Equal("ARTCCs: ZNY, ZOB", AreaRow());

		tab.Area = SubServiceArea.Roi;
		Assert.Equal("The default ROI, which isn't set", AreaRow());

		DefaultRoiStore.Set(DefaultRoi);
		Assert.Equal("The default ROI: SW 32.5, -120 / NE 37, -114", AreaRow());

		tab.OverrideRoi = true;
		tab.SwLat = "40";
		tab.SwLon = "-80";
		tab.NeLat = "41";
		tab.NeLon = "-79";
		Assert.Equal("The ROI specific to ARTCC Boundaries: SW 40, -80 / NE 41, -79", AreaRow());

		tab.Area = SubServiceArea.Everything;
		Assert.Equal("Everything", AreaRow());
	}

	// ---- Procedures: the cards that add to the area ----

	private static ProceduresViewModel Procedures(SubServiceOutputKinds on = Changes | Json)
	{
		ProceduresViewModel tab = new();
		tab.LoadCycleDependentLists(new NasrCsvDataCollection
		{
			Apt = new AptCsvDataCollection
			{
				AptBase = [new AptCsvDataModel.AptBase { ArptId = "CLE", IcaoId = "KCLE", RespArtccId = "ZOB", BaseLatDecimal = 41.41, BaseLongDecimal = -81.85 }],
			},
		});
		Attach(tab, AiracSubServices.ProceduresKey, on);
		return tab;
	}

	[Fact]
	public void procedures_cards_that_add_to_the_area_say_what_they_add_to()
	{
		ProceduresViewModel tab = Procedures();
		List<string?> changed = Changed(tab);

		tab.Area = SubServiceArea.Artccs;

		Assert.Contains(nameof(ProceduresViewModel.AirportsIntro), changed);
		Assert.Equal("Even if outside the facilities you tick, the airports you list here will be included.", tab.AirportsIntro);
		Assert.Equal(
			"Even if outside the facilities you tick, the procedures you list here will be included at every airport they serve, such as an arrival shared by several airports.",
			tab.ProcedureNamesIntro);
		Assert.Equal("Even if outside the facilities you tick, the airport and procedure pairs you add here will be included.", tab.AirportProceduresIntro);
		Assert.StartsWith("Only charts of the types you tick are included from the facilities you tick and the airports you list.", tab.ChartTypesIntro, StringComparison.Ordinal);

		tab.Area = SubServiceArea.Roi;

		Assert.Equal("Even if outside the ROI, the airports you list here will be included.", tab.AirportsIntro);
		Assert.StartsWith("Only charts of the types you tick are included from the ROI and the airports you list.", tab.ChartTypesIntro, StringComparison.Ordinal);

		tab.Area = SubServiceArea.None;

		Assert.Equal("The airports you list here will be included.", tab.AirportsIntro);
		Assert.StartsWith("Only charts of the types you tick are included from the airports you list.", tab.ChartTypesIntro, StringComparison.Ordinal);
		Assert.True(tab.AreListsUsed);
	}

	/// <summary>Everything already has every airport: the cards aren't used, keep their lists, and aren't sent.</summary>
	[Fact]
	public void procedures_with_everything_keeps_the_lists_but_doesnt_use_them()
	{
		UserConfigFile.TrySetValue("Services.AiracService.Procedures.Airports", "CLE");
		ProceduresViewModel tab = Procedures();

		tab.Area = SubServiceArea.Everything;

		Assert.False(tab.AreListsUsed);
		Assert.Equal("Only charts of the types you tick are included.", tab.ChartTypesIntro);
		Assert.Equal(["CLE"], tab.Airports);
		Assert.DoesNotContain("Airports", tab.BuildSettingsBlock().Keys);
		Assert.DoesNotContain("Facilities", tab.BuildSettingsBlock().Keys);
		Assert.Empty(tab.ValidationErrors);
	}

	[Fact]
	public void procedures_with_no_area_needs_something_listed()
	{
		ProceduresViewModel tab = Procedures();
		tab.Area = SubServiceArea.None;

		Assert.Contains(
			"With \"None\" picked on the Area card, list at least one airport or procedure for the documents to include, or pick another area.",
			tab.ValidationErrors);

		tab.Airports.Add("CLE");
		tab.Area = SubServiceArea.Artccs;
		tab.Area = SubServiceArea.None;

		Assert.Empty(tab.ValidationErrors);
		Assert.Equal("CLE", tab.BuildSettingsBlock()["Airports"]);
	}

	[Fact]
	public void procedures_facilities_area_needs_a_facility_ticked_and_sends_the_facilities()
	{
		ProceduresViewModel tab = Procedures();
		tab.Area = SubServiceArea.Artccs;

		Assert.Contains("Tick at least one facility on the Area card, or pick another area.", tab.ValidationErrors);

		tab.Facilities.Single(f => f.Artcc == "ZOB").IsSelected = true;

		Assert.Empty(tab.ValidationErrors);
		Assert.Equal("ZOB", tab.BuildSettingsBlock()["Facilities"]);
		Assert.Equal("Facilities: ZOB", tab.BuildPreviewSummary().Single().Rows.Single(row => row.Label == "Area").Value);
	}

	/// <summary>With only the alias file on, the area picks nothing, so it isn't checked.</summary>
	[Fact]
	public void procedures_area_isnt_checked_with_only_the_alias_file_on()
	{
		ProceduresViewModel tab = Procedures(Alias);
		tab.Area = SubServiceArea.Artccs;

		Assert.Empty(tab.ValidationErrors);
	}
}
