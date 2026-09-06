using FEBuddyLibrary.Models.Location;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnitTests")]
namespace FEBuddyLibrary.Handlers;
/// <summary>
/// A static class that handles all logic for coordinate math and conversions.
/// </summary>
public class CoordinateHandler
{
    // ---------- Public Methods ---------- 
    /// <summary>
    /// Check a Decimal Latitude and Longitude to see if they are valid.
    /// </summary>
    /// <param name="latitude">double: Latitude</param>
    /// <param name="longitude">double: Longitude</param>
    /// <returns>bool: Returns true if the Latitude AND Longitude are valid Decimals, otherwise returns False.</returns>
    public static bool IsValidDecimal(double latitude, double longitude)
    {
        bool isValidLatitude = latitude >= -90 && latitude <= 90;
        bool isValidLongitude = longitude >= -180 && longitude <= 180;

        return isValidLatitude && isValidLongitude;
    }

    /// <summary>
    /// Check a DMS Latitude and Longitude to see if they are valid.
    /// </summary>
    /// <param name="Lat">string: Latitude - Format: ['N', 'S']DDD.MM.SS.SSS</param>
    /// <param name="Lon">string: Longitude - Format: ['E', 'W']DDD.MM.SS.SSS</param>
    /// <returns>bool: Returns True if the Latitude AND Longitude are valid DMS formats, otherwise returns False.</returns>
    public static bool IsValidDMS(string Lat, string Lon)
    {
        if (string.IsNullOrEmpty(Lat) || string.IsNullOrEmpty(Lon) ||
            Lat.Length < 2 || Lon.Length < 2)
            return false;

        char latDirection = Char.ToUpper(Lat[0]);
        string latCoordinates = Lat.Substring(1);

        char lonDirection = Char.ToUpper(Lon[0]);
        string lonCoordinates = Lon.Substring(1);

        if (latDirection != 'N' && latDirection != 'S')
            return false;

        if (lonDirection != 'E' && lonDirection != 'W')
            return false;

        string[] latParts = latCoordinates.Split('.');
        string[] lonParts = lonCoordinates.Split('.');

        if (latParts.Length != 4 || lonParts.Length != 4)
            return false;

        if (!int.TryParse(latParts[0], out int latDegrees) ||
            !int.TryParse(latParts[1], out int latMinutes) ||
            !int.TryParse(latParts[2], out int latSeconds) ||
            !int.TryParse(latParts[3], out int latMilliseconds) ||
            !int.TryParse(lonParts[0], out int lonDegrees) ||
            !int.TryParse(lonParts[1], out int lonMinutes) ||
            !int.TryParse(lonParts[2], out int lonSeconds) ||
            !int.TryParse(lonParts[3], out int lonMilliseconds))
            return false;

        if (latDegrees < 0 || latDegrees > 90 || latMinutes < 0 || latMinutes >= 60 || latSeconds < 0 || latSeconds >= 60 || latMilliseconds >= 1000 ||
            lonDegrees < 0 || lonDegrees > 180 || lonMinutes < 0 || lonMinutes >= 60 || lonSeconds < 0 || lonSeconds >= 60 || lonMilliseconds >= 1000)
            return false;

        return true;
    }


