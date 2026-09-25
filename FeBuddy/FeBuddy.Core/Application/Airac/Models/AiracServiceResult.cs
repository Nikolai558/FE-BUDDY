using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Application.Models;

namespace FeBuddy.Core.Application.Airac.Models;

/// <summary>
/// The result of one <c>AiracService.RunAsync</c> call: each selected sub-service's own result,
/// plus every message they reported, combined.
/// </summary>
public sealed record AiracServiceResult : ServiceResult
{
	/// <summary>
	/// The cycle folder the run wrote into (<see cref="AiracServiceSettings.CycleOutputDirectory"/>).
	/// It exists only if the run wrote something.
	/// </summary>
	public required string OutputDirectory { get; init; }

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
	/// The Arrivals sub-service result, or <see langword="null"/> when Arrivals was not part of
	/// this run.
	/// </summary>
	public ArrivalServiceResult? Arrivals { get; init; }

	/// <summary>
	/// The NAVAIDs sub-service result, or <see langword="null"/> when NAVAIDs was not part of
	/// this run.
	/// </summary>
	public NavaidServiceResult? Navaids { get; init; }
}
