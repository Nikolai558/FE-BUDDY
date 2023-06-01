using System.Runtime;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnitTests")]
namespace FEBuddyLibrary.Models.Location;

/// <summary>
/// A model for a single point on the earth containing both the Decimal format and DMS format for Latitude and Longitude.
/// </summary>
public class Location
{
    // ---------- Properties ---------- 
    public string DmsLat { get; private set; }
    public string DmsLon { get; private set; }
    public double DecLat { get; private set; }
    public double DecLon { get; private set; }

    // ---------- Constructors ---------- 
    /// <summary>
    /// Create a Location class with a DMS Latitude and Longitude. Decimals are calculated from the DMS input.
    /// </summary>
    /// <param name="Lat">string: Latitude DMS - Format: ['N', 'S']DDD.MM.SS.SSS</param>
    /// <param name="Lon">string: Longitude DMS - Format: ['E', 'W']DDD.MM.SS.SSS</param>
    /// <exception cref="ArgumentException">Throws an ArgumentException if either params are not in the correct format.</exception>
    public Location(string Lat, string Lon)
    {
		bool isValid = IsValidDMS(Lat, Lon);
		if (isValid)
		{
			DmsLat = Lat;
			DmsLon = Lon;
			DecLat = (double)ToDecimal(Lat);
			DecLon = (double)ToDecimal(Lon);
			return;
		}
		throw new ArgumentException("Invalid DMS input when creating a Location class.");
    }

    /// <summary>
    /// Create a Location class with a Decimal Latitude and Longitude. DMS is calculated from the Decimal input.
    /// </summary>
    /// <param name="Lat">double: Latitude Decimal format.</param>
    /// <param name="Lon">double: Longitude Decimal format.</param>
    /// <exception cref="ArgumentException"></exception>
    public Location(double Lat, double Lon)
    {
		bool isValid = IsValidDecimal(Lat, Lon);
		if (isValid)
		{
			DecLat = Lat;
			DecLon = Lon;
			DmsLat = ToDMS(Lat, true);
			DmsLon = ToDMS(Lon, false);
			return;
		}
		throw new ArgumentException("Invalid decimal input when creating a Location class.");
    }

    // ---------- Methods ---------- 
    /// <summary>
    /// Check a Decimal Latitude and Longitude to see if they are valid.
    /// </summary>
    /// <param name="Lat">double: Latitude</param>
    /// <param name="Lon">double: Longitude</param>
    /// <returns>bool: Returns true if the Latitude AND Longitude are valid Decimals, otherwise returns False.</returns>
    internal static bool IsValidDecimal(double Lat, double Lon)
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
    internal static bool IsValidDMS(string Lat, string Lon)
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
    internal static string ToDMS(double Dec, bool IsLat)
    {
		// returns a string in the form DDD.MM.SS.SSS
		// where D = degrees, M = minutes, S = seconds
		// example: 43.518948 becomes N043.31.08.418
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
	internal static double ToDecimal(string DMS)
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
}
