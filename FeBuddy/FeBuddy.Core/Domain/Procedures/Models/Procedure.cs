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

	/// <summary>
	/// Every page (main and continuation), in page order: the main page, then <c>CONT.1</c>,
	/// <c>CONT.2</c>... A re-added procedure lists a page twice, once deleted and once added. A
	/// continuation page whose own main page is gone this cycle joins the procedure with its
	/// <c>procuid</c>, under its own name (e.g. <c>MINNEAPOLIS NINE, CONT.1</c> in
	/// <c>MINNEAPOLIS ONE</c>).
	/// </summary>
	public required IReadOnlyList<ProcedurePage> Pages { get; init; }

	/// <summary>How the procedure changed this cycle.</summary>
	public required ProcedureChange Change { get; init; }

	/// <summary>
	/// The main page's current PDF name, or <see langword="null"/> when <see cref="Change"/> is
	/// <see cref="ProcedureChange.Deleted"/> (there is no current chart). A procedure with no main
	/// page this cycle has its first page's when it is unchanged, and none when it changed.
	/// </summary>
	public string? CurrentPdfName { get; init; }

	/// <summary>
	/// The main page's PDF name in the previous cycle's metafile, for <see cref="ProcedureChange.Deleted"/>
	/// and <see cref="ProcedureChange.ReAdded"/> only; <see langword="null"/> when it could not be
	/// found there (or the previous metafile was not available).
	/// </summary>
	public string? PreviousPdfName { get; init; }

	/// <summary>
	/// The procedure's pages in the previous cycle's metafile, at most one per page number, in page
	/// order - found for a procedure with a deleted page (every <see cref="ProcedureChange.Deleted"/>
	/// and <see cref="ProcedureChange.ReAdded"/> one, and a changed one that lost a page). Empty
	/// otherwise, or when the previous metafile was not available.
	/// </summary>
	public IReadOnlyList<ProcedurePage> PreviousPages { get; init; } = [];

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

	/// <summary>
	/// Set by <c>ProcedureSelection</c> only when the procedure is included because its route has a
	/// fix in the user's facility and for no other reason - not the area, not a pick - so the
	/// documents say why it's there. <see langword="null"/> otherwise.
	/// </summary>
	public FacilityFixMatch? IncludedForFacilityFixes { get; init; }
}
