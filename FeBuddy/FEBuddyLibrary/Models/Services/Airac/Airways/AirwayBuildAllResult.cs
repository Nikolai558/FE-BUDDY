namespace FEBuddyLibrary.Models.Services.Airac.Airways;

/// <summary>
/// The result of building every airway from parsed NASR data: the successfully-built
/// airways plus every non-fatal warning collected along the way.
/// </summary>
/// <param name="Airways">
/// Every airway that produced usable geometry (and, when an ROI was configured, survived
/// clipping to it).
/// </param>
/// <param name="Warnings">Non-fatal problems noticed while building airways.</param>
/// <param name="ExcludedAirwayIds">
/// IDs of airways excluded from all output because at least one of their waypoints could not
/// be resolved (remediation plan 3.2a). Border crossings do not count - they are normalized
/// away before geometry building.
/// </param>
public sealed record AirwayBuildAllResult(
	IReadOnlyList<Airway> Airways,
	IReadOnlyList<string> Warnings,
	IReadOnlyList<string> ExcludedAirwayIds);
