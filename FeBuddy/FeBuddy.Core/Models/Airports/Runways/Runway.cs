namespace FeBuddy.Core.Models.Airports.Runways;

/// <summary>
/// Represents a runway used in aviation for aircraft takeoff and landing.
/// A runway consists of a base portion and, optionally, a reciprocal portion.
/// </summary>
public class Runway
{
    /// <summary>
    /// Gets or sets the group identifier of the runway, which may include both the base and reciprocal runway designators.
    /// </summary>
    public string Group { get; set; }

    /// <summary>
    /// Gets the base portion of the runway identifier extracted from the 'Group'.
    /// </summary>
    public string Base { get { return Group.Split('/')[0]; } }

    /// <summary>
    /// Gets the reciprocal portion of the runway identifier extracted from the 'Group'.
    /// If the reciprocal portion is not present, an empty string is returned.
    /// </summary>
    public string Reciprocal { get { return Group.Split('/').Length == 2 ? Group.Split('/')[1] : ""; } }

    /// <summary>
    /// Gets or sets the length of the runway.
    /// </summary>
    public string Length { get; set; }

    /// <summary>
    /// Gets or sets the width of the runway.
    /// </summary>
    public string Width { get; set; }

    /// <summary>
    /// Gets or sets the starting location of the base portion of the runway represented by a <see cref="Location.Location"/> object.
    /// </summary>
    public Location.Location BaseStartLocation { get; set; }

    /// <summary>
    /// Gets or sets the ending location of the base portion of the runway represented by a <see cref="Location.Location"/> object.
    /// </summary>
    public Location.Location BaseEndLocation { get; set; }

    /// <summary>
    /// Gets the starting location of the reciprocal portion of the runway.
    /// The reciprocal start location is the same as the base end location.
    /// </summary>
    public Location.Location ReciprocalStartLocation { get { return BaseEndLocation; } }

    /// <summary>
    /// Gets the ending location of the reciprocal portion of the runway.
    /// The reciprocal end location is the same as the base start location.
    /// </summary>
    public Location.Location ReciprocalEndLocation { get { return BaseStartLocation; } }

    /// <summary>
    /// Gets or sets the magnetic heading of the base portion of the runway.
    /// </summary>
    public string BaseHeading { get; set; }

    /// <summary>
    /// Gets or sets the magnetic heading of the reciprocal portion of the runway.
    /// </summary>
    public string ReciprocalHeading { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Runway"/> class with the specified parameters.
    /// </summary>
    /// <param name="group">The group identifier of the runway, which may include both the base and reciprocal runway designators.</param>
    /// <param name="length">The length of the runway.</param>
    /// <param name="width">The width of the runway.</param>
    /// <param name="baseStartLat">The latitude of the starting location of the base portion of the runway.</param>
    /// <param name="baseStartLon">The longitude of the starting location of the base portion of the runway.</param>
    /// <param name="baseHeading">The magnetic heading of the base portion of the runway.</param>
    /// <param name="reciprocalHeading">The magnetic heading of the reciprocal portion of the runway.</param>
    public Runway(string group, string length, string width, double baseStartLat, double baseStartLon, string baseHeading, string reciprocalHeading)
    {
        Group = group;
        Length = length;
        Width = width;
        BaseStartLocation = new Location.Location(baseStartLat, baseStartLon);
        BaseHeading = baseHeading;
        ReciprocalHeading = reciprocalHeading;
    }
}
