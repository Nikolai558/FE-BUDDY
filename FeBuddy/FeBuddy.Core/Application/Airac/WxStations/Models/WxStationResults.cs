using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.WxStations.Models;
using FeBuddy.Core.Infrastructure.Geojson;

namespace FeBuddy.Core.Application.Airac.WxStations.Models;

/// <summary>The outcome of parsing the raw Wx Stations settings dictionary.</summary>
/// <param name="Settings">The typed, validated settings.</param>
/// <param name="Messages">Non-fatal parsing messages (e.g. unrecognized keys that were ignored).</param>
public sealed record WxStationSettingsParseResult(WxStationSettings Settings, IReadOnlyList<ServiceMessage> Messages);

/// <summary>The outcome of building every included station from the parsed Wx station data.</summary>
/// <param name="Stations">Every included station, ordered by ICAO ID (ignoring case), stable.</param>
/// <param name="TotalStationCount">How many <c>&lt;Station&gt;</c> rows the source file listed, before any selection rule.</param>
/// <param name="Messages">Messages collected while building (e.g. METAR stations left out for unusable coordinates).</param>
public sealed record WxStationBuildResult(IReadOnlyList<WxStation> Stations, int TotalStationCount, IReadOnlyList<ServiceMessage> Messages);

/// <summary>The outcome of generating the Wx Stations GeoJSON output.</summary>
/// <param name="Files">The files written and how many rendered Features each holds.</param>
public sealed record WxStationGeojsonGenerateResult(GeojsonFileSet Files);

/// <summary>
/// The result of running the top-level Wx Stations sub-service (<c>WxStationService.Run</c>):
/// what was built and written, plus timing and every message collected along the way.
/// </summary>
public sealed record WxStationServiceResult : ServiceResult
{
	/// <summary>
	/// How many stations from the source file were included (before ROI filtering, which applies
	/// to the GeoJSON output).
	/// </summary>
	public required int StationCount { get; init; }

	/// <summary>How many <c>&lt;Station&gt;</c> rows the source file listed in total, before any selection rule.</summary>
	public required int TotalStationCount { get; init; }

	/// <summary>How many of the included stations fell inside the ROI, and so appear in the GeoJSON output.</summary>
	public required int GeojsonStationCount { get; init; }

	/// <summary>Full paths of every GeoJSON file written.</summary>
	public required IReadOnlyList<string> GeojsonFilesWritten { get; init; }

	/// <summary>Rendered Feature count for each path in <see cref="GeojsonFilesWritten"/>.</summary>
	public required IReadOnlyDictionary<string, int> GeojsonFeatureCountsByFile { get; init; }
}
