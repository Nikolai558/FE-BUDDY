namespace FeBuddy.Core.Domain.Airports.Models;

/// <summary>One end of a runway, with the coordinates NASR publishes for it.</summary>
/// <param name="EndId">The end's identifier, <c>APT_RWY_END.RWY_END_ID</c> (e.g. <c>16L</c>).</param>
/// <param name="Latitude">The end's latitude in decimal degrees.</param>
/// <param name="Longitude">The end's longitude in decimal degrees.</param>
public sealed record AirportRunwayEnd(string EndId, double Latitude, double Longitude);

/// <summary>
/// One runway at an airport: its identifier, its published length and surface, and the two
/// ends that give it geometry.
/// </summary>
/// <remarks>
/// A runway can be present for the alias file's "longest runway" line while still having no
/// geometry: NASR occasionally publishes a runway whose end coordinates are blank. Both ends
/// are therefore nullable, and the GeoJSON writer checks <see cref="HasGeometry"/> before
/// drawing it.
/// </remarks>
public sealed record AirportRunway
{
	/// <summary>Runway identifier, <c>APT_RWY.RWY_ID</c> (e.g. <c>16L/34R</c>).</summary>
	public required string RunwayId { get; init; }

	/// <summary>Published length in feet, <c>APT_RWY.RWY_LEN</c>.</summary>
	public required int Length { get; init; }

	/// <summary>Surface type code, <c>APT_RWY.SURFACE_TYPE_CODE</c> (e.g. <c>ASPH-G</c>).</summary>
	public string? SurfaceType { get; init; }

	/// <summary>The first end, ordered by <c>RWY_END_ID</c>, or <see langword="null"/> when NASR published no usable coordinates.</summary>
	public AirportRunwayEnd? FirstEnd { get; init; }

	/// <summary>The second end, ordered by <c>RWY_END_ID</c>, or <see langword="null"/> when NASR published no usable coordinates.</summary>
	public AirportRunwayEnd? SecondEnd { get; init; }

	/// <summary>Whether both ends resolved, so this runway can be drawn as a LineString.</summary>
	public bool HasGeometry => FirstEnd is not null && SecondEnd is not null;
}
