namespace FeBuddy.Core.Models.Services.Airac.Airports;

/// <summary>
/// The CRC ERAM property-default classes the Airports sub-service uses.
/// </summary>
/// <remarks>
/// Airways keys its defaults by altitude class; Airports has no equivalent, so it keys them by
/// the kind of thing being drawn instead. The settings-key shape is unchanged
/// (<c>Crc.&lt;Class&gt;.&lt;Kind&gt;.&lt;property&gt;</c>), which keeps one CRC property
/// contract across every sub-service.
/// </remarks>
public enum AirportCrcClass
{
	/// <summary>The airport points - the Symbols and Text files.</summary>
	Airports = 0,

	/// <summary>The runway centrelines - the Lines file.</summary>
	Runways = 1,
}
