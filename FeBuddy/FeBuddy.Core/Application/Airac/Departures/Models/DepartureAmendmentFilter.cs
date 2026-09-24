namespace FeBuddy.Core.Application.Airac.Departures.Models;

/// <summary>
/// How the amendment-date filter decides which procedures are recent enough to keep. Every mode
/// is a lower bound only: an amendment that becomes effective after the cutoff - including one
/// dated in the future - is always kept.
/// </summary>
public enum DepartureAmendmentFilter
{
	/// <summary>No amendment-date filter: every procedure is kept, whatever its amendment date.</summary>
	None = 0,

	/// <summary>
	/// Keep procedures whose current amendment became effective within the last
	/// <see cref="DepartureSettings.AmendedWithinCycles"/> cycles, counting the cycle the data
	/// came from as the first. Cycles are counted back from each procedure's own cycle date in
	/// 28-day steps.
	/// </summary>
	Cycles = 1,

	/// <summary>
	/// Keep procedures whose current amendment became effective on or after today minus
	/// <see cref="DepartureSettings.AmendedWithinDays"/> days, where today is the local date of
	/// the run.
	/// </summary>
	Days = 2,

	/// <summary>
	/// Keep procedures whose current amendment became effective on or after
	/// <see cref="DepartureSettings.AmendedOnOrAfter"/>.
	/// </summary>
	Date = 3,
}
