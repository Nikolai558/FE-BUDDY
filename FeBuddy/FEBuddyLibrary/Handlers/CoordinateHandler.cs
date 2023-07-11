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
    const double Antimeridian = 180;

    if ((StartPoint.DecLon < Antimeridian && EndPoint.DecLon > -Antimeridian) || (StartPoint.DecLon > -Antimeridian && EndPoint.DecLon < Antimeridian))
    {
      var bearing = Bearing(StartPoint, EndPoint);

      if ((StartPoint.DecLon < 0 && (bearing > 0 && bearing < 180)) || (StartPoint.DecLon > 0 && (bearing > 180 && bearing < 360)))
      {
        // We Cross the Meridian Line but not the antimeridian.
        return false;
      }

      return true;
    }
    else
    {
      return false;
    }
  }

  /// <summary>
  /// Get the bearing between two points.
  /// </summary>
  /// <param name="pointA">Location: Starting point</param>
  /// <param name="pointB">Location: Ending point</param>
  /// <returns>double: Returns the bearing from the starting point to the ending point.</returns>
  public static double Bearing(Location pointA, Location pointB)
  {
    // convert the points to radians
    double lat1 = pointA.DecLat * Math.PI / 180;
    double lon1 = pointA.DecLon * Math.PI / 180;
    double lat2 = pointB.DecLat * Math.PI / 180;
    double lon2 = pointB.DecLon * Math.PI / 180;
    // get the differences between the two points
    double dLon = lon2 - lon1;
    // do some math
    double y = Math.Sin(dLon) * Math.Cos(lat2);
    double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
    // get the bearing
    double bearing = Math.Atan2(y, x);
    bearing = bearing * 180 / Math.PI;
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
    Location startLocation = new Location(pointA.DecLat, pointA.DecLon);
    Location endLocation = new Location(pointB.DecLat, pointB.DecLon);
    double midPointStartLon;
    double midPointEndLon;
    double midPointLat;
    
    // Calculate the AM Longitiude ( Always -180 or 180 )
    midPointStartLon = pointA.DecLon < 0 ? -180 : 180;
    midPointEndLon = pointB.DecLon < 0 ? -180 : 180;

    startLocation.DecLon = startLocation.DecLon < 0 ? startLocation.DecLon + 180 : startLocation.DecLon - 180;
    endLocation.DecLon = endLocation.DecLon < 0 ? endLocation.DecLon + 180 : endLocation.DecLon - 180;

    // Calculate the AM Lattitude
    var slope = (pointA.DecLat - pointB.DecLat) / (startLocation.DecLon - endLocation.DecLon);
    midPointLat = pointA.DecLat - (slope * startLocation.DecLon);

    // Create Location Classes for the AM Points.
    var midPointStart = new Location(midPointLat, midPointStartLon);
    var midPointEnd = new Location(midPointLat, midPointEndLon);

    // Return a List of Locations Starting Point, AM Point 1, AM Point 2, Ending Point
    return new List<Location>() { pointA, midPointStart, midPointEnd, pointB};
  }
}
