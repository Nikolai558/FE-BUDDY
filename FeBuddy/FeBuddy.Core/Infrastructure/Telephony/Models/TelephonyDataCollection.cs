namespace FeBuddy.Core.Infrastructure.Telephony.Models;

/// <summary>
/// The parsed FAA telephony pages: every row of the ICAO register (Chapter 3, Section 1) and of the
/// U.S. special call signs (Chapter 3, Section 4), as <see cref="Parsers.TelephonyHtmlParser"/>
/// returns them - and the VATSIM-Radar Virtual Airline List, when the user includes it.
/// </summary>
public class TelephonyDataCollection
{
	/// <summary>Every row of the ICAO register, in page order (by company).</summary>
	public List<TelephonyHtmlDataModel.Assignment> Assignments { get; set; } = [];

	/// <summary>
	/// Every U.S. special call sign, in page order; empty when FE-Buddy has no copy of the Section 4
	/// page - it is optional, and a run goes on without it.
	/// </summary>
	public List<TelephonyHtmlDataModel.SpecialCallSign> SpecialCallSigns { get; set; } = [];

	/// <summary>
	/// Every virtual airline on the VATSIM-Radar Virtual Airline List, in the list's order (see
	/// <see cref="Parsers.VatsimRadarAirlineParser"/>); empty when the user does not include it, or
	/// FE-Buddy has no copy of it.
	/// </summary>
	public List<VatsimRadarAirline> VatsimRadarAirlines { get; set; } = [];
}
