namespace FeBuddy.Core.Domain.Airways.Models;

/// <summary>
/// One resolved waypoint on an airway: its identifier, NASR point type and coordinates.
/// </summary>
/// <param name="PointId">The waypoint identifier (a <c>FROM_POINT</c> or <c>TO_POINT</c> value).</param>
/// <param name="PointType">
/// The NASR <c>FROM_PT_TYPE</c> for this point (e.g. "VOR", "WP", "NDB/DME"), which picks the
/// Symbol Feature's <c>style</c>. <see langword="null"/> for an airway's last point: NASR only
/// gives a point's type where it is a segment's <c>FROM_POINT</c>, and the last point never is.
/// </param>
/// <param name="Latitude">Decimal latitude, in degrees.</param>
/// <param name="Longitude">Decimal longitude, in degrees.</param>
public sealed record AirwayPoint(
	string PointId,
	string? PointType,
	double Latitude,
	double Longitude);
