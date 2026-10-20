using System.Globalization;

using FeBuddy.Core.Domain.Geo.Models;

using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Domain.Geo;

/// <summary>
/// Validates Region of Interest (ROI) input and clips geometry to an ROI's rectangular
/// bounds, including an ROI that crosses the antimeridian (AM), the 180° meridian.
/// </summary>
/// <remarks>
/// The two validation methods are public so the GUI's ROI screens check input with exactly the
/// rules the settings parsers apply.
/// </remarks>
public static class RoiFilter
{
	/// <summary>
	/// Validates that all four ROI coordinate strings parse as decimal degrees within valid
	/// latitude (-90..90) and longitude (-360..360) ranges. A longitude can be past ±180 so an ROI
	/// that crosses the antimeridian (AM) can be written with its west edge less than its east edge
	/// (e.g. -206 … -124). Does not check the coordinates' relative position; use
	/// <see cref="IsCoordinatesRelativePositionValid"/> for that once format validation has passed.
	/// </summary>
	/// <param name="swLatText">Southwest corner latitude, as user-entered text.</param>
	/// <param name="swLonText">Southwest corner longitude, as user-entered text.</param>
	/// <param name="neLatText">Northeast corner latitude, as user-entered text.</param>
	/// <param name="neLonText">Northeast corner longitude, as user-entered text.</param>
	/// <param name="error">
	/// When this method returns <see langword="false"/>, a human-readable message describing
	/// the first problem found; otherwise <see langword="null"/>.
	/// </param>
	/// <returns><see langword="true"/> when every coordinate is a valid decimal value in range.</returns>
	public static bool IsCoordinateValidFormat(
		string? swLatText,
		string? swLonText,
		string? neLatText,
		string? neLonText,
		out string? error)
	{
		return
			TryParseDegrees(swLatText, -90, 90, "Southwest Latitude", out _, out error) &&
			TryParseDegrees(swLonText, -360, 360, "Southwest Longitude", out _, out error) &&
			TryParseDegrees(neLatText, -90, 90, "Northeast Latitude", out _, out error) &&
			TryParseDegrees(neLonText, -360, 360, "Northeast Longitude", out _, out error);
	}

	/// <summary>
	/// Validates that the northeast corner is actually northeast of the southwest corner, and that
	/// the box is less than 360° wide.
	/// </summary>
	/// <param name="swLat">Southwest corner latitude, in decimal degrees.</param>
	/// <param name="swLon">Southwest corner longitude, in decimal degrees.</param>
	/// <param name="neLat">Northeast corner latitude, in decimal degrees.</param>
	/// <param name="neLon">Northeast corner longitude, in decimal degrees.</param>
	/// <param name="error">
	/// When this method returns <see langword="false"/>, a human-readable message describing
	/// the problem; otherwise <see langword="null"/>.
	/// </param>
	/// <returns><see langword="true"/> when NeLat &gt; SwLat, NeLon &gt; SwLon and NeLon - SwLon &lt; 360.</returns>
	/// <remarks>
	/// An ROI that crosses the antimeridian (AM) is written with one edge past ±180, e.g.
	/// <c>-206 … -124</c> or <c>154 … 236</c>. A west edge greater than the east edge (<c>154 … -124</c>)
	/// is refused: a settings file or an import can't be asked what it meant. The GUI can ask, with
	/// <see cref="AsCrossingAntimeridian"/>.
	/// </remarks>
	public static bool IsCoordinatesRelativePositionValid(
		double swLat,
		double swLon,
		double neLat,
		double neLon,
		out string? error)
	{
		if (neLat <= swLat)
		{
			error = "The Northeast Latitude must be greater than the Southwest Latitude.";
			return false;
		}

		if (neLon <= swLon)
		{
			error = neLon < swLon
				? "The Southwest Longitude must be west of the Northeast Longitude. For an ROI that crosses the " +
				  "antimeridian (AM), take 360 off the Southwest Longitude (154 becomes -206), as drawing it on the Map does."
				: "The Southwest Longitude must be west of the Northeast Longitude.";
			return false;
		}

		if (neLon - swLon >= 360)
		{
			error = "The Region of Interest must be less than 360° wide.";
			return false;
		}

		error = null;
		return true;
	}

	/// <summary>
	/// The ROI that corners mean when their one problem is a Southwest Longitude east of the
	/// Northeast Longitude, read as an ROI that crosses the antimeridian (AM): 360 taken off the
	/// Southwest Longitude, then the box moved by whole 360s so its centre is within -180..180, as a
	/// box drawn across the AM on the Map is written. <c>SW 44, 154 / NE 90, -124</c> becomes
	/// <c>SW 44, -206 / NE 90, -124</c>.
	/// </summary>
	/// <param name="swLatText">Southwest corner latitude, as user-entered text.</param>
	/// <param name="swLonText">Southwest corner longitude, as user-entered text.</param>
	/// <param name="neLatText">Northeast corner latitude, as user-entered text.</param>
	/// <param name="neLonText">Northeast corner longitude, as user-entered text.</param>
	/// <returns>
	/// The ROI across the AM, or <see langword="null"/> when the corners have another problem, or none.
	/// </returns>
	/// <remarks>
	/// For the GUI to offer as a question. <see cref="IsCoordinatesRelativePositionValid"/> still
	/// refuses such corners, since a settings file or an import can't be asked what it meant.
	/// </remarks>
	public static RegionOfInterest? AsCrossingAntimeridian(string? swLatText, string? swLonText, string? neLatText, string? neLonText)
	{
		if (!IsCoordinateValidFormat(swLatText, swLonText, neLatText, neLonText, out _))
		{
			return null;
		}

		double swLat = Parse(swLatText);
		double swLon = Parse(swLonText);
		double neLat = Parse(neLatText);
		double neLon = Parse(neLonText);

		if (neLat <= swLat || neLon >= swLon)
		{
			return null;
		}

		double west = swLon - 360;
		double shift = 360 * Math.Floor((((west + neLon) / 2) + 180) / 360);
		west -= shift;
		double east = neLon - shift;

		return IsCoordinatesRelativePositionValid(swLat, west, neLat, east, out _)
			? new RegionOfInterest(swLat, west, neLat, east)
			: null;
	}

