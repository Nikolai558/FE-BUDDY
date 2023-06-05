using FEBuddyLibrary.Handlers;

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

    bool isValid = CoordinateHandler.IsValidDMS(Lat, Lon);
    if (isValid)
    {
      // Possible Optimization - Do we really need BOTH DMS and DEC at Location Creation Time?
      DmsLat = Lat;
      DmsLon = Lon;
      DecLat = (double)CoordinateHandler.ToDecimal(Lat);
      DecLon = (double)CoordinateHandler.ToDecimal(Lon);
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
    bool isValid = CoordinateHandler.IsValidDecimal(Lat, Lon);
    if (isValid)
    {
      // Possible Optimization - Do we really need BOTH DMS and DEC at Location Creation Time?
      DecLat = Lat;
      DecLon = Lon;
      DmsLat = CoordinateHandler.ToDMS(Lat, true);
      DmsLon = CoordinateHandler.ToDMS(Lon, false);
      return;
    }
    throw new ArgumentException("Invalid decimal input when creating a Location class.");
  }


}
