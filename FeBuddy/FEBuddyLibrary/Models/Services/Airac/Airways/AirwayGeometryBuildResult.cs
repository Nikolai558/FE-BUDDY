using NetTopologySuite.Geometries;

namespace FEBuddyLibrary.Models.Services.Airac.Airways;

/// <summary>
/// The result of building one airway's LineString geometry: the resulting LineStrings and
/// any non-fatal warnings encountered while resolving waypoint coordinates.
/// </summary>
/// <param name="LineStrings">
/// One or more continuous LineStrings making up the airway. Empty when no usable geometry
/// could be built at all.
/// </param>
/// <param name="Warnings">
/// Non-fatal problems encountered while building this airway's geometry (e.g. an
/// unresolvable waypoint).
/// </param>
/// <param name="UnresolvedWaypointIds">
/// Waypoint IDs on this airway that could not be resolved to coordinates and are a genuine
/// data fault (border crossings are normalized away upstream and are never listed here). When
/// this is non-empty, <c>AirwayBuilder</c> excludes the whole airway from all output
/// (remediation plan 3.2a).
/// </param>
public sealed record AirwayGeometryBuildResult(
	IReadOnlyList<LineString> LineStrings,
	IReadOnlyList<string> Warnings,
	IReadOnlyList<string> UnresolvedWaypointIds);
