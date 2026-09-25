namespace FeBuddy.Core.Application.Conversions.SctToGeojson.Models;

/// <summary>What happened to one sector file in an SCT2 to GeoJSON run.</summary>
public sealed record SctFileConversion
{
	/// <summary>The sector file that was read.</summary>
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
