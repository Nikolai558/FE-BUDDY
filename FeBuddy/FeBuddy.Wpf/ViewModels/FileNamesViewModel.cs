using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Infrastructure.Configuration;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>File Names</b> tab inside the AIRAC Service screen, just before Preview Settings: every file
/// the selected sub-services will write, by folder, and - when the user chooses to rename files - a
/// name of their own for any of them.
/// </summary>
/// <remarks>
/// <para>
/// The list is read from the other tabs' settings as they are now, each time the tab is shown (see
/// <see cref="GeojsonSubServiceViewModel.OutputFileEntries"/>), as the vNAS Alias Upload tab reads
/// its alias files. A file's choice - whether to rename it, and its new name - is kept by file key,
/// including files the current settings do not write, so a file that drops off the list and comes
/// back keeps its choice.
/// </para>
/// <para>
/// While renaming is on, every file ticked for renaming needs a new name. A file with no choice yet
/// starts ticked, so a file a changed setting adds to the list - one the user has never seen here -
/// is flagged until they name it or untick it.
/// </para>
/// <para>
/// Saved under <c>Services.AiracService.FileNames</c>: <c>RenameFiles</c> (<c>Y</c> / <c>N</c>), and
/// one numbered entry per file with a choice - <c>Files.1.Key</c>, <c>Files.1.Rename</c>,
/// <c>Files.1.Name</c> and so on, numbered because a key such as <c>Airways.txt</c> has a dot in it.
/// A run gets the new names of the ticked files it writes (<see cref="AiracServiceSettings.FileNames"/>).
/// </para>
/// </remarks>
public sealed class FileNamesViewModel : SubServiceSettingsViewModel
{
	private const string Node = "Services.AiracService.FileNames";
	private const string RenameFilesKey = "RenameFiles";
	private const string FilesKey = "Files";
	private const string KeyField = "Key";
	private const string RenameField = "Rename";
	private const string NameField = "Name";

	// Every file's choice by key: what was saved, plus the user's edits since.
	private readonly Dictionary<string, FileNameChoice> _choices = new(StringComparer.OrdinalIgnoreCase);

	// The files that had a saved choice when the tab was last loaded or saved; any other file is new.
	private readonly HashSet<string> _savedKeys = new(StringComparer.OrdinalIgnoreCase);

	private Func<IEnumerable<OutputFileEntry>> _listFiles = () => [];
	private Func<string> _cycleFolderName = () => string.Empty;
	private string? _filesSignature;
	private bool _renameFiles;
	private bool _changingAll;

	/// <summary>Builds the tab and restores its saved settings. It lists no files until <see cref="AttachToService"/>.</summary>
	public FileNamesViewModel()
	{
		RenameAllCommand = new RelayCommand(() => SetAllRename(true), () => RenameFiles && Rows.Any(row => row.CanRename && !row.Rename));
		RenameNoneCommand = new RelayCommand(() => SetAllRename(false), () => RenameFiles && Rows.Any(row => row.CanRename && row.Rename));

		Saved += (_, _) => ReadSavedKeys();

		LoadFromConfig();
	}

	/// <inheritdoc />
	public override string NodePath => Node;

	/// <inheritdoc />
	public override string Title => "File Names";

