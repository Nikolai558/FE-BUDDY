using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;
using FeBuddy.Core.Application.Airac.ConcatenateAliases.Models;
using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Application.Airac.Fixes.Models;
using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Application.Airac.WxStations.Models;
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
	/// The duplicate alias report (<c>Duplicate_Alias_Commands.txt</c>) this run wrote, or
	/// <see langword="null"/> when the run wrote no alias file, so there was nothing to check.
	/// </summary>
	public DuplicateAliasReportResult? DuplicateAliasReport { get; init; }

	/// <summary>
	/// The choices the user made for duplicated alias commands when the run stopped for them
	/// (<see cref="AiracServiceSettings.ReviewDuplicateAliases"/>), to save for later runs; empty when
	/// it didn't stop.
	/// </summary>
	public IReadOnlyList<DuplicateAliasRule> DuplicateAliasChoicesMade { get; init; } = [];

	/// <summary>
	/// The saved choices for no duplicated command of this run's - its duplicate has gone - which the
	/// run didn't use.
	/// </summary>
	public IReadOnlyList<DuplicateAliasRule> UnusedDuplicateAliasChoices { get; init; } = [];

	/// <summary>
	/// Whether the user stopped the run when it asked about duplicated alias commands. It saved no
	/// alias file and no <c>Combined_Alias.txt</c> then; the other files it had written stay.
	/// </summary>
	public bool StoppedAtDuplicateReview { get; init; }

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

	/// <summary>
	/// The ARTCC Boundaries sub-service result, or <see langword="null"/> when ARTCC Boundaries
	/// was not part of this run.
	/// </summary>
	public ArtccBoundaryServiceResult? ArtccBoundaries { get; init; }

	/// <summary>
	/// The Fixes sub-service result, or <see langword="null"/> when Fixes was not part of this
	/// run.
	/// </summary>
	public FixServiceResult? Fixes { get; init; }

	/// <summary>
	/// The Wx Stations sub-service result, or <see langword="null"/> when Wx Stations was not part
	/// of this run.
	/// </summary>
	public WxStationServiceResult? WxStations { get; init; }

	/// <summary>
	/// The Procedures sub-service result, or <see langword="null"/> when Procedures was not part of
	/// this run.
	/// </summary>
	public ProcedureServiceResult? Procedures { get; init; }

	/// <summary>
	/// The Telephony sub-service result, or <see langword="null"/> when Telephony was not part of
	/// this run.
	/// </summary>
	public TelephonyServiceResult? Telephony { get; init; }

	/// <summary>
	/// What writing <c>Aliases\Combined_Alias.txt</c> produced, or <see langword="null"/> when it was
	/// not written: Concatenate Aliases was not in the run, or its combining was off.
	/// </summary>
	public CombinedAliasResult? CombinedAlias { get; init; }
}
