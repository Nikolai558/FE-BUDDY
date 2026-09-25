namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>Where the DAT to GeoJSON conversion takes its <c>.dat</c> files from.</summary>
public enum DatSourceType
{
	/// <summary>Every <c>.dat</c> file in one folder. The folder is saved.</summary>
	Folder,

	/// <summary>One or more files picked individually. The picks are not saved.</summary>
	Files,
}
