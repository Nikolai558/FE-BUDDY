using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.Models;

using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Domain.Airac;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Logging;

using FeBuddy.UnitTests.Application.Airac.Procedures.Fixtures;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the Procedure Charts tab's two cards that list procedures by name - Additional Procedures to
/// Include and Additional Airport-Specific Procedures to Include: they offer departures and arrivals
/// only, list each by its root name so it's followed through renumbering, read names an earlier
/// FE-Buddy saved in full as their roots, and say what each listed name brings in from the cycle -
/// against a throwaway config and a small d-TPP Metafile.
/// </summary>
[Collection("AppLog")]
public sealed class ProcedureNameCardsTests : IDisposable
{
	private const string Node = "Services.AiracService.Procedures";

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_ProcedureNames_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public ProcedureNameCardsTests()
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

	/// <summary>Cycle 2611: an arrival and an approach at LAS; a SID, an ODP and an airport diagram at PIT.</summary>
	private static DtppMetafileDataCollection Metafile() => ProcedureTestData.Dtpp(
		"2611",
		airports: [ProcedureTestData.AirportRow("LAS", "KLAS"), ProcedureTestData.AirportRow("PIT", "KPIT")],
		records:
		[
			ProcedureTestData.RecordRow("LAS", 10, "STR", "BRWNZ FIVE (RNAV)", "00001BRWNZ.PDF"),
			ProcedureTestData.RecordRow("LAS", 11, "STR", "BRWNZ FIVE (RNAV), CONT.1", "00001BRWNZ_C.PDF"),
			ProcedureTestData.RecordRow("LAS", 20, "IAP", "ILS OR LOC RWY 26L", "00001IL26L.PDF"),
			ProcedureTestData.RecordRow("PIT", 30, "DP", "GRUUB ONE (RNAV)", "00002GRUUB.PDF"),
			ProcedureTestData.RecordRow("PIT", 31, "ODP", "PITT TWO (OBSTACLE)", "00002PITT.PDF"),
			ProcedureTestData.RecordRow("PIT", 40, "APD", "AIRPORT DIAGRAM", "00002AD.PDF"),
		]);

	private static ProceduresViewModel Tab()
	{
		ProceduresViewModel tab = new();
		tab.SetDtppData("2611", ProcedureNameIndex.Build(Metafile()), "2610", previousAvailable: true, downloadedLocal: DateTime.Now);
		return tab;
	}

	/// <summary>
	/// Another cycle picked: the old cycle's chart data no longer describes it, so it goes until the new
	/// cycle's arrives - the cards don't offer, or resolve against, last cycle's procedures meanwhile.
	/// </summary>
	[Fact]
	public void picking_another_cycle_drops_the_old_cycles_chart_data()
	{
		ProceduresViewModel tab = Tab();
		tab.SetSelectedCycle(AiracCycleResolver.GetCycle(AiracCyclePosition.Next, new DateOnly(2026, 10, 5)));

		Assert.Equal("2611", tab.DtppCycleId);

		tab.SetSelectedCycle(AiracCycleResolver.GetCycle(AiracCyclePosition.Next, new DateOnly(2026, 11, 2)));

		Assert.False(tab.IsDtppKnown);
		Assert.Null(tab.DtppCycleId);
		Assert.Empty(tab.ProcedureNameOptions);
		Assert.Null(tab.DtppWarning);
	}

	// ---- departures and arrivals only ----

	[Fact]
	public void the_cards_offer_departures_and_arrivals_only()
	{
		ProceduresViewModel tab = Tab();

		Assert.Equal(["BRWNZ FIVE (RNAV)", "GRUUB ONE (RNAV)", "PITT TWO (OBSTACLE)"], tab.ProcedureNameOptions);

		tab.AddPairCommand.Execute(null);
		tab.PairAirportText = "PIT";
		Assert.Equal(["GRUUB ONE (RNAV)", "PITT TWO (OBSTACLE)"], tab.PairProcedureOptions);

		tab.PairAirportText = "KLAS";
		Assert.Equal(["BRWNZ FIVE (RNAV)"], tab.PairProcedureOptions);
	}

