namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>Where a file conversion takes its source files from.</summary>
public enum ConversionSourceType
{
	/// <summary>Every matching file in one folder. The folder is saved.</summary>
	Folder,

	/// <summary>One or more files picked individually. The picks are not saved.</summary>
	Files,
}
