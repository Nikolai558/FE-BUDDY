using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Navaids.Models;
using FeBuddy.Core.Infrastructure.Geojson;

namespace FeBuddy.Core.Application.Airac.Navaids.Models;

/// <summary>The outcome of parsing the raw NAVAIDs settings dictionary.</summary>
/// <param name="Settings">The typed, validated settings.</param>
/// <param name="Messages">Non-fatal parsing messages (e.g. unrecognized keys that were ignored).</param>
public sealed record NavaidSettingsParseResult(NavaidSettings Settings, IReadOnlyList<ServiceMessage> Messages);

/// <summary>The outcome of building every NAVAID from the parsed NASR data.</summary>
/// <param name="Navaids">
/// The built NAVAIDs, ordered by identifier, then type, then name (ignoring case), then latitude
/// and longitude.
/// </param>
/// <param name="Messages">Messages collected while building (skipped SHUTDOWN/blank-ID records, unrecognized types).</param>
public sealed record NavaidBuildAllResult(IReadOnlyList<Navaid> Navaids, IReadOnlyList<ServiceMessage> Messages);

/// <summary>The outcome of generating the NAVAIDs GeoJSON output.</summary>
/// <param name="Files">The files written and how many rendered Features each holds.</param>
/// <param name="Messages">
/// Messages collected while generating - e.g. one warning per NAVAID type CRC has no mapped
/// symbol style for, when a merged Symbols file needs one per Feature.
/// </param>
public sealed record NavaidGeojsonGenerateResult(GeojsonFileSet Files, IReadOnlyList<ServiceMessage> Messages);

/// <summary>The outcome of generating the NAVAIDs alias file.</summary>
/// <param name="FilePath">The file written, or <see langword="null"/> when there was nothing to write.</param>
/// <param name="CommandCount">How many alias commands (lines) were written.</param>
/// <param name="Messages">Messages collected while generating.</param>
public sealed record NavaidAliasGenerateResult(
	string? FilePath,
	int CommandCount,
	IReadOnlyList<ServiceMessage> Messages);

/// <summary>
/// The result of running the top-level NAVAIDs sub-service (<c>NavaidService.Run</c>): what was
/// built and written, plus timing and every message collected along the way.
/// </summary>
public sealed record NavaidServiceResult : ServiceResult
{
	/// <summary>
	/// How many NAVAIDs were included (after the NAV_STATUS/blank-ID checks in the builder and
	/// the <c>ExcludedTypes</c> filter) - before ROI filtering, which applies to GeoJSON only.
	/// </summary>
	public required int NavaidCount { get; init; }

	/// <summary>How many of those NAVAIDs fell inside the ROI, and so appear in the GeoJSON output.</summary>
	public required int GeojsonNavaidCount { get; init; }

	/// <summary>Full paths of every GeoJSON file written.</summary>
	public required IReadOnlyList<string> GeojsonFilesWritten { get; init; }

	/// <summary>Rendered Feature count for each path in <see cref="GeojsonFilesWritten"/>.</summary>
	public required IReadOnlyDictionary<string, int> GeojsonFeatureCountsByFile { get; init; }

	/// <summary>The alias file's path, or <see langword="null"/> when it was not generated or had nothing to write.</summary>
	public string? AliasFilePath { get; init; }

	/// <summary>How many alias commands were written.</summary>
	public int AliasCommandCount { get; init; }
}
