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
/// <para>
/// An ROI that crosses the antimeridian (AM), the 180° meridian, has one edge past ±180: the west
/// edge is always less than the east edge, so <c>-206 … -124</c> and <c>154 … 236</c> are the same box.
/// </para>
/// <para>
/// Construction does not validate the corners. Check user input with
/// <see cref="RoiFilter.IsCoordinateValidFormat"/> and
/// <see cref="RoiFilter.IsCoordinatesRelativePositionValid"/> first.
/// </para>
/// </remarks>
public sealed record RegionOfInterest(double SwLat, double SwLon, double NeLat, double NeLon)
{
	/// <summary>
	/// Whether the ROI crosses the antimeridian (AM): with its west edge moved by whole 360s to within
	/// -180..180, its east edge is past 180.
	/// </summary>
	public bool CrossesAntimeridian => WestWithinWorld + (NeLon - SwLon) > 180;

	/// <summary>
	/// The west edge moved by whole 360s to within -180 (included) and 180 (not included).
	/// </summary>
	private double WestWithinWorld => SwLon - (360 * Math.Floor((SwLon + 180) / 360));

	/// <summary>
	/// The ROI as plain boxes within -180..180 (X = longitude, Y = latitude), for clipping: one box, or
	/// two when it <see cref="CrossesAntimeridian"/> - its west edge to 180, and -180 to its east edge.
	/// </summary>
	/// <returns>The boxes, west first.</returns>
	public IReadOnlyList<Envelope> ToEnvelopes()
	{
		double west = WestWithinWorld;
		double east = west + (NeLon - SwLon);

		return east > 180
			? [new Envelope(x1: west, x2: 180, y1: SwLat, y2: NeLat), new Envelope(x1: -180, x2: east - 360, y1: SwLat, y2: NeLat)]
			: [new Envelope(x1: west, x2: east, y1: SwLat, y2: NeLat)];
	}
}
