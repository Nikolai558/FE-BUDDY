using FeBuddy.Core.Infrastructure.SharedData.Models;

namespace FeBuddy.Core.Infrastructure.Telephony.Models;

/// <summary>
/// What happened when FE-Buddy refreshed the two parts of the virtual airline list (see
/// <see cref="TelephonyDownloader.RefreshVirtualAirlineListAsync"/>): each part's copy to use, and - only
/// while there is no copy of either part - the older single copy beta.4 and earlier kept.
/// </summary>
/// <param name="Gng">GNG's fictional airlines.</param>
/// <param name="VatsimRadar">VATSIM-Radar's own list, from GitHub.</param>
/// <param name="OlderCopyPath">
/// The older single copy (<see cref="TelephonyFiles.OlderVatsimRadarAirlinesFileName"/>), to use instead
/// of both parts; <see langword="null"/> when either part has a copy, or there is no older copy.
/// </param>
/// <param name="OlderCopyDownloadedUtc">When the older copy was downloaded; <see langword="null"/> without one.</param>
public sealed record VirtualAirlineListRefreshResult(
	SharedDataRefreshResult Gng,
	SharedDataRefreshResult VatsimRadar,
	string? OlderCopyPath = null,
	DateTime? OlderCopyDownloadedUtc = null)
{
	/// <summary>Whether there is anything to read the list from: a copy of either part, or the older copy.</summary>
	public bool HasCopy => Gng.HasCopy || VatsimRadar.HasCopy || OlderCopyPath is not null;
}
