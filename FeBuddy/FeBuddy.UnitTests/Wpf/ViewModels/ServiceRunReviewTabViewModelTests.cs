using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

using FeBuddy.Core.Application.Conversions.Models;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the Review tab's run feed (<see cref="ServiceRunReviewTabViewModel"/>): steps follow
/// the progress reports, the run's end settles every step, and a report that lands after the run
/// has ended cannot leave a step "working...".
/// </summary>
public sealed class ServiceRunReviewTabViewModelTests
{
	private static readonly SourceFilesConversionResult Result = new()
	{
		Messages = [],
		Elapsed = TimeSpan.FromSeconds(0.3),
		OutputDirectory = "out",
		Files = [],
	};

	[Fact]
	public void steps_follow_the_reports_and_the_end_of_the_run_finishes_any_still_working()
	{
		ServiceRunReviewTabViewModel review = new();
		review.BeginRun(["Airports"]);

		review.ReportStep("Airports", "Building airports", isComplete: false);
		review.ReportStep("AIRAC", "Checking the alias files for duplicate commands", isComplete: false);

		Assert.Equal([RunStepStatus.Working, RunStepStatus.Working], review.Steps.Select(s => s.Status));

		review.CompleteRun(Result, "done", [], [], outputDirectory: null);

		Assert.Equal(["Airports", "AIRAC"], review.Steps.Select(s => s.Name));
		Assert.All(review.Steps, step => Assert.Equal(RunStepStatus.Finished, step.Status));
	}

	/// <summary>
	/// What the AIRAC Service showed on a quick run: the duplicate-alias check's report reached the
	/// feed after the run had finished, and its step stayed "working..." for good.
	/// </summary>
	[Fact]
	public void a_report_that_lands_after_the_run_ended_is_dropped()
	{
		ServiceRunReviewTabViewModel review = new();
		review.BeginRun(["Airports"]);
		review.ReportStep("Airports", "Airports complete", isComplete: true);
		review.CompleteRun(Result, "done", [], [], outputDirectory: null);

		review.ReportStep("AIRAC", "Checking the alias files for duplicate commands", isComplete: false);
		review.ReportStep("Airports", "late", isComplete: false);

		RunStep step = Assert.Single(review.Steps);
		Assert.Equal(RunStepStatus.Finished, step.Status);
		Assert.Equal("Airports complete", step.Detail);
	}

	[Fact]
	public void a_report_that_lands_after_a_failed_run_is_dropped_too()
	{
		ServiceRunReviewTabViewModel review = new();
		review.BeginRun(["Airports"]);
		review.FailRun("boom");

		review.ReportStep("Airports", "late", isComplete: false);

		Assert.Equal(RunStepStatus.Failed, Assert.Single(review.Steps).Status);
	}
}
