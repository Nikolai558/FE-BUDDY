namespace FeBuddy.Core.Models.Navaids;

/// <summary>
/// Represents a VHF Omni-directional Range (VOR) radio navigation aid.
/// This class extends the <see cref="NavaidBase"/> class and provides specific properties for VORs.
/// </summary>
public class VOR : NavaidBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VOR"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The identifier of the VOR.</param>
    /// <param name="frequency">The frequency of the VOR.</param>
    /// <param name="name">The name of the VOR.</param>
    /// <param name="type">The type of the VOR.</param>
    /// <param name="lat">The latitude of the VOR's location.</param>
    /// <param name="lon">The longitude of the VOR's location.</param>
    public VOR(string id, string frequency, string name, string type, double lat, double lon)
    {
        Id = id;
        Frequency = frequency;
        Name = name;
        Type = type;
        Location = new Location.Location(lat, lon);
    }
}
