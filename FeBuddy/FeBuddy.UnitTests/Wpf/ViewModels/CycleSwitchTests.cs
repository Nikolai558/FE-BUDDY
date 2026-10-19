using System.Windows.Threading;

using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

using FeBuddy.UnitTests.Application.Airac.Procedures.Fixtures;
using FeBuddy.UnitTests.TestSupport;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Picking another cycle on the General tab rebuilds every tab's cycle-built lists. What the user
/// ticked and hasn't saved stays ticked for an entry the new cycle still has, a new entry starts as
/// saved, and the tab works out again whether it still differs from what is saved. Against a
/// throwaway config.
/// </summary>
[Collection("AppLog")]
public sealed class CycleSwitchTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_CycleSwitch_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public CycleSwitchTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		UserConfigFile.ConfigureForTesting(Path.Combine(_root, "config"));
	}

	/// <summary>Restores the real config and log, and deletes the folder.</summary>
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

	private static AiracCycleOptionLists Stars(params string[] artccs)
	{
		StarCsvDataCollection stars = new();

		foreach (string artcc in artccs)
		{
			stars.StarBase.Add(new StarCsvDataModel.StarBase { Artcc = artcc });
		}

		return AiracCycleOptionListBuilder.Build(new NasrCsvDataCollection { Star = stars });
	}

	private static AiracCycleOptionLists Airways(params string[] designations)
	{
		AwyCsvDataCollection airways = new();

		foreach (string designation in designations)
		{
			airways.AwyBase.Add(new AwyCsvDataModel.AwyBase { AwyId = designation + "1" });
		}

		return AiracCycleOptionListBuilder.Build(new NasrCsvDataCollection { Awy = airways });
	}

	[Fact]
	public void arrivals_keep_an_unsaved_tick_across_a_cycle_switch()
	{
		ArrivalsViewModel arrivals = new();
		arrivals.LoadCycleDependentLists(Stars("ZOB", "ZNY"));
		arrivals.Artccs.Single(toggle => toggle.Artcc == "ZOB").IsSelected = true;
		Assert.True(arrivals.IsDirty);

		arrivals.LoadCycleDependentLists(Stars("ZOB", "ZNY", "ZDC"));

		Assert.True(arrivals.Artccs.Single(toggle => toggle.Artcc == "ZOB").IsSelected);
		Assert.False(arrivals.Artccs.Single(toggle => toggle.Artcc == "ZDC").IsSelected);
		Assert.True(arrivals.IsDirty);

		// Back to what is saved: nothing to save any more.
		arrivals.Artccs.Single(toggle => toggle.Artcc == "ZOB").IsSelected = false;
		Assert.False(arrivals.IsDirty);
	}

	[Fact]
	public void airways_keep_an_unsaved_untick_across_a_cycle_switch_and_a_new_designation_starts_as_saved()
	{
		AirwaysViewModel airways = new();
		airways.LoadCycleDependentLists(Airways("J", "V"));
		airways.Designations.Single(toggle => toggle.Designation == "J").Included = false;
		Assert.True(airways.IsDirty);

		airways.LoadCycleDependentLists(Airways("J", "V", "Q"));

		Assert.False(airways.Designations.Single(toggle => toggle.Designation == "J").Included);
		Assert.True(airways.Designations.Single(toggle => toggle.Designation == "Q").Included);
		Assert.True(airways.IsDirty);
	}

	/// <summary>A switch with nothing unsaved leaves the tab clean, on its saved settings.</summary>
	[Fact]
	public void a_cycle_switch_with_nothing_unsaved_leaves_the_tab_clean()
	{
		UserConfigFile.TrySetValue("Services.AiracService.Arrivals.ArtccFilter", "ZNY");
		ArrivalsViewModel arrivals = new();
		arrivals.LoadCycleDependentLists(Stars("ZOB"));

		arrivals.LoadCycleDependentLists(Stars("ZOB", "ZNY"));

		Assert.True(arrivals.Artccs.Single(toggle => toggle.Artcc == "ZNY").IsSelected);
		Assert.False(arrivals.IsDirty);
	}

	/// <summary>
	/// Picking a cycle the FAA hasn't published: the tabs say so instead of "Loading" for good, and the
	/// run can't start, since the tabs have no lists for it.
	/// </summary>
	[Fact]
	public async Task picking_a_cycle_that_wont_load_says_why_and_the_run_waits()
	{
		AppEnvironment.LaunchUtcNow = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);
		AiracCycleDataCache.ConfigureForTesting(new AiracCycleDataCache(
			probe: (cycle, _) => Task.FromResult(cycle.AiracCycleId == "2611"
				? AiracCyclePublicationState.NotYetPublished
				: AiracCyclePublicationState.Published),
			download: (cycle, _) => Task.FromResult(Directory.CreateDirectory(Path.Combine(_root, "cycles", cycle.AiracCycleId)).FullName),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection())));
		await AiracCycleDataCache.Instance.PrepareCyclesAsync(
			AppEnvironment.GetAiracCycle(AiracCyclePosition.Previous),
			AppEnvironment.GetAiracCycle(AiracCyclePosition.Current),
			AppEnvironment.GetAiracCycle(AiracCyclePosition.Next));

		AiracServiceViewModel screen = new();
		AiracGeneralTabViewModel general = screen.Tabs.OfType<AiracGeneralTabViewModel>().Single();
		ArrivalsViewModel arrivals = screen.Tabs.OfType<ArrivalsViewModel>().Single();
		Assert.True(screen.IsReady);

		general.SelectedCyclePosition = AiracCyclePosition.Next;

		Assert.Equal("AIRAC 2611 isn't published by the FAA yet. Pick another cycle on the General tab.", arrivals.CycleWaitingText);
		Assert.False(arrivals.IsCycleReady);
		Assert.False(screen.RunCommand.CanExecute(null));
	}

	// ---- the screen: handing a cycle's data to the tabs ----

	/// <summary>The cycle folder <see cref="Prepare"/> downloads a cycle into.</summary>
	private string CycleFolder(string cycleId) => Directory.CreateDirectory(Path.Combine(_root, "cycles", cycleId)).FullName;

	/// <summary>
	/// Fixes the clock on 5 Oct 2026 (previous 2609, current 2610, next 2611), sets up a cache whose
	/// steps the test gives, and prepares the three cycles; with <paramref name="wait"/> false it
	/// returns once the current cycle is ready, leaving the rest of the launch running.
	/// </summary>
	private void Prepare(
		string? notPublished = null,
		Func<string, Task>? beforeDownload = null,
		Func<string, Task<DtppMetafileDataCollection?>>? loadDtpp = null,
		bool wait = true)
	{
		AppEnvironment.LaunchUtcNow = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);
		AiracCycleDataCache.ConfigureForTesting(new AiracCycleDataCache(
			probe: (cycle, _) => Task.FromResult(cycle.AiracCycleId == notPublished
				? AiracCyclePublicationState.NotYetPublished
				: AiracCyclePublicationState.Published),
			download: async (cycle, _) =>
			{
				await (beforeDownload?.Invoke(cycle.AiracCycleId) ?? Task.CompletedTask);
				return CycleFolder(cycle.AiracCycleId);
			},
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()),
			loadDtpp: (directory, _) => loadDtpp?.Invoke(Path.GetFileName(directory)) ?? Task.FromResult<DtppMetafileDataCollection?>(null)));

		Task prepare = AiracCycleDataCache.Instance.PrepareCyclesAsync(
			AppEnvironment.GetAiracCycle(AiracCyclePosition.Previous),
			AppEnvironment.GetAiracCycle(AiracCyclePosition.Current),
			AppEnvironment.GetAiracCycle(AiracCyclePosition.Next));

		if (wait)
		{
			prepare.GetAwaiter().GetResult();
		}
		else
		{
			AiracCycleDataCache.Instance.GetAsync("2610").GetAwaiter().GetResult();
		}
	}

	/// <summary>A d-TPP Metafile for the current cycle only.</summary>
	private static Task<DtppMetafileDataCollection?> CurrentCycleOnly(string cycleId) =>
		Task.FromResult(cycleId == "2610" ? ProcedureTestData.Dtpp("2610") : null);

	/// <summary>Runs what the screen has queued for this thread.</summary>
	private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);

	/// <summary>
	/// Runs what the screen has queued for this thread until <paramref name="done"/> holds. The
	/// round count only stops a broken test hanging; no test depends on how long the work takes.
	/// </summary>
	private static void PumpUntil(Func<bool> done)
	{
		for (int round = 0; round < 3000 && !done(); round++)
		{
			Pump();
			Thread.Sleep(10);
		}

		Assert.True(done());
	}

	/// <summary>Gives work started off this thread a chance to come back, for a test that checks nothing did.</summary>
	private static void PumpAWhile()
	{
		for (int round = 0; round < 20; round++)
		{
			Pump();
			Thread.Sleep(10);
		}
	}

	[Fact]
	public void going_back_to_a_loaded_cycle_brings_procedure_charts_data_back() => StaThread.Run(() =>
	{
		Prepare(notPublished: "2611", loadDtpp: CurrentCycleOnly);
		AiracServiceViewModel screen = new();
		AiracGeneralTabViewModel general = screen.Tabs.OfType<AiracGeneralTabViewModel>().Single();
		ProceduresViewModel procedures = screen.Tabs.OfType<ProceduresViewModel>().Single();
		PumpUntil(() => procedures.IsDtppKnown);

		general.SelectedCyclePosition = AiracCyclePosition.Next;
		Pump();
		Assert.False(procedures.IsDtppKnown);

		general.SelectedCyclePosition = AiracCyclePosition.Current;
		PumpUntil(() => procedures.IsDtppKnown);

		Assert.Equal("2610", procedures.DtppCycleId);
	});

	/// <summary>While a picked cycle hasn't loaded, a change on the General tab never brings back the last cycle's metafile.</summary>
	[Fact]
	public void a_cycle_that_hasnt_loaded_never_gets_the_last_cycles_metafile() => StaThread.Run(() =>
	{
		Prepare(notPublished: "2611", loadDtpp: CurrentCycleOnly);
		AiracServiceViewModel screen = new();
		AiracGeneralTabViewModel general = screen.Tabs.OfType<AiracGeneralTabViewModel>().Single();
		ProceduresViewModel procedures = screen.Tabs.OfType<ProceduresViewModel>().Single();
		PumpUntil(() => procedures.DtppCycleId == "2610");

		general.SelectedCyclePosition = AiracCyclePosition.Next;
		Pump();
		general.SubServices.First(row => row.Key != AiracSubServices.ProceduresKey).IsIncluded = false;
		PumpAWhile();

		Assert.Null(procedures.DtppCycleId);
	});

	/// <summary>A cycle load that another pick overtook leaves the newer pick marked as loading.</summary>
	[Fact]
	public void an_overtaken_load_leaves_the_newer_pick_loading() => StaThread.Run(() =>
	{
		TaskCompletionSource next = new(TaskCreationOptions.RunContinuationsAsynchronously);
		TaskCompletionSource previous = new(TaskCreationOptions.RunContinuationsAsynchronously);
		Prepare(beforeDownload: cycleId => cycleId switch
		{
			"2611" => next.Task,
			"2609" => previous.Task,
			_ => Task.CompletedTask,
		}, wait: false);

		AiracServiceViewModel screen = new();
		AiracGeneralTabViewModel general = screen.Tabs.OfType<AiracGeneralTabViewModel>().Single();
		PumpUntil(() => screen.IsReady);

		general.SelectedCyclePosition = AiracCyclePosition.Next;
		general.SelectedCyclePosition = AiracCyclePosition.Previous;
		Assert.Equal("2609", screen.LoadingCycleId);

		next.SetResult();
		PumpUntil(() => AiracCycleDataCache.Instance.GetEntry("2611")!.State == CycleDataState.Ready);
		PumpAWhile();

		Assert.Equal("2609", screen.LoadingCycleId);
		previous.SetResult();
		PumpUntil(() => screen.LoadingCycleId is null);
	});

	/// <summary>Picking the selected cycle again after it failed tries it again.</summary>
	[Fact]
	public void picking_a_failed_cycle_again_tries_it_again() => StaThread.Run(() =>
	{
		int nextDownloads = 0;
		Prepare(beforeDownload: cycleId =>
		{
			if (cycleId == "2611")
			{
				Interlocked.Increment(ref nextDownloads);
				throw new IOException("network down");
			}

			return Task.CompletedTask;
		});

		AiracServiceViewModel screen = new();
		AiracGeneralTabViewModel general = screen.Tabs.OfType<AiracGeneralTabViewModel>().Single();
		PumpUntil(() => screen.IsReady);

		general.SelectedCyclePosition = AiracCyclePosition.Next;
		PumpUntil(() => screen.LoadingCycleId is null && Volatile.Read(ref nextDownloads) == 4);

		general.CycleOptions.Single(option => option.Position == AiracCyclePosition.Next).PickCommand.Execute(null);

		PumpUntil(() => Volatile.Read(ref nextDownloads) > 4);
	});

	/// <summary>The current cycle's metafile is read once, however often the General tab changes while it loads.</summary>
	[Fact]
	public void a_cycles_metafile_is_read_once() => StaThread.Run(() =>
	{
		TaskCompletionSource firstRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
		int reads = 0;
		Prepare(loadDtpp: async cycleId =>
		{
			if (cycleId != "2610")
			{
				return null;
			}

			if (Interlocked.Increment(ref reads) == 1)
			{
				await firstRead.Task;
			}

			return ProcedureTestData.Dtpp("2610");
		});

		AiracServiceViewModel screen = new();
		AiracGeneralTabViewModel general = screen.Tabs.OfType<AiracGeneralTabViewModel>().Single();
		ProceduresViewModel procedures = screen.Tabs.OfType<ProceduresViewModel>().Single();
		PumpUntil(() => Volatile.Read(ref reads) == 1);

		SubServiceRow row = general.SubServices.First(row => row.Key != AiracSubServices.ProceduresKey);
		row.IsIncluded = false;
		row.IsIncluded = true;
		PumpAWhile();

		firstRead.SetResult();
		PumpUntil(() => procedures.IsDtppKnown);

		Assert.Equal(1, Volatile.Read(ref reads));
	});
}
