namespace FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;

/// <summary>
/// The FE-Buddy custom properties an ARTCC boundary Feature can carry. The user picks which ones
/// they want; each selected value is written as <c>"feb.&lt;name&gt;"</c>.
/// </summary>
/// <remarks>
/// There is deliberately no latitude, longitude or point-list property: every Feature's geometry
/// already carries its coordinates. A blank value (e.g. no <see cref="IcaoId"/>, or no ARB_BASE
/// row at all) is omitted rather than written as an empty string.
/// </remarks>
public enum ArtccBoundaryFebProperty
{
	/// <summary><c>feb.locationId</c></summary>
	LocationId = 0,

	/// <summary><c>feb.locationName</c></summary>
	LocationName = 1,

	/// <summary><c>feb.locationType</c></summary>
	LocationType = 2,

	/// <summary><c>feb.icaoId</c></summary>
	IcaoId = 3,

	/// <summary><c>feb.computerId</c></summary>
	ComputerId = 4,

	/// <summary>
	/// <c>feb.altitude</c> - the ring's altitude structure exactly as NASR writes it: <c>HIGH</c>,
	/// <c>LOW</c> or <c>UNLIMITED</c>.
	/// </summary>
	Altitude = 5,

	/// <summary>
	/// <c>feb.type</c> - the ring's boundary type (<c>ARTCC</c>, <c>CTA</c>, <c>FIR</c>,
	/// <c>CTA/FIR</c> or <c>UTA</c>). Lets the overlapping oceanic CTA and FIR rings be told apart.
	/// </summary>
	Type = 6,

	/// <summary><c>feb.city</c></summary>
	City = 7,

	/// <summary><c>feb.countryCode</c></summary>
	CountryCode = 8,
}
