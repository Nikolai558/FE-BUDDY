using FEBuddyLibrary.Models.Location;
using Microsoft.VisualBasic;
using System.Diagnostics.Metrics;
using System.Drawing;
using System;
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
  /// <param name="Lat">double: Latitude</param>
  /// <param name="Lon">double: Longitude</param>
  /// <returns>bool: Returns true if the Latitude AND Longitude are valid Decimals, otherwise returns False.</returns>
  public static bool IsValidDecimal(double Lat, double Lon)
  {
    if (Lat > 90 || Lat < -90) return false;
    if (Lon > 180 || Lon < -180) return false;
    return true;
  }

  /// <summary>
  /// Check a DMS Latitude and Longitude to see if they are valid.
  /// </summary>
  /// <param name="Lat">string: Latitude - Format: ['N', 'S']DDD.MM.SS.SSS</param>
  /// <param name="Lon">string: Longitude - Format: ['E', 'W']DDD.MM.SS.SSS</param>
  /// <returns>bool: Returns True if the Latitude AND Longitude are valid DMS formats, otherwise returns False.</returns>
  public static bool IsValidDMS(string Lat, string Lon)
  {
    if (Lat == null || Lon == null) return false;
    if (Lat == "" || Lon == "") return false;
    if (Lat.Split('.').Count() != 4 || Lon.Split('.').Count() != 4) return false;
    if (Lat[0] != 'N' && Lat[0] != 'S') return false;
    if (Lon[0] != 'E' && Lon[0] != 'W') return false;

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
    // get the hemisphere (N, S, E, or W)
    string hemisphere = "";
    if (IsLat && Dec < 0) hemisphere = "S";
    if (IsLat && Dec >= 0) hemisphere = "N";
    if (!IsLat && Dec < 0) hemisphere = "W";
    if (!IsLat && Dec >= 0) hemisphere = "E";
    // get the degrees, minutes, and seconds
    double absLat = Math.Abs(Dec);
    double degrees = Math.Floor(absLat);
    double minutes = Math.Floor((absLat - degrees) * 60);
    double seconds = Math.Round((absLat - degrees - minutes / 60) * 3600, 3);
    double miliseconds = (seconds - Math.Floor(seconds)) * 1000;
    // put it all together
    string dms = hemisphere + degrees.ToString("000") + "." + minutes.ToString("00") + "." + seconds.ToString("00") + "." + miliseconds.ToString("000");
    return dms;
  }

  /// <summary>
  /// Converts DMS format to Decimal format.
  /// </summary>
  /// <param name="DMS">string: Latitude or Longitude - Format: ['N', 'S', 'E', 'W']DDD.MM.SS.SSS</param>
  /// <returns>double: Returns the decimal version of the Latitude or Longitude passed in.</returns>
  public static double ToDecimal(string DMS)
  {
    string[] parts = DMS.Split('.');
    double degrees = double.Parse(parts[0][1..]);
    double minutes = double.Parse(parts[1]);
    double seconds = double.Parse(parts[2]);
    double milliseconds = double.Parse(parts[3]);
    double result = degrees + (minutes / 60) + (seconds / 3600) + (milliseconds / 3600000);
    if (DMS[0] == 'S' || DMS[0] == 'W') result *= (double)-1;
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
    // convert the points to radians
    double lat1 = PointA.DecLat * Math.PI / 180;
    double lon1 = PointA.DecLon * Math.PI / 180;
    double lat2 = PointB.DecLat * Math.PI / 180;
    double lon2 = PointB.DecLon * Math.PI / 180;
    // get the differences between the two points
    double dLat = lat2 - lat1;
    double dLon = lon2 - lon1;
    // do some math
    double a = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);
    double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    double d = 6371e3 * c;
    // convert the distance to nautical miles
    double nm = d / 1852;
    // return the result
    if (Round) return Math.Round(nm, 0);
    else return Math.Round(nm, 6);
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
    
    const double Antimeridian = 180;
    
    // The code then checks if either of the following conditions is true:
    //   a.If StartPoint.DecLon(the decimal longitude of the start point) is less than Antimeridian and
    //     EndPoint.DecLon(the decimal longitude of the end point) is greater than - Antimeridian.
    //     This condition checks if the line segment spans from a longitude before the antimeridian to a longitude after it.
    //   b.If StartPoint.DecLon is greater than - Antimeridian and EndPoint.DecLon is less than Antimeridian.
    //     This condition checks if the line segment spans from a longitude after the antimeridian to a longitude before it.
    // If either of these conditions is true, it means the line segment crosses the antimeridian.
    if ((StartPoint.DecLon < Antimeridian && EndPoint.DecLon > -Antimeridian) || (StartPoint.DecLon > -Antimeridian && EndPoint.DecLon < Antimeridian))
    {
      // The bearing represents the angle between the north direction and the direction from the start point to the end point.
      var bearing = Bearing(StartPoint, EndPoint);

      // Then, the code checks if either of the following conditions is true:
      //   a.If StartPoint.DecLon is less than 0(meaning the start point is west of the prime meridian) and
      //     the bearing is greater than 0 and less than 180.This condition checks if the line segment crosses
      //     the meridian line(the prime meridian at 0 degrees longitude) but not the antimeridian.
      //   b.If StartPoint.DecLon is greater than 0(meaning the start point is east of the prime meridian) and
      //     the bearing is greater than 180 and less than 360.This condition checks if the line segment crosses
      //     the meridian line but not the antimeridian.
      // If either of these conditions is true, it means the line segment crosses the meridian line but not the antimeridian.
      if ((StartPoint.DecLon < 0 && (bearing > 0 && bearing < 180)) || (StartPoint.DecLon > 0 && (bearing > 180 && bearing < 360)))
      {
        // If the line segment crosses the meridian line but not the antimeridian, the method returns false.
        return false;
      }

      // If none of the above conditions are true, the method returns true to indicate that the line segment crosses the antimeridian.
      return true;
    }
    else
    {
      // If the initial condition above is false, meaning the line segment does not span across the antimeridian, the method returns false as well.
      return false;
    }
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
    return new List<Location>() { pointA, midPointStart, midPointEnd, pointB};
  }
}
