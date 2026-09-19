namespace FeBuddy.Core.Models.Airports;
/// <summary>
/// Represents a weather station used for collecting meteorological data.
/// </summary>
public class WxStation
{
    /// <summary>
    /// Gets or sets the identifier of the weather station's sensor.
    /// </summary>
    public string SensorIdent { get; set; }

    /// <summary>
    /// Gets or sets the type of the weather station's sensor.
    /// </summary>
    public string SensorType { get; set; }

    /// <summary>
    /// Gets or sets the commissioning status of the weather station.
    /// </summary>
    public string CommisioningStatus { get; set; }

    /// <summary>
    /// Gets or sets the Navaid flag of the weather station, indicating its relationship with a navigation aid (Navaid).
    /// </summary>
    public string NavaidFlag { get; set; }

    /// <summary>
    /// Gets or sets the location of the weather station represented by a <see cref="Location.Location"/> object.
    /// </summary>
    public Location.Location Location { get; set; }

    /// <summary>
    /// Gets or sets the elevation of the weather station's location.
    /// </summary>
    public string Elevation { get; set; }

    /// <summary>
    /// Gets or sets the primary frequency used by the weather station for communication.
    /// </summary>
    public string StationFrequency { get; set; }

    /// <summary>
    /// Gets or sets the secondary frequency used by the weather station for communication.
    /// </summary>
    public string SecondStationFrequency { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WxStation"/> class with the specified parameters.
    /// </summary>
    /// <param name="ident">The identifier of the weather station's sensor.</param>
    /// <param name="type">The type of the weather station's sensor.</param>
    /// <param name="commisioningStatus">The commissioning status of the weather station.</param>
    /// <param name="navaidFlag">The Navaid flag of the weather station, indicating its relationship with a navigation aid (Navaid).</param>
    /// <param name="lat">The latitude of the weather station's location.</param>
    /// <param name="lon">The longitude of the weather station's location.</param>
    /// <param name="elevation">The elevation of the weather station's location.</param>
    /// <param name="freq">The primary frequency used by the weather station for communication.</param>
    /// <param name="scondFreq">The secondary frequency used by the weather station for communication.</param>
    public WxStation(string ident, string type, string commisioningStatus, string navaidFlag, double lat, double lon, string elevation, string freq, string scondFreq)
    {
        SensorIdent = ident;
        SensorType = type;
        CommisioningStatus = commisioningStatus;
        NavaidFlag = navaidFlag;
        Location = new Location.Location(lat, lon);
        Elevation = elevation;
        StationFrequency = freq;
        SecondStationFrequency = scondFreq;
    }
}
