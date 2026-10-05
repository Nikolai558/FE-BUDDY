using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers what a tab says about its problems (<see cref="ServiceTabViewModel"/>,
/// <see cref="ServiceValidation"/>): every problem listed at its top, each area keyed for its card's
/// outline, and the Preview Settings tab pointing at the tabs to fix.
/// </summary>
public sealed class ServiceTabValidationTests
{
	[Fact]
	public void every_problem_is_listed_and_each_area_keeps_its_first_message()
	{
		TestTab tab = new(("Files", "No files are ticked."), ("Files", "Pick a layout."), ("Roi", "Set a region."));

		tab.Revalidate();

		Assert.Equal(["No files are ticked.", "Pick a layout.", "Set a region."], tab.ValidationErrors);
		Assert.Equal("No files are ticked.", tab.ValidationError);
		Assert.Equal("No files are ticked.", tab.FieldErrors["Files"]);
		Assert.Equal("Set a region.", tab.FieldErrors["Roi"]);
		Assert.Equal(ServiceTabStatus.Invalid, tab.Status);
		Assert.Equal("3 things on this page need your attention. Their areas are outlined in red below.", tab.ValidationSummary);
	}

	[Fact]
	public void one_problem_is_summed_up_in_the_singular()
	{
		TestTab tab = new(("Roi", "Set a region."));

		tab.Revalidate();

		Assert.Equal("One thing on this page needs your attention. Its area is outlined in red below.", tab.ValidationSummary);
	}

	/// <summary>Validation runs on every keystroke, so the list is only replaced when it changes.</summary>
	[Fact]
	public void the_list_is_only_replaced_when_the_problems_change()
	{
		TestTab tab = new(("Roi", "Set a region."));
		tab.Revalidate();
		List<string?> changed = [];
		tab.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		tab.Revalidate();
		tab.Problems.Clear();
		tab.Revalidate();

		Assert.Equal(1, changed.Count(name => name == nameof(ServiceTabViewModel.ValidationErrors)));
		Assert.Empty(tab.ValidationErrors);
		Assert.Equal(ServiceTabStatus.Ok, tab.Status);
	}

	[Fact]
	public void the_preview_points_to_the_red_dots_on_the_left()
	{
		TestTab fixes = new("Fixes", ("FixUses", "No fix uses are ticked."));
		TestTab airways = new("Airways", ("Roi", "Set a region."));
		fixes.Revalidate();
		airways.Revalidate();

		ServicePreviewTabViewModel one = Preview(fixes);
		ServicePreviewTabViewModel two = Preview(fixes, airways);

		Assert.Equal(
			"Fixes needs fixing before the run. In the list of tabs on the left, click the tab with a red dot: " +
			"the red box at its top lists what to fix, and each area to fix is outlined in red.",
			one.BlockingIssue);
		Assert.StartsWith("Fixes and Airways need fixing before the run.", two.BlockingIssue, StringComparison.Ordinal);
		Assert.Contains("click each tab with a red dot", two.BlockingIssue, StringComparison.Ordinal);
	}

	[Fact]
	public void the_preview_names_unsaved_tabs_by_their_amber_dot()
	{
		TestTab navaids = new("NAVAIDs");
		navaids.Edit();

		ServicePreviewTabViewModel preview = Preview(navaids);

		Assert.Null(preview.BlockingIssue);
		Assert.Equal(
			"NAVAIDs has unsaved changes (an amber dot in the list on the left). They are saved before the run starts.",
			preview.UnsavedNotice);
	}

	private static ServicePreviewTabViewModel Preview(params TestTab[] tabs)
	{
		ServicePreviewTabViewModel preview = new("Preview Settings", "Run", new RelayCommand(() => { }), () => tabs);
		preview.Refresh();
		return preview;
	}

	/// <summary>A tab whose problems are whatever the test gives it, each against an area key.</summary>
	private sealed class TestTab(string title, params (string Area, string Message)[] problems) : ServiceTabViewModel
	{
		public TestTab(params (string Area, string Message)[] problems)
			: this("Test", problems)
		{
		}

		public List<(string Area, string Message)> Problems { get; } = [.. problems];

		public override string Title => title;

		public override IReadOnlyList<ServicePreviewSection> BuildPreviewSummary() => [];

		internal void Edit() => MarkDirty();

		protected override void Validate(ServiceValidation validation)
		{
			foreach ((string area, string message) in Problems)
			{
				validation.AddArea(area, message);
			}
		}
	}
}
