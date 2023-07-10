using FEBuddyLibrary.Handlers;
using System.Text.Json;

namespace FEBuddyLibrary.Models.Location;

/// <summary>
/// A model for a single point on the earth containing both the Decimal format and DMS format for Latitude and Longitude.
/// </summary>
public class Location
{
  // ---------- Properties ---------- 
  public string DmsLat { get { return DmsLat; } set { SetDmsLat(value); }}
  public string DmsLon { get { return DmsLon; } set { SetDmsLon(value); } }
  public double DecLat { get { return DecLat; } set { SetDecLat(value); } }
  public double DecLon { get { return DecLon; } set { SetDecLon(value); } }

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
      return;
    }
    throw new ArgumentException("Invalid decimal input when creating a Location class.");
  }

  // ---------- Setter Functions ---------- 

  /// <summary>
  /// Set the DMS Lattitude property of the this Location Class. Will update the Decimal Version to reflect new DMS passed in.
  /// </summary>
  /// <param name="value">string: Latitude DMS - Format: ['N', 'S']DDD.MM.SS.SSS</param>
  private void SetDmsLat(string value)
  {
    DmsLat = value;
    DecLat = (double)CoordinateHandler.ToDecimal(value);
  }

  /// <summary>
  /// Set the DMS Longitidue property of the this Location Class. Will update the Decimal Version to reflect new DMS passed in.
  /// </summary>
  /// <param name="value">string: Longitude DMS - Format: ['E', 'W']DDD.MM.SS.SSS</param>
  private void SetDmsLon(string value)
  {
    DmsLon = value;
    DecLon = (double)CoordinateHandler.ToDecimal(value);
  }

  /// <summary>
  /// Set the Decimal Lattitude property of the this Location Class. Will update the DMS Version to reflect new Decimal passed in.
  /// </summary>
  /// <param name="value">double: Latitude Decimal format.</param>
  private void SetDecLat(double value)
  {
    DecLat = value;
    DmsLat = CoordinateHandler.ToDMS(value, true);
  }

  /// <summary>
  /// Set the Decimal Longitude property of the this Location Class. Will update the DMS Version to reflect new Decimal passed in.
  /// </summary>
  /// <param name="value">double: Longitude Decimal format.</param>
  private void SetDecLon(double value)
  {
    DecLon = value;
    DmsLon = CoordinateHandler.ToDMS(value, false);
  }


}
