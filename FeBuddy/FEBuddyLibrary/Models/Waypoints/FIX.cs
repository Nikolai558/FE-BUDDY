namespace FEBuddyLibrary.Models.Waypoints;

/// <summary>
/// Represents a navigation fix in the aviation domain.
/// A fix is a geographical point used for navigation and defined by specific coordinates.
/// </summary>
public class FIX
{
    /// <summary>
    /// Gets or sets the identifier of the navigation fix.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the category of the navigation fix, if applicable.
    /// Categories can be used to classify fixes based on their usage or significance.
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    /// Gets or sets the intended use of the navigation fix.
    /// Describes the purpose or function of the fix in the navigation system.
    /// </summary>
    public string Use { get; set; }

    /// <summary>
    /// Gets or sets the High Altitude ARTCC (Air Route Traffic Control Center) associated with the fix, if applicable.
    /// </summary>
    public string HiArtcc { get; set; }

    /// <summary>
    /// Gets or sets the Low Altitude ARTCC (Air Route Traffic Control Center) associated with the fix, if applicable.
    /// </summary>
    public string LowArtcc { get; set; }

    /// <summary>
    /// Gets or sets the location of the navigation fix represented by a <see cref="Location.Location"/> object.
    /// </summary>
    public Location.Location Location { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FIX"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The identifier of the navigation fix.</param>
    /// <param name="category">The category of the navigation fix, if applicable.</param>
    /// <param name="use">The intended use of the navigation fix.</param>
    /// <param name="hiArtcc">The High Altitude ARTCC (Air Route Traffic Control Center) associated with the fix, if applicable.</param>
    /// <param name="lowArtcc">The Low Altitude ARTCC (Air Route Traffic Control Center) associated with the fix, if applicable.</param>
    /// <param name="lat">The latitude of the navigation fix's location.</param>
    /// <param name="lon">The longitude of the navigation fix's location.</param>
    public FIX(string id, string category, string use, string hiArtcc, string lowArtcc, double lat, double lon)
    {
        Id = id;
        Category = category;
        Use = use;
        HiArtcc = hiArtcc;
        LowArtcc = lowArtcc;
        Location = new Location.Location(lat, lon);
    }
}
