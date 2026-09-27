namespace FeBuddy.Core.Domain.Procedures.Models;

/// <summary>
/// How a procedure changed between the previous d-TPP Metafile cycle and this one.
/// </summary>
public enum ProcedureChange
{
	/// <summary>No <c>useraction</c> applies to any of the procedure's pages this cycle.</summary>
	None,

	/// <summary>The procedure (or the page the change is reported for) was added this cycle (<c>useraction</c> "A").</summary>
	New,

	/// <summary>The procedure (or the page the change is reported for) was changed this cycle (<c>useraction</c> "C").</summary>
	Changed,

	/// <summary>The procedure (or the page the change is reported for) was deleted this cycle (<c>useraction</c> "D").</summary>
	Deleted,

	/// <summary>
	/// The procedure was deleted and then re-added within the same metafile: one record for the
	/// airport and base chart name is flagged "D" and another is flagged "A". Reported as a change.
	/// </summary>
	ReAdded,
}
