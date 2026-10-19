using FeBuddy.Core.Infrastructure.SharedData.Models;

namespace FeBuddy.Core.Infrastructure.Telephony.Models;

/// <summary>
/// What happened when FE-Buddy refreshed the two parts of the virtual airline list (see
/// <see cref="TelephonyDownloader.RefreshVirtualAirlineListAsync"/>): each part's copy to use.
/// </summary>
/// <param name="Gng">GNG's fictional airlines.</param>
/// <param name="VatsimRadar">VATSIM-Radar's own list, from GitHub.</param>
public sealed record VirtualAirlineListRefreshResult(SharedDataRefreshResult Gng, SharedDataRefreshResult VatsimRadar)
{
	/// <summary>Whether there is anything to read the list from: a copy of either part.</summary>
	public bool HasCopy => Gng.HasCopy || VatsimRadar.HasCopy;
}
