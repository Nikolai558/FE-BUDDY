namespace FeBuddy.Core.Domain.ArtccBoundaries.Models;

/// <summary>
/// The altitude structure of an ARTCC boundary ring, from <c>ARB_SEG.ALTITUDE</c>.
/// </summary>
/// <remarks>
/// Declared in NASR publication order (also the order output files list them in) so a default
/// sort by this enum reads High, Low, Unlimited. See <see cref="ArtccBoundaryAltitudes"/> for
/// parsing and for the exact spelling NASR writes.
/// </remarks>
public enum ArtccBoundaryAltitude
{
	/// <summary><c>HIGH</c>.</summary>
	High,

	/// <summary><c>LOW</c>.</summary>
	Low,

	/// <summary><c>UNLIMITED</c>.</summary>
	Unlimited,
}
