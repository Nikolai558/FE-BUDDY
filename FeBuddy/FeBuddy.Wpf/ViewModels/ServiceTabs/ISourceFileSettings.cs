using System.Collections.ObjectModel;
using System.Windows.Input;

using FeBuddy.Wpf.ViewModels.Models;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// The Source Files card (<c>Views/Cards/SourceFilesCard</c>): a conversion's input, either
/// every matching file in a folder or files picked one or several at a time.
/// </summary>
public interface ISourceFileSettings
{
	/// <summary>The kind of file, as the card words it, e.g. <c>.dat</c> or <c>.sct2 / .sct</c>.</summary>
	string FileTypeLabel { get; }

	/// <summary>Whether every matching file in <see cref="SourceFolder"/> is converted.</summary>
	bool SourceIsFolder { get; set; }

	/// <summary>Whether the files in <see cref="SourceFiles"/> are converted.</summary>
	bool SourceIsFiles { get; set; }

	/// <summary>The folder to convert. Saved.</summary>
	string SourceFolder { get; set; }

	/// <summary>What is in <see cref="SourceFolder"/>, e.g. <c>3 .dat files in this folder.</c></summary>
	string FolderSummary { get; }

	/// <summary>The picked files. Not saved.</summary>
	ObservableCollection<SourceFileItem> SourceFiles { get; }

	/// <summary>Picks <see cref="SourceFolder"/> with a folder dialog.</summary>
	ICommand BrowseFolderCommand { get; }

	/// <summary>Adds one or more files to <see cref="SourceFiles"/> with a file dialog.</summary>
	ICommand BrowseFilesCommand { get; }

	/// <summary>Empties <see cref="SourceFiles"/>.</summary>
	ICommand ClearFilesCommand { get; }

	/// <summary>Takes one file off <see cref="SourceFiles"/>; the command parameter is the <see cref="SourceFileItem"/>.</summary>
	ICommand RemoveFileCommand { get; }
}
