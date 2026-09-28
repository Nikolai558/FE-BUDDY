namespace FeBuddy.Core.Application.Conversions.Models;

/// <summary>
/// The settings every file conversion shares: where the source files are and where the output
/// goes. Each conversion's own settings record derives from this and adds what is its own.
/// Read by <c>ConversionSettingsReader</c>.
/// </summary>
public abstract record ConversionSettings
{
	/// <summary>Directory the converted files are written under.</summary>
	public required string OutputDirectory { get; init; }

	/// <summary>Whether to put the output inside a <c>FE-Buddy_Output</c> folder. Default <see langword="true"/>.</summary>
	public bool AddFeBuddyOutputFolder { get; init; } = true;

	/// <summary>Decimal places kept per coordinate, 0-15. Default 6.</summary>
	public int CoordinatePrecision { get; init; } = 6;

	/// <summary>
	/// A folder to convert every matching file in (not its sub-folders), or
	/// <see langword="null"/> when <see cref="SourceFiles"/> names the files instead.
	/// </summary>
	public string? SourceFolder { get; init; }

	/// <summary>The individual files to convert; empty when <see cref="SourceFolder"/> is used.</summary>
	public IReadOnlyList<string> SourceFiles { get; init; } = [];
}
