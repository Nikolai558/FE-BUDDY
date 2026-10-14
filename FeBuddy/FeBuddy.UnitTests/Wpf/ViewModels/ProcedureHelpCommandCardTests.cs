using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the Procedure Charts tab's Chart Recall Help Command card: the command is on and keeps its
/// name to start; a new name loses the dot typed in front and must be letters and numbers only; what the tab saves
/// and sends; nothing is checked while the alias file is off; and the name the Alias Command Guide
/// shows, from what was saved - against a throwaway config.
/// </summary>
[Collection("AppLog")]
public sealed class ProcedureHelpCommandCardTests : IDisposable
{
	private const string Node = "Services.AiracService.Procedures";
	private const string NameError = "HelpCommandName";

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_ProcedureHelpCommand_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public ProcedureHelpCommandCardTests()
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

	/// <summary>The tab with only the given outputs on, as the General tab would set them: the alias file alone by default.</summary>
	private static ProceduresViewModel Tab(SubServiceOutputKinds on = SubServiceOutputKinds.Alias)
	{
		ProceduresViewModel tab = new();
		SubServiceRow row = new(AiracSubServices.All.Single(d => d.Key == AiracSubServices.ProceduresKey), () => { });
		row.Load(included: true, on);
		tab.AttachOutputs(row);
		return tab;
	}

	[Fact]
	public void the_help_command_is_on_under_its_own_name_to_start()
	{
		ProceduresViewModel tab = Tab();

		Assert.True(tab.IncludeHelpCommand);
		Assert.False(tab.RenameHelpCommand);
		Assert.Equal(string.Empty, tab.HelpCommandName);
		Assert.Equal("helpCHARTRECALL", tab.HelpCommand);
		Assert.False(tab.IsDirty);

		IReadOnlyDictionary<string, string> block = tab.BuildSettingsBlock();
		Assert.Equal("Y", block["IncludeHelpCommand"]);
		Assert.False(block.ContainsKey("HelpCommandName"));
	}

	[Fact]
	public void the_question_names_the_default_command()
	{
		Assert.Equal(
			"The command is `.helpCHARTRECALL` by default. Would you like to rename it?",
			ProceduresViewModel.RenameHelpCommandQuestion);
	}

	[Theory]
	[InlineData("helpCR", "helpCR")]
	[InlineData(".helpCR", "helpCR")]
	[InlineData("..helpCR", "helpCR")]
	[InlineData(".", "")]
	public void a_dot_typed_in_front_of_the_new_name_is_dropped(string typed, string expected)
	{
		ProceduresViewModel tab = Tab();

		tab.HelpCommandName = typed;

		Assert.Equal(expected, tab.HelpCommandName);
	}

	[Fact]
	public void the_new_name_is_used_and_sent_only_while_renaming()
	{
		ProceduresViewModel tab = Tab();
		tab.HelpCommandName = "helpCR";

		Assert.Equal("helpCHARTRECALL", tab.HelpCommand);
		Assert.False(tab.BuildSettingsBlock().ContainsKey("HelpCommandName"));

		tab.RenameHelpCommand = true;
		Assert.Equal("helpCR", tab.HelpCommand);
		Assert.Equal("helpCR", tab.BuildSettingsBlock()["HelpCommandName"]);

		// Back to No: the default again, and the name is kept for next time.
		tab.RenameHelpCommand = false;
		Assert.Equal("helpCHARTRECALL", tab.HelpCommand);
		Assert.Equal("helpCR", tab.HelpCommandName);
	}

	[Fact]
	public void with_the_help_command_off_no_name_is_sent()
	{
		ProceduresViewModel tab = Tab();
		tab.RenameHelpCommand = true;
		tab.HelpCommandName = "helpCR";

		tab.IncludeHelpCommand = false;

		IReadOnlyDictionary<string, string> block = tab.BuildSettingsBlock();
		Assert.Equal("N", block["IncludeHelpCommand"]);
		Assert.False(block.ContainsKey("HelpCommandName"));
	}

	[Theory]
	[InlineData("", "Type the command's new name, or pick No to keep .helpCHARTRECALL.")]
	[InlineData("   ", "Type the command's new name, or pick No to keep .helpCHARTRECALL.")]
	[InlineData("help CR", "Use letters and numbers only: no spaces or symbols.")]
	[InlineData("help.CR", "Use letters and numbers only: no spaces or symbols.")]
	[InlineData("help_CR", "Use letters and numbers only: no spaces or symbols.")]
	[InlineData("help{CR}", "Use letters and numbers only: no spaces or symbols.")]
	[InlineData("helpCR", null)]
	[InlineData("helpCR2", null)]
	[InlineData(" helpCR ", null)]
	public void a_new_name_must_be_typed_and_be_letters_and_numbers_only(string typed, string? error)
	{
		ProceduresViewModel tab = Tab();
		tab.RenameHelpCommand = true;

		tab.HelpCommandName = typed;

		Assert.Equal(error, tab.FieldErrors[NameError]);
	}

