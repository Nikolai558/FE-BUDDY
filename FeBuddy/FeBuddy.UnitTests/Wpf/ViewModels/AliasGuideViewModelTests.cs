using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Application.AliasGuide;
using FeBuddy.Core.Application.AliasGuide.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers <see cref="AliasGuideViewModel"/>: the page shows the guide the export writes, with the help
/// command as saved on Procedure Charts; Back runs what the page was given; and both exports are there -
/// against a throwaway config.
/// </summary>
[Collection("AppLog")]
public sealed class AliasGuideViewModelTests : IDisposable
{
	private const string Node = "Services.AiracService.Procedures";

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_AliasGuide_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public AliasGuideViewModelTests()
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

	/// <summary>The Chart Recall section's last command: the help command.</summary>
	private static GuideCommand HelpCommand(AliasGuideDocument guide) =>
		Assert.Single(guide.Sections, s => s.Id == "chart-recall").Blocks.OfType<GuideCommandTable>().First().Commands[^1];

	[Fact]
	public void the_page_shows_the_guide_the_export_writes()
	{
		AliasGuideDocument exported = AliasGuideContent.Build(ProceduresViewModel.SavedHelpCommand());

		AliasGuideViewModel page = new(() => { });

		Assert.Equal(exported.Title, page.Document.Title);
		Assert.Equal(exported.Lead, page.Document.Lead);
		Assert.Equal(exported.ReadingNotes.Select(note => note.Text), page.Document.ReadingNotes.Select(note => note.Text));
		Assert.Equal(exported.Sections.Select(section => section.Title), page.Document.Sections.Select(section => section.Title));
		Assert.Equal(HelpCommand(exported).Syntax, HelpCommand(page.Document).Syntax);
		Assert.Contains("every AIRAC cycle", page.Document.Lead, StringComparison.Ordinal);
	}

	[Fact]
	public void the_page_shows_the_help_command_by_its_saved_name()
	{
		UserConfigFile.TrySetValue($"{Node}.RenameHelpCommand", "Y");
		UserConfigFile.TrySetValue($"{Node}.HelpCommandName", "helpCR");

		AliasGuideViewModel page = new(() => { });

		Assert.Equal([".helpCR"], HelpCommand(page.Document).Syntax);
	}

	[Fact]
	public void with_the_help_command_off_the_page_shows_its_default_name()
	{
		UserConfigFile.TrySetValue($"{Node}.IncludeHelpCommand", "N");

		AliasGuideViewModel page = new(() => { });

		Assert.Equal([".helpCHARTRECALL"], HelpCommand(page.Document).Syntax);
		Assert.Contains("Your facility may have given it another name, or left it out.", HelpCommand(page.Document).Notes);
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
