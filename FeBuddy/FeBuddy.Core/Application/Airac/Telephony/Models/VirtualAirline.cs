namespace FeBuddy.Core.Application.Airac.Telephony.Models;

/// <summary>
/// A virtual airline - one the user listed on the Telephony tab, or one from the virtual airline
/// list - merged into <c>Telephony.txt</c> as a card of its own, marked
/// <c>--VA--</c>. Values are trimmed; the card prints them upper case.
/// </summary>
/// <param name="Designator">Its three-letter designator, e.g. <c>DVA</c>.</param>
/// <param name="Telephony">Its telephony, e.g. <c>DELTA</c>.</param>
/// <param name="Organization">Its virtual organization, e.g. <c>Delta Virtual</c>.</param>
public sealed record VirtualAirline(string Designator, string Telephony, string Organization)
{
	/// <summary>
	/// Whether this is the same virtual airline as the one given - the same 3LD, telephony and
	/// virtual organization - compared as the card prints them: trimmed and upper case, so a change
	/// of case alone is still the same airline.
	/// </summary>
	/// <param name="designator">The other's 3LD.</param>
	/// <param name="telephony">The other's telephony.</param>
	/// <param name="organization">The other's virtual organization.</param>
	/// <returns><see langword="true"/> when all three match.</returns>
	public bool IsSameAs(string designator, string telephony, string organization) =>
		HasSameCallSign(designator, telephony) && Same(Organization, organization);

	/// <summary>
	/// Whether this has the same 3LD and telephony as the ones given, whatever the virtual organization -
	/// trimmed and ignoring case. <c>Telephony.txt</c> writes only the first operator with each.
	/// </summary>
	/// <param name="designator">The other's 3LD.</param>
	/// <param name="telephony">The other's telephony.</param>
	/// <returns><see langword="true"/> when both match.</returns>
	public bool HasSameCallSign(string designator, string telephony) =>
		Same(Designator, designator) && Same(Telephony, telephony);

	private static bool Same(string mine, string theirs) => string.Equals(mine.Trim(), theirs.Trim(), StringComparison.OrdinalIgnoreCase);
}
