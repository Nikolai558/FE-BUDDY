using FEBuddyLibrary.Models.Services.General;

namespace FEBuddyLibrary.Models.Services.Airac.Airways;

/// <summary>
/// The result of running the top-level Airways service (<c>AirwayService.Run</c>): what was
/// built and written, plus timing and warnings.
/// </summary>
public sealed record AirwayServiceResult : ServiceResult
{
	/// <summary>How many airways were successfully built (and survived ROI clipping/buffering, if configured).</summary>
	public required int AirwayCount { get; init; }

	/// <summary>Full paths of every GeoJSON file written.</summary>
	public required IReadOnlyList<string> GeojsonFilesWritten { get; init; }

	/// <summary>Rendered Feature count for each path in <see cref="GeojsonFilesWritten"/>.</summary>
	public required IReadOnlyDictionary<string, int> GeojsonFeatureCountsByFile { get; init; }

	/// <summary>The alias file's path, or <see langword="null"/> when it was not generated or had nothing to write.</summary>
	public string? AliasFilePath { get; init; }

	/// <summary>How many airway lines were written to the alias file.</summary>
	public int AliasAirwayLineCount { get; init; }

	/// <summary>
	/// IDs of airways excluded from every output (GeoJSON, alias, counts) because at least one
	/// of their waypoints could not be resolved (remediation plan 3.2a).
	/// </summary>
	public IReadOnlyList<string> ExcludedAirwayIds { get; init; } = Array.Empty<string>();
}
