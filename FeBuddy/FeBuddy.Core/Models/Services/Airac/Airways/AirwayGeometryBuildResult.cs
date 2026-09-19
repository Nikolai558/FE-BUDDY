using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Services.General;

using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Models.Services.Airac.Airways;

/// <summary>
/// The result of building one airway's LineString geometry: the resulting LineStrings, any
/// levelled messages, and the IDs of any waypoints that could not be resolved.
/// </summary>
/// <param name="LineStrings">
/// One or more continuous LineStrings making up the airway. Empty when no usable geometry
/// could be built at all.
/// </param>
/// <param name="Messages">Levelled messages raised while building this airway's geometry (remediation plan 3.8).</param>
/// <param name="UnresolvedWaypointIds">
/// Waypoint IDs on this airway that could not be resolved to coordinates and are a genuine
/// data fault (border crossings are normalized away upstream and are never listed here). When
/// this is non-empty, <c>AirwayBuilder</c> excludes the whole airway from all output
/// (remediation plan 3.2a).
/// </param>
public sealed record AirwayGeometryBuildResult(
	IReadOnlyList<LineString> LineStrings,
	IReadOnlyList<ServiceMessage> Messages,
	IReadOnlyList<string> UnresolvedWaypointIds)
{
	/// <summary>Backwards-compatible text-only view of the Warning/Error entries in <see cref="Messages"/>.</summary>
	public IReadOnlyList<string> Warnings =>
		Messages.Where(m => m.Level is LogLevel.Warning or LogLevel.Error).Select(m => m.Text).ToArray();
}
