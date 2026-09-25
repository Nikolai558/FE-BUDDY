namespace FeBuddy.Core.Application.Airac.Arrivals.Models;

/// <summary>
/// The CRC ERAM property-default classes the Arrivals sub-service uses.
/// </summary>
/// <remarks>
/// Arrivals has a single class: every Lines, Symbols and Text file shares one set of defaults per
/// kind. It is still an enum so the settings-key shape
/// (<c>Crc.&lt;Class&gt;.&lt;Kind&gt;.&lt;property&gt;</c>) matches every other sub-service, and so a
/// later split is a new member rather than a new contract.
/// </remarks>
public enum ArrivalCrcClass
{
	/// <summary>Every arrival procedure - the Lines, Symbols and Text files.</summary>
	Arrivals = 0,
}
