using FeBuddy.Core.Application.Models;

namespace FeBuddy.Core.Application.Conversions.DatToGeojson.Models;

/// <summary>
/// The result of a DAT to GeoJSON run (<c>DatToGeojsonService.Run</c>): what happened to each
/// file, plus timing and every message collected along the way.
/// </summary>
public sealed record DatToGeojsonServiceResult : ServiceResult
{
	/// <summary>One entry per <c>.dat</c> file, in the order they were converted.</summary>
	public required IReadOnlyList<DatFileConversion> Files { get; init; }

	/// <summary>The folder the GeoJSON files were written into.</summary>
	public required string OutputDirectory { get; init; }

	/// <summary>Full paths of every GeoJSON file written.</summary>
	public IReadOnlyList<string> GeojsonFilesWritten =>
		[.. Files.Select(f => f.OutputPath).OfType<string>()];

	/// <summary>How many files could not be converted.</summary>
	public int FailedCount => Files.Count(f => f.Error is not null);
}
