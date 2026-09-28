using System.Globalization;

using FeBuddy.Core.Domain.Geo.Models;

namespace FeBuddy.Core.Domain.Geo;

/// <summary>
/// Coordinate math on the Earth's surface: validating and converting between decimal degrees and
/// DMS, distance and bearing between two points, the point reached along a bearing, and
/// splitting a segment where it crosses the antimeridian.
/// </summary>
/// <remarks>
/// <para>
/// The Earth is treated as a sphere of radius 6,371 km. Distances and bearings are great-circle
/// values, accurate to well under a percent - ample for drawing lines on a radar scope.
/// </para>
/// <para>
/// DMS text is FE-Buddy's fixed-width form <c>[N|S|E|W]DDD.MM.SS.mmm</c>, e.g.
/// <c>N043.31.08.418</c>: hemisphere, three-digit degrees, then minutes, seconds and
/// milliseconds of arc.
/// </para>
/// </remarks>
public static class GeoMath
{
	/// <summary>Mean Earth radius, in metres.</summary>
	internal const double EarthRadiusMetres = 6371e3;

	/// <summary>Metres in one international nautical mile.</summary>
	internal const double MetresPerNauticalMile = 1852;

	private const int MillisecondsPerDegree = 3_600_000;

	/// <summary>Whether a latitude and longitude in decimal degrees are both in range.</summary>
	/// <param name="latitude">Latitude, -90 to 90.</param>
	/// <param name="longitude">Longitude, -180 to 180.</param>
	/// <returns><see langword="true"/> when both are in range.</returns>
	public static bool IsValidDecimal(double latitude, double longitude) =>
		latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;

	/// <summary>Whether a latitude and longitude are both valid DMS text.</summary>
	/// <param name="latitude">Latitude, e.g. <c>N043.31.08.418</c>.</param>
	/// <param name="longitude">Longitude, e.g. <c>W112.03.50.103</c>.</param>
	/// <returns>
	/// <see langword="true"/> when both have the right hemisphere letter, four numeric parts,
	/// and every part in range; <see langword="false"/> otherwise, including for null or empty text.
	/// </returns>
	public static bool IsValidDms(string? latitude, string? longitude) =>
		IsValidDmsPart(latitude, 'N', 'S', maxDegrees: 90) && IsValidDmsPart(longitude, 'E', 'W', maxDegrees: 180);

	/// <summary>Converts decimal degrees to DMS text.</summary>
	/// <param name="degrees">The latitude or longitude in decimal degrees.</param>
	/// <param name="isLatitude"><see langword="true"/> for a latitude (N/S), <see langword="false"/> for a longitude (E/W).</param>
	/// <returns>The DMS text, rounded to the nearest millisecond of arc, e.g. <c>N043.31.08.418</c>.</returns>
	public static string ToDms(double degrees, bool isLatitude)
	{
		char hemisphere = isLatitude
			? (degrees < 0 ? 'S' : 'N')
			: (degrees < 0 ? 'W' : 'E');

		// Split whole milliseconds of arc, so rounding can never produce 60 seconds or minutes.
		long totalMilliseconds = (long)Math.Round(Math.Abs(degrees) * MillisecondsPerDegree, MidpointRounding.AwayFromZero);

		long wholeDegrees = totalMilliseconds / MillisecondsPerDegree;
		long minutes = totalMilliseconds / 60_000 % 60;
		long seconds = totalMilliseconds / 1_000 % 60;
		long milliseconds = totalMilliseconds % 1_000;

		return string.Create(CultureInfo.InvariantCulture, $"{hemisphere}{wholeDegrees:000}.{minutes:00}.{seconds:00}.{milliseconds:000}");
	}

	/// <summary>Converts DMS text to decimal degrees.</summary>
	/// <param name="dms">A latitude or longitude, e.g. <c>N043.31.08.418</c>. Must be valid (see <see cref="IsValidDms"/>).</param>
	/// <returns>The value in decimal degrees, negative for S and W, rounded to 7 places (about 1 cm).</returns>
	public static double ToDecimal(string dms)
	{
		string[] parts = dms.Split('.');

		double degrees = double.Parse(parts[0][1..], CultureInfo.InvariantCulture);
		double minutes = double.Parse(parts[1], CultureInfo.InvariantCulture);
		double seconds = double.Parse(parts[2], CultureInfo.InvariantCulture);
		double milliseconds = double.Parse(parts[3], CultureInfo.InvariantCulture);

		double result = degrees + minutes / 60 + seconds / 3600 + milliseconds / MillisecondsPerDegree;

		if (dms[0] is 'S' or 'W')
		{
			result = -result;
		}

		return Math.Round(result, 7);
	}

	/// <summary>The great-circle distance between two points, in nautical miles.</summary>
	/// <param name="pointA">The first point.</param>
	/// <param name="pointB">The second point.</param>
	/// <param name="round"><see langword="true"/> to round to whole miles; otherwise rounded to 6 places.</param>
	/// <returns>The distance in nautical miles.</returns>
	/// <remarks>Uses the haversine formula, which stays accurate for very short distances.</remarks>
	public static double Distance(Location pointA, Location pointB, bool round = true)
	{
		double lat1 = ToRadians(pointA.DecLat);
		double lon1 = ToRadians(pointA.DecLon);
		double lat2 = ToRadians(pointB.DecLat);
		double lon2 = ToRadians(pointB.DecLon);
		double dLat = lat2 - lat1;
		double dLon = lon2 - lon1;

		double a = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);
		double centralAngle = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
		double distanceNm = EarthRadiusMetres * centralAngle / MetresPerNauticalMile;

