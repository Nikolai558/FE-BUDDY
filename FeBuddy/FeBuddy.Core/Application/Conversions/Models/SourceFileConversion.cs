namespace FeBuddy.Core.Application.Conversions.Models;

/// <summary>
/// What happened to one source file in a conversion that writes several GeoJSON files per source
/// (SCT2 to GeoJSON, ERAM to GeoJSON).
/// </summary>
public sealed record SourceFileConversion
{
	/// <summary>The source file that was read.</summary>
	public required string SourcePath { get; init; }

	/// <summary>Every GeoJSON file written for it; empty when it could not be read or had nothing to draw.</summary>
	public IReadOnlyList<string> OutputPaths { get; init; } = [];

	/// <summary>How many rendered Features were written, across its files.</summary>
	public int FeaturesWritten { get; init; }

	/// <summary>How many of the file's records could not be used and were skipped.</summary>
	public int RecordsSkipped { get; init; }

	/// <summary>Why the file was not converted, or <see langword="null"/> when it was.</summary>
	public string? Error { get; init; }
}
