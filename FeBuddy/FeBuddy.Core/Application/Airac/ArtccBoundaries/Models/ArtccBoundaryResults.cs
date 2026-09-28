using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.ArtccBoundaries.Models;

namespace FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;

/// <summary>The outcome of parsing the raw ARTCC Boundaries settings dictionary.</summary>
/// <param name="Settings">The typed, validated settings.</param>
/// <param name="Messages">Non-fatal parsing messages (e.g. unrecognized keys that were ignored).</param>
public sealed record ArtccBoundarySettingsParseResult(ArtccBoundarySettings Settings, IReadOnlyList<ServiceMessage> Messages);

/// <summary>The outcome of reading every ARTCC boundary ring from the parsed NASR data.</summary>
/// <param name="Rings">
/// Every ring built, ordered by LocationId, then altitude (High, Low, Unlimited), then ring order.
/// </param>
/// <param name="Messages">
/// Messages collected while reading - e.g. an unrecognized <c>ALTITUDE</c> value, a LocationId with
/// no <c>ARB_BASE</c> row, or a ring skipped for having fewer than two distinct points.
/// </param>
public sealed record ArtccBoundaryBuildAllResult(IReadOnlyList<ArtccBoundaryRing> Rings, IReadOnlyList<ServiceMessage> Messages);

/// <summary>
/// The result of running the top-level ARTCC Boundaries sub-service
/// (<c>ArtccBoundaryService.Run</c>): what was built and written, plus timing and every message
/// collected along the way.
/// </summary>
public sealed record ArtccBoundaryServiceResult : ServiceResult
{
	/// <summary>How many locations (LocationIds) are in scope, after <c>LocationFilter</c>.</summary>
	public required int LocationCount { get; init; }

	/// <summary>How many boundary-ring Features were written across every GeoJSON file.</summary>
	public required int RingCount { get; init; }

	/// <summary>Full paths of every GeoJSON file written.</summary>
	public required IReadOnlyList<string> GeojsonFilesWritten { get; init; }

	/// <summary>Rendered Feature count for each path in <see cref="GeojsonFilesWritten"/>.</summary>
	public required IReadOnlyDictionary<string, int> GeojsonFeatureCountsByFile { get; init; }
}
