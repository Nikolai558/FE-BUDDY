namespace FEBuddyLibrary.Models.Waypoints;

/// <summary>
/// Represents boundary points with location and descriptive information.
/// </summary>
public class BoundaryPoints
{
    /// <summary>
    /// Gets or sets the unique identifier for the boundary point.
    /// </summary>
    public string Identifier { get; set; }

    /// <summary>
    /// Gets or sets the name of the center associated with the boundary point.
    /// </summary>
    public string CenterName { get; set; }

    /// <summary>
    /// Gets or sets the decoded name of the boundary point.
    /// </summary>
    public string DecodedName { get; set; }

    /// <summary>
    /// Gets or sets the geographic location of the boundary point.
    /// </summary>
    public Location.Location Location { get; set; }

    /// <summary>
    /// Gets or sets the description of the boundary point.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the sequence information related to the boundary point.
    /// </summary>
    public string Sequence { get; set; }

    /// <summary>
    /// Gets or sets the legal information associated with the boundary point.
    /// </summary>
    public string Legal { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating whether to move to the beginning after this boundary point.
    /// </summary>
    public bool ToBeginningAfterThis { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoundaryPoints"/> class.
    /// </summary>
    /// <param name="identifier">The unique identifier for the boundary point.</param>
    /// <param name="centerName">The name of the center associated with the boundary point.</param>
    /// <param name="decodedName">The decoded name of the boundary point.</param>
    /// <param name="latitude">The latitude of the boundary point's geographic location.</param>
    /// <param name="longitude">The longitude of the boundary point's geographic location.</param>
    /// <param name="description">The description of the boundary point.</param>
    /// <param name="sequence">The sequence information related to the boundary point.</param>
    /// <param name="legal">The legal information associated with the boundary point.</param>
    /// <param name="returnToBeginning">A flag indicating whether to move to the beginning after this boundary point.</param>
    public BoundaryPoints(string identifier, string centerName, string decodedName, double latitude, double longitude, string description, string sequence, string legal, bool returnToBeginning)
    {
        Identifier = identifier;
        CenterName = centerName;
        DecodedName = decodedName;
        Location = new Location.Location(latitude, longitude);
        Description = description;
        Sequence = sequence;
        Legal = legal;
        ToBeginningAfterThis = returnToBeginning;
    }
}