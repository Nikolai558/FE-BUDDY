using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the Procedure Charts tab's External Procedures in Our Airspace card (Feature #367): on to start,
/// naming the facility set in Settings; greyed out, saying why, with no facility; its footer; what the tab saves and
/// sends - off without a facility, nothing with Everything; that it's enough to pick with the area
/// "None"; and its line in What You'll Get - against a throwaway config.
/// </summary>
[Collection("AppLog")]
public sealed class ProcedureFixInFacilityCardTests : IDisposable
{
	private const string Node = "Services.AiracService.Procedures";
	private const string Key = "IncludeProceduresWithFixInFacility";

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_ProcedureFixInFacility_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public ProcedureFixInFacilityCardTests()
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

	/// <summary>The tab with both documents on, and <paramref name="facility"/> set in Settings ▸ Facility.</summary>
	private static ProceduresViewModel Tab(string? facility = "ZDC")
	{
		if (facility is not null)
		{
			UserConfigFile.TrySetValue("Services.AiracService.UserArtccId", facility);
		}

		ProceduresViewModel tab = new();
		SubServiceRow row = new(AiracSubServices.All.Single(d => d.Key == AiracSubServices.ProceduresKey), () => { });
		row.Load(included: true, SubServiceOutputKinds.ProcedureChanges | SubServiceOutputKinds.ProceduresJson);
		tab.AttachOutputs(row);
		return tab;
	}

	[Fact]
	public void the_option_is_on_to_start_and_names_the_facility()
	{
		ProceduresViewModel tab = Tab();

		Assert.True(tab.IncludeProceduresWithFixInFacility);
		Assert.True(tab.CanIncludeProceduresWithFixInFacility);
		Assert.Equal("Include procedures for non-`ZDC` airports that have a waypoint inside `ZDC`", tab.IncludeFixInFacilityLabel);
		Assert.Equal(
			"A departure or arrival is included when any point on its route, transitions included, is a fix or NAVAID that NASR puts in ZDC. "
			+ "Only the chart types ticked under Chart Types are included.",
			tab.IncludeFixInFacilityToolTip);
		Assert.Null(tab.IncludeFixInFacilityNote);
		Assert.Equal("Y", tab.BuildSettingsBlock()[Key]);
	}

	[Fact]
	public void the_cards_footer_says_what_its_options_apply_to()
	{
		Assert.Equal("only apply to Departures (ODP/SID) and Arrivals (STAR). IAPs may be included in future updates.", ProceduresViewModel.ExternalProceduresScopeNote);
		Assert.Equal("don't apply to `Faa_Chart_Recall.txt`", ProceduresViewModel.ExternalProceduresChartRecallNote);
	}

	[Fact]
	public void with_no_facility_set_the_option_is_greyed_out_says_why_and_is_sent_off()
	{
		ProceduresViewModel tab = Tab(facility: null);

		Assert.False(tab.CanIncludeProceduresWithFixInFacility);
		Assert.Equal("Include procedures for airports outside your facility that have a waypoint inside it", tab.IncludeFixInFacilityLabel);
		Assert.Contains("is a fix or NAVAID that NASR puts in your facility.", tab.IncludeFixInFacilityToolTip, StringComparison.Ordinal);
		Assert.Equal("Set your facility in Settings ▸ Facility to use this card.", tab.IncludeFixInFacilityNote);
		Assert.Equal("N", tab.BuildSettingsBlock()[Key]);
	}

	[Fact]
	public void with_everything_the_option_is_not_used_or_sent()
	{
		ProceduresViewModel tab = Tab();

		tab.Area = SubServiceArea.Everything;

		Assert.Equal("\"Everything\" already includes every airport, so this card isn't used.", tab.IncludeFixInFacilityNote);
		Assert.False(tab.BuildSettingsBlock().ContainsKey(Key));
	}

	[Fact]
	public void turned_off_it_is_saved_sent_and_read_back()
	{
		ProceduresViewModel tab = Tab();

		tab.IncludeProceduresWithFixInFacility = false;

		Assert.True(tab.IsDirty);
		Assert.Equal("N", tab.BuildSettingsBlock()[Key]);

		Assert.True(tab.Save());

		Assert.Equal("N", UserConfigFile.GetValue($"{Node}.{Key}"));
		Assert.False(Tab().IncludeProceduresWithFixInFacility);
	}

	/// <summary>With the area "None", the option alone gives the documents something to include - with a facility to have them in.</summary>
	[Fact]
	public void with_the_area_none_the_option_is_enough_to_pick()
	{
		const string NothingToInclude = "With \"None\" picked on the Area card, list at least one airport or procedure for the documents to include, or pick another area.";

		ProceduresViewModel tab = Tab();
		tab.Area = SubServiceArea.None;
		tab.Revalidate();

		Assert.DoesNotContain(NothingToInclude, tab.ValidationErrors);

		tab.IncludeProceduresWithFixInFacility = false;

		Assert.Contains(NothingToInclude, tab.ValidationErrors);
	}

	[Fact]
	public void with_only_the_option_no_chart_type_ticked_is_flagged()
	{
		ProceduresViewModel tab = Tab();
		tab.Area = SubServiceArea.None;

		foreach (ProcedureOptionToggle type in tab.ChartTypeToggles)
		{
			type.IsSelected = false;
		}

		Assert.Contains(tab.ValidationErrors, error => error.StartsWith("No chart types are ticked", StringComparison.Ordinal));
	}

	[Fact]
	public void what_youll_get_adds_the_neighbors_procedures()
	{
		ProceduresViewModel tab = Tab();
		tab.Area = SubServiceArea.Artccs;

		Assert.Equal(
			["Charts for airports within ZDC", "along with departures and arrivals at other airports whose route has a fix in ZDC"],
			Lines(tab.WhatYoullGet[0]));

		tab.Area = SubServiceArea.None;

		Assert.Equal(["Departures and arrivals at other airports whose route has a fix in ZDC"], Lines(tab.WhatYoullGet[0]));
		Assert.Equal("with only these types: IAP, STAR, DP, ODP, DAU, and APD", Lines(tab.WhatYoullGet[0], includes: true)[1]);
	}

	private static string[] Lines(SummaryBlock block, bool includes = false) =>
		[.. (includes ? block.Includes : block.Lines).Select(line => $"{line.JoinWord} {line.Text}".Trim())];
}