	[Fact]
	public void an_approach_can_not_be_added()
	{
		ProceduresViewModel tab = Tab();

		tab.NewProcedureText = "ILS OR LOC RWY 26L";

		Assert.Equal("Not a departure or arrival in this cycle.", tab.NewProcedureHint);
		Assert.False(tab.AddProcedureCommand.CanExecute(null));
	}

	// ---- listed by root name ----

	[Fact]
	public void a_procedure_picked_by_its_full_name_is_listed_by_its_root_with_its_current_name()
	{
		ProceduresViewModel tab = Tab();

		tab.NewProcedureText = "BRWNZ FIVE (RNAV)";
		tab.AddProcedureCommand.Execute(null);

		Assert.Equal(["BRWNZ"], tab.ProcedureNames);
		Assert.Equal(new ProcedureNameRow("BRWNZ", "Cycle 2611: BRWNZ FIVE (RNAV)", IsMissing: false), Assert.Single(tab.ProcedureNameRows));
		Assert.Equal("BRWNZ", tab.BuildSettingsBlock()["Procedures"]);
		Assert.Empty(tab.NewProcedureText);
	}

	[Fact]
	public void a_root_typed_in_any_case_is_added_as_the_faa_spells_it_and_once_only()
	{
		ProceduresViewModel tab = Tab();

		tab.NewProcedureText = "gruub";
		Assert.Empty(tab.NewProcedureHint);
		tab.AddProcedureCommand.Execute(null);

		Assert.Equal(["GRUUB"], tab.ProcedureNames);

		// Another version of it is the same procedure.
		tab.NewProcedureText = "GRUUB TWO";
		Assert.Equal("Already in the list.", tab.NewProcedureHint);
		Assert.False(tab.AddProcedureCommand.CanExecute(null));
	}

	[Fact]
	public void full_names_an_earlier_fe_buddy_saved_read_as_their_roots_without_unsaved_changes()
	{
		UserConfigFile.TrySetValue($"{Node}.Procedures", "BRWNZ FOUR (RNAV),BRWNZ FIVE,GRUUB ONE (RNAV)");
		UserConfigFile.TrySetValue($"{Node}.AirportProcedures", "LAS|BRWNZ FOUR (RNAV),LAS|BRWNZ FIVE (RNAV)");

		ProceduresViewModel tab = Tab();

		Assert.Equal(["BRWNZ", "GRUUB"], tab.ProcedureNames);
		ProcedureAirportPickItem pair = Assert.Single(tab.AirportProcedures);
		Assert.Equal(("LAS", "BRWNZ", "LAS — BRWNZ"), (pair.Airport, pair.ProcedureName, pair.Label));
		Assert.False(tab.IsDirty);

		// The FAA renumbered BRWNZ FOUR (RNAV) to FIVE: the card shows what the pick brings in now.
		Assert.Equal("Cycle 2611: BRWNZ FIVE (RNAV)", tab.ProcedureNameRows[0].Detail);
		Assert.Equal("Cycle 2611: BRWNZ FIVE (RNAV)", pair.Detail);
		Assert.False(pair.IsMissing);
	}

