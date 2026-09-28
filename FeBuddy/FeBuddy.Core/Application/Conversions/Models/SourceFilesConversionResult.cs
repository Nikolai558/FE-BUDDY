namespace FeBuddy.Core.Application.Conversions.Models;

/// <summary>
/// The result of a conversion that writes several GeoJSON files per source file (SCT2 to
/// GeoJSON, ERAM to GeoJSON): what happened to each file, plus timing and every message
/// collected along the way.
/// </summary>
public sealed record SourceFilesConversionResult : ConversionServiceResult
{
	/// <summary>One entry per source file, in the order they were converted.</summary>
	public required IReadOnlyList<SourceFileConversion> Files { get; init; }

	/// <inheritdoc />
	public override int SourceFileCount => Files.Count;

	/// <inheritdoc />
	public override IReadOnlyList<string> GeojsonFilesWritten => [.. Files.SelectMany(f => f.OutputPaths)];

	/// <inheritdoc />
	public override int FailedCount => Files.Count(f => f.Error is not null);

	/// <summary>How many rendered Features were written, across every file.</summary>
	public int FeaturesWritten => Files.Sum(f => f.FeaturesWritten);
}
