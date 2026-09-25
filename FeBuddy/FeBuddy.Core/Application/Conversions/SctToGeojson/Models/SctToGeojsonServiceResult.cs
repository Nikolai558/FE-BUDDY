using FeBuddy.Core.Application.Conversions.Models;

namespace FeBuddy.Core.Application.Conversions.SctToGeojson.Models;

/// <summary>
/// The result of an SCT2 to GeoJSON run (<c>SctToGeojsonService.Run</c>): what happened to each
/// file, plus timing and every message collected along the way.
/// </summary>
public sealed record SctToGeojsonServiceResult : ConversionServiceResult
{
	/// <summary>One entry per sector file, in the order they were converted.</summary>
	public required IReadOnlyList<SctFileConversion> Files { get; init; }

	/// <inheritdoc />
	public override int SourceFileCount => Files.Count;

	/// <inheritdoc />
	public override IReadOnlyList<string> GeojsonFilesWritten => [.. Files.SelectMany(f => f.OutputPaths)];

	/// <inheritdoc />
	public override int FailedCount => Files.Count(f => f.Error is not null);
}
