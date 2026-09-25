namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>One picked input file in a conversion's file list: its name, with the full path behind it.</summary>
/// <param name="Path">The file's full path.</param>
public sealed record SourceFileItem(string Path)
{
	/// <summary>The file name the list shows; the full path is its tool-tip.</summary>
	public string Name => System.IO.Path.GetFileName(Path);
}
