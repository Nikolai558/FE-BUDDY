using FeBuddy.Core.Application.Models;

namespace FeBuddy.Core.Application.Conversions.Models;

/// <summary>
/// What every file conversion's result can say about a run, whatever it converts: how many
/// source files it read, how many failed, and what it wrote where.
/// </summary>
/// <remarks>
/// Lets the GUI describe any conversion's run the same way; each conversion's own result adds
/// its per-file detail.
/// </remarks>
public abstract record ConversionServiceResult : ServiceResult
{
	/// <summary>The folder the converted files were written into.</summary>
	public required string OutputDirectory { get; init; }

	/// <summary>How many source files the run read.</summary>
	public abstract int SourceFileCount { get; }

	/// <summary>How many source files could not be converted.</summary>
	public abstract int FailedCount { get; }

	/// <summary>Full paths of every GeoJSON file written, across source files.</summary>
	public abstract IReadOnlyList<string> GeojsonFilesWritten { get; }
}
