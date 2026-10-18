namespace FeBuddy.Core.Application.Conversions.Models;

/// <summary>What <see cref="ConversionSourceFolder.Count"/> found in a conversion's source folder.</summary>
/// <param name="Exists">Whether the folder is there.</param>
/// <param name="Files">How many files the conversion would read; 0 when the folder isn't there or couldn't be read.</param>
/// <param name="Problem">Why the folder couldn't be read (no permission, a share that didn't answer), or <see langword="null"/>.</param>
public sealed record SourceFolderCount(bool Exists, int Files, string? Problem)
{
	/// <summary>The folder isn't there.</summary>
	public static SourceFolderCount Missing { get; } = new(false, 0, null);

	/// <summary>Whether the folder is there and its files were counted.</summary>
	public bool Counted => Exists && Problem is null;
}
