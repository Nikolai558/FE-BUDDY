using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Airways.Models;

namespace FeBuddy.Core.Application.Airac.Airways.Models;

/// <summary>
/// The result of building every airway from parsed NASR data: the successfully-built airways,
/// every levelled message, and the IDs of airways excluded entirely.
/// </summary>
/// <param name="Airways">
/// Every airway that produced usable geometry. With an ROI set, airways outside it are kept
/// too, marked <see cref="Airway.CrossesRoi"/> <see langword="false"/>: the alias file can
/// still list them.
/// </param>
/// <param name="Messages">Levelled messages noticed while building airways.</param>
/// <param name="ExcludedAirwayIds">
/// IDs of airways excluded from all output because at least one of their waypoints could not
/// be resolved. Border crossings do not count - they are normalized
/// away before geometry building.
/// </param>
public sealed record AirwayBuildAllResult(
	IReadOnlyList<Airway> Airways,
	IReadOnlyList<ServiceMessage> Messages,
	IReadOnlyList<string> ExcludedAirwayIds);