	[Fact]
	public void the_name_is_not_checked_while_it_is_not_used()
	{
		ProceduresViewModel tab = Tab();
		tab.RenameHelpCommand = true;
		Assert.NotNull(tab.FieldErrors[NameError]);

		tab.RenameHelpCommand = false;
		Assert.Null(tab.FieldErrors[NameError]);

		tab.RenameHelpCommand = true;
		tab.IncludeHelpCommand = false;
		Assert.Null(tab.FieldErrors[NameError]);
	}

	/// <summary>The card greys out while the alias file is off, so its box isn't checked then.</summary>
	[Fact]
	public void the_name_is_not_checked_while_the_alias_file_is_off()
	{
		UserConfigFile.TrySetValue($"{Node}.Airports", "CLE");
		ProceduresViewModel tab = Tab(SubServiceOutputKinds.ProceduresJson);

		tab.RenameHelpCommand = true;

		Assert.Null(tab.FieldErrors[NameError]);
	}

	[Fact]
	public void the_choices_are_saved_and_read_back()
	{
		ProceduresViewModel tab = Tab();
		tab.RenameHelpCommand = true;
		tab.HelpCommandName = "helpCR";
		tab.IncludeHelpCommand = false;

		Assert.True(tab.Save());

		Assert.Equal("N", UserConfigFile.GetValue($"{Node}.IncludeHelpCommand"));
		Assert.Equal("Y", UserConfigFile.GetValue($"{Node}.RenameHelpCommand"));
		Assert.Equal("helpCR", UserConfigFile.GetValue($"{Node}.HelpCommandName"));

		ProceduresViewModel reopened = Tab();
		Assert.False(reopened.IncludeHelpCommand);
		Assert.True(reopened.RenameHelpCommand);
		Assert.Equal("helpCR", reopened.HelpCommandName);
		Assert.False(reopened.IsDirty);
	}

	/// <summary>A name saved with its dot (by hand) reads without it, and the tab isn't marked changed for it.</summary>
	[Fact]
	public void a_saved_name_with_its_dot_reads_without_it()
	{
		UserConfigFile.TrySetValue($"{Node}.RenameHelpCommand", "Y");
		UserConfigFile.TrySetValue($"{Node}.HelpCommandName", ".helpCR");

		ProceduresViewModel tab = Tab();

		Assert.Equal("helpCR", tab.HelpCommandName);
		Assert.Equal("helpCR", tab.HelpCommand);
		Assert.False(tab.IsDirty);
	}

	// ---- the name Info ▸ Alias Command Guide shows ----

	[Fact]
	public void with_nothing_saved_the_guide_shows_the_default_name()
	{
		Assert.Equal("helpCHARTRECALL", ProceduresViewModel.SavedHelpCommand());
	}

	[Fact]
	public void the_guide_shows_a_saved_new_name()
	{
		UserConfigFile.TrySetValue($"{Node}.RenameHelpCommand", "Y");
		UserConfigFile.TrySetValue($"{Node}.HelpCommandName", ".helpCR");

		Assert.Equal("helpCR", ProceduresViewModel.SavedHelpCommand());
	}

	[Theory]
	[InlineData("N", "helpCR")]
	[InlineData("Y", "")]
	[InlineData("Y", "   ")]
	[InlineData("Y", "help CR")]
	[InlineData("Y", "help{CR}")]
	[InlineData("Y", "help_CR")]
	public void the_guide_shows_the_default_name_unless_a_usable_new_name_is_saved(string rename, string name)
	{
		UserConfigFile.TrySetValue($"{Node}.RenameHelpCommand", rename);
		UserConfigFile.TrySetValue($"{Node}.HelpCommandName", name);

		Assert.Equal("helpCHARTRECALL", ProceduresViewModel.SavedHelpCommand());
	}

	[Theory]
	[InlineData($"{Node}.IncludeHelpCommand")]
	[InlineData("Services.AiracService.Outputs.Procedures.Alias")]
	public void with_the_help_command_or_the_alias_file_off_the_guide_has_no_name_for_it(string key)
	{
		UserConfigFile.TrySetValue($"{Node}.RenameHelpCommand", "Y");
		UserConfigFile.TrySetValue($"{Node}.HelpCommandName", "helpCR");
		UserConfigFile.TrySetValue(key, "N");

		Assert.Null(ProceduresViewModel.SavedHelpCommand());
	}

	[Fact]
	public void the_guide_reads_what_was_saved_not_unsaved_changes()
	{
		ProceduresViewModel tab = Tab();
		tab.RenameHelpCommand = true;
		tab.HelpCommandName = "helpCR";

		Assert.Equal("helpCHARTRECALL", ProceduresViewModel.SavedHelpCommand());

		Assert.True(tab.Save());
		Assert.Equal("helpCR", ProceduresViewModel.SavedHelpCommand());
	}

	[Fact]
	public void changing_a_choice_marks_the_tab_changed()
	{
		ProceduresViewModel tab = Tab();

		tab.IncludeHelpCommand = false;
		Assert.True(tab.IsDirty);

		tab.IncludeHelpCommand = true;
		Assert.False(tab.IsDirty);
	}
}
