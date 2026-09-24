using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Departures.Models;

namespace FeBuddy.Core.Application.Airac.Departures.Models;

/// <summary>The outcome of parsing the raw Departures settings dictionary.</summary>
/// <param name="Settings">The typed, validated settings.</param>
/// <param name="Messages">Non-fatal parsing messages (e.g. unrecognized keys that were ignored).</param>
public sealed record DepartureSettingsParseResult(DepartureSettings Settings, IReadOnlyList<ServiceMessage> Messages);

/// <summary>The outcome of reading every procedure out of the DP tables.</summary>
/// <param name="Procedures">Every procedure, ordered by ARTCC then name.</param>
/// <param name="Messages">Messages collected while reading.</param>
public sealed record DepartureProcedureReadResult(IReadOnlyList<DepartureProcedure> Procedures, IReadOnlyList<ServiceMessage> Messages);

/// <summary>The outcome of splitting procedures per airport and locating every point.</summary>
/// <param name="AirportProcedures">Every airport + procedure whose points were all found, ordered by airport then code.</param>
/// <param name="SkippedCount">How many airport + procedure pairs were left out because a point could not be found.</param>
/// <param name="Messages">Messages collected while locating.</param>
public sealed record DepartureLocateResult(
	IReadOnlyList<DepartureAirportProcedure> AirportProcedures,
	int SkippedCount,
	IReadOnlyList<ServiceMessage> Messages);

/// <summary>The outcome of generating the Departures alias file.</summary>
/// <param name="FilePath">The file written, or <see langword="null"/> when there was nothing to write.</param>
/// <param name="CommandCount">How many alias commands were written (one per airport + procedure).</param>
/// <param name="Messages">Messages collected while generating.</param>
public sealed record DepartureAliasGenerateResult(
	string? FilePath,
	int CommandCount,
	IReadOnlyList<ServiceMessage> Messages);

/// <summary>
/// The result of running the top-level Departures sub-service (<c>DepartureService.Run</c>):
/// what was built and written, plus timing and every message collected along the way.
/// </summary>
public sealed record DepartureServiceResult : ServiceResult
{
	/// <summary>How many procedures were in <c>DP_BASE</c>, before any filtering.</summary>
	public required int ProcedureCount { get; init; }

	/// <summary>How many procedures passed the procedure-level filters (type, ARTCC, amendment date).</summary>
	public required int ProceduresInScopeCount { get; init; }

	/// <summary>How many airport + procedure pairs were output (after the ROI filter).</summary>
	public required int AirportProcedureCount { get; init; }

	/// <summary>How many airport + procedure pairs were left out because one of their points could not be found.</summary>
	public required int SkippedForMissingPointsCount { get; init; }

	/// <summary>Full paths of every GeoJSON file written.</summary>
	public required IReadOnlyList<string> GeojsonFilesWritten { get; init; }

	/// <summary>Rendered Feature count for each path in <see cref="GeojsonFilesWritten"/>.</summary>
	public required IReadOnlyDictionary<string, int> GeojsonFeatureCountsByFile { get; init; }

	/// <summary>The alias file's path, or <see langword="null"/> when it was not generated or had nothing to write.</summary>
	public string? AliasFilePath { get; init; }

	/// <summary>How many alias commands were written.</summary>
	public int AliasCommandCount { get; init; }
}