		return Math.Round(distanceNm, round ? 0 : 6);
	}

	/// <summary>
	/// Whether the segment between two points crosses the antimeridian (±180° longitude).
	/// </summary>
	/// <param name="startPoint">The segment's start.</param>
	/// <param name="endPoint">The segment's end.</param>
	/// <returns><see langword="true"/> when the shorter way between the points passes through ±180°.</returns>
	/// <remarks>
	/// A segment is assumed to take the shorter way round. The short way passes through ±180°
	/// exactly when the two longitudes are more than 180° apart, so (40, -170) to (40, 170)
	/// counts as crossing even if the route was meant to go the long way east.
	/// </remarks>
	public static bool CrossesAntimeridian(Location startPoint, Location endPoint) =>
		Math.Abs(startPoint.DecLon - endPoint.DecLon) > 180;

	/// <summary>The initial great-circle bearing from one point to another.</summary>
	/// <param name="pointA">The starting point.</param>
	/// <param name="pointB">The destination.</param>
	/// <returns>The true bearing in degrees, 0 to 360, clockwise from north.</returns>
	public static double Bearing(Location pointA, Location pointB)
	{
		double lat1 = ToRadians(pointA.DecLat);
		double lon1 = ToRadians(pointA.DecLon);
		double lat2 = ToRadians(pointB.DecLat);
		double lon2 = ToRadians(pointB.DecLon);
		double dLon = lon2 - lon1;

		double y = Math.Sin(dLon) * Math.Cos(lat2);
		double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

		// Atan2 returns -180..180; shift into 0..360.
		return (ToDegrees(Math.Atan2(y, x)) + 360) % 360;
	}

	/// <summary>
	/// Splits a segment that crosses the antimeridian into its two halves, one each side.
	/// </summary>
	/// <param name="pointA">The segment's start.</param>
	/// <param name="pointB">The segment's end.</param>
	/// <returns>
	/// Four points: <paramref name="pointA"/>, where the segment meets the antimeridian on
	/// <paramref name="pointA"/>'s side (longitude ±180 matching its sign), the same crossing on
	/// <paramref name="pointB"/>'s side, and <paramref name="pointB"/>.
	/// </returns>
	/// <remarks>
	/// Only meaningful when <see cref="CrossesAntimeridian"/> is <see langword="true"/>. The
	/// crossing latitude is interpolated linearly in longitude, which is what a map draws.
	/// </remarks>
	public static List<Location> SplitLineSegmentAtAntimeridian(Location pointA, Location pointB)
	{
		// Measure longitude from the antimeridian instead of Greenwich, so the two points sit
		// either side of zero and the crossing is where the line reaches it.
		double startLon = pointA.DecLon < 0 ? pointA.DecLon + 180 : pointA.DecLon - 180;
		double endLon = pointB.DecLon < 0 ? pointB.DecLon + 180 : pointB.DecLon - 180;

		double slope = (pointA.DecLat - pointB.DecLat) / (startLon - endLon);
		double crossingLatitude = pointA.DecLat - slope * startLon;

		Location crossingOnStartSide = new(crossingLatitude, pointA.DecLon < 0 ? -180 : 180);
		Location crossingOnEndSide = new(crossingLatitude, pointB.DecLon < 0 ? -180 : 180);

		return [pointA, crossingOnStartSide, crossingOnEndSide, pointB];
	}

	/// <summary>
	/// The point reached by travelling a distance along a great circle from a starting bearing.
	/// </summary>
	/// <param name="origin">The starting point.</param>
	/// <param name="bearingDegrees">The initial true bearing in degrees, clockwise from north.</param>
	/// <param name="distanceNm">How far to travel, in nautical miles.</param>
	/// <returns>The destination, with longitude wrapped into -180 to 180.</returns>
	public static Location PointAtDistanceAndBearing(Location origin, double bearingDegrees, double distanceNm)
	{
		const double earthRadiusNm = EarthRadiusMetres / MetresPerNauticalMile;

		double lat1 = ToRadians(origin.DecLat);
		double lon1 = ToRadians(origin.DecLon);
		double bearing = ToRadians(bearingDegrees);
		double angularDistance = distanceNm / earthRadiusNm;

		double lat2 = Math.Asin(
			Math.Sin(lat1) * Math.Cos(angularDistance) +
			Math.Cos(lat1) * Math.Sin(angularDistance) * Math.Cos(bearing));

		double lon2 = lon1 + Math.Atan2(
			Math.Sin(bearing) * Math.Sin(angularDistance) * Math.Cos(lat1),
			Math.Cos(angularDistance) - Math.Sin(lat1) * Math.Sin(lat2));

		// Wrap longitude past ±180, and guard latitude against floating-point drift at the poles.
		double longitude = (ToDegrees(lon2) + 540) % 360 - 180;
		double latitude = Math.Clamp(ToDegrees(lat2), -90, 90);

		return new Location(latitude, longitude);
	}

	private static double ToRadians(double degrees) => degrees * Math.PI / 180;

	private static double ToDegrees(double radians) => radians * 180 / Math.PI;

	private static bool IsValidDmsPart(string? dms, char positive, char negative, int maxDegrees)
	{
		if (string.IsNullOrEmpty(dms) || dms.Length < 2)
		{
			return false;
		}

		char hemisphere = char.ToUpperInvariant(dms[0]);
		if (hemisphere != positive && hemisphere != negative)
		{
			return false;
		}

		string[] parts = dms[1..].Split('.');

		return parts.Length == 4
			&& int.TryParse(parts[0], out int degrees) && degrees >= 0 && degrees <= maxDegrees
			&& int.TryParse(parts[1], out int minutes) && minutes is >= 0 and < 60
			&& int.TryParse(parts[2], out int seconds) && seconds is >= 0 and < 60
			&& int.TryParse(parts[3], out int milliseconds) && milliseconds < 1000;
	}
}
