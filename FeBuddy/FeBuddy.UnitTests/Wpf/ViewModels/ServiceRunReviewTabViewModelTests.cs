using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

using FeBuddy.Core.Application.Conversions.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

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

	/// <summary>A group's copy button copies its messages, a blank line between each, backticks kept for pasting as code.</summary>
	[Fact]
	public void a_message_group_copies_its_messages_a_blank_line_apart()
	{
		SubServiceMessageGroup group = new("Warning", LogLevel.Warning, ["First `vor`.", "Second."]);

		Assert.Equal($"First `vor`.{Environment.NewLine}{Environment.NewLine}Second.", group.CopyText);
		Assert.Equal("Copy these 2 messages", group.CopyToolTip);

		SubServiceMessageGroup one = new("Info", LogLevel.Info, ["Only."]);
		Assert.Equal("Only.", one.CopyText);
		Assert.Equal("Copy this message", one.CopyToolTip);
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

	/// <summary>
	/// The review counts the files a run wrote, set once, rather than listing thousands one by one;
	/// a new run starts from none, and a failed run counts what it managed to write.
	/// </summary>
	[Fact]
	public void the_files_written_are_counted_once()
	{
		ServiceRunReviewTabViewModel review = new();
		review.BeginRun(["Departures"]);
		List<string> changed = [];
		review.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

		review.CompleteRun(Result, "done", [], [.. Enumerable.Range(0, 5000).Select(i => $"DP{i}.geojson")], outputDirectory: "out");

		Assert.Equal(5000, review.FilesWrittenCount);
		Assert.True(review.HasFiles);
		Assert.Single(changed, nameof(ServiceRunReviewTabViewModel.FilesWrittenCount));

		review.BeginRun(["Departures"]);
		Assert.Equal(0, review.FilesWrittenCount);
		Assert.False(review.HasFiles);

		review.FailRun("boom", ["A.geojson", "B.geojson"], "out");
		Assert.Equal(2, review.FilesWrittenCount);

		review.BeginRun(["Departures"]);
		review.FailRun("boom");
		Assert.False(review.HasFiles);
	}

	/// <summary>
	/// An expanded message list is one flat list for the view to virtualize: each group's heading,
	/// then its messages, the last of each closing its group's panel.
	/// </summary>
	[Fact]
	public void the_message_lists_are_each_groups_heading_then_its_messages()
	{
		SubServiceRunResult result = new("Airways", "2 airways",
		[
			new ServiceMessage(LogLevel.Info, "Airways", "Airway 'J1': routine one."),
			new ServiceMessage(LogLevel.Warning, "Airways", "Airway 'J2': check this."),
			new ServiceMessage(LogLevel.Info, "Airways", "Airway 'J1': routine two."),
			new ServiceMessage(LogLevel.Info, "Airways", "Airway 'V3': routine three."),
		],
		m => m.Text[8..10]);

		Assert.Equal<object>(
		[
			result.Attention[0],
			new SubServiceMessageRow("Airway 'J2': check this.", true),
		], result.AttentionRows);

		Assert.Equal<object>(
		[
			result.Info[0],
			new SubServiceMessageRow("Airway 'J1': routine one.", false),
			new SubServiceMessageRow("Airway 'J1': routine two.", true),
			result.Info[1],
			new SubServiceMessageRow("Airway 'V3': routine three.", true),
		], result.InfoRows);
		Assert.Equal(["J1", "V3"], result.Info.Select(g => g.Title));
		Assert.Equal(3, result.InfoMessageCount);
	}

	/// <summary>Tens of thousands of messages are grouped and listed at once, so finishing a run never stalls the window.</summary>
	[Fact]
	public void tens_of_thousands_of_messages_are_listed_at_once()
	{
		ServiceMessage[] messages = [.. Enumerable.Range(0, 50_000).Select(i => new ServiceMessage(
			i % 10 == 0 ? LogLevel.Warning : LogLevel.Info, "Departures", $"Procedure 'DP{i % 500}': message {i}."))];
		System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

		SubServiceRunResult result = new("Departures", "lots", messages, m => m.Text[11..m.Text.IndexOf('\'', 11)]);

		Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1), $"Took {stopwatch.Elapsed}.");
		Assert.Equal(50_000, result.AttentionRows.OfType<SubServiceMessageRow>().Count() + result.InfoRows.OfType<SubServiceMessageRow>().Count());
		Assert.Equal(5_000, result.AttentionMessageCount);
	}
}
