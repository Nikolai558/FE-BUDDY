namespace FeBuddy.Core.Domain.Procedures.Models;

/// <summary>
/// One procedure at one airport - one d-TPP Metafile chart series (its main page plus any
/// continuation pages folded together), as <c>ProcedureBuilder</c> assembles it.
/// </summary>
public sealed record Procedure
{
	/// <summary>The base chart name, with any trailing <c>, CONT.n</c> removed, e.g. <c>ILS OR LOC RWY 28C</c>.</summary>
	public required string Name { get; init; }

	/// <summary>The d-TPP Metafile chart code, e.g. <c>IAP</c>, <c>STR</c>, <c>DP</c>.</summary>
	public required string ChartCode { get; init; }

	/// <summary>The chart sequence number of the procedure's first page - what orders it within the airport.</summary>
	public required int ChartSeq { get; init; }

	/// <summary>Every page (main and continuation), in file order.</summary>
	public required IReadOnlyList<ProcedurePage> Pages { get; init; }

	/// <summary>How the procedure changed this cycle.</summary>
	public required ProcedureChange Change { get; init; }

	/// <summary>
	/// The name of the page the change is reported for: usually <see cref="Name"/>, but a
	/// continuation page's full name (e.g. <c>THRNE FOUR (RNAV), CONT.1</c>) when only that page
	/// carries the <c>useraction</c>.
	/// </summary>
	public required string ReportedName { get; init; }

	/// <summary>The PDF name of the page named by <see cref="ReportedName"/>.</summary>
	public required string ReportedPdfName { get; init; }

	/// <summary>
	/// The main page's current PDF name, or <see langword="null"/> when <see cref="Change"/> is
	/// <see cref="ProcedureChange.Deleted"/> (there is no current chart).
	/// </summary>
	public string? CurrentPdfName { get; init; }

	/// <summary>
	/// The chart's PDF name in the previous cycle's metafile, for <see cref="ProcedureChange.Deleted"/>
	/// and <see cref="ProcedureChange.ReAdded"/> only; <see langword="null"/> when it could not be
	/// found there (or the previous metafile was not available).
	/// </summary>
	public string? PreviousPdfName { get; init; }

	/// <summary>
	/// Whether a <see cref="ProcedureChange.Deleted"/> record's PDF placeholder is
	/// <c>DEL_APT_SERVED.PDF</c> - the FAA removed the chart from this airport's served list, rather
	/// than deleting the chart itself.
	/// </summary>
	public bool IsRemovedFromAirportOnly { get; init; }

	/// <summary>The FAA database's procedure identifier (<c>procuid</c>), or <see langword="null"/> when it has none.</summary>
	public int? ProcUid { get; init; }

	/// <summary>The amendment number (<c>amdtnum</c>), FAA IAPs only.</summary>
	public string? AmdtNum { get; init; }

	/// <summary>The amendment date (<c>amdtdate</c>), parsed from its <c>MM/DD/YYYY</c> text.</summary>
	public DateOnly? AmdtDate { get; init; }

	/// <summary>The SID/STAR computer code (<c>faanfd18</c>), or <see langword="null"/> for anything else.</summary>
	public string? ComputerCode { get; init; }

	/// <summary>The civil/military production code (<c>civil</c>): <c>C</c>, <c>D</c>, <c>N</c> or <c>H</c>.</summary>
	public string? Civil { get; init; }
}
