using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Procedures.Models;

namespace FeBuddy.Core.Application.Airac.Procedures.Models;

/// <summary>The outcome of parsing the raw Procedures settings dictionary.</summary>
/// <param name="Settings">The typed, validated settings.</param>
/// <param name="Messages">Non-fatal parsing messages (e.g. unrecognized keys that were ignored).</param>
public sealed record ProcedureSettingsParseResult(ProcedureSettings Settings, IReadOnlyList<ServiceMessage> Messages);

/// <summary>The outcome of building every airport and procedure from the parsed d-TPP Metafile and NASR data.</summary>
/// <param name="Airports">Every metafile airport, in file order.</param>
/// <param name="Messages">Messages collected while building.</param>
public sealed record ProcedureBuildResult(IReadOnlyList<ProcedureAirport> Airports, IReadOnlyList<ServiceMessage> Messages);

/// <summary>The outcome of writing <c>Procedure_Changes.md</c>.</summary>
/// <param name="FilePath">The path written, or <see langword="null"/> when the document was not generated.</param>
/// <param name="Messages">Messages collected while writing.</param>
public sealed record ProcedureChangesWriteResult(string? FilePath, IReadOnlyList<ServiceMessage> Messages);

/// <summary>The outcome of writing <c>Procedures.json</c>.</summary>
/// <param name="FilePath">The path written, or <see langword="null"/> when the document was not generated.</param>
/// <param name="Messages">Messages collected while writing.</param>
public sealed record ProceduresJsonWriteResult(string? FilePath, IReadOnlyList<ServiceMessage> Messages);

/// <summary>
/// The result of running the top-level Procedures sub-service (<c>ProcedureService.Run</c>): what
/// was built and written, plus timing and every message collected along the way.
/// </summary>
/// <remarks>
/// The counts from <see cref="AirportCount"/> to <see cref="ReAddedCount"/> describe the two
/// documents' selection; the alias file covers every airport in the metafile and has its own counts.
/// </remarks>
public sealed record ProcedureServiceResult : ServiceResult
{
	/// <summary>How many airports were included (had at least one included procedure) after selection.</summary>
	public required int AirportCount { get; init; }

	/// <summary>How many procedures were included across every included airport.</summary>
	public required int ProcedureCount { get; init; }

	/// <summary>How many included procedures were newly added this cycle.</summary>
	public required int NewCount { get; init; }

	/// <summary>How many included procedures changed this cycle, including re-added ones.</summary>
	public required int ChangedCount { get; init; }

	/// <summary>How many included procedures were deleted this cycle.</summary>
	public required int DeletedCount { get; init; }

	/// <summary>How many included procedures were deleted and then re-added within this cycle's metafile.</summary>
	public required int ReAddedCount { get; init; }

	/// <summary>Full paths of every document written (<c>Procedure_Changes.md</c>, <c>Procedures.json</c>).</summary>
	public required IReadOnlyList<string> FilesWritten { get; init; }

	/// <summary>
	/// Full path of <c>FAA_CHART_RECALL.txt</c>, or <see langword="null"/> when it was not written (not
	/// requested, no metafile, or no command to write).
	/// </summary>
	public string? AliasFilePath { get; init; }

	/// <summary>How many commands <c>FAA_CHART_RECALL.txt</c> holds; 0 when it was not written.</summary>
	public int AliasCommandCount { get; init; }

	/// <summary>How many airports have at least one command in <c>FAA_CHART_RECALL.txt</c>; 0 when it was not written.</summary>
	public int AliasAirportCount { get; init; }
}
