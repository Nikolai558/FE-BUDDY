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
/// unresolvable mid-airway waypoint that forced a gap).
/// </param>
public sealed record AirwayGeometryBuildResult(
	IReadOnlyList<LineString> LineStrings,
	IReadOnlyList<string> Warnings);
