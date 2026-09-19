using FeBuddy.Core.Models.Services.Airac.Airways;
using FeBuddy.Core.Models.Services.General;

namespace FeBuddy.Core.Models.Services.Airac;

/// <summary>
/// The aggregated result of one <c>AiracService.RunAsync</c> call: each selected sub-service's
/// own result, a combined warning list, and the cross-sub-service excluded-airway summary.
/// </summary>
public sealed record AiracServiceResult : ServiceResult
{
	/// <summary>
	/// The Airways sub-service result, or <see langword="null"/> when Airways was not part of
	/// this run.
	/// </summary>
	public AirwayServiceResult? Airways { get; init; }

	/// <summary>
	/// IDs of airways excluded from all output because they had an unresolvable waypoint
	/// (Phase 3.2). Empty until that behaviour lands.
	/// </summary>
	public IReadOnlyList<string> ExcludedAirwayIds { get; init; } = Array.Empty<string>();
}
