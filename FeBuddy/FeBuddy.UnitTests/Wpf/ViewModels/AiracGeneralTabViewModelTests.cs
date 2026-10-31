using System.Collections.Specialized;

using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Domain.Airac;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Dtpp;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the General tab's table (<see cref="AiracGeneralTabViewModel"/>, <see cref="SubServiceRow"/>):
/// everything included and on to start, the outputs a sub-service doesn't offer always off, the
/// last output kept on, and where the choices are saved - against a throwaway config. Also the
/// cycle rows' states, including a cycle that is ready but has no d-TPP Metafile yet.
/// </summary>
[Collection("AppLog")]
public sealed class AiracGeneralTabViewModelTests : IDisposable
{
	private const string Node = "Services.AiracService.General";

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_General_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public AiracGeneralTabViewModelTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		UserConfigFile.ConfigureForTesting(Path.Combine(_root, "config"));
	}

	/// <summary>Restores the real cycle cache, clock, config and log, and deletes the folder.</summary>
	public void Dispose()
	{
		AiracCycleDataCache.ConfigureForTesting(null);
		AppEnvironment.ResetForTesting();
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

	private static SubServiceRow Row(AiracGeneralTabViewModel general, string key) => general.RowFor(key)!;

	[Fact]
	public void with_nothing_saved_every_sub_service_is_in_with_every_output_it_offers_on()
	{
		AiracGeneralTabViewModel general = new();

		Assert.All(general.SubServices, row => Assert.True(row.IsIncluded));
		Assert.All(general.SubServices, row => Assert.Equal(row.Descriptor.Outputs, row.OutputsOn));
		Assert.False(general.IsDirty);
	}

	/// <summary>Add Custom Aliases makes nothing of its own, so it is not a row; it comes in by itself.</summary>
	[Fact]
	public void the_table_lists_every_sub_service_but_add_custom_aliases_in_rail_order()
	{
		AiracGeneralTabViewModel general = new();

		Assert.Equal(
			[.. AiracSubServices.All.Where(d => d.Key != AiracSubServices.ConcatenateAliasesKey).Select(d => d.DisplayName)],
			general.SubServices.Select(row => row.DisplayName));
		Assert.Null(general.RowFor(AiracSubServices.ConcatenateAliasesKey));
	}

	[Fact]
	public void an_output_a_sub_service_does_not_offer_is_always_off_and_greyed_out()
	{
		AiracGeneralTabViewModel general = new();
		SubServiceRow fixes = Row(general, AiracSubServices.FixesKey);

		fixes.Load(included: true, SubServiceOutputKinds.Alias | SubServiceOutputKinds.Geojson);

		Assert.False(fixes.Alias);
		Assert.False(fixes.OffersAlias);
		Assert.Equal("Fixes doesn't make an alias file.", fixes.AliasToolTip);
		Assert.Equal("Fixes doesn't make Procedure_Changes.md; only Procedure Charts does.", fixes.ProcedureChangesToolTip);
		Assert.EndsWith("Its settings are on the Fixes tab, in the list to the left.", fixes.GeojsonToolTip);
	}

	[Fact]
	public void unticking_the_last_output_leaves_the_sub_service_out()
	{
		AiracGeneralTabViewModel general = new();
		SubServiceRow airports = Row(general, AiracSubServices.AirportsKey);
		SubServiceRow fixes = Row(general, AiracSubServices.FixesKey);
		int changes = 0;
		general.SubServicesChanged += (_, _) => changes++;

		airports.Geojson = false;
		Assert.True(airports.IsIncluded);

		airports.Alias = false;
		fixes.Geojson = false;

		Assert.False(general.RowFor(AiracSubServices.AirportsKey)!.IsIncluded);
		Assert.Equal(SubServiceOutputKinds.None, airports.OutputsOn);
		Assert.False(general.RowFor(AiracSubServices.FixesKey)!.IsIncluded);
		Assert.Equal(3, changes);
	}

	[Fact]
	public void leaving_a_sub_service_out_unticks_its_outputs()
	{
		AiracGeneralTabViewModel general = new();
		SubServiceRow airports = Row(general, AiracSubServices.AirportsKey);
		int changes = 0;
		general.SubServicesChanged += (_, _) => changes++;

		airports.IsIncluded = false;

		Assert.False(general.RowFor(AiracSubServices.AirportsKey)!.IsIncluded);
		Assert.False(airports.Alias);
		Assert.False(airports.Geojson);
		Assert.True(airports.OffersAlias);
		Assert.Equal(1, changes);
		Assert.True(general.IsDirty);
	}

	[Fact]
	public void including_a_sub_service_again_ticks_every_output_it_offers()
	{
		AiracGeneralTabViewModel general = new();
		SubServiceRow procedures = Row(general, AiracSubServices.ProceduresKey);
		procedures.ProceduresJson = false;
		procedures.IsIncluded = false;

		procedures.IsIncluded = true;

		Assert.Equal(procedures.Descriptor.Outputs, procedures.OutputsOn);
	}

	[Fact]
	public void ticking_an_output_of_a_sub_service_left_out_brings_it_in_with_just_that_output()
	{
		AiracGeneralTabViewModel general = new();
		SubServiceRow airports = Row(general, AiracSubServices.AirportsKey);
		airports.IsIncluded = false;
		int changes = 0;
		general.SubServicesChanged += (_, _) => changes++;

		airports.Geojson = true;

		Assert.True(general.RowFor(AiracSubServices.AirportsKey)!.IsIncluded);
		Assert.Equal(SubServiceOutputKinds.Geojson, airports.OutputsOn);
		Assert.Equal(1, changes);
	}

	[Fact]
	public void the_choices_are_saved_under_the_general_tab_and_read_back()
	{
		AiracGeneralTabViewModel general = new();
		Row(general, AiracSubServices.WxStationsKey).IsIncluded = false;
		Row(general, AiracSubServices.AirportsKey).Geojson = false;
		Row(general, AiracSubServices.ProceduresKey).ProceduresJson = false;

		Assert.True(general.Save());

		Assert.DoesNotContain("WxStations", UserConfigFile.GetValue($"{Node}.SelectedSubServices")!, StringComparison.Ordinal);
		Assert.Equal("N", UserConfigFile.GetValue($"{Node}.Outputs.Airports.Geojson"));
		Assert.Equal("Y", UserConfigFile.GetValue($"{Node}.Outputs.Airports.Alias"));
		Assert.Equal("N", UserConfigFile.GetValue($"{Node}.Outputs.Procedures.ProceduresJson"));
		Assert.Null(UserConfigFile.GetValue($"{Node}.Outputs.Fixes.Alias"));

		AiracGeneralTabViewModel reloaded = new();
		Assert.False(reloaded.RowFor(AiracSubServices.WxStationsKey)!.IsIncluded);
		Assert.Equal(SubServiceOutputKinds.None, Row(reloaded, AiracSubServices.WxStationsKey).OutputsOn);
		Assert.False(Row(reloaded, AiracSubServices.AirportsKey).Geojson);
		Assert.False(Row(reloaded, AiracSubServices.ProceduresKey).ProceduresJson);
		Assert.True(Row(reloaded, AiracSubServices.ProceduresKey).ProcedureChanges);
	}

	/// <summary>
	/// The General tab's node sits beside the other tabs' nodes, so saving another tab or the Map's
	/// default ROI doesn't offer the General tab an undo, and the General tab's undo restores only its
	/// own settings, not every AIRAC tab's.
	/// </summary>
	[Fact]
	public void the_general_tabs_save_and_undo_never_reach_another_tabs_settings()
	{
		const string airwaysKey = "Services.AiracService.Airways.FixBufferNm";
		AiracGeneralTabViewModel general = new();

		UserConfigFile.TrySetValue(airwaysKey, "2");
		UserConfigFile.Save("Services.AiracService.Airways");
		UserConfigFile.TrySetValue(UserConfigKeys.DefaultRoiFilter, "Y");
		UserConfigFile.Save("General.DefaultRoi");

		Assert.False(general.CanUndo);

		Row(general, AiracSubServices.FixesKey).IsIncluded = false;
		Assert.True(general.Save());
		Row(general, AiracSubServices.FixesKey).IsIncluded = true;
		Assert.True(general.Save());
		general.UndoLastSave();

		Assert.False(general.RowFor(AiracSubServices.FixesKey)!.IsIncluded);
		Assert.Equal("2", UserConfigFile.GetValue(airwaysKey));
		Assert.Equal("Y", UserConfigFile.GetValue(UserConfigKeys.DefaultRoiFilter));
	}

	/// <summary>
	/// Exporting each sub-service's own alias file is off until saved on; it is saved under this tab's node, the
	/// run reads it as saved, and Undo last save takes it back.
	/// </summary>
	[Fact]
	public void exporting_the_individual_alias_files_is_off_until_saved_on_and_undoes_with_the_tab()
	{
		AiracGeneralTabViewModel general = new();

		Assert.False(general.ExportIndividualAliasFiles);
		Assert.False(AiracGeneralTabViewModel.IsSavedIndividualAliasExportOn());

		Assert.True(general.Save());
		Assert.Equal("N", UserConfigFile.GetValue($"{Node}.ExportIndividualAliasFiles"));

		general.ExportIndividualAliasFiles = true;
		Assert.True(general.IsDirty);
		Assert.False(AiracGeneralTabViewModel.IsSavedIndividualAliasExportOn());

		Assert.True(general.Save());
		Assert.Equal("Y", UserConfigFile.GetValue($"{Node}.ExportIndividualAliasFiles"));
		Assert.True(AiracGeneralTabViewModel.IsSavedIndividualAliasExportOn());
		Assert.True(new AiracGeneralTabViewModel().ExportIndividualAliasFiles);

		general.UndoLastSave();

		Assert.False(general.ExportIndividualAliasFiles);
		Assert.False(AiracGeneralTabViewModel.IsSavedIndividualAliasExportOn());
	}

	/// <summary>
	/// The Alias File Exports card shows only while an included sub-service writes an alias file. Its
	/// tooltip names the files Yes would export, by FE-Buddy's names; it, its first line and the preview
	/// name the combined file as the File Names tab has it, which is never this tab's to save.
	/// </summary>
	[Fact]
	public void the_alias_file_exports_card_shows_only_while_an_alias_file_is_made()
	{
		AiracGeneralTabViewModel general = new();
		List<string?> changed = [];
		general.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		Assert.True(general.ShowsAliasFileExports);
		Assert.Equal(
			"Yes also saves these in the Aliases folder: Airports.txt, Airways.txt, Arrivals.txt, Departures.txt, Navaids.txt, Faa_Chart_Recall.txt, Telephony.txt.\n" +
			"Each holds only its own sub-service's commands, which are in Combined_Alias.txt either way.",
			general.AliasFileExportsToolTip);
		Assert.Equal("Every alias command goes into `Combined_Alias.txt`, the one alias file vNAS takes.", general.AliasFileExportsIntro);
		Assert.Equal("Only `Combined_Alias.txt`: each sub-service's own alias file isn't exported", AliasFilesPreview(general));

		foreach (SubServiceRow row in general.SubServices.Where(row => row.OffersAlias && row.Key != AiracSubServices.ProceduresKey))
		{
			row.Alias = false;
		}

		Assert.StartsWith("Yes also saves these in the Aliases folder: Faa_Chart_Recall.txt.\n", general.AliasFileExportsToolTip, StringComparison.Ordinal);

		changed.Clear();
		Row(general, AiracSubServices.ProceduresKey).Alias = false;

		Assert.False(general.ShowsAliasFileExports);
		Assert.Contains(nameof(AiracGeneralTabViewModel.ShowsAliasFileExports), changed);
		Assert.Null(AliasFilesPreview(general));

		Row(general, AiracSubServices.AirportsKey).Alias = true;
		general.ExportIndividualAliasFiles = true;

		Assert.True(general.ShowsAliasFileExports);
		Assert.Equal("`Combined_Alias.txt`, and each sub-service's own: `Airports.txt`", AliasFilesPreview(general));

		AiracGeneralTabViewModel renamed = new() { CombinedAliasFileName = "ZOB vNAS.txt" };

		Assert.Equal("Every alias command goes into `ZOB vNAS.txt`, the one alias file vNAS takes.", renamed.AliasFileExportsIntro);
		Assert.EndsWith("which are in ZOB vNAS.txt either way.", renamed.AliasFileExportsToolTip, StringComparison.Ordinal);
		Assert.Equal("Only `ZOB vNAS.txt`: each sub-service's own alias file isn't exported", AliasFilesPreview(renamed));
		Assert.False(renamed.IsDirty);
	}

	/// <summary>A reload (Discard, Undo, an import) shows or hides the card for the outputs it brings back.</summary>
	[Fact]
	public void a_reload_shows_or_hides_the_alias_file_exports_card()
	{
		AiracGeneralTabViewModel general = new();

		foreach (SubServiceRow row in general.SubServices.Where(row => row.OffersAlias))
		{
			row.Alias = false;
		}

		Assert.True(general.Save());
		Assert.False(new AiracGeneralTabViewModel().ShowsAliasFileExports);

		List<string?> changed = [];
		general.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
		Row(general, AiracSubServices.AirportsKey).Alias = true;
		changed.Clear();
		general.RevertChanges();

		Assert.False(general.ShowsAliasFileExports);
		Assert.Contains(nameof(AiracGeneralTabViewModel.ShowsAliasFileExports), changed);
	}

	/// <summary>The General section's Alias files row, or <see langword="null"/> while it has none.</summary>
	private static string? AliasFilesPreview(AiracGeneralTabViewModel general) =>
		general.BuildPreviewSummary()[0].Rows.SingleOrDefault(row => row.Label == "Alias files")?.Value;

	/// <summary>Settings edited by hand with every output off would make nothing: they load with every output on.</summary>
	[Fact]
	public void every_output_off_loads_with_every_output_on()
	{
		UserConfigFile.TrySetValue($"{Node}.Outputs.Navaids.Alias", "N");
		UserConfigFile.TrySetValue($"{Node}.Outputs.Navaids.Geojson", "N");

		AiracGeneralTabViewModel general = new();

		Assert.Equal(SubServiceOutputKinds.Alias | SubServiceOutputKinds.Geojson, Row(general, AiracSubServices.NavaidsKey).OutputsOn);
	}

	[Fact]
	public void the_preview_names_each_included_sub_service_with_its_outputs()
	{
		AiracGeneralTabViewModel general = new();
		foreach (SubServiceRow row in general.SubServices.Where(row => row.Key is not (AiracSubServices.AirportsKey or AiracSubServices.ProceduresKey)))
		{
			row.IsIncluded = false;
		}

		Row(general, AiracSubServices.ProceduresKey).Alias = false;

		string subServices = general.BuildPreviewSummary()[0].Rows.Single(row => row.Label == "Sub-services").Value;

		Assert.Equal("Airports (Alias, GeoJSON)\nProcedure Charts (Procedure Changes, Procedures JSON)", subServices);
	}

	/// <summary>A tab reads its outputs from its row and follows it, without being marked unsaved.</summary>
	[Fact]
	public void a_tab_follows_its_row()
	{
		AiracGeneralTabViewModel general = new();
		AirportsViewModel airports = new();
		airports.AttachOutputs(Row(general, AiracSubServices.AirportsKey));

		Row(general, AiracSubServices.AirportsKey).Geojson = false;

		Assert.False(airports.GenerateGeojson);
		Assert.Equal("N", airports.BuildSettingsBlock()["GenerateGeojson"]);
		Assert.Equal(["Airports.txt"], airports.OutputFileEntries().Select(file => file.Key));
		Assert.False(airports.IsDirty);
	}

	/// <summary>On 5 Oct 2026 the cycles are 2609, 2610 and 2611; only 2611 is missing its metafile.</summary>
	[Fact]
	public async Task a_ready_cycle_without_its_d_tpp_metafile_reads_partial_with_a_note_and_a_tooltip()
	{
		await PrepareCyclesAsync(new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc), withDtpp: id => id != "2611");

		AiracGeneralTabViewModel general = new();
		AiracGeneralTabViewModel.CycleOption current = general.CycleOptions.Single(o => o.Position == AiracCyclePosition.Current);
		AiracGeneralTabViewModel.CycleOption next = general.CycleOptions.Single(o => o.Position == AiracCyclePosition.Next);

		Assert.Equal("ready", current.State);
		Assert.Equal(AiracGeneralTabViewModel.CycleStatus.Ready, current.Status);
		Assert.Null(current.DtppNote);
		Assert.Null(current.ToolTip);

		Assert.Equal("partial", next.State);
		Assert.Equal(AiracGeneralTabViewModel.CycleStatus.Partial, next.Status);
		Assert.Equal("d-TPP Metafile not available yet", next.DtppNote);
		Assert.StartsWith("Cycle 2611's NASR data is ready, but its d-TPP Metafile isn't.", next.ToolTip, StringComparison.Ordinal);
	}

	/// <summary>
	/// The tab saves which cycle - <c>Previous</c>, <c>Current</c> or <c>Next</c> - not its ID, so next cycle it reopens
	/// on the same kind of cycle, never on last cycle's data by mistake.
	/// </summary>
	[Fact]
	public void the_cycle_is_saved_as_its_position_and_reopens_on_it()
	{
		AiracGeneralTabViewModel general = new() { SelectedCyclePosition = AiracCyclePosition.Next };
		Assert.True(general.Save());

		Assert.Equal("Next", UserConfigFile.GetValue($"{Node}.CyclePosition"));
		Assert.Null(UserConfigFile.GetValue($"{Node}.AiracCycleId"));
		Assert.Equal(AiracCyclePosition.Next, new AiracGeneralTabViewModel().SelectedCyclePosition);
	}

	/// <summary>Only <c>CyclePosition</c> is read; any other value or key opens on Current.</summary>
	[Theory]
	[InlineData($"{Node}.CyclePosition", "Previous")]
	[InlineData($"{Node}.CyclePosition", "Current")]
	[InlineData($"{Node}.CyclePosition", "2")]
	[InlineData("Services.AiracService.AiracCycleId", "2609")]
	public void anything_but_a_saved_next_opens_on_current(string key, string value)
	{
		AppEnvironment.LaunchUtcNow = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);
		UserConfigFile.TrySetValue(key, value);

		AiracGeneralTabViewModel general = new();

		Assert.Equal(AiracCyclePosition.Current, general.SelectedCyclePosition);
		Assert.False(general.IsDirty);
	}

	/// <summary>
	/// A saved Next the FAA hasn't published yet opens on Current, still clean, so the saved choice
	/// stands for when it is; a Next picked this session stays picked.
	/// </summary>
	[Fact]
	public async Task a_saved_next_cycle_not_yet_published_opens_on_current()
	{
		UserConfigFile.TrySetValue($"{Node}.CyclePosition", "Next");
		await PrepareCyclesAsync(new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc), withDtpp: _ => true, notPublished: "2611");
		int cycleChanges = 0;

		AiracGeneralTabViewModel general = new();
		general.CycleChanged += (_, _) => cycleChanges++;

		Assert.Equal(AiracCyclePosition.Current, general.SelectedCyclePosition);
		Assert.False(general.IsDirty);

		general.SelectedCyclePosition = AiracCyclePosition.Next;
		general.SetReadiness(general.IsReady, general.WaitingMessage);

		Assert.Equal(AiracCyclePosition.Next, general.SelectedCyclePosition);
		Assert.Equal(1, cycleChanges);
	}

	/// <summary>
	/// Reading the settings again (here Discard) keeps an unpublished saved Next on Current, as launch
	/// does: no unsaved mark, no cycle change, and the same message.
	/// </summary>
	[Fact]
	public async Task a_reload_keeps_an_unpublished_saved_next_on_current()
	{
		UserConfigFile.TrySetValue($"{Node}.CyclePosition", "Next");
		await PrepareCyclesAsync(new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc), withDtpp: _ => true, notPublished: "2611");
		AiracGeneralTabViewModel general = new();
		int cycleChanges = 0;
		general.CycleChanged += (_, _) => cycleChanges++;
		List<string> toasts = [];
		NotifyCollectionChangedEventHandler shown = (_, e) => toasts.AddRange(e.NewItems?.OfType<ToastItem>().Select(toast => toast.Title) ?? []);
		Toast.Items.CollectionChanged += shown;

		try
		{
			general.SubServices[0].IsIncluded = false;
			general.RevertChanges();
		}
		finally
		{
			Toast.Items.CollectionChanged -= shown;
		}

		Assert.Equal(AiracCyclePosition.Current, general.SelectedCyclePosition);
		Assert.False(general.IsDirty);
		Assert.Equal(0, cycleChanges);
		Assert.Contains("Opened on the current AIRAC cycle", toasts);
	}

	[Fact]
	public void a_cycle_still_being_prepared_is_working_and_has_no_tooltip()
	{
		AiracGeneralTabViewModel general = new();

		Assert.All(general.CycleOptions, option =>
		{
			Assert.Equal(AiracGeneralTabViewModel.CycleStatus.Working, option.Status);
			Assert.Null(option.DtppNote);
			Assert.Null(option.ToolTip);
		});
	}

	[Fact]
	public void the_tooltip_gives_the_dates_to_expect_the_metafile_and_what_waits_for_it()
	{
		AiracCycleInfo next = AiracCycleResolver.GetCycle(AiracCyclePosition.Next, new DateOnly(2026, 10, 5));
		DateOnly from = next.EffectiveDateUtc.AddDays(-18);
		DateOnly to = next.EffectiveDateUtc.AddDays(-15);

		string tip = AiracGeneralTabViewModel.CycleOption.DescribeMissingDtpp(next, new DateOnly(2026, 10, 5));

		Assert.Contains($"expect it between {from:dd MMM} and {to:dd MMM yyyy}", tip, StringComparison.Ordinal);
		Assert.Contains("Procedure_Changes.md, Procedures.json and Faa_Chart_Recall.txt", tip, StringComparison.Ordinal);
		Assert.Contains("you can't add procedures or airport-specific procedures", tip, StringComparison.Ordinal);
		Assert.DoesNotContain("should be out by now", tip, StringComparison.Ordinal);
	}

	/// <summary>Past the FAA's usual window, the metafile should exist: the download probably failed.</summary>
	[Fact]
	public void past_the_usual_dates_the_tooltip_says_the_download_may_have_failed()
	{
		AiracCycleInfo current = AiracCycleResolver.GetCycle(AiracCyclePosition.Current, new DateOnly(2026, 10, 5));

		string tip = AiracGeneralTabViewModel.CycleOption.DescribeMissingDtpp(current, new DateOnly(2026, 10, 5));

		Assert.Contains("should be out by now and the download may have failed", tip, StringComparison.Ordinal);
		Assert.DoesNotContain("expect it between", tip, StringComparison.Ordinal);
	}

	/// <summary>
	/// Fixes the launch clock at <paramref name="utcNow"/> and prepares the three cycles from it, each
	/// in its own folder, writing a d-TPP Metafile into the folders <paramref name="withDtpp"/> picks.
	/// </summary>
	private async Task PrepareCyclesAsync(DateTime utcNow, Func<string, bool> withDtpp, string? notPublished = null)
	{
		AppEnvironment.LaunchUtcNow = utcNow;

		AiracCycleDataCache.ConfigureForTesting(new AiracCycleDataCache(
			probe: (cycle, _) => Task.FromResult(cycle.AiracCycleId == notPublished
				? AiracCyclePublicationState.NotYetPublished
				: AiracCyclePublicationState.Published),
			download: (cycle, _) => Task.FromResult(Directory.CreateDirectory(Path.Combine(_root, "cycles", cycle.AiracCycleId)).FullName),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()),
			ensureDtpp: (cycleDirectory, cycleId, _) =>
			{
				if (withDtpp(cycleId))
				{
					File.WriteAllText(Path.Combine(cycleDirectory, DtppFiles.FileName), string.Empty);
				}

				return Task.CompletedTask;
			}));

		await AiracCycleDataCache.Instance.PrepareCyclesAsync(
			AppEnvironment.GetAiracCycle(AiracCyclePosition.Previous),
			AppEnvironment.GetAiracCycle(AiracCyclePosition.Current),
			AppEnvironment.GetAiracCycle(AiracCyclePosition.Next),
			AiracLaunchPlan.Everything);
	}
}
