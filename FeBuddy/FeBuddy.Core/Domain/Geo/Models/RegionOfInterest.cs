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
/// Construction does not validate the corners. Check user input with
/// <see cref="RoiFilter.IsCoordinateValidFormat"/> and
/// <see cref="RoiFilter.IsCoordinatesRelativePositionValid"/> first.
/// </remarks>
public sealed record RegionOfInterest(double SwLat, double SwLon, double NeLat, double NeLon)
{
	/// <summary>
	/// Converts this ROI to an NTS <see cref="Envelope"/> (X = longitude, Y = latitude).
	/// </summary>
	/// <returns>The envelope.</returns>
	public Envelope ToEnvelope() =>
		new(x1: SwLon, x2: NeLon, y1: SwLat, y2: NeLat);

	/// <summary>
	/// Converts this ROI to a closed NTS <see cref="Polygon"/> for clipping.
	/// </summary>
	/// <returns>The rectangle as a polygon.</returns>
	/// <exception cref="InvalidOperationException">Thrown for a degenerate ROI whose corners were never validated.</exception>
	public Polygon ToPolygon()
	{
		Geometry envelopeGeometry = Wgs84.Factory.ToGeometry(ToEnvelope());

		if (envelopeGeometry is not Polygon polygon)
		{
			// A validated ROI always has area, so ToGeometry returns a Polygon; a zero-width
			// one comes back as a line or point.
			throw new InvalidOperationException(
				"Region of Interest did not produce a valid rectangular polygon. " +
				"Ensure NeLat > SwLat and NeLon > SwLon.");
		}

		return polygon;
	}
}
