using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the "What You'll Get" card of a few sub-service tabs (issue #314): one line per filter
/// that is set, a block per output only where the outputs get different things, the outputs that
/// are off left out, and the card told again when a setting, the region or an output changes -
/// against a throwaway config.
/// </summary>
[Collection("AppLog")]
public sealed class WhatYoullGetTests : IDisposable
{
	private const SubServiceOutputKinds Alias = SubServiceOutputKinds.Alias;
	private const SubServiceOutputKinds Geojson = SubServiceOutputKinds.Geojson;
	private const SubServiceOutputKinds Changes = SubServiceOutputKinds.ProcedureChanges;
	private const SubServiceOutputKinds Json = SubServiceOutputKinds.ProceduresJson;

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_WhatYoullGet_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public WhatYoullGetTests()
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

	/// <summary>A block's lines as the card reads them: <c>AND in ZNY or ZOB</c>.</summary>
	private static string[] Lines(SummaryBlock block) => [.. block.Lines.Select(line => $"{line.JoinWord} {line.Text}".Trim())];

	/// <summary>Turns on only <paramref name="on"/> for the tab's sub-service, as the General tab would.</summary>
	private static SubServiceRow Attach(GeojsonSubServiceViewModel tab, string key, SubServiceOutputKinds on)
	{
		SubServiceRow row = new(AiracSubServices.All.Single(d => d.Key == key), () => { });
		row.Load(included: true, on);
		tab.AttachOutputs(row);
		return row;
	}

	private static List<string?> Changed(ServiceTabViewModel tab)
	{
		List<string?> changed = [];
		tab.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
		return changed;
	}

	// ---- Departures ----

	[Fact]
	public void departures_lists_only_the_filters_that_are_set()
	{
		DeparturesViewModel tab = new();

		SummaryBlock block = Assert.Single(tab.WhatYoullGet);
		Assert.Equal(Alias | Geojson, block.Outputs);
		Assert.Equal(["SIDs and obstacle departures", "AND in every ARTCC"], Lines(block));
	}

	[Fact]
	public void departures_narrows_by_artcc_amendment_and_region_and_the_card_is_told_of_each_change()
	{
		UserConfigFile.TrySetValue("Services.AiracService.Departures.ArtccFilter", "ZOB,ZNY");
		UserConfigFile.TrySetValue("Services.AiracService.Departures.Amendment.Filter", "Cycles");
		UserConfigFile.TrySetValue("Services.AiracService.Departures.Amendment.WithinCycles", "4");
		DeparturesViewModel tab = new();
		List<string?> changed = Changed(tab);

		tab.IncludeObstacleDepartures = false;
		tab.OverrideRoi = true;

		Assert.Contains(nameof(ServiceTabViewModel.WhatYoullGet), changed);
		Assert.Contains(nameof(ServiceTabViewModel.WhatYoullGetOutputs), changed);
		Assert.Equal(
			[
				"SIDs only, no obstacle departures",
				"AND in ZNY or ZOB",
				"AND amended in the last 4 cycles",
				"AND at an airport inside the region (this tab's own ROI)",
			],
			Lines(Assert.Single(tab.WhatYoullGet)));
	}

	[Fact]
	public void an_output_turned_off_on_the_general_tab_leaves_the_card()
	{
		DeparturesViewModel tab = new();
		SubServiceRow row = Attach(tab, AiracSubServices.DeparturesKey, Alias | Geojson);
		List<string?> changed = Changed(tab);

		row.Geojson = false;

		Assert.Contains(nameof(ServiceTabViewModel.WhatYoullGet), changed);
		Assert.Equal(Alias, Assert.Single(tab.WhatYoullGet).Outputs);
		Assert.Equal(Alias, tab.WhatYoullGetOutputs);

		row.Alias = false;

		Assert.Empty(tab.WhatYoullGet);
		Assert.Equal(SubServiceOutputKinds.None, tab.WhatYoullGetOutputs);
	}

	// ---- Airports ----

	/// <summary>With no region both outputs get the same, so the card has one block; the default ROI splits them.</summary>
	[Fact]
	public void airports_splits_into_a_block_per_output_once_a_region_narrows_the_geojson()
	{
		AirportsViewModel tab = new();
		Assert.Equal(["every open airport"], Lines(Assert.Single(tab.WhatYoullGet)));
		Assert.Equal(Alias | Geojson, tab.WhatYoullGetOutputs);
		List<string?> changed = Changed(tab);

		DefaultRoiStore.Set(new RegionOfInterest(39.5, -85.25, 43.75, -78.5));

		Assert.Contains(nameof(ServiceTabViewModel.WhatYoullGet), changed);
		Assert.Equal([Geojson, Alias], tab.WhatYoullGet.Select(block => block.Outputs));
		Assert.Equal(["every open airport", "AND with its reference point inside the region (your default ROI)"], Lines(tab.WhatYoullGet[0]));
		Assert.Equal(["every open airport, whatever the region"], Lines(tab.WhatYoullGet[1]));

		Attach(tab, AiracSubServices.AirportsKey, Alias);

		Assert.Equal(["every open airport, whatever the region"], Lines(Assert.Single(tab.WhatYoullGet)));
	}

	// ---- Airways ----

	private static AirwaysViewModel AirwaysTab(params string[] designations)
	{
		AwyCsvDataCollection airways = new();

		foreach (string designation in designations)
		{
			airways.AwyBase.Add(new AwyCsvDataModel.AwyBase { AwyId = designation + "1", AwyDesignation = designation, AwyLocation = "C" });
		}

		AirwaysViewModel tab = new();
		tab.LoadCycleDependentLists(new NasrCsvDataCollection { Awy = airways });
		return tab;
	}

	[Fact]
	public void airways_names_the_excluded_types_and_says_whether_the_region_narrows_the_alias_file()
	{
		AirwaysViewModel tab = AirwaysTab("J", "V", "Y");
		tab.Designations.Single(d => d.Designation == "Y").Included = false;

		Assert.Equal(["every airway except the Y airways"], Lines(Assert.Single(tab.WhatYoullGet)));

		tab.OverrideRoi = true;

		Assert.Equal(["every airway except the Y airways", "AND that cross the region, cut off at its edge (this tab's own ROI)"], Lines(tab.WhatYoullGet[0]));
		Assert.Equal(["every airway except the Y airways, whatever the region"], Lines(tab.WhatYoullGet[1]));

		tab.AliasRoiAirwaysOnly = true;

		Assert.Equal(
			["every airway except the Y airways", "AND that cross the region, each with all of its waypoints (this tab's own ROI)"],
			Lines(tab.WhatYoullGet[1]));
	}

	/// <summary>Before a cycle is loaded there are no toggles: the saved exclusions stand in.</summary>
	[Fact]
	public void airways_before_a_cycle_uses_the_saved_exclusions()
	{
		UserConfigFile.TrySetValue("Services.AiracService.Airways.ExcludedDesignations", "V,J");

		Assert.Equal(["every airway except the J and V airways"], Lines(Assert.Single(new AirwaysViewModel().WhatYoullGet)));
	}

	// ---- Procedures ----

	private static ProceduresViewModel ProceduresTab(SubServiceOutputKinds on)
	{
		ProceduresViewModel tab = new();
		Attach(tab, AiracSubServices.ProceduresKey, on);
		return tab;
	}

	[Fact]
	public void procedures_lists_each_way_an_airport_comes_in_then_the_types_then_what_is_added()
	{
		UserConfigFile.TrySetValue("Services.AiracService.Procedures.Facilities", "ZOB");
		UserConfigFile.TrySetValue("Services.AiracService.Procedures.Airports", "CLE,DTW");
		UserConfigFile.TrySetValue("Services.AiracService.Procedures.IncludeRoiAirports", "Y");
		UserConfigFile.TrySetValue("Services.AiracService.Procedures.Procedures", "BRWNZ FIVE");
		ProceduresViewModel tab = ProceduresTab(Alias | Changes | Json);
		tab.OverrideRoi = true;

		Assert.Equal([Changes | Json, Alias], tab.WhatYoullGet.Select(block => block.Outputs));
		Assert.Equal(
			[
				"every chart at airports in ZOB",
				"OR at CLE or DTW",
				"OR at an airport inside the region (this tab's own ROI)",
				"AND of these types: IAP, STR, DP, ODP, DAU and APD",
				"PLUS BRWNZ FIVE, wherever it's published",
				"AND in Procedure_Changes.md, only those added, changed or deleted this cycle",
			],
			Lines(tab.WhatYoullGet[0]));
		Assert.Equal(["every chart at every airport in the d-TPP metafile, whatever you pick for the documents"], Lines(tab.WhatYoullGet[1]));
	}

	[Fact]
	public void procedures_counts_a_long_airport_list_and_flags_no_chart_type()
	{
		UserConfigFile.TrySetValue("Services.AiracService.Procedures.Airports", "CLE,DTW,CAK,PIT,ORD,MDW,BUF");
		UserConfigFile.TrySetValue("Services.AiracService.Procedures.Procedures", "BRWNZ FIVE,CLVLD TWO");
		ProceduresViewModel tab = ProceduresTab(Json);

		foreach (ProcedureOptionToggle type in tab.ChartTypeToggles)
		{
			type.IsSelected = false;
		}

		Assert.Equal(
			[
				"every chart at the 7 airports you listed",
				"AND of no type yet: tick a chart type",
				"PLUS BRWNZ FIVE and CLVLD TWO, wherever they're published",
			],
			Lines(Assert.Single(tab.WhatYoullGet)));
	}

	[Fact]
	public void procedures_with_nothing_picked_says_what_to_pick()
	{
		Assert.Equal(
			["nothing yet: pick a facility, an airport or a procedure"],
			Lines(Assert.Single(ProceduresTab(Json).WhatYoullGet)));
	}
}
