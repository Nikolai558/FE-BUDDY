using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Domain.Geo.Models;

/// <summary>
/// A lat/lon axis-aligned rectangular Region of Interest (ROI): a box defined by its
/// southwest (bottom-left) and northeast (top-right) corners, used to filter and clip
/// service output to an area of interest (typically an ARTCC plus a surrounding buffer).
/// </summary>
/// <param name="SwLat">Southwest corner latitude, in decimal degrees.</param>
/// <param name="SwLon">Southwest corner longitude, in decimal degrees.</param>
/// <param name="NeLat">Northeast corner latitude, in decimal degrees.</param>
/// <param name="NeLon">Northeast corner longitude, in decimal degrees.</param>
/// <remarks>
/// Construction does not itself validate the coordinates. Use
/// <see cref="RoiFilter.IsCoordinateValidFormat"/> and
/// <see cref="RoiFilter.IsCoordinatesRelativePositionValid"/> before building
/// one from user input (the GUI's ROI save button will call these directly; the airway
/// settings parser calls them internally).
/// </remarks>
public sealed record RegionOfInterest(double SwLat, double SwLon, double NeLat, double NeLon)
{
	/// <summary>
	/// Converts this ROI to an NTS <see cref="Envelope"/> (X = longitude, Y = latitude).
	/// </summary>
	public Envelope ToEnvelope() =>
		new(x1: SwLon, x2: NeLon, y1: SwLat, y2: NeLat);

	/// <summary>
	/// Converts this ROI to a closed NTS <see cref="Polygon"/> suitable for geometry
	/// intersection (clipping) operations.
	/// </summary>
	public Polygon ToPolygon()
	{
		Geometry envelopeGeometry = Wgs84.Factory.ToGeometry(ToEnvelope());

		if (envelopeGeometry is not Polygon polygon)
		{
			// ToEnvelope() always produces a valid rectangle for a validated ROI
			// (NeLat > SwLat and NeLon > SwLon), so GeometryFactory.ToGeometry always
			// returns a Polygon. This is only reachable for a degenerate ROI that bypassed
			// RoiFilter's validation.
			throw new InvalidOperationException(
				"Region of Interest did not produce a valid rectangular polygon. " +
				"Ensure NeLat > SwLat and NeLon > SwLon.");
		}

		return polygon;
	}
}
