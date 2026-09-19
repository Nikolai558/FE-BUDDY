namespace FeBuddy.Core.Models.Navaids;

/// <summary>
/// Represents a Non-Directional Beacon (NDB) radio navigation aid.
/// This class extends the <see cref="NavaidBase"/> class and provides specific properties for NDBs.
/// </summary>
public class NDB : NavaidBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NDB"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The identifier of the NDB.</param>
    /// <param name="frequency">The frequency of the NDB.</param>
    /// <param name="name">The name of the NDB.</param>
    /// <param name="type">The type of the NDB.</param>
    /// <param name="lat">The latitude of the NDB's location.</param>
    /// <param name="lon">The longitude of the NDB's location.</param>
    public NDB(string id, string frequency, string name, string type, double lat, double lon)
    {
        Id = id;
        Frequency = frequency;
        Name = name;
        Type = type;
        Location = new Location.Location(lat, lon);
    }
}
