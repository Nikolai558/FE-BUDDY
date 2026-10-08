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
				"Every SID (no obstacle departures)",
				"in ZNY and ZOB",
				"and only those amended in the last 4 cycles",
				"and only those from an airport inside the region (this tab's own)",
			],
			Lines(Assert.Single(tab.WhatYoullGet)));
	}

	/// <summary>With no ARTCC picked, the first narrowing sits right below the first line, so it reads "but only".</summary>
	[Fact]
	public void departures_with_no_artcc_narrows_with_but_only()
	{
		UserConfigFile.TrySetValue("Services.AiracService.Departures.Amendment.Filter", "Days");
		UserConfigFile.TrySetValue("Services.AiracService.Departures.Amendment.WithinDays", "1");

		Assert.Equal(
			["Every SID and obstacle departure", "but only those amended in the last day"],
			Lines(Assert.Single(new DeparturesViewModel().WhatYoullGet)));
	}

	// ---- Arrivals ----

	/// <summary>
	/// Issue #334: ARTCCs are listed with "and" - the list is inclusive - and the Oxford comma; the
	/// block reads as one sentence: every STAR for airports in them, and only those amended this cycle.
	/// </summary>
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
				"for airports in ZAB, ZLA, and ZOA",
				"and only those amended this cycle",
				"and only those with at least one fix inside the region (your default)",
			],
			Lines(Assert.Single(new ArrivalsViewModel().WhatYoullGet)));
	}

	[Theory]
	[InlineData("Days", "WithinDays", "30", "but only those amended in the last 30 days")]
	[InlineData("Days", "WithinDays", "1", "but only those amended in the last day")]
	[InlineData("Cycles", "WithinCycles", "3", "but only those amended in the last 3 cycles")]
	[InlineData("Date", "OnOrAfter", "2026-09-01", "but only those amended on or after 2026-09-01")]
	[InlineData("Date", "OnOrAfter", "", "but only those amended on or after a date not yet picked")]
	public void arrivals_words_each_amendment_filter(string filter, string key, string value, string expected)
	{
		UserConfigFile.TrySetValue("Services.AiracService.Arrivals.Amendment.Filter", filter);
		UserConfigFile.TrySetValue($"Services.AiracService.Arrivals.Amendment.{key}", value);

		Assert.Equal(["Every STAR", expected], Lines(Assert.Single(new ArrivalsViewModel().WhatYoullGet)));
	}

	[Fact]
	public void arrivals_with_a_region_by_airport_keeps_those_for_an_airport_inside_it()
	{
		DefaultRoiStore.Set(new RegionOfInterest(32.5, -120.0, 37.0, -114.0));

		Assert.Equal(
			["Every STAR", "but only those for an airport inside the region (your default)"],
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
		Assert.Equal(["Every open airport", "but only those with their reference point inside the region (your default)"], Lines(tab.WhatYoullGet[0]));
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

		Assert.Equal(["Every airway except the Y airways", "but only those that cross the region, cut off at its edge (this tab's own)"], Lines(tab.WhatYoullGet[0]));
		Assert.Equal(["Every airway except the Y airways, in the region or not"], Lines(tab.WhatYoullGet[1]));

		tab.AliasRoiAirwaysOnly = true;

		Assert.Equal(
			["Every airway except the Y airways", "but only those that cross the region, each with all of its fixes (this tab's own)"],
			Lines(tab.WhatYoullGet[1]));
	}

	[Fact]
	public void airways_names_what_is_kept_when_that_is_the_shorter_list()
	{
		AirwaysViewModel tab = AirwaysTab("A", "B", "G", "J", "Q", "R", "T", "V", "Y");

		foreach (DesignationToggle type in tab.Designations.Where(d => d.Designation is not "J" and not "Q"))
		{
			type.Included = false;
		}

		Assert.Equal(["The J and Q airways"], Lines(Assert.Single(tab.WhatYoullGet)));
	}

	/// <summary>With no type ticked there are no airways, so the region has nothing to narrow.</summary>
	[Fact]
	public void airways_with_no_type_ticked_says_so_and_leaves_the_region_out()
	{
		AirwaysViewModel tab = AirwaysTab("J", "V");
		tab.OverrideRoi = true;

		foreach (DesignationToggle type in tab.Designations)
		{
			type.Included = false;
		}

		Assert.Equal(["No airways: tick a type under Airway Types to Include"], Lines(Assert.Single(tab.WhatYoullGet)));
	}

	/// <summary>Before a cycle is loaded there are no toggles: the saved exclusions stand in.</summary>
	[Fact]
	public void airways_before_a_cycle_uses_the_saved_exclusions()
	{
		UserConfigFile.TrySetValue("Services.AiracService.Airways.ExcludedDesignations", "V,J,Q");

		Assert.Equal(["Every airway except the J, Q, and V airways"], Lines(Assert.Single(new AirwaysViewModel().WhatYoullGet)));
	}

	// ---- ARTCC Boundaries ----

	[Fact]
	public void artcc_boundaries_names_its_artccs_and_keeps_the_parts_inside_the_region()
	{
		Assert.Equal(["Every ARTCC boundary"], Lines(Assert.Single(new ArtccBoundariesViewModel().WhatYoullGet)));

		DefaultRoiStore.Set(new RegionOfInterest(32.5, -120.0, 37.0, -114.0));
		UserConfigFile.TrySetValue("Services.AiracService.ArtccBoundaries.LocationFilter", "ZLA,ZOA");

		Assert.Equal(
			["The boundaries of ZLA and ZOA", "but only the parts inside the region (your default)"],
			Lines(Assert.Single(new ArtccBoundariesViewModel().WhatYoullGet)));
	}

	// ---- Fixes ----

	private static FixesViewModel FixesTab()
	{
		FixCsvDataCollection fixes = new();
		fixes.FixBase.Add(new FixCsvDataModel.FixBase { FixId = "AAA", FixUseCode = "WP", Charts = "IAP,ENROUTE LOW" });
		fixes.FixBase.Add(new FixCsvDataModel.FixBase { FixId = "BBB", FixUseCode = "RP", Charts = "STAR" });
		fixes.FixBase.Add(new FixCsvDataModel.FixBase { FixId = "CCC", FixUseCode = "MW", Charts = null });

		FixesViewModel tab = new();
		tab.LoadCycleDependentLists(new NasrCsvDataCollection { Fix = fixes });
		return tab;
	}

	[Theory]
	[InlineData("FixUse", "ExcludedFixUses", "MIL-WYPNT", "Fixes used as RPRTNG-PNT or WYPNT")]
	[InlineData("Chart", "ExcludedCharts", "STAR", "Fixes on ENROUTE LOW, IAP, or no chart")]
	[InlineData("ChartAndFixUse", "Combinations", "IAP+WYPNT,STAR+RPRTNG-PNT", "Fixes in these chart + fix use combinations: IAP + WYPNT and STAR + RPRTNG-PNT")]
	public void fixes_names_what_each_layout_keeps_as_its_boxes_do(string outputBy, string key, string value, string expected)
	{
		UserConfigFile.TrySetValue("Services.AiracService.Fixes.OutputBy", outputBy);
		UserConfigFile.TrySetValue($"Services.AiracService.Fixes.{key}", value);

		Assert.Equal([expected], Lines(Assert.Single(FixesTab().WhatYoullGet)));
	}

	/// <summary>With no fixes, the region has nothing to narrow.</summary>
	[Theory]
	[InlineData("FixUse", "ExcludedFixUses", "MIL-WYPNT,RPRTNG-PNT,WYPNT", "No fixes: tick a fix use")]
	[InlineData("Chart", "ExcludedCharts", "IAP,ENROUTE-LOW,STAR,NO-CHART", "No fixes: tick a chart")]
	[InlineData("ChartAndFixUse", "Combinations", "", "No fixes until you add a chart + fix use combination")]
	public void fixes_with_none_picked_says_what_to_pick_and_leaves_the_region_out(string outputBy, string key, string value, string expected)
	{
		DefaultRoiStore.Set(new RegionOfInterest(32.5, -120.0, 37.0, -114.0));
		UserConfigFile.TrySetValue("Services.AiracService.Fixes.OutputBy", outputBy);
		UserConfigFile.TrySetValue($"Services.AiracService.Fixes.{key}", value);

		Assert.Equal([expected], Lines(Assert.Single(FixesTab().WhatYoullGet)));
	}

	[Fact]
	public void fixes_in_the_region_read_but_only_those_inside_it()
	{
		DefaultRoiStore.Set(new RegionOfInterest(32.5, -120.0, 37.0, -114.0));

		Assert.Equal(["Every fix", "but only those inside the region (your default)"], Lines(Assert.Single(FixesTab().WhatYoullGet)));
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
				"Charts for airports within ZOB",
				"along with CLE and DTW",
				"along with airports within the region (this tab's own)",
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
		Assert.Equal(["Charts for the 13 airports you listed"], Lines(block));
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
				"Charts for the 7 airports you listed",
				"along with BRWNZ FIVE and CLVLD TWO, wherever they're published",
			],
			Lines(block));
		Assert.Equal(["with only these types: none yet, so tick one under Chart Types"], Includes(block));
	}

	/// <summary>With nothing picked there is nothing for a document to keep, so there is no "Outputs include".</summary>
	[Fact]
	public void procedures_with_nothing_picked_says_what_to_pick()
	{
		SummaryBlock block = Assert.Single(ProceduresTab(Changes | Json).WhatYoullGet);

		Assert.Equal(["Nothing yet: pick a facility, an airport, or a procedure"], Lines(block));
		Assert.Empty(block.Includes);
	}

	[Fact]
	public void procedures_with_only_the_region_starts_with_its_airports()
	{
		DefaultRoiStore.Set(new RegionOfInterest(39.5, -85.25, 43.75, -78.5));
		UserConfigFile.TrySetValue("Services.AiracService.Procedures.IncludeRoiAirports", "Y");

		Assert.Equal(
			["Charts for airports within the region (your default)"],
			Lines(Assert.Single(ProceduresTab(Json).WhatYoullGet)));
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
