using FeBuddy.Core.Application.Conversions.Models;

namespace FeBuddy.Core.Application.Conversions.DatToGeojson.Models;

/// <summary>
/// The result of a DAT to GeoJSON run (<c>DatToGeojsonService.Run</c>): what happened to each
/// file, plus timing and every message collected along the way.
/// </summary>
public sealed record DatToGeojsonServiceResult : ConversionServiceResult
{
	/// <summary>One entry per <c>.dat</c> file, in the order they were converted.</summary>
	public required IReadOnlyList<DatFileConversion> Files { get; init; }

	/// <inheritdoc />
	public override int SourceFileCount => Files.Count;

	/// <inheritdoc />
	public override IReadOnlyList<string> GeojsonFilesWritten =>
		[.. Files.Select(f => f.OutputPath).OfType<string>()];

	/// <inheritdoc />
	public override int FailedCount => Files.Count(f => f.Error is not null);
}
