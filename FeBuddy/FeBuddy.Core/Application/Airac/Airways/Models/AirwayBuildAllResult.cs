using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Airways.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Airac.Airways.Models;

/// <summary>
/// The result of building every airway from parsed NASR data: the successfully-built airways,
/// every levelled message, and the IDs of airways excluded entirely.
/// </summary>
/// <param name="Airways">
/// Every airway that produced usable geometry (and, when an ROI was configured, survived
/// clipping to it).
/// </param>
/// <param name="Messages">Levelled messages noticed while building airways (remediation plan 3.8).</param>
/// <param name="ExcludedAirwayIds">
/// IDs of airways excluded from all output because at least one of their waypoints could not
/// be resolved (remediation plan 3.2a). Border crossings do not count - they are normalized
/// away before geometry building.
/// </param>
public sealed record AirwayBuildAllResult(
	IReadOnlyList<Airway> Airways,
	IReadOnlyList<ServiceMessage> Messages,
	IReadOnlyList<string> ExcludedAirwayIds)
{
	/// <summary>Backwards-compatible text-only view of the Warning/Error entries in <see cref="Messages"/>.</summary>
	public IReadOnlyList<string> Warnings =>
		Messages.Where(m => m.Level is LogLevel.Warning or LogLevel.Error).Select(m => m.Text).ToArray();
}
