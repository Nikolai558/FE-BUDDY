using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Domain.Airac;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the Departures and Arrivals amendment date question: "No" keeps every procedure, "Yes"
/// brings back the way picked last (by cycles to start), and the question and the cycles caption
/// name the cycle picked on the General tab - against a throwaway config.
/// </summary>
[Collection("AppLog")]
public sealed class AmendmentQuestionTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_AmendmentQuestion_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public AmendmentQuestionTests()
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

	/// <summary>Cycle 2610, effective 2026-10-01.</summary>
	private static AiracCycleInfo Cycle2610 => AiracCycleResolver.GetCycle(AiracCyclePosition.Current, new DateOnly(2026, 10, 1));

	[Fact]
	public void arrivals_yes_starts_by_cycles_and_no_keeps_every_arrival()
	{
		ArrivalsViewModel tab = new();
		Assert.True(tab.AmendmentAny);
		Assert.False(tab.AmendmentLimited);

		tab.AmendmentLimited = true;

		Assert.True(tab.AmendmentByCycles);
		Assert.False(tab.AmendmentAny);
		Assert.True(tab.IsDirty);

		tab.AmendmentByDays = true;
		tab.AmendmentAny = true;

		Assert.False(tab.AmendmentLimited);
		Assert.False(tab.AmendmentByDays);

		// Yes again brings back the way picked last; Yes while already narrowing changes nothing.
		tab.AmendmentLimited = true;
		Assert.True(tab.AmendmentByDays);
		tab.AmendmentLimited = true;
		Assert.True(tab.AmendmentByDays);
		tab.AmendmentLimited = false;
		Assert.True(tab.AmendmentByDays);
	}

	[Fact]
	public void departures_yes_starts_by_cycles_and_no_keeps_every_departure()
	{
		DeparturesViewModel tab = new();
		Assert.True(tab.AmendmentAny);

		tab.AmendmentLimited = true;

		Assert.True(tab.AmendmentByCycles);

		tab.AmendmentByDate = true;
		tab.AmendmentAny = true;
		tab.AmendmentLimited = true;

		Assert.True(tab.AmendmentByDate);
		Assert.False(tab.AmendmentAny);
	}

	/// <summary>A saved way to narrow loads as "Yes", and is the one "Yes" brings back after "No".</summary>
	[Fact]
	public void a_saved_filter_loads_as_yes_and_is_remembered()
	{
		UserConfigFile.TrySetValue("Services.AiracService.Arrivals.Amendment.Filter", "Date");
		UserConfigFile.TrySetValue("Services.AiracService.Departures.Amendment.Filter", "Days");
		ArrivalsViewModel arrivals = new();
		DeparturesViewModel departures = new();

		Assert.True(arrivals.AmendmentLimited);
		Assert.True(departures.AmendmentLimited);

		arrivals.AmendmentAny = true;
		departures.AmendmentAny = true;
		arrivals.AmendmentLimited = true;
		departures.AmendmentLimited = true;

		Assert.True(arrivals.AmendmentByDate);
		Assert.True(departures.AmendmentByDays);
	}

	[Fact]
	public void the_question_and_the_cycles_caption_name_the_selected_cycle()
	{
		ArrivalsViewModel arrivals = new();
		DeparturesViewModel departures = new();

		Assert.EndsWith("from the selected cycle?", arrivals.AmendmentQuestion, StringComparison.Ordinal);
		Assert.Equal("(1 = only procedures amended in the selected cycle)", departures.AmendmentCyclesCaption);

		List<string?> changed = [];
		departures.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
		arrivals.SetSelectedCycle(Cycle2610);
		departures.SetSelectedCycle(Cycle2610);

		Assert.EndsWith("from the 2610 cycle?", arrivals.AmendmentQuestion, StringComparison.Ordinal);
		Assert.Equal("(1 = only procedures amended in 2610)", arrivals.AmendmentCyclesCaption);
		Assert.EndsWith("from the 2610 cycle?", departures.AmendmentQuestion, StringComparison.Ordinal);
		Assert.Equal("(1 = only procedures amended in 2610)", departures.AmendmentCyclesCaption);
		Assert.Contains(nameof(DeparturesViewModel.AmendmentCyclesCaption), changed);
		Assert.Throws<ArgumentNullException>(() => arrivals.SetSelectedCycle(null!));
	}
}
