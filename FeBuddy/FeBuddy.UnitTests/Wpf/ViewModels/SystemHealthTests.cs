using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.Models;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Airac.Models;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the Systems drawer (issue #322): the AIRAC data row says which cycles are ready, and turns
/// amber with a note when a ready cycle has no d-TPP Metafile, as the General tab's "partial" does; the
/// drawer's heading names that instead of "1 needs attention" when it is the only issue.
/// </summary>
public sealed class SystemHealthTests
{
	private const AiracCyclePosition Previous = AiracCyclePosition.Previous;
	private const AiracCyclePosition Current = AiracCyclePosition.Current;
	private const AiracCyclePosition Next = AiracCyclePosition.Next;
	private const CycleDataState Ready = CycleDataState.Ready;

	private static readonly HealthRow Internet = new("Internet", "connected", StatusKind.Ok);
	private static readonly HealthRow Updates = new("Updates", "latest version", StatusKind.Ok);

	private static HealthRow Airac(AiracCycleReadiness readiness, params (AiracCyclePosition, CycleDataState, bool)[] cycles) =>
		ShellViewModel.DescribeAiracData(readiness, cycles);

	[Fact]
	public void every_cycle_ready_with_its_metafile_is_green()
	{
		HealthRow row = Airac(AiracCycleReadiness.Ready, (Previous, Ready, true), (Current, Ready, true), (Next, Ready, true));

		Assert.Equal(new HealthRow("AIRAC data", "previous / current / next ready", StatusKind.Ok), row);
		Assert.Equal("All systems nominal", ShellViewModel.SummarizeHealth([Internet, row, Updates]));
	}

	/// <summary>A next cycle the FAA hasn't published is normal: the row says so and stays green.</summary>
	[Fact]
	public void a_next_cycle_not_published_yet_is_said_and_stays_green()
	{
		HealthRow row = Airac(AiracCycleReadiness.Ready, (Previous, Ready, true), (Current, Ready, true), (Next, CycleDataState.NotYetPublished, false));

		Assert.Equal("previous / current ready, next not published yet", row.Detail);
		Assert.Equal(StatusKind.Ok, row.Kind);
	}

	[Fact]
	public void a_missing_next_metafile_turns_the_drawer_amber_and_names_it_in_the_heading()
	{
		HealthRow row = Airac(AiracCycleReadiness.Ready, (Previous, Ready, true), (Current, Ready, true), (Next, Ready, false));

		Assert.Equal(StatusKind.Warn, row.Kind);
		Assert.Equal("previous / current / next ready", row.Detail);
		Assert.Equal("next: no d-TPP Metafile yet", row.Note);
		Assert.Equal("Ready except next d-TPP Metafile", ShellViewModel.SummarizeHealth([Internet, row, Updates]));
	}

	[Fact]
	public void two_missing_metafiles_are_both_named()
	{
		HealthRow row = Airac(AiracCycleReadiness.Ready, (Previous, Ready, false), (Current, Ready, true), (Next, Ready, false));

		Assert.Equal("previous and next: no d-TPP Metafiles yet", row.Note);
		Assert.Equal("Ready except previous and next d-TPP Metafiles", row.OnlyIssueSummary);
	}

	/// <summary>With anything else to look at, the heading counts as usual, so the user opens the drawer.</summary>
	[Fact]
	public void with_another_issue_the_heading_counts_them()
	{
		HealthRow airac = Airac(AiracCycleReadiness.Ready, (Current, Ready, true), (Next, Ready, false));
		HealthRow update = new("Updates", "v3.1.0 available", StatusKind.Warn);

		Assert.Equal("2 need attention", ShellViewModel.SummarizeHealth([Internet, airac, update]));
		Assert.Equal("1 needs attention", ShellViewModel.SummarizeHealth([Internet, Airac(AiracCycleReadiness.Ready, (Current, Ready, true)), update]));
	}

	/// <summary>A failed cycle as well as a missing metafile is more than the metafile: the heading counts it.</summary>
	[Fact]
	public void a_failed_cycle_with_a_missing_metafile_keeps_the_usual_heading()
	{
		HealthRow row = Airac(AiracCycleReadiness.Degraded, (Previous, CycleDataState.Failed, false), (Current, Ready, true), (Next, Ready, false));

		Assert.Equal(new HealthRow("AIRAC data", "ready — a best-effort cycle failed", StatusKind.Warn, "next: no d-TPP Metafile yet"), row);
		Assert.Equal("1 needs attention", ShellViewModel.SummarizeHealth([Internet, row, Updates]));
	}

	[Theory]
	[InlineData(AiracCycleReadiness.Unavailable, "current cycle failed to download or parse", StatusKind.Down)]
	[InlineData(AiracCycleReadiness.Waiting, "downloading and parsing…", StatusKind.Warn)]
	public void a_cycle_not_ready_says_why(AiracCycleReadiness readiness, string detail, StatusKind kind)
	{
		Assert.Equal(new HealthRow("AIRAC data", detail, kind), Airac(readiness, (Current, CycleDataState.Parsing, false)));
	}
}
