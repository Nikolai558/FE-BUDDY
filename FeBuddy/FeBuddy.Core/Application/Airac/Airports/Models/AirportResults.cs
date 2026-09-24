using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Airports.Models;

namespace FeBuddy.Core.Application.Airac.Airports.Models;

/// <summary>The outcome of parsing the raw Airports settings dictionary.</summary>
/// <param name="Settings">The typed, validated settings.</param>
/// <param name="Messages">Non-fatal parsing messages (e.g. unrecognized keys that were ignored).</param>
public sealed record AirportSettingsParseResult(AirportSettings Settings, IReadOnlyList<ServiceMessage> Messages);

/// <summary>The outcome of building every airport from the parsed NASR data.</summary>
/// <param name="Airports">The built airports, ordered by FAA identifier.</param>
/// <param name="Messages">Messages collected while building (e.g. runways with no usable coordinates).</param>
public sealed record AirportBuildAllResult(IReadOnlyList<Airport> Airports, IReadOnlyList<ServiceMessage> Messages);

/// <summary>The outcome of generating the Airports GeoJSON files.</summary>
/// <param name="FilesWritten">Full paths of every file written.</param>
/// <param name="RenderedFeatureCountsByFile">Rendered Feature count for each written path.</param>
/// <param name="Messages">Messages collected while generating.</param>
public sealed record AirportGeojsonGenerateResult(
	IReadOnlyList<string> FilesWritten,
	IReadOnlyDictionary<string, int> RenderedFeatureCountsByFile,
	IReadOnlyList<ServiceMessage> Messages);

/// <summary>The outcome of generating the Airports alias file.</summary>
/// <param name="FilePath">The file written, or <see langword="null"/> when there was nothing to write.</param>
/// <param name="CommandCount">How many alias commands were written (an airport with an ICAO ID contributes two).</param>
/// <param name="Messages">Messages collected while generating.</param>
public sealed record AirportAliasGenerateResult(
	string? FilePath,
	int CommandCount,
	IReadOnlyList<ServiceMessage> Messages);

/// <summary>
/// The result of running the top-level Airports sub-service (<c>AirportService.Run</c>): what
/// was built and written, plus timing and every message collected along the way.
/// </summary>
public sealed record AirportServiceResult : ServiceResult
{
	/// <summary>How many airports were built (before ROI filtering, which applies to GeoJSON only).</summary>
	public required int AirportCount { get; init; }

	/// <summary>How many of those airports fell inside the ROI, and so appear in the GeoJSON output.</summary>
	public required int AirportsInRoiCount { get; init; }

	/// <summary>Full paths of every GeoJSON file written.</summary>
	public required IReadOnlyList<string> GeojsonFilesWritten { get; init; }

	/// <summary>Rendered Feature count for each path in <see cref="GeojsonFilesWritten"/>.</summary>
	public required IReadOnlyDictionary<string, int> GeojsonFeatureCountsByFile { get; init; }

	/// <summary>The alias file's path, or <see langword="null"/> when it was not generated or had nothing to write.</summary>
	public string? AliasFilePath { get; init; }

	/// <summary>How many alias commands were written.</summary>
	public int AliasCommandCount { get; init; }
}
