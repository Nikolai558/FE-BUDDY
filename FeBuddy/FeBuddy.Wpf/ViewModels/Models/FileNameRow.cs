using FeBuddy.Wpf.Mvvm;

using FeBuddy.Core.Application.Airac.Models;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One file on the File Names tab: FE-Buddy's name for it, whether the user renames it, and the new
/// name.
/// </summary>
/// <remarks>
/// The tab keeps every file's choice by key and rebuilds these whenever the list of files changes,
/// so a row is only ever a view of that choice; <paramref name="onChanged"/> hands each change back
/// to it. The tab also sets <see cref="Error"/>, as it alone can see every file's name.
/// </remarks>
/// <param name="file">The file.</param>
/// <param name="rename">Whether it starts ticked for renaming.</param>
/// <param name="newName">The new name it starts with, without an extension.</param>
/// <param name="onChanged">Called with this row when the user ticks or unticks it, or edits its new name.</param>
public sealed class FileNameRow(OutputFileEntry file, bool rename, string newName, Action<FileNameRow> onChanged) : ObservableObject
{
	private readonly Action<FileNameRow> _onChanged = onChanged;
	private bool _rename = rename;
	private string _newName = newName;
	private string? _error;

	/// <summary>The file key, e.g. <c>Airways_High_Lines</c>.</summary>
	public string Key => file.Key;

	/// <summary>FE-Buddy's name for the file, e.g. <c>Airways_High_Lines.geojson</c>.</summary>
	public string FileName => file.FileName;

	/// <summary>
	/// What writes the file, e.g. <c>Airways</c>, and for a file that can't be renamed, why.
	/// </summary>
	public string Caption => file.CanRename
		? file.SubService
		: $"{file.SubService} · one file per procedure, named from the FAA's data, so these keep their names";

	/// <summary>Whether the user can give the file a name of their own.</summary>
	public bool CanRename => file.CanRename;

	/// <summary>The extension the file keeps whatever it is named, e.g. <c>.geojson</c>.</summary>
	public string Extension => OutputFileNames.ExtensionOf(file.Key);

	/// <summary>Whether the file is renamed. When it is not, its new-name box is greyed out and it keeps FE-Buddy's name.</summary>
	public bool Rename
	{
		get => _rename;
		set
		{
			if (SetProperty(ref _rename, value))
			{
				_onChanged(this);
			}
		}
	}

	/// <summary>The new name, without an extension, as the user typed it.</summary>
	public string NewName
	{
		get => _newName;
		set
		{
			if (SetProperty(ref _newName, value))
			{
				_onChanged(this);
			}
		}
	}

	/// <summary>What is wrong with the new name, or <see langword="null"/>. Set by the tab.</summary>
	public string? Error
	{
		get => _error;
		set
		{
			if (SetProperty(ref _error, value))
			{
				OnPropertyChanged(nameof(HasError));
			}
		}
	}

	/// <summary>Whether <see cref="Error"/> is set.</summary>
	public bool HasError => Error is not null;
}
