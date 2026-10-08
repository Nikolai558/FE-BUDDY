using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

using FeBuddy.Core.Application.Airac.Telephony;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the "What You'll Get" card of a few sub-service tabs (issues #314, #333, #334): one line
/// per filter that is set, each after the first narrowing ("with only") or adding ("along with"),
/// lists that read as inclusive, a block per output only where the outputs get different things,
/// the outputs that are off left out, and the card told again when a setting, the region or an
/// output changes - against a throwaway config.
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

	/// <summary>A block's lines as the card reads them: <c>with only those in ZNY and ZOB</c>.</summary>
	private static string[] Lines(SummaryBlock block) => Read(block.Lines);

	/// <summary>A block's "Outputs include" lines as the card reads them.</summary>
	private static string[] Includes(SummaryBlock block) => Read(block.Includes);

	private static string[] Read(IReadOnlyList<SummaryLine> lines) => [.. lines.Select(line => $"{line.JoinWord} {line.Text}".Trim())];

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
		Assert.Equal(["Every SID and obstacle departure"], Lines(block));
		Assert.Empty(block.Includes);
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
				"Every SID, but no obstacle departures",
				"with only those in ZNY and ZOB",
				"with only those amended in the last 4 cycles",
				"with only those at an airport inside the region (this tab's own)",
			],
			Lines(Assert.Single(tab.WhatYoullGet)));
	}

	// ---- Arrivals ----

	/// <summary>Issue #334: ARTCCs are listed with "and" - the list is inclusive - and the Oxford comma.</summary>
	[Fact]
	public void arrivals_lists_its_artccs_as_inclusive_with_the_amendment_and_a_waypoint_region()
	{
		DefaultRoiStore.Set(new RegionOfInterest(32.5, -120.0, 37.0, -114.0));
		UserConfigFile.TrySetValue("Services.AiracService.Arrivals.ArtccFilter", "ZLA,ZOA,ZAB");
		UserConfigFile.TrySetValue("Services.AiracService.Arrivals.Amendment.Filter", "Cycles");
		UserConfigFile.TrySetValue("Services.AiracService.Arrivals.Amendment.WithinCycles", "1");
		UserConfigFile.TrySetValue("Services.AiracService.Arrivals.Roi.Mode", "Waypoint");

		Assert.Equal(
			[
				"Every STAR",
				"with only those for airports in ZAB, ZLA, and ZOA",
				"with only those amended this cycle",
				"with only those with at least one fix inside the region (your default)",
			],
			Lines(Assert.Single(new ArrivalsViewModel().WhatYoullGet)));
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
		Assert.Equal(["Every open airport"], Lines(Assert.Single(tab.WhatYoullGet)));
		Assert.Equal(Alias | Geojson, tab.WhatYoullGetOutputs);
		List<string?> changed = Changed(tab);

		DefaultRoiStore.Set(new RegionOfInterest(39.5, -85.25, 43.75, -78.5));

		Assert.Contains(nameof(ServiceTabViewModel.WhatYoullGet), changed);
		Assert.Equal([Geojson, Alias], tab.WhatYoullGet.Select(block => block.Outputs));
		Assert.Equal(["Every open airport", "with only those with their reference point inside the region (your default)"], Lines(tab.WhatYoullGet[0]));
		Assert.Equal(["Every open airport, in the region or not"], Lines(tab.WhatYoullGet[1]));

		Attach(tab, AiracSubServices.AirportsKey, Alias);

		Assert.Equal(["Every open airport, in the region or not"], Lines(Assert.Single(tab.WhatYoullGet)));
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

		Assert.Equal(["Every airway except the Y airways"], Lines(Assert.Single(tab.WhatYoullGet)));

		tab.OverrideRoi = true;

		Assert.Equal(["Every airway except the Y airways", "with only those that cross the region, cut off at its edge (this tab's own)"], Lines(tab.WhatYoullGet[0]));
		Assert.Equal(["Every airway except the Y airways, in the region or not"], Lines(tab.WhatYoullGet[1]));

		tab.AliasRoiAirwaysOnly = true;

		Assert.Equal(
			["Every airway except the Y airways", "with only those that cross the region, each with all of its fixes (this tab's own)"],
			Lines(tab.WhatYoullGet[1]));
	}

	/// <summary>Before a cycle is loaded there are no toggles: the saved exclusions stand in.</summary>
	[Fact]
	public void airways_before_a_cycle_uses_the_saved_exclusions()
	{
		UserConfigFile.TrySetValue("Services.AiracService.Airways.ExcludedDesignations", "V,J,Q");

		Assert.Equal(["Every airway except the J, Q, and V airways"], Lines(Assert.Single(new AirwaysViewModel().WhatYoullGet)));
	}

	// ---- Telephony ----

	[Fact]
	public void telephony_adds_the_virtual_airline_list_along_with_the_faa_operators()
	{
		UserConfigFile.TrySetValue("Services.AiracService.Telephony.IncludeVatsimRadarVirtualAirlines", "Y");

		Assert.Equal(
			[
				"Every operator in the FAA's telephony pages, except expired U.S. special call signs",
				$"along with the {VatsimRadarVirtualAirlines.ListName}",
			],
			Lines(Assert.Single(new TelephonyViewModel().WhatYoullGet)));
	}

	// ---- Procedures ----

	private static ProceduresViewModel ProceduresTab(SubServiceOutputKinds on)
	{
		ProceduresViewModel tab = new();
		Attach(tab, AiracSubServices.ProceduresKey, on);
		return tab;
	}

	/// <summary>
	/// Issues #333 and #334: each way an airport comes in adds to the first, never "or"; under
	/// "Outputs include", what Procedure_Changes.md keeps, then the chart types, which never narrow a
	/// procedure picked by name.
	/// </summary>
	[Fact]
	public void procedures_adds_each_way_an_airport_comes_in_then_says_what_the_outputs_include()
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
				"Charts within ZOB",
				"along with CLE and DTW",
				"along with airports inside the region (this tab's own)",
				"along with BRWNZ FIVE, wherever it's published",
			],
			Lines(tab.WhatYoullGet[0]));
		Assert.Equal(
			[
				"`Procedure_Changes.md`: only those that were added, changed, or deleted this cycle",
				"with only these types: IAP, STAR, DP, ODP, DAU, and APD (the procedures you named are always included)",
			],
			Includes(tab.WhatYoullGet[0]));
		Assert.Equal(
			["Every chart at every airport in the d-TPP metafile, not just the ones you chose for `Procedure_Changes.md` and `Procedures.json`"],
			Lines(tab.WhatYoullGet[1]));
	}

	[Fact]
	public void procedures_with_only_the_airports_listed_starts_with_them()
	{
		UserConfigFile.TrySetValue("Services.AiracService.Procedures.Airports", "CLE,DTW,CAK,PIT,ORD,MDW,BUF,IAD,DCA,BWI,RIC,ORF,PHL");
		ProceduresViewModel tab = ProceduresTab(Changes);

		SummaryBlock block = Assert.Single(tab.WhatYoullGet);
		Assert.Equal(["Charts at the 13 airports you listed"], Lines(block));
		Assert.Equal(
			[
				"`Procedure_Changes.md`: only those that were added, changed, or deleted this cycle",
				"with only these types: IAP, STAR, DP, ODP, DAU, and APD",
			],
			Includes(block));
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

		SummaryBlock block = Assert.Single(tab.WhatYoullGet);
		Assert.Equal(
			[
				"Charts at the 7 airports you listed",
				"along with BRWNZ FIVE and CLVLD TWO, wherever they're published",
			],
			Lines(block));
		Assert.Equal(["with only these types: none yet, so tick one under Chart Types"], Includes(block));
	}

	[Fact]
	public void procedures_with_nothing_picked_says_what_to_pick()
	{
		SummaryBlock block = Assert.Single(ProceduresTab(Json).WhatYoullGet);

		Assert.Equal(["Nothing yet: pick a facility, an airport, or a procedure"], Lines(block));
		Assert.Empty(block.Includes);
	}

	/// <summary>A procedure picked by name or with its airport, and nothing else, gets no chart types line.</summary>
	[Fact]
	public void procedures_picked_one_by_one_need_no_chart_types()
	{
		UserConfigFile.TrySetValue("Services.AiracService.Procedures.AirportProcedures", "CLE|ILS OR LOC RWY 06L");
		ProceduresViewModel tab = ProceduresTab(Changes | Json);

		SummaryBlock block = Assert.Single(tab.WhatYoullGet);
		Assert.Equal(["CLE — ILS OR LOC RWY 06L"], Lines(block));
		Assert.Equal(["`Procedure_Changes.md`: only those that were added, changed, or deleted this cycle"], Includes(block));
	}

	[Theory]
	[InlineData(Alias | Changes, "Every chart at every airport in the d-TPP metafile, not just the ones you chose for `Procedure_Changes.md`")]
	[InlineData(Alias | Json, "Every chart at every airport in the d-TPP metafile, not just the ones you chose for `Procedures.json`")]
	[InlineData(Alias, "Every chart at every airport in the d-TPP metafile")]
	public void the_procedures_alias_line_names_only_the_documents_that_are_on(SubServiceOutputKinds on, string expected)
	{
		UserConfigFile.TrySetValue("Services.AiracService.Procedures.Facilities", "ZOB");

		Assert.Equal([expected], Lines(ProceduresTab(on).WhatYoullGet.Single(block => block.Outputs == Alias)));
	}

	/// <summary>The d-TPP calls a STAR <c>STR</c>: the checkbox and the summary say STAR, and STR is still what is sent.</summary>
	[Fact]
	public void the_star_chart_type_shows_as_star_but_is_sent_as_str()
	{
		ProcedureOptionToggle star = Assert.Single(ProceduresTab(Json).ChartTypeToggles, type => type.Token == "STR");

		Assert.Equal("STARs (STAR)", star.Label);
	}
}
