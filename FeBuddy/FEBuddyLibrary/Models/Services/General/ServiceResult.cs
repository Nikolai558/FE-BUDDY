namespace FEBuddyLibrary.Models.Services.General;

/// <summary>
/// Common result shape shared by every FE-Buddy service's top-level entry point.
/// </summary>
/// <remarks>
/// Individual services (e.g. <c>AirwayServiceResult</c>) derive from this record and add
/// service-specific fields such as file paths written and per-item counts.
///
/// <para>
/// <see cref="Warnings"/> is always the complete list, regardless of
/// <see cref="Configuration.DevMode"/>. A service that skips a bad record and continues
/// (see the Airways service's mid-airway unresolved-waypoint handling) must never let that
/// problem go unreported just because developer mode is off — silently dropping warnings
/// would defeat the purpose of collecting them in the first place. Trimming detail for
/// display (e.g. summarizing many similar warnings) is a presentation concern and belongs in
/// the caller (<c>FEBuddyTest</c>'s <c>ConsoleReport</c> today, the GUI later), not in the
/// library.
/// </para>
/// </remarks>
public abstract record ServiceResult
{
	/// <summary>
	/// Non-fatal problems encountered while the service ran (e.g. an airway with an
	/// unresolvable waypoint that was skipped rather than aborting the whole run). The
	/// service still completed; these are things the caller should surface to the user.
	/// </summary>
	public required IReadOnlyList<string> Warnings { get; init; }

	/// <summary>Total wall-clock time the service took to run.</summary>
	public required TimeSpan Elapsed { get; init; }
}