	[Fact]
	public void a_listed_name_the_cycle_has_no_departure_or_arrival_for_is_flagged()
	{
		UserConfigFile.TrySetValue($"{Node}.Procedures", "ZZZZZ ONE,ILS OR LOC RWY 26L");
		UserConfigFile.TrySetValue($"{Node}.AirportProcedures", "PIT|BRWNZ,LAS|ILS OR LOC RWY 26L");

		ProceduresViewModel tab = Tab();

		Assert.Equal(
			[
				new ProcedureNameRow("ZZZZZ", "Cycle 2611 has no departure or arrival named ZZZZZ, so it's left out.", IsMissing: true),
				new ProcedureNameRow("ILS OR LOC RWY 26L", "Cycle 2611 has no departure or arrival named ILS OR LOC RWY 26L, so it's left out.", IsMissing: true),
			],
			tab.ProcedureNameRows);
		Assert.All(tab.AirportProcedures, pair => Assert.True(pair.IsMissing));
		Assert.Equal("Cycle 2611 has no departure or arrival named BRWNZ at PIT, so it's left out.", tab.AirportProcedures[0].Detail);
	}

	[Fact]
	public void without_the_cycles_metafile_a_listed_name_says_nothing_yet()
	{
		UserConfigFile.TrySetValue($"{Node}.Procedures", "BRWNZ");
		UserConfigFile.TrySetValue($"{Node}.AirportProcedures", "LAS|BRWNZ");

		ProceduresViewModel tab = new();

		Assert.Equal(new ProcedureNameRow("BRWNZ", null, IsMissing: false), Assert.Single(tab.ProcedureNameRows));
		Assert.Null(Assert.Single(tab.AirportProcedures).Detail);

		tab.SetDtppData("2611", null, "2610", previousAvailable: false, downloadedLocal: null);

		Assert.Null(Assert.Single(tab.ProcedureNameRows).Detail);
	}

	[Fact]
	public void deleting_a_listed_name_takes_it_off_the_card()
	{
		UserConfigFile.TrySetValue($"{Node}.Procedures", "BRWNZ,GRUUB");
		ProceduresViewModel tab = Tab();

		tab.DeleteProcedureCommand.Execute("BRWNZ");

		Assert.Equal(["GRUUB"], tab.ProcedureNameRows.Select(row => row.Name));
		Assert.True(tab.IsDirty);
	}

	// ---- airport-specific ----

	[Fact]
	public void an_airport_specific_pick_is_listed_by_its_root_and_edits_from_its_current_name()
	{
		ProceduresViewModel tab = Tab();

		tab.AddPairCommand.Execute(null);
		tab.PairAirportText = "KPIT";
		tab.SelectedPairProcedure = "GRUUB ONE (RNAV)";
		tab.ConfirmPairCommand.Execute(null);

		ProcedureAirportPickItem pair = Assert.Single(tab.AirportProcedures);
		Assert.Equal(("PIT", "GRUUB", "PIT — GRUUB"), (pair.Airport, pair.ProcedureName, pair.Label));
		Assert.Equal("Cycle 2611: GRUUB ONE (RNAV)", pair.Detail);
		Assert.Equal("PIT|GRUUB", tab.BuildSettingsBlock()["AirportProcedures"]);

		// Editing shows the version the cycle has.
		tab.EditPairCommand.Execute(pair);
		Assert.Equal("GRUUB ONE (RNAV)", tab.SelectedPairProcedure);

		tab.SelectedPairProcedure = "PITT TWO (OBSTACLE)";
		tab.ConfirmPairCommand.Execute(null);

		Assert.Equal(("PIT", "PITT", "PIT — PITT"), (pair.Airport, pair.ProcedureName, pair.Label));
		Assert.Equal("Cycle 2611: PITT TWO (OBSTACLE)", pair.Detail);
	}

	[Fact]
	public void the_same_airport_specific_procedure_is_listed_once()
	{
		UserConfigFile.TrySetValue($"{Node}.AirportProcedures", "PIT|GRUUB");
		ProceduresViewModel tab = Tab();

		tab.AddPairCommand.Execute(null);
		tab.PairAirportText = "PIT";
		tab.SelectedPairProcedure = "GRUUB ONE (RNAV)";

		Assert.Equal("Already in the list.", tab.PairEditorHint);
		Assert.False(tab.ConfirmPairCommand.CanExecute(null));
	}
}