	/// <summary>Whether the user renames some files. Off by default: every file keeps FE-Buddy's name.</summary>
	public bool RenameFiles
	{
		get => _renameFiles;
		set
		{
			if (SetProperty(ref _renameFiles, value))
			{
				MarkDirty();
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	/// <summary>Every file the run will write, grouped by the folder it goes in.</summary>
	public ObservableCollection<FileNameFolder> Folders { get; } = [];

	/// <summary>Whether the run writes any file at all (otherwise the tab says so).</summary>
	public bool HasFiles => Folders.Count > 0;

	/// <summary>Ticks every file for renaming.</summary>
	public ICommand RenameAllCommand { get; }

	/// <summary>Unticks every file, so each keeps FE-Buddy's name until the user ticks it.</summary>
	public ICommand RenameNoneCommand { get; }

	private IEnumerable<FileNameRow> Rows => Folders.SelectMany(folder => folder.Files);

	/// <summary>The files the user renames: ticked, with a new name that can be used.</summary>
	private IEnumerable<FileNameRow> RenamedRows => Rows.Where(row => row.CanRename && IsRenamed(row));

	// ================= the other tabs =================

	/// <summary>
	/// Lets the tab see what the AIRAC Service's other tabs will write, and the cycle folder it goes in.
	/// </summary>
	/// <param name="listFiles">Every file the selected sub-services' settings write right now.</param>
	/// <param name="cycleFolderName">The selected cycle's folder name, e.g. <c>AIRAC_2610</c>.</param>
	internal void AttachToService(Func<IEnumerable<OutputFileEntry>> listFiles, Func<string> cycleFolderName)
	{
		_listFiles = listFiles;
		_cycleFolderName = cycleFolderName;
		RefreshFiles();
	}

	/// <summary>
	/// Re-reads the files from the other tabs. The rows are rebuilt only when the list changed, so a
	/// box the user is typing in keeps its focus. Always re-validates: a new file can make the tab invalid.
	/// </summary>
	internal void RefreshFiles()
	{
		List<OutputFileEntry> files = [.. _listFiles()];
		string cycleFolder = _cycleFolderName();
		string signature = string.Join('|', files.Select(file => $"{file.Folder}/{file.FileName}/{file.Key}/{file.CanRename}").Prepend(cycleFolder));

		if (signature != _filesSignature)
		{
			_filesSignature = signature;
			Folders.Clear();

			// The cycle folder itself first, then its folders in name order.
			foreach (IGrouping<string, OutputFileEntry> folder in files
				.GroupBy(file => file.Folder, StringComparer.OrdinalIgnoreCase)
				.OrderBy(folder => folder.Key.Length == 0 ? 0 : 1)
				.ThenBy(folder => folder.Key, StringComparer.OrdinalIgnoreCase))
			{
				Folders.Add(new FileNameFolder(
					folder.Key.Length == 0 ? cycleFolder : Path.Combine(cycleFolder, folder.Key),
					[.. folder.Select(NewRow)]));
			}

			OnPropertyChanged(nameof(HasFiles));
			CommandManager.InvalidateRequerySuggested();
		}

		Revalidate();
	}

	/// <summary>
	/// The new names for the run: each ticked file's key and new name, for the files the run writes.
	/// </summary>
	/// <returns>The block, or <see langword="null"/> when no file is renamed.</returns>
	internal IReadOnlyDictionary<string, string>? BuildFileNamesBlock()
	{
		Dictionary<string, string> block = new(StringComparer.OrdinalIgnoreCase);

		foreach (FileNameRow row in RenamedRows)
		{
			block[row.Key] = row.NewName.Trim();
		}

		return block.Count > 0 ? block : null;
	}

	// ================= save contract =================

	/// <inheritdoc />
	public override IReadOnlyList<ServicePreviewSection> BuildPreviewSummary()
	{
		RefreshFiles();

		FileNameRow[] renamed = [.. RenamedRows];

		List<ServicePreviewRow> rows =
		[
			new ServicePreviewRow("Rename files", !RenameFiles
				? "No - every file keeps FE-Buddy's name"
				: renamed.Length == 0 ? "Yes, but no file has a new name yet" : $"Yes - {renamed.Length} file(s)"),
		];

		rows.AddRange(renamed.Select(row => new ServicePreviewRow(row.FileName, row.NewName.Trim() + row.Extension)));

		return [new ServicePreviewSection(Title, rows)];
	}

	/// <inheritdoc />
	protected override void LoadFromConfig()
	{
		_renameFiles = GetBool(RenameFilesKey, false);

		_choices.Clear();

		foreach ((string key, FileNameChoice choice) in ReadSavedChoices())
		{
			_choices[key] = choice;
		}

		ReadSavedKeys();

		// Rebuild the rows from the reloaded choices.
		_filesSignature = null;

		OnPropertyChanged(nameof(RenameFiles));
		RefreshFiles();
		ClearDirty();
	}

	/// <inheritdoc />
	protected override void WriteToConfig()
	{
		Set(RenameFilesKey, YesNo(RenameFiles));

		// The list is written whole: a dropped choice must not leave its numbered keys behind.
		RemoveSubtree(FilesKey);

		int number = 0;

		foreach ((string key, FileNameChoice choice) in _choices.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
		{
			// A file still ticked with no name has no choice to keep: it stays new.
			if (choice.Rename && choice.Name.Trim().Length == 0)
			{
				continue;
			}

			number++;
			string prefix = $"{FilesKey}.{number.ToString(CultureInfo.InvariantCulture)}.";

			Set(prefix + KeyField, key);
			Set(prefix + RenameField, YesNo(choice.Rename));
			Set(prefix + NameField, choice.Name.Trim());
		}
	}

	/// <inheritdoc />
	/// <remarks>
	/// Only while renaming is on: every ticked file needs a usable new name
	/// (<see cref="OutputFileNames.Problem"/>), and no two files may end up with the same name - they
	/// would be written over each other. A file that can't be renamed keeps its name, so it is never
	/// checked.
	/// </remarks>
	protected override void Validate(ServiceValidation validation)
	{
		List<FileNameRow> rows = [.. Rows.Where(row => row.CanRename)];

		foreach (FileNameRow row in rows)
		{
			// "New" only once the user has saved choices: before that, every file is new.
			row.Error = !RenameFiles || !row.Rename ? null
				: row.NewName.Trim().Length > 0 ? OutputFileNames.Problem(row.NewName)
				: _savedKeys.Count > 0 && !_savedKeys.Contains(row.Key) ? "This file is new here. Type a new name for it, or untick it to keep FE-Buddy's name."
				: "Type a new name, or untick this file to keep FE-Buddy's name.";
		}

		if (!RenameFiles)
		{
			return;
		}

		foreach (IGrouping<string, FileNameRow> clash in rows
			.GroupBy(WrittenName, StringComparer.OrdinalIgnoreCase)
			.Where(clash => clash.Count() > 1))
		{
			foreach (FileNameRow row in clash.Where(row => IsRenamed(row) && row.Error is null))
			{
				FileNameRow other = clash.First(candidate => !ReferenceEquals(candidate, row));

				row.Error = IsRenamed(other)
					? $"{other.FileName} is being renamed {WrittenName(other)} too. Give each file its own name."
					: $"{other.FileName} is already the name of another file. Give this file a name of its own.";
			}
		}

		FileNameRow[] invalid = [.. rows.Where(row => row.HasError)];

		if (invalid.Length > 0)
		{
			validation.Add($"{invalid[0].FileName}: {invalid[0].Error}" + (invalid.Length > 1
				? $" {invalid.Length - 1} more file name(s) need attention too - see the marked boxes."
				: string.Empty));
		}
	}

	// ================= private =================

	/// <summary>Whether a row's file is written under a new name: ticked, with a name that can be used.</summary>
	private bool IsRenamed(FileNameRow row) =>
		RenameFiles && row.Rename && row.NewName.Trim().Length > 0 && OutputFileNames.Problem(row.NewName) is null;

	/// <summary>The name a row's file is written under.</summary>
	private string WrittenName(FileNameRow row) => IsRenamed(row) ? row.NewName.Trim() + row.Extension : row.FileName;

	private FileNameRow NewRow(OutputFileEntry file)
	{
		FileNameChoice choice = _choices.GetValueOrDefault(file.Key, FileNameChoice.New);
		return new FileNameRow(file, choice.Rename, choice.Name, OnRowChanged);
	}

	private void OnRowChanged(FileNameRow row)
	{
		_choices[row.Key] = new FileNameChoice(row.Rename, row.NewName);

		if (!_changingAll)
		{
			MarkDirty();
			CommandManager.InvalidateRequerySuggested();
		}
	}

	private void SetAllRename(bool rename)
	{
		_changingAll = true;

		try
		{
			foreach (FileNameRow row in Rows.Where(row => row.CanRename))
			{
				row.Rename = rename;
			}
		}
		finally
		{
			_changingAll = false;
		}

		MarkDirty();
		CommandManager.InvalidateRequerySuggested();
	}

	/// <summary>Notes which files have a saved choice, so any other file counts as new.</summary>
	private void ReadSavedKeys()
	{
		_savedKeys.Clear();
		_savedKeys.UnionWith(ReadSavedChoices().Select(saved => saved.Key));
		Revalidate();
	}

	/// <summary>Every saved choice, from the numbered <c>Files</c> entries. An entry with no key is skipped.</summary>
	private static IEnumerable<(string Key, FileNameChoice Choice)> ReadSavedChoices()
	{
		string prefix = $"{Node}.{FilesKey}.";
		SortedDictionary<int, Dictionary<string, string>> byNumber = [];

		foreach ((string key, string value) in UserConfigFile.SnapshotValues())
		{
			if (!key.StartsWith(prefix, StringComparison.Ordinal))
			{
				continue;
			}

			string[] parts = key[prefix.Length..].Split('.');

			if (parts.Length == 2 && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int number))
			{
				if (!byNumber.TryGetValue(number, out Dictionary<string, string>? fields))
				{
					fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
					byNumber[number] = fields;
				}

				fields[parts[1]] = value.Trim();
			}
		}

		foreach (Dictionary<string, string> fields in byNumber.Values)
		{
			if (fields.GetValueOrDefault(KeyField) is not { Length: > 0 } fileKey)
			{
				continue;
			}

			string rename = fields.GetValueOrDefault(RenameField, "Y");

			yield return (fileKey, new FileNameChoice(
				rename.Equals("Y", StringComparison.OrdinalIgnoreCase) || rename.Equals("true", StringComparison.OrdinalIgnoreCase),
				fields.GetValueOrDefault(NameField, string.Empty)));
		}
	}

	/// <summary>One file's choice: whether to rename it, and its new name as typed.</summary>
	/// <param name="Rename">Whether the file is ticked for renaming.</param>
	/// <param name="Name">Its new name, without an extension.</param>
	private readonly record struct FileNameChoice(bool Rename, string Name)
	{
		/// <summary>A file with no choice yet: ticked, with no name, so renaming it asks for one.</summary>
		public static FileNameChoice New { get; } = new(true, string.Empty);
	}
}
