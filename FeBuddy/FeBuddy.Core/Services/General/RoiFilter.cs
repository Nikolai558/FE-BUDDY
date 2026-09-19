using System.Globalization;

using FeBuddy.Core.Models.Services.General;

using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Services.General;

/// <summary>
/// Validates Region of Interest (ROI) input and clips geometry to an ROI's rectangular
/// bounds.
/// </summary>
/// <remarks>
/// <see cref="IsCoordinateValidFormat"/> and <see cref="IsCoordinatesRelativePositionValid"/>
/// are exposed as public static methods specifically so the GUI's ROI settings screen can
/// call the same validation the Airways settings parser uses, without duplicating the rules.
/// </remarks>
public static class RoiFilter
{
	/// <summary>
	/// Validates that all four ROI coordinate strings parse as decimal degrees within valid
	/// latitude (-90..90) and longitude (-180..180) ranges. Does not check the coordinates'
	/// relative position; use <see cref="IsCoordinatesRelativePositionValid"/> for that once
	/// format validation has passed.
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
			TryParseDegrees(swLonText, -180, 180, "Southwest Longitude", out _, out error) &&
			TryParseDegrees(neLatText, -90, 90, "Northeast Latitude", out _, out error) &&
			TryParseDegrees(neLonText, -180, 180, "Northeast Longitude", out _, out error);
	}

	/// <summary>
	/// Validates that the northeast corner is actually northeast of the southwest corner.
	/// </summary>
	/// <param name="swLat">Southwest corner latitude, in decimal degrees.</param>
	/// <param name="swLon">Southwest corner longitude, in decimal degrees.</param>
	/// <param name="neLat">Northeast corner latitude, in decimal degrees.</param>
	/// <param name="neLon">Northeast corner longitude, in decimal degrees.</param>
	/// <param name="error">
	/// When this method returns <see langword="false"/>, a human-readable message describing
	/// the problem; otherwise <see langword="null"/>.
	/// </param>
	/// <returns><see langword="true"/> when NeLat &gt; SwLat and NeLon &gt; SwLon.</returns>
	/// <remarks>
	/// An ROI that crosses the antimeridian (SwLon &gt; NeLon) is rejected here, not
	/// supported, per this build's decisions log.
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
				? "The Northeast Longitude must be greater than the Southwest Longitude. " +
				  "A Region of Interest that crosses the antimeridian is not currently supported."
				: "The Northeast Longitude must be greater than the Southwest Longitude.";
			return false;
		}

		error = null;
		return true;
	}

	/// <summary>
	/// Clips a LineString or MultiLineString to an ROI's rectangular bounds, returning only
	/// the line components of the intersection.
	/// </summary>
	/// <param name="geometry">The LineString or MultiLineString to clip.</param>
	/// <param name="roi">The Region of Interest to clip to.</param>
	/// <param name="geometryFactory">The geometry factory used to build the resulting geometry.</param>
	/// <returns>
	/// A single <see cref="LineString"/> when exactly one line component remains after
	/// clipping, a <see cref="MultiLineString"/> when more than one remains, or
	/// <see langword="null"/> when the geometry does not intersect the ROI at all.
	/// </returns>
	/// <remarks>
	/// NTS's <see cref="Geometry.Intersection(Geometry)"/> can return a Point,
	/// GeometryCollection, or empty geometry at the edges of a clip in addition to
	/// LineString/MultiLineString; this method discards any non-line component (a single
	/// touching point at the ROI boundary carries no useful line geometry) before deciding
	/// what to return.
	/// </remarks>
	public static Geometry? ClipLineGeometry(
		Geometry geometry,
		RegionOfInterest roi,
		GeometryFactory geometryFactory)
	{
		ArgumentNullException.ThrowIfNull(geometry);
		ArgumentNullException.ThrowIfNull(roi);
		ArgumentNullException.ThrowIfNull(geometryFactory);

		Geometry clipped = geometry.Intersection(roi.ToPolygon());

		List<LineString> lineComponents = new();
		CollectLineStrings(clipped, lineComponents);

		return lineComponents.Count switch
		{
			0 => null,
			1 => lineComponents[0],
			_ => geometryFactory.CreateMultiLineString(lineComponents.ToArray())
		};
	}

	/// <summary>
	/// Determines whether a point falls inside (or on the boundary of) an ROI's rectangular
	/// bounds.
	/// </summary>
	/// <param name="roi">The Region of Interest to test against.</param>
	/// <param name="latitude">The point's latitude, in decimal degrees.</param>
	/// <param name="longitude">The point's longitude, in decimal degrees.</param>
	public static bool Contains(RegionOfInterest roi, double latitude, double longitude)
	{
		ArgumentNullException.ThrowIfNull(roi);

		return roi.ToEnvelope().Contains(longitude, latitude);
	}

	/// <summary>
	/// Recursively collects every LineString component out of a geometry that may be a
	/// LineString, MultiLineString, or GeometryCollection (as produced by an intersection).
	/// Point/MultiPoint components and empty geometry are silently discarded.
	/// </summary>
	private static void CollectLineStrings(Geometry geometry, List<LineString> result)
	{
		switch (geometry)
		{
			case LineString lineString when !lineString.IsEmpty:
				result.Add(lineString);
				break;

			case MultiLineString multiLineString:
				for (int i = 0; i < multiLineString.NumGeometries; i++)
				{
					CollectLineStrings(multiLineString.GetGeometryN(i), result);
				}
				break;

			case GeometryCollection geometryCollection:
				for (int i = 0; i < geometryCollection.NumGeometries; i++)
				{
					CollectLineStrings(geometryCollection.GetGeometryN(i), result);
				}
				break;

				// Point, MultiPoint, and empty geometry contribute no line component.
		}
	}

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
		if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
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
