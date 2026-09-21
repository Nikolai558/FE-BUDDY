using FeBuddy.Core.Handlers.General;

namespace FeBuddy.Core.Models.Location;

/// <summary>
/// A model for a single point on the earth containing both the Decimal format and DMS format for Latitude and Longitude.
/// </summary>
public class Location
{
    // ---------- Properties ---------- 
    private string dmsLat;
    private string dmsLon;
    private double decLat;
    private double decLon;


    public string DmsLat { get { return dmsLat; } set { SetDmsLat(value); } }
    public string DmsLon { get { return dmsLon; } set { SetDmsLon(value); } }
    public double DecLat { get { return decLat; } set { SetDecLat(value); } }
    public double DecLon { get { return decLon; } set { SetDecLon(value); } }

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
    internal void SetDmsLat(string value)
    {
        dmsLat = value;
        decLat = (double)CoordinateHandler.ToDecimal(value);
    }

    /// <summary>
    /// Set the DMS Longitidue property of the this Location Class. Will update the Decimal Version to reflect new DMS passed in.
    /// </summary>
    /// <param name="value">string: Longitude DMS - Format: ['E', 'W']DDD.MM.SS.SSS</param>
    internal void SetDmsLon(string value)
    {
        dmsLon = value;
        decLon = (double)CoordinateHandler.ToDecimal(value);
    }

    /// <summary>
    /// Set the Decimal Lattitude property of the this Location Class. Will update the DMS Version to reflect new Decimal passed in.
    /// </summary>
    /// <param name="value">double: Latitude Decimal format.</param>
    internal void SetDecLat(double value)
    {
        decLat = value;
        dmsLat = CoordinateHandler.ToDMS(value, true);
    }

    /// <summary>
    /// Set the Decimal Longitude property of the this Location Class. Will update the DMS Version to reflect new Decimal passed in.
    /// </summary>
    /// <param name="value">double: Longitude Decimal format.</param>
    internal void SetDecLon(double value)
    {
        decLon = value;
        dmsLon = CoordinateHandler.ToDMS(value, false);
    }

    // ---------- Overrided Functions ---------- 
    /// <summary>
    /// Overide function to compare two Location classes. Will return true if both DMS and Decimal values are the same.
    /// </summary>
    /// <param name="obj">Location: Other Location Class to compare too</param>
    /// <returns>bool: Returns True if both Location Classes are the same.</returns>
    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType()) return false;

        Location other = (Location)obj;
        return dmsLat == other.dmsLat && dmsLon == other.dmsLon && decLat == other.decLat && decLon == other.decLon;
    }

    /// <summary>
    /// Overide function to return a HashCode for this Location class.
    /// </summary>
    /// <returns>int: Returns Location Class Hash Code. </returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(dmsLat, dmsLon, decLat, decLon);
    }
}
