using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Fixes.Models;
using FeBuddy.Core.Infrastructure.Geojson;

namespace FeBuddy.Core.Application.Airac.Fixes.Models;

/// <summary>The outcome of parsing the raw Fixes settings dictionary.</summary>
/// <param name="Settings">The typed, validated settings.</param>
/// <param name="Messages">Non-fatal parsing messages (e.g. unrecognized keys that were ignored).</param>
public sealed record FixSettingsParseResult(FixSettings Settings, IReadOnlyList<ServiceMessage> Messages);

/// <summary>The outcome of building every fix from the parsed NASR data.</summary>
/// <param name="Fixes">Every fix built, ordered by identifier (ignoring case), stable.</param>
/// <param name="Messages">Messages collected while building (skipped blank-FIX_ID records).</param>
public sealed record FixBuildAllResult(IReadOnlyList<Fix> Fixes, IReadOnlyList<ServiceMessage> Messages);

/// <summary>The outcome of generating the Fixes GeoJSON output.</summary>
/// <param name="Files">The files written and how many rendered Features each holds.</param>
public sealed record FixGeojsonGenerateResult(GeojsonFileSet Files);

/// <summary>
/// The result of running the top-level Fixes sub-service (<c>FixService.Run</c>): what was built
/// and written, plus timing and every message collected along the way.
/// </summary>
public sealed record FixServiceResult : ServiceResult
{
	/// <summary>
	/// How many fixes were built from NASR data (before ROI filtering, which applies to the
	/// GeoJSON output).
	/// </summary>
	public required int FixCount { get; init; }

	/// <summary>How many of those fixes fell inside the ROI, and so appear in the GeoJSON output.</summary>
	public required int GeojsonFixCount { get; init; }

	/// <summary>Full paths of every GeoJSON file written.</summary>
	public required IReadOnlyList<string> GeojsonFilesWritten { get; init; }

	/// <summary>Rendered Feature count for each path in <see cref="GeojsonFilesWritten"/>.</summary>
	public required IReadOnlyDictionary<string, int> GeojsonFeatureCountsByFile { get; init; }
}
