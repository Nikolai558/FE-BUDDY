namespace FeBuddy.Core.Domain.ArtccBoundaries.Models;

/// <summary>
/// One closed boundary ring: a contiguous run of <c>ARB_SEG</c> rows for one location and
/// altitude, closed back to its own starting point. Each ring becomes exactly one GeoJSON
/// Feature (see <c>ArtccBoundaryGeojsonWriter</c>).
/// </summary>
/// <remarks>
/// A single (LocationId, Altitude) group can hold more than one ring - e.g. ZAK's UNLIMITED
/// group is a CTA ring followed by a FIR ring - which <c>ArtccBoundaryBuilder</c> tells apart by
/// <c>ARB_SEG.POINT_SEQ</c> resetting to a lower value.
/// </remarks>
public sealed class ArtccBoundaryRing
{
	/// <summary>The ring's location.</summary>
	public required ArtccBoundaryLocation Location { get; init; }

	/// <summary>The ring's altitude structure, <c>ARB_SEG.ALTITUDE</c>.</summary>
	public required ArtccBoundaryAltitude Altitude { get; init; }

	/// <summary>
	/// The ring's boundary type - <c>ARTCC</c>, <c>CTA</c>, <c>FIR</c>, <c>CTA/FIR</c> or
	/// <c>UTA</c> - from its first row's <c>ARB_SEG.TYPE</c>, trimmed. Lets the overlapping
	/// oceanic CTA and FIR rings within one (LocationId, Altitude) group be told apart.
	/// </summary>
	public required string Type { get; init; }

	/// <summary>
	/// The ring's points, in <c>POINT_SEQ</c> order and closed - its first point is repeated as
	/// the last when NASR's own last point differs from it.
	/// </summary>
	public required IReadOnlyList<ArtccBoundaryPoint> Points { get; init; }
}
