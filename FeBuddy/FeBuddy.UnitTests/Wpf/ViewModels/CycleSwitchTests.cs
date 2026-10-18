using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

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
}
