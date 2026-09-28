namespace FeBuddy.Core.Application.Airac.Departures.Models;

/// <summary>
/// The CRC ERAM property-default classes the Departures sub-service uses.
/// </summary>
/// <remarks>
/// Departures has a single class: every Lines, Symbols and Text file shares one set of
/// defaults per kind. It is still an enum so the settings-key shape
/// (<c>Crc.&lt;Class&gt;.&lt;Kind&gt;.&lt;property&gt;</c>) matches every other sub-service, and so a
/// later split (SIDs vs obstacle departures, say) is a new member rather than a new contract.
/// </remarks>
public enum DepartureCrcClass
{
	/// <summary>Every departure procedure - the Lines, Symbols and Text files.</summary>
	Departures = 0,
}
