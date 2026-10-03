namespace FeBuddy.Core.Application.Airac.Telephony.Models;

/// <summary>
/// A virtual airline the user listed on the Telephony tab, merged into <c>Telephony.txt</c> as a
/// card of its own, marked <c>--VA--</c>. Values are trimmed; the card prints them upper case.
/// </summary>
/// <param name="Designator">Its three-letter designator, e.g. <c>DVA</c>.</param>
/// <param name="Telephony">Its telephony, e.g. <c>DELTA</c>.</param>
/// <param name="Organization">Its virtual organization, e.g. <c>Delta Virtual</c>.</param>
public sealed record VirtualAirline(string Designator, string Telephony, string Organization);
