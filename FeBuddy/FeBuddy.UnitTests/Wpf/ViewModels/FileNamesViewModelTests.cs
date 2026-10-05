using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers <see cref="FileNamesViewModel"/>: the files listed by folder, which new names are flagged,
/// a file the other tabs add after a save, and what is saved and sent to a run - against a stand-in
/// list of files and a throwaway config.
/// </summary>
[Collection("AppLog")]
public sealed class FileNamesViewModelTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_FileNames_" + Guid.NewGuid().ToString("N"));

	// What the other tabs write, as the test sets it.
	private readonly List<OutputFileEntry> _files =
	[
		OutputFileEntry.Renamable("Airways_High_Lines", "Geojson", "Airways"),
		OutputFileEntry.Renamable("Airways.txt", "Aliases", "Airways"),
		new("Departures_Lines", @"Geojson\<ARTCC>\<airport>", "<airport>_<procedure>_Lines.geojson", "Departures", CanRename: false),
		OutputFileEntry.Renamable("Duplicate_Alias_Commands.txt", string.Empty, "AIRAC Service"),
	];

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public FileNamesViewModelTests()
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

	/// <summary>The cycle folder itself first, then its folders by name; renaming is off to start with.</summary>
	[Fact]
	public void the_files_are_listed_by_folder_and_keep_their_names_by_default()
	{
		FileNamesViewModel tab = NewTab();

		Assert.False(tab.RenameFiles);
		Assert.True(tab.HasFiles);
		Assert.Equal(
			["AIRAC_2610", @"AIRAC_2610\Aliases", @"AIRAC_2610\Geojson", @"AIRAC_2610\Geojson\<ARTCC>\<airport>"],
			tab.Folders.Select(folder => folder.Folder));
		Assert.Equal("Duplicate_Alias_Commands.txt", Assert.Single(tab.Folders[0].Files).FileName);
		Assert.Null(tab.ValidationError);
		Assert.Null(tab.BuildFileNamesBlock());
	}

	[Fact]
	public void with_no_files_the_tab_says_so()
	{
		_files.Clear();
		FileNamesViewModel tab = NewTab();

		Assert.False(tab.HasFiles);
		Assert.Empty(tab.Folders);
	}

	/// <summary>Every file starts ticked, so turning renaming on asks for a name for each - except one that can't be renamed.</summary>
	[Fact]
	public void turning_renaming_on_asks_for_a_name_for_every_ticked_file()
	{
		FileNamesViewModel tab = NewTab();

		tab.RenameFiles = true;

		Assert.True(tab.IsDirty);
		Assert.Equal("Type a new name, or untick this file to keep FE-Buddy's name.", Row(tab, "Airways.txt").Error);
		Assert.Null(Row(tab, "Departures_Lines").Error);
		Assert.StartsWith("Duplicate_Alias_Commands.txt: Type a new name", tab.ValidationError, StringComparison.Ordinal);
		Assert.EndsWith("2 more file name(s) need attention too - see the marked boxes.", tab.ValidationError, StringComparison.Ordinal);
	}

	[Fact]
	public void naming_or_unticking_a_file_clears_it()
	{
		FileNamesViewModel tab = RenamingTab();

		Row(tab, "Airways_High_Lines").NewName = "ZOB High";
		Row(tab, "Airways.txt").Rename = false;
		Row(tab, "Duplicate_Alias_Commands.txt").NewName = "Dupes";

		Assert.All(tab.Folders.SelectMany(folder => folder.Files), row => Assert.Null(row.Error));
		Assert.Null(tab.ValidationError);
	}

	/// <summary>The same rules as Core's, from the typed name.</summary>
	[Theory]
	[InlineData("ZOB High.geojson", "Leave off .geojson: FE-Buddy adds the extension itself.")]
	[InlineData("ZOB/High", @"A file name can't contain \ / : * ? "" < > |.")]
	[InlineData("CON", "Windows keeps CON as a device name, so no file can have it. Choose another name.")]
	public void a_name_that_cannot_be_used_says_why(string name, string expected)
	{
		FileNamesViewModel tab = RenamingTab();

		Row(tab, "Airways_High_Lines").NewName = name;

		Assert.Equal(expected, Row(tab, "Airways_High_Lines").Error);
	}

	/// <summary>Two files would be written over each other - whether the other is renamed too or keeps its name.</summary>
	[Fact]
	public void two_files_may_not_end_up_with_one_name()
	{
		FileNamesViewModel tab = RenamingTab();
		Row(tab, "Airways.txt").NewName = "Duplicate_Alias_Commands";
		Row(tab, "Duplicate_Alias_Commands.txt").Rename = false;
		Row(tab, "Airways_High_Lines").NewName = "ZOB";

		Assert.Equal(
			"Duplicate_Alias_Commands.txt is already the name of another file. Give this file a name of its own.",
			Row(tab, "Airways.txt").Error);

		Row(tab, "Duplicate_Alias_Commands.txt").Rename = true;
		Row(tab, "Duplicate_Alias_Commands.txt").NewName = "ZOB";
		Row(tab, "Airways.txt").NewName = "ZOB Aliases";

		// ZOB.txt and ZOB.geojson differ by their extension.
		Assert.All(tab.Folders.SelectMany(folder => folder.Files), row => Assert.Null(row.Error));

		Row(tab, "Airways.txt").NewName = "zob";

		Assert.Equal("Duplicate_Alias_Commands.txt is being renamed ZOB.txt too. Give each file its own name.", Row(tab, "Airways.txt").Error);
		Assert.Equal("Airways.txt is being renamed zob.txt too. Give each file its own name.", Row(tab, "Duplicate_Alias_Commands.txt").Error);
	}

	[Fact]
	public void rename_all_and_rename_none_tick_every_file_that_can_be_renamed()
	{
		FileNamesViewModel tab = RenamingTab();

		tab.RenameNoneCommand.Execute(null);

		Assert.All(tab.Folders.SelectMany(folder => folder.Files).Where(row => row.CanRename), row => Assert.False(row.Rename));
		Assert.Null(tab.ValidationError);
		Assert.False(tab.RenameNoneCommand.CanExecute(null));

		tab.RenameAllCommand.Execute(null);

		Assert.All(tab.Folders.SelectMany(folder => folder.Files).Where(row => row.CanRename), row => Assert.True(row.Rename));
		Assert.NotNull(tab.ValidationError);
	}

	/// <summary>Only the ticked files with a name, and none at all while renaming is off.</summary>
	[Fact]
	public void the_run_gets_the_new_names_of_the_ticked_files()
	{
		FileNamesViewModel tab = RenamingTab();
		Row(tab, "Airways_High_Lines").NewName = "  ZOB High ";
		Row(tab, "Airways.txt").NewName = "ZOB Airways";
		Row(tab, "Airways.txt").Rename = false;

		IReadOnlyDictionary<string, string> block = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(tab.BuildFileNamesBlock());

		Assert.Equal("ZOB High", Assert.Single(block, pair => pair.Key == "Airways_High_Lines").Value);
		Assert.Single(block);

		tab.RenameFiles = false;

		Assert.Null(tab.BuildFileNamesBlock());
	}

	/// <summary>
	/// Every choice is saved, unticked files and their names included; a file still ticked with no
	/// name is not, so it is still new next time.
	/// </summary>
	[Fact]
	public void saving_keeps_every_choice_and_reloading_brings_it_back()
	{
		FileNamesViewModel tab = RenamingTab();
		Row(tab, "Airways_High_Lines").NewName = "ZOB High";
		Row(tab, "Airways.txt").NewName = "ZOB Airways";
		Row(tab, "Airways.txt").Rename = false;
		Row(tab, "Duplicate_Alias_Commands.txt").Rename = false;

		Assert.True(tab.Save());
		Assert.False(tab.IsDirty);

		UserConfigFile.ReadAll();
		Assert.Equal("Y", UserConfigFile.GetValue("Services.AiracService.FileNames.RenameFiles"));
		Assert.Equal("Airways.txt", UserConfigFile.GetValue("Services.AiracService.FileNames.Files.1.Key"));
		Assert.Equal("N", UserConfigFile.GetValue("Services.AiracService.FileNames.Files.1.Rename"));
		Assert.Equal("ZOB Airways", UserConfigFile.GetValue("Services.AiracService.FileNames.Files.1.Name"));

		FileNamesViewModel reloaded = NewTab();

		Assert.True(reloaded.RenameFiles);
		Assert.Equal("ZOB High", Row(reloaded, "Airways_High_Lines").NewName);
		Assert.False(Row(reloaded, "Airways.txt").Rename);
		Assert.Equal("ZOB Airways", Row(reloaded, "Airways.txt").NewName);
		Assert.False(Row(reloaded, "Duplicate_Alias_Commands.txt").Rename);
		Assert.False(reloaded.IsDirty);
		Assert.Null(reloaded.ValidationError);
	}

	/// <summary>The case the tab exists for: a setting on another tab adds a file after the names were saved.</summary>
	[Fact]
	public void a_file_another_tab_adds_after_a_save_is_flagged_as_new()
	{
		FileNamesViewModel tab = RenamingTab();
		Row(tab, "Airways_High_Lines").NewName = "ZOB High";
		Row(tab, "Airways.txt").Rename = false;
		Row(tab, "Duplicate_Alias_Commands.txt").Rename = false;
		Assert.True(tab.Save());

		_files.Add(OutputFileEntry.Renamable("Airways_Low_Lines", "Geojson", "Airways"));
		tab.RefreshFiles();

		FileNameRow added = Row(tab, "Airways_Low_Lines");
		Assert.True(added.Rename);
		Assert.Equal("This file is new here. Type a new name for it, or untick it to keep FE-Buddy's name.", added.Error);
		Assert.False(tab.IsDirty);
		Assert.Equal(
			"Airways_Low_Lines.geojson: This file is new here. Type a new name for it, or untick it to keep FE-Buddy's name.",
			tab.ValidationError);
	}

	/// <summary>A file that drops off the list and comes back keeps its name.</summary>
	[Fact]
	public void a_file_that_leaves_the_list_keeps_its_choice()
	{
		FileNamesViewModel tab = RenamingTab();
		Row(tab, "Airways_High_Lines").NewName = "ZOB High";

		OutputFileEntry high = _files[0];
		_files.Remove(high);
		tab.RefreshFiles();
		Assert.DoesNotContain(tab.Folders.SelectMany(folder => folder.Files), row => row.Key == "Airways_High_Lines");

		_files.Add(high);
		tab.RefreshFiles();

		Assert.Equal("ZOB High", Row(tab, "Airways_High_Lines").NewName);
	}

	/// <summary>The combined alias file was vNAS_Alias.txt: a name the user gave it then carries over.</summary>
	[Fact]
	public void a_name_saved_for_vnas_alias_txt_carries_over_to_combined_alias_txt()
	{
		UserConfigFile.TrySetValue("Services.AiracService.FileNames.RenameFiles", "Y");
		UserConfigFile.TrySetValue("Services.AiracService.FileNames.Files.1.Key", "vNAS_Alias.txt");
		UserConfigFile.TrySetValue("Services.AiracService.FileNames.Files.1.Rename", "Y");
		UserConfigFile.TrySetValue("Services.AiracService.FileNames.Files.1.Name", "ZOB Aliases");
		_files.Add(OutputFileEntry.Renamable("Combined_Alias.txt", "Aliases", "Concatenate Aliases"));

		FileNamesViewModel tab = NewTab();

		FileNameRow combined = Row(tab, "Combined_Alias.txt");
		Assert.True(combined.Rename);
		Assert.Equal("ZOB Aliases", combined.NewName);
		Assert.Null(combined.Error);
	}

	/// <summary>Putting a change back the way it was clears the unsaved mark.</summary>
	[Fact]
	public void undoing_an_edit_by_hand_leaves_the_tab_clean()
	{
		FileNamesViewModel tab = NewTab();

		tab.RenameFiles = true;
		tab.RenameFiles = false;

		Assert.False(tab.IsDirty);
	}

	[Fact]
	public void the_preview_lists_each_new_name()
	{
		FileNamesViewModel tab = NewTab();
		Assert.Equal("No - every file keeps FE-Buddy's name", Assert.Single(Assert.Single(tab.BuildPreviewSummary()).Rows).Value);

		tab.RenameFiles = true;
		Row(tab, "Airways_High_Lines").NewName = "ZOB High";

		ServicePreviewSection section = Assert.Single(tab.BuildPreviewSummary());
		Assert.Equal("File Names", section.Title);
		Assert.Collection(
			section.Rows,
			row => Assert.Equal(("Rename files", "Yes - 1 file(s)"), (row.Label, row.Value)),
			row => Assert.Equal(("Airways_High_Lines.geojson", "ZOB High.geojson"), (row.Label, row.Value)));
	}

	private FileNamesViewModel NewTab()
	{
		FileNamesViewModel tab = new();
		tab.AttachToService(() => _files, () => "AIRAC_2610");
		return tab;
	}

	private FileNamesViewModel RenamingTab()
	{
		FileNamesViewModel tab = NewTab();
		tab.RenameFiles = true;
		return tab;
	}

	private static FileNameRow Row(FileNamesViewModel tab, string key) =>
		tab.Folders.SelectMany(folder => folder.Files).Single(row => row.Key == key);
}
