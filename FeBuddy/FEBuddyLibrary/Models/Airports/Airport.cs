namespace FEBuddyLibrary.Models.Airports;
/// <summary>
/// Represents an airport in the aviation domain.
/// </summary>
public class Airport
{
    /// <summary>
    /// Gets or sets the type of the airport.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets the FAA identifier of the airport.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the airport.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the location of the airport represented by a <see cref="Location.Location"/> object.
    /// </summary>
    public Location.Location Location { get; set; }

    /// <summary>
    /// Gets or sets the elevation of the airport.
    /// </summary>
    public string Elevation { get; set; }

    /// <summary>
    /// Gets or sets the Air Route Traffic Control Center (ARTCC) responsible for the airport's airspace.
    /// </summary>
    public string ResponsibleArtcc { get; set; }

    /// <summary>
    /// Gets or sets the status of the airport (e.g., open, closed).
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Gets or sets whether the airport has a control tower (e.g., yes, no).
    /// </summary>
    public string Tower { get; set; }

    /// <summary>
    /// Gets or sets the Common Traffic Advisory Frequency (CTAF) used by the airport for non-controlled communication.
    /// </summary>
    public string Ctaf { get; set; }

    /// <summary>
    /// Gets or sets the International Civil Aviation Organization (ICAO) code of the airport.
    /// </summary>
    public string Icao { get; set; }

    /// <summary>
    /// Gets or sets the magnetic variation of the airport's location.
    /// </summary>
    public string MagneticVariation { get; set; }

    /// <summary>
    /// Gets or sets the list of runways available at the airport.
    /// </summary>
    public List<Runways.Runway> Runways { get; set; }

    /// <summary>
    /// Gets or sets the weather station associated with the airport represented by a <see cref="WxStation"/> object.
    /// </summary>
    public WxStation WeatherStation { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Airport"/> class with the specified parameters.
    /// </summary>
    /// <param name="type">The type of the airport.</param>
    /// <param name="id">The identifier of the airport.</param>
    /// <param name="name">The name of the airport.</param>
    /// <param name="location">The location of the airport represented by a <see cref="Location.Location"/> object.</param>
    /// <param name="elevation">The elevation of the airport.</param>
    /// <param name="responsibleArtcc">The Air Route Traffic Control Center (ARTCC) responsible for the airport's airspace.</param>
    /// <param name="status">The status of the airport (e.g., open, closed).</param>
    /// <param name="tower">Whether the airport has a control tower (e.g., yes, no).</param>
    /// <param name="ctaf">The Common Traffic Advisory Frequency (CTAF) used by the airport for non-controlled communication.</param>
    /// <param name="icao">The International Civil Aviation Organization (ICAO) code of the airport.</param>
    /// <param name="magneticVariation">The magnetic variation of the airport's location.</param>
    /// <param name="runways">The list of runways available at the airport.</param>
    /// <param name="weatherStation">The weather station associated with the airport represented by a <see cref="WxStation"/> object.</param>
    public Airport(string type, string id, string name, double lat, double lon, string elevation, string responsibleArtcc, string status, string tower, string ctaf, string icao, string magneticVariation, List<Runways.Runway> runways, WxStation weatherStation)
    {
        Type = type;
        Id = id;
        Name = name;
        Location = new Location.Location(lat, lon);
        Elevation = elevation;
        ResponsibleArtcc = responsibleArtcc;
        Status = status;
        Tower = tower;
        Ctaf = ctaf;
        Icao = icao;
        MagneticVariation = magneticVariation;
        Runways = runways;
        WeatherStation = weatherStation;
    }
}
