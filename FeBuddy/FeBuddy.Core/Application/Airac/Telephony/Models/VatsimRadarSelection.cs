namespace FeBuddy.Core.Application.Airac.Telephony.Models;

/// <summary>What Telephony writes from the virtual airline list, and what it leaves out.</summary>
/// <param name="VirtualAirlines">The virtual airlines that can be written, each once, sorted by 3LD, telephony and virtual organization.</param>
/// <param name="LeftOut">The 3LDs of those that cannot (a 3LD that isn't three letters, say), in the same order.</param>
public sealed record VatsimRadarSelection(IReadOnlyList<VirtualAirline> VirtualAirlines, IReadOnlyList<string> LeftOut);

/// <summary>FE-Buddy's kept copies of the virtual airline list, as the Telephony tab reads them.</summary>
/// <param name="VirtualAirlines">What a run would write from them.</param>
/// <param name="DownloadedUtc">When the older of the copies read was downloaded.</param>
public sealed record VatsimRadarCopy(IReadOnlyList<VirtualAirline> VirtualAirlines, DateTime DownloadedUtc)
{
	/// <summary>When GNG's part was downloaded; <see langword="null"/> when FE-Buddy has no copy of it it can read.</summary>
	public DateTime? GngDownloadedUtc { get; init; }

	/// <summary>When VATSIM-Radar's part was downloaded; <see langword="null"/> when FE-Buddy has no copy of it it can read.</summary>
	public DateTime? VatsimRadarDownloadedUtc { get; init; }

	/// <summary>Whether this is the older single copy beta.4 and earlier kept, read while neither part has a copy.</summary>
	public bool IsOlderCopy { get; init; }
}
