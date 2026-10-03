using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Application.AliasGuide;
using FeBuddy.Core.Application.AliasGuide.Models;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers <see cref="AliasGuideViewModel"/>: the page shows the guide the export writes, Back runs
/// what the page was given, and both exports are there.
/// </summary>
public sealed class AliasGuideViewModelTests
{
	[Fact]
	public void the_page_shows_the_guide_the_export_writes()
	{
		AliasGuideDocument exported = AliasGuideContent.Build();

		AliasGuideViewModel page = new(() => { });

		Assert.Equal(exported.Title, page.Document.Title);
		Assert.Equal(exported.Lead, page.Document.Lead);
		Assert.Equal(exported.ReadingNotes.Select(note => note.Text), page.Document.ReadingNotes.Select(note => note.Text));
		Assert.Equal(exported.Sections.Select(section => section.Title), page.Document.Sections.Select(section => section.Title));
		Assert.Contains("every AIRAC cycle", page.Document.Lead, StringComparison.Ordinal);
	}

	[Fact]
	public void back_runs_the_action_the_page_was_given()
	{
		int calls = 0;
		AliasGuideViewModel page = new(() => calls++);

		page.BackCommand.Execute(null);

		Assert.Equal(1, calls);
	}

	/// <summary>Both open dialogs, so they are not run here.</summary>
	[Fact]
	public void the_guide_and_the_practice_page_can_both_be_exported()
	{
		AliasGuideViewModel page = new(() => { });

		Assert.True(page.ExportGuideCommand.CanExecute(null));
		Assert.True(page.ExportPracticeCommand.CanExecute(null));
	}
}
