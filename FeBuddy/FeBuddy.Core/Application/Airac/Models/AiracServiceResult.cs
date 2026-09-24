using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Application.Models;

namespace FeBuddy.Core.Application.Airac.Models;

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
	/// The Airports sub-service result, or <see langword="null"/> when Airports was not part of
	/// this run.
	/// </summary>
	public AirportServiceResult? Airports { get; init; }

	/// <summary>
	/// The Departures sub-service result, or <see langword="null"/> when Departures was not part
	/// of this run.
	/// </summary>
	public DepartureServiceResult? Departures { get; init; }

	/// <summary>
	/// IDs of airways excluded from all output because they had an unresolvable waypoint
	/// (Phase 3.2). Empty until that behaviour lands.
	/// </summary>
	public IReadOnlyList<string> ExcludedAirwayIds { get; init; } = [];
}
