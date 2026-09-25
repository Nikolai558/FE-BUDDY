using FeBuddy.Core.Application.Models;

using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Application.Airac.Airways.Models;

/// <summary>
/// The result of building one airway's LineString geometry: the resulting LineStrings, any
/// levelled messages, and the IDs of any waypoints that could not be resolved.
/// </summary>
/// <param name="LineStrings">
/// One or more continuous LineStrings making up the airway. Empty when no usable geometry
/// could be built at all.
/// </param>
/// <param name="Messages">Levelled messages raised while building this airway's geometry.</param>
/// <param name="UnresolvedWaypointIds">
/// Waypoint IDs on this airway that could not be resolved to coordinates and are a genuine
/// data fault (border crossings are normalized away upstream and are never listed here). When
/// this is non-empty, <c>AirwayBuilder</c> excludes the whole airway from all output.
/// </param>
public sealed record AirwayGeometryBuildResult(
	IReadOnlyList<LineString> LineStrings,
	IReadOnlyList<ServiceMessage> Messages,
	IReadOnlyList<string> UnresolvedWaypointIds);
