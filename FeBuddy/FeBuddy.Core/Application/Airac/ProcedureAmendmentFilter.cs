namespace FeBuddy.Core.Application.Airac;

/// <summary>
/// The amendment filter Departures and Arrivals share - keep only the procedures amended in the last
/// so many cycles or days, or on or after a date: its limits and its date format, for the settings
/// parsers and the tabs that check the values as they are typed.
/// </summary>
public static class ProcedureAmendmentFilter
{
	/// <summary>The most cycles back <c>AmendedWithinCycles</c> can reach.</summary>
	public const int MaxWithinCycles = 1000;

	/// <summary>The most days back <c>AmendedWithinDays</c> can reach: about a hundred years.</summary>
	public const int MaxWithinDays = 36500;

	/// <summary>How <c>AmendedOnOrAfter</c> is written, e.g. <c>2026-01-01</c>.</summary>
	public const string DateFormat = "yyyy-MM-dd";
}
