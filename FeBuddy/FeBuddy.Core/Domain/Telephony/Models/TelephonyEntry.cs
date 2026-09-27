namespace FeBuddy.Core.Domain.Telephony.Models;

/// <summary>Where a <see cref="TelephonyEntry"/> comes from, which decides how its card is labelled.</summary>
public enum TelephonyEntryKind
{
	/// <summary>
	/// An ICAO assignment from the FAA register (JO 7340.2, Chapter 3, Section 1): a three-letter
	/// designator, its telephony, company and country.
	/// </summary>
	IcaoAssignment,

	/// <summary>
	/// A U.S. special call sign (JO 7340.2, Chapter 3, Section 4): an identifier, its telephony,
	/// operating agency and expiration date.
	/// </summary>
	UsSpecialCallSign,
}

/// <summary>
/// One operator the Telephony alias file has a card for: every text value upper case and trimmed,
/// ready to print.
/// </summary>
/// <param name="Kind">Where it comes from.</param>
/// <param name="Identifier">
/// The three-letter designator (<c>AVA</c>) for an ICAO assignment, or the identifier (<c>ARSIX</c>)
/// for a U.S. special call sign.
/// </param>
/// <param name="Telephony">The spoken call sign, e.g. <c>AVIANCA</c>, <c>AIR SIX</c>.</param>
/// <param name="Organization">The company (ICAO assignment) or operating agency (U.S. special call sign).</param>
/// <param name="Detail">The country (ICAO assignment) or expiration date as printed, e.g. <c>24-FEB-2027</c> or <c>N/A</c> (U.S. special call sign).</param>
public sealed record TelephonyEntry(TelephonyEntryKind Kind, string Identifier, string Telephony, string Organization, string Detail);
