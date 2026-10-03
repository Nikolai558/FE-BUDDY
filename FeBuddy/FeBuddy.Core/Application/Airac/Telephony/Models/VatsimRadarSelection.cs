namespace FeBuddy.Core.Application.Airac.Telephony.Models;

/// <summary>What Telephony writes from the VATSIM-Radar Virtual Airline List, and what it leaves out.</summary>
/// <param name="VirtualAirlines">The virtual airlines that can be written, each once, sorted by 3LD, telephony and virtual organization.</param>
/// <param name="LeftOut">The 3LDs of those that cannot (a 3LD that isn't three letters, say), in the same order.</param>
public sealed record VatsimRadarSelection(IReadOnlyList<VirtualAirline> VirtualAirlines, IReadOnlyList<string> LeftOut);

/// <summary>FE-Buddy's kept copy of the VATSIM-Radar Virtual Airline List, as the Telephony tab reads it.</summary>
/// <param name="VirtualAirlines">What a run would write from it.</param>
/// <param name="DownloadedUtc">When it was downloaded.</param>
public sealed record VatsimRadarCopy(IReadOnlyList<VirtualAirline> VirtualAirlines, DateTime DownloadedUtc);