    /// <summary>
    /// Converts Decimal format to DMS format.
    /// </summary>
    /// <param name="Dec">double: Decimal Format for either Latitude or Longitude</param>
    /// <param name="IsLat">bool: If the Decimal you passed in is a Lattitude, pass "true" for this parameter, otherwise pass "false"</param>
    /// <returns>string: DMS format of the value passed in. Format: ['N', 'S', 'E', 'W']DDD.MM.SS.SSS</returns>
    public static string ToDMS(double Dec, bool IsLat)
    {
        // Determine the hemisphere (North, South, East, or West) based on the value of Dec and the IsLat flag.
        string hemisphere = (IsLat && Dec < 0) ? "S" : (IsLat && Dec >= 0) ? "N" : (!IsLat && Dec < 0) ? "W" : "E";

        // get the degrees, minutes, and seconds
        // Ensure that the latitude or longitude value is positive.
        double absLat = Math.Abs(Dec);
        // degrees is calculated by taking the integer part of absLat
        double degrees = Math.Floor(absLat);
        // minutes is obtained by subtracting the integer part of degrees from absLat, multiplying the result by 60, and taking the integer part
        double minutes = Math.Floor((absLat - degrees) * 60);
        // seconds is calculated by subtracting the degree and minute components from absLat, dividing the result by 60, multiplying by 3600, and rounding the result to three decimal places
        double seconds = Math.Round((absLat - degrees - minutes / 60) * 3600, 3);
        // milliseconds is obtained by subtracting the integer part of seconds from seconds, multiplying the result by 1000,
        double miliseconds = (seconds - Math.Floor(seconds)) * 1000;

        // Concatenate the hemisphere, degrees, minutes, seconds, and milliseconds into a string representation of the DMS format.
        // The ToString method is used with custom format strings to ensure that each component has a specific format
        // (e.g., degrees are represented with three digits, minutes and seconds with two digits, and milliseconds with three digits).
        string dms = hemisphere + degrees.ToString("000") + "." + minutes.ToString("00") + "." + seconds.ToString("00") + "." + miliseconds.ToString("000");
        return dms;
    }

    /// <summary>
    /// This method is intended to convert a string representing a coordinate in degrees, minutes, and seconds (DMS) format to its decimal representation.
    /// </summary>
    /// <param name="DMS">string: Latitude or Longitude - Format: ['N', 'S', 'E', 'W']DDD.MM.SS.SSS</param>
    /// <returns>double: Returns the decimal version of the Latitude or Longitude passed in.</returns>
    public static double ToDecimal(string DMS)
    {
        // splits the input string DMS into an array of strings using the period ('.') as the delimiter.
        // The resulting array will contain four elements: degrees, minutes, seconds, and milliseconds.
        string[] parts = DMS.Split('.');

        // This line extracts the degrees portion from the first element of the parts array.
        // The [1..] notation is used to extract a substring starting from the second character (index 1) until the end.
        // The extracted substring is then parsed into a double value and assigned to the degrees variable.
        double degrees = double.Parse(parts[0][1..]);

        // The remaining three lines extract the minutes, seconds, and milliseconds portions from the parts array.
        double minutes = double.Parse(parts[1]);
        double seconds = double.Parse(parts[2]);
        double milliseconds = double.Parse(parts[3]);

        // This line calculates the decimal representation of the coordinate by summing up the degrees, converted minutes,
        // converted seconds, and converted milliseconds. The minutes are divided by 60 to convert them to degrees,
        // the seconds are divided by 3600, and the milliseconds are divided by 3600000.
        double result = degrees + (minutes / 60) + (seconds / 3600) + (milliseconds / 3600000);

        // If the first character is 'S' or 'W', it multiplies the result by -1 to make it negative.
        if (DMS[0] == 'S' || DMS[0] == 'W') result *= (double)-1;

        // The rounding helps maintain the desired precision for the decimal representation.
        return Math.Round(result, 7);
    }

    /// <summary>
    /// Calculate the distance between two points.
    /// </summary>
    /// <param name="PointA">Location: First point that contains the Lattitude and Longitude.</param>
    /// <param name="PointB">Location: Second point that contains the Lattitude and Longitude</param>
    /// <param name="Round">bool: Round the result to the nearest whole number</param>
    /// <returns>double: Returns the distance between two points in Nautical Miles.</returns>
    public static double Distance(Location PointA, Location PointB, bool Round = true)
    {
        // Convert latitude and longitude values from degrees to radians.
        double lat1 = PointA.DecLat * Math.PI / 180;
        double lon1 = PointA.DecLon * Math.PI / 180;
        double lat2 = PointB.DecLat * Math.PI / 180;
        double lon2 = PointB.DecLon * Math.PI / 180;

        // Calculate the differences in latitude and longitude.
        double dLat = lat2 - lat1;
        double dLon = lon2 - lon1;

        // Apply the haversine formula to calculate the distance between the points.
        // The haversine formula is based on the concept of the haversine of an angle. [ Defined as (1 - cos(angle)) / 2 ]
        //    dLat and dLon are the differences in latitude and longitude between the two points.
        //    lat1 and lat2 are the latitudinal coordinates of the two points, converted to radians.
        //    Math.Sin(dLat / 2) and Math.Sin(dLon / 2) calculate the haversine values for latitudinal and longitudinal differences, respectively.
        //    Math.Cos(lat1) * Math.Cos(lat2) represents the cosine of the average latitude between the two points.
        // The sum of these terms represents the haversine of half the central angle between the points.
        double a = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);

