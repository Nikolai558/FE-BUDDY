namespace FEBuddyLibrary.Models.Services.Airac.Airways;

/// <summary>
/// One resolved waypoint on an airway: its identifier, NASR point type, coordinates, and
/// which NASR data source it was resolved from.
/// </summary>
/// <param name="PointId">The waypoint identifier (a <c>FROM_POINT</c> or <c>TO_POINT</c> value).</param>
/// <param name="PointType">
/// The NASR <c>FROM_PT_TYPE</c> for this point (e.g. "VOR", "WP", "NDB/DME"). Used by
/// <c>AirwayGeojsonService</c> to choose the Symbol feature's <c>style</c>. Reference-only
/// points (null <c>FROM_PT_TYPE</c>) are excluded before this model is built, so this is
/// never null in practice for a resolved <see cref="AirwayPoint"/>.
/// </param>
/// <param name="Latitude">Decimal latitude, in degrees.</param>
/// <param name="Longitude">Decimal longitude, in degrees.</param>
/// <param name="FoundIn">
/// Which NASR data source the coordinates were resolved from: "fix", "navaid", or "airport".
/// </param>
public sealed record AirwayPoint(
	string PointId,
	string? PointType,
	double Latitude,
	double Longitude,
	string FoundIn);