	/// <summary>
	/// Clips lines to an ROI's rectangular bounds. Output coordinates stay within -180..180: an ROI
	/// that crosses the antimeridian (AM) is clipped as two boxes, its west edge to 180 and -180 to its
	/// east edge.
	/// </summary>
	/// <param name="lines">The lines to clip, treated as one geometry.</param>
	/// <param name="roi">The Region of Interest to clip to.</param>
	/// <returns>The line pieces inside the ROI; empty when none of the lines reaches it.</returns>
	/// <remarks>
	/// <para>
	/// When the ROI crosses the AM, the lines are first split there (<see cref="AntimeridianSplitter"/>),
	/// whether or not the sub-service's <c>SplitAtAntimeridian</c> is on: a segment from 170 to -170
	/// can't otherwise be clipped against boxes either side of the AM.
	/// </para>
	/// <para>
	/// An intersection can also produce points - a line that only touches the ROI's edge or
	/// corner. Those carry no drawable line, so they are dropped.
	/// </para>
	/// </remarks>
	public static IReadOnlyList<LineString> ClipLines(IReadOnlyList<LineString> lines, RegionOfInterest roi)
	{
		ArgumentNullException.ThrowIfNull(lines);
		ArgumentNullException.ThrowIfNull(roi);

		IReadOnlyList<LineString> toClip = roi.CrossesAntimeridian ? AntimeridianSplitter.Split(lines) : lines;
		Geometry combined = Wgs84.Factory.CreateMultiLineString([.. toClip]);
		List<LineString> clipped = [];

		foreach (Envelope box in roi.ToEnvelopes())
		{
			CollectLineStrings(combined.Intersection(ToPolygon(box)), clipped);
		}

		return clipped;
	}

	/// <summary>
	/// Determines whether a point falls inside (or on the boundary of) an ROI's rectangular
	/// bounds, including an ROI that crosses the antimeridian (AM).
	/// </summary>
	/// <param name="roi">The Region of Interest to test against.</param>
	/// <param name="latitude">The point's latitude, in decimal degrees.</param>
	/// <param name="longitude">The point's longitude, in decimal degrees.</param>
	/// <returns><see langword="true"/> when the point is inside or on the edge.</returns>
	/// <remarks>
	/// The point's longitude is moved by whole 360s to at or past the ROI's west edge and less than 360
	/// beyond it, then tested against the east edge.
	/// </remarks>
	public static bool Contains(RegionOfInterest roi, double latitude, double longitude)
	{
		ArgumentNullException.ThrowIfNull(roi);

		if (latitude < roi.SwLat || latitude > roi.NeLat)
		{
			return false;
		}

		double pastWest = (longitude - roi.SwLon) % 360;
		if (pastWest < 0)
		{
			pastWest += 360;
		}

		return pastWest <= roi.NeLon - roi.SwLon;
	}

	/// <summary>A box as a closed polygon to clip against.</summary>
	/// <exception cref="InvalidOperationException">Thrown for a degenerate ROI whose corners were never validated.</exception>
	private static Polygon ToPolygon(Envelope box) =>
		Wgs84.Factory.ToGeometry(box) as Polygon
		?? throw new InvalidOperationException(
			"Region of Interest did not produce a valid rectangular polygon. " +
			"Ensure NeLat > SwLat and NeLon > SwLon.");

	/// <summary>
	/// Recursively collects every non-empty LineString out of a geometry. A MultiLineString is
	/// a GeometryCollection, so one case covers both; points and empty geometry add nothing.
	/// </summary>
	private static void CollectLineStrings(Geometry geometry, List<LineString> result)
	{
		switch (geometry)
		{
			case LineString lineString when !lineString.IsEmpty:
				result.Add(lineString);
				break;

			case GeometryCollection geometryCollection:
				for (int i = 0; i < geometryCollection.NumGeometries; i++)
				{
					CollectLineStrings(geometryCollection.GetGeometryN(i), result);
				}
				break;
		}
	}

	private static double Parse(string? text) => double.Parse(text!, NumberStyles.Float, CultureInfo.InvariantCulture);

	/// <summary>
	/// Parses one coordinate value and checks it against the valid range for its axis.
	/// </summary>
	private static bool TryParseDegrees(
		string? text,
		double min,
		double max,
		string fieldName,
		out double value,
		out string? error)
	{
		// NaN and infinity parse, but fail every range comparison and so would pass the range check.
		if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || !double.IsFinite(value))
		{
			error = $"{fieldName} '{text}' is not a valid decimal coordinate.";
			return false;
		}

		if (value < min || value > max)
		{
			error = $"{fieldName} {value} is out of range. Valid range: {min} to {max}.";
			return false;
		}

		error = null;
		return true;
	}
}