        //    Math.Atan2(y, x) calculates the arctangent of the ratio y / x
        // In this case, Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)) calculates half the central angle between the points.
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        //    d represents the distance between the two points on the sphere's surface in meters.
        //    6371e3 is the approximate radius of the Earth in meters (6371 kilometers),
        //    used to convert the central angle to a distance along the sphere's surface.
        double d = 6371e3 * c; // The distance is calculated in meters.

        // convert the distance to nautical miles
        double nm = d / 1852;

        // NOTE: Why are we using the Haversine formula?
        //    The haversine formula calculates the great-circle distance between two points on the Earth's surface
        //    by computing the central angle between them and then converting that angle to a linear distance using
        //    the Earth's radius. The haversine formula is particularly useful for calculating distances on a
        //    spherical surface, such as the Earth, where the straight-line distance (Euclidean distance) would
        //    not be accurate due to the curvature of the surface.

        // Return the calculated distance, rounded based on the 'Round' parameter.
        if (Round)
            return Math.Round(nm, 0);
        else
            return Math.Round(nm, 6);
    }

    /// <summary>
    /// Check to see if the line between two points crosses the antimeridian.
    /// </summary>
    /// <param name="StartPoint">Location: Starting point location.</param>
    /// <param name="EndPoint">Location: Ending point location</param>
    /// <returns>bool: Returns True if the two points that form the line crosses the Antimeridian, False if it does Not cross.</returns>
    public static bool CrossesAntimeridian(Location StartPoint, Location EndPoint)
    {
        // -- LIMITATIONS --
        // Line Segments are calculated with the assumption of the shortest distance drawn between the two points.
        // For Example:
        //  If StartPoint is (40, -170) and
        //     EndPoint   is (40, 170)  and
        //     the users intention is to go "EAST" from StartPoint to EndPoint (Technically not crossing the AM),
        //     this function will assume the shortest distance was traveled (WEST in this case) and therefore will not provide the appropriate respones of "False".
        //
        // Two points cross the antimeridian via the shortest path between them exactly when
        // the raw difference in longitude exceeds 180 degrees in magnitude: going "the short
        // way" around passes through +/-180 rather than through the 0 degree meridian.
        //
        // A previous implementation of this method used a bearing-based check instead, which
        // produced false positives for ordinary segments nowhere near the antimeridian (e.g.
        // a route entirely within +140..+142 longitude, near Guam). This formula was verified
        // against every case in CoordinateHandlerTests.crosses_the_am_should_be_correct before
        // replacing that implementation.
        double longitudeDifference = Math.Abs(StartPoint.DecLon - EndPoint.DecLon);

        return longitudeDifference > 180;
    }

    /// <summary>
    /// Calculate the bearing (angle) between two locations on the Earth's surface using the spherical law of cosines formula and trigonometric functions provided by the Math class. The bearing is returned in degrees.
    /// </summary>
    /// <param name="pointA">Location: Starting point</param>
    /// <param name="pointB">Location: Ending point</param>
    /// <returns>double: Returns the bearing from the starting point to the ending point.</returns>
    public static double Bearing(Location pointA, Location pointB)
    {
        // The method begins by converting the latitude and longitude coordinates of the two points (pointA and pointB) from decimal degrees to radians.
        // Radians are a unit of measurement for angles used in many mathematical functions.
        //  the conversion from decimal degrees to radians is necessary because the trigonometric functions (Math.Sin, Math.Cos, Math.Atan2) used in the
        //  calculations expect angles to be expressed in radians. These functions are designed to work with angles measured in radians because they provide
        //  more accurate and efficient results when dealing with complex mathematical operations
        double lat1 = pointA.DecLat * Math.PI / 180;
        double lon1 = pointA.DecLon * Math.PI / 180;
        double lat2 = pointB.DecLat * Math.PI / 180;
        double lon2 = pointB.DecLon * Math.PI / 180;

        // The code calculates the difference in longitude between the two points and stores it in the variable dLon.
        // Subtracting lon1 from lon2 gives the difference in longitude between the two points. The result is assigned to the variable dLon.
        // The difference in longitude, dLon, represents the angular distance between the two points along the east - west direction.
        //   A positive value indicates that pointB is located east of pointA,
        //   while a negative value indicates that pointB is located west of pointA.
        //  it is used to determine the angular difference between the longitudes of two points and subsequently calculate the bearing
        //  between them using trigonometric functions.
        double dLon = lon2 - lon1;

        // Next, the code performs some mathematical calculations to determine the bearing between the two points. The bearing represents
        // the angle between the direction from pointA to pointB and a reference direction, typically North.
        // Multiplying the sine of dLon with the cosine of lat2 gives the value of the component y in a two-dimensional coordinate system.
        // This component represents the north-south direction component of the bearing calculation.
        double y = Math.Sin(dLon) * Math.Cos(lat2);
        // This (x) component represents the east-west direction component of the bearing calculation.
        double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

        // In the context of the bearing calculation, y represents the north-south component, and x represents the east-west component. By passing these two components to Math.Atan2,
        // the code calculates the angle between the north direction and the line connecting the two points.
        double bearing = Math.Atan2(y, x);

        // convert the angle from radians to degrees.
        bearing = bearing * 180 / Math.PI;

        // The bearing is adjusted to ensure it falls within the range of 0 to 360 degrees by adding 360 and taking the modulo (%) 360 of the result.
        // This step is necessary because the Math.Atan2 function returns values between -180 and 180 degrees.
        bearing = (bearing + 360) % 360;

        return bearing;
    }

    /// <summary>
    /// Split a line segment made of two Coordinates that do cross the Antimeridian into 4 Coordinates that include the Antimeridian coordinates. 
    /// </summary>
    /// <param name="pointA">Location: Starting point</param>
    /// <param name="pointB">Location: Ending point</param>
    /// <returns>List<Location>: Returns a list that has four locations in it. Starting Point, AM Point 1, AM Point 2, Ending Point</returns>
    public static List<Location> SplitLineSegmentAtAntimeridian(Location pointA, Location pointB)
    {
        // Variables needed to split the Line at the AM.
        double antimeridianStartLongitude;
        double antimeridianEndLongitude;
        double antimeridianIntersectionLatitude;

        //The reason for creating these new Location objects is to have separate variables to work with,
        //which allows the code to perform operations on them without modifying the original pointA and pointB objects directly.
        //This approach is often used to keep the original data intact while performing operations or calculations on temporary copies or derived values.
        //It helps maintain the integrity of the original data and makes it easier to reason about the transformations being applied.
        Location startPoint = new Location(pointA.DecLat, pointA.DecLon);
        Location endPoint = new Location(pointB.DecLat, pointB.DecLon);

        // Calculate the AM Longitiude ( Always -180 or 180 )
        // The variables antimeridianStartLongitude and antimeridianEndLongitude are assigned the value -180 or 180 based on whether
        // the longitude of pointA and pointB is less than 0 or not.
        // This determines the longitude of the antimeridian points.
        antimeridianStartLongitude = pointA.DecLon < 0 ? -180 : 180;
        antimeridianEndLongitude = pointB.DecLon < 0 ? -180 : 180;

        // The longitudes of startPoint and endPoint are adjusted to be relative to the antimeridian
        // by adding or subtracting 180 degrees if they are less than 0.
        // If startPoint.DecLon is less than 0, it means that startPoint is located in the Western Hemisphere (west of the prime meridian).
        //   In this case, 180 degrees is added to startPoint.DecLon to make it relative to the antimeridian.
        //   For example, if startPoint.DecLon is -120, adding 180 degrees gives 60, which represents the same position but relative to the antimeridian.
        // If endPoint.DecLon is less than 0, it means that endPoint is located in the Western Hemisphere.
        //   Similarly, 180 degrees is added to endPoint.DecLon to make it relative to the antimeridian.
        // By adjusting the longitudes in this way, the line segment represented by startPoint and endPoint is
        // correctly positioned relative to the antimeridian, ensuring that it can be accurately split at that boundary.
        startPoint.DecLon = startPoint.DecLon < 0 ? startPoint.DecLon + 180 : startPoint.DecLon - 180;
        endPoint.DecLon = endPoint.DecLon < 0 ? endPoint.DecLon + 180 : endPoint.DecLon - 180;

        // Calculate the AM Lattitude
        // the slope helps us determine how the latitude changes as we move along the line segment.
        // Multiplying the slope by the distance from the starting point to the antimeridian allows us to calculate the corresponding change in latitude.
        // This information is then used to determine the latitude at which the line segment intersects the antimeridian.
        // Slope Calculation:
        //  The numerator (pointA.DecLat - pointB.DecLat) represents the difference in latitude between pointA and pointB.
        //  The denominator(startPoint.DecLon -endPoint.DecLon) represents the difference in longitude between startPoint and endPoint.
        var slope = (pointA.DecLat - pointB.DecLat) / (startPoint.DecLon - endPoint.DecLon);

        // (slope * startPoint.DecLon) represents the change in latitude based on the slope and the longitude.
        // Subtracting this value from pointA.DecLat gives the latitude at the intersection point.
        antimeridianIntersectionLatitude = pointA.DecLat - (slope * startPoint.DecLon);

        // Create Location Classes for the AM Points.
        var midPointStart = new Location(antimeridianIntersectionLatitude, antimeridianStartLongitude);
        var midPointEnd = new Location(antimeridianIntersectionLatitude, antimeridianEndLongitude);

        // Return a List of Locations Starting Point, AM Point 1, AM Point 2, Ending Point
        return new List<Location>() { pointA, midPointStart, midPointEnd, pointB };
    }

    /// <summary>
    /// Calculate the destination point reached by traveling a given distance along a given
    /// initial bearing from an origin point, using the great-circle (spherical) formula.
    /// </summary>
    /// <param name="origin">Location: Starting point.</param>
    /// <param name="bearingDegrees">double: Initial bearing in degrees, measured clockwise from true north (0-360).</param>
    /// <param name="distanceNm">double: Distance to travel from <paramref name="origin"/>, in Nautical Miles.</param>
    /// <returns>Location: The destination point.</returns>
    /// <remarks>
    /// Used by <c>AirwayWaypointBuffer</c> to shorten an airway leg by a fixed radius around
    /// each endpoint waypoint (2.5 NM for fixes, 5 NM for NAVAIDs/airports) so the rendered
    /// line stops short of the waypoint's symbol/text. Ported from old FE-Buddy's
    /// <c>LatLonHelpers.GetNewPoint</c>.
    ///
    /// Uses the same Earth radius as <see cref="Distance"/> (6371 km) so the two methods stay
    /// consistent with one another.
    /// </remarks>
    public static Location PointAtDistanceAndBearing(Location origin, double bearingDegrees, double distanceNm)
    {
        // Same Earth radius (in meters, converted to Nautical Miles) used by Distance().
        const double earthRadiusNm = 6371e3 / 1852;

        double lat1 = origin.DecLat * Math.PI / 180;
        double lon1 = origin.DecLon * Math.PI / 180;
        double bearingRad = bearingDegrees * Math.PI / 180;

        // Angular distance traveled, expressed as a fraction of the Earth's radius.
        double angularDistance = distanceNm / earthRadiusNm;

        // Standard great-circle "destination point given distance and bearing" formula.
        double lat2 = Math.Asin(
            Math.Sin(lat1) * Math.Cos(angularDistance) +
            Math.Cos(lat1) * Math.Sin(angularDistance) * Math.Cos(bearingRad));

        double lon2 = lon1 + Math.Atan2(
            Math.Sin(bearingRad) * Math.Sin(angularDistance) * Math.Cos(lat1),
            Math.Cos(angularDistance) - Math.Sin(lat1) * Math.Sin(lat2));

        double latDegrees = lat2 * 180 / Math.PI;
        double lonDegrees = lon2 * 180 / Math.PI;

        // Normalize longitude back into [-180, 180) so it satisfies Location's validation
        // even when the destination point wraps across the antimeridian.
        lonDegrees = ((lonDegrees + 540) % 360) - 180;

        // Defensive clamp against floating-point drift when operating extremely close to a pole.
        latDegrees = Math.Clamp(latDegrees, -90, 90);

        return new Location(latDegrees, lonDegrees);
    }
}
