using FeBuddy.Core.Domain.ArtccBoundaries.Models;

namespace FeBuddy.Core.Domain.ArtccBoundaries;

/// <summary>
/// The <c>ARB_SEG.ALTITUDE</c> vocabulary and the rules FE-Buddy applies to it: parsing NASR's
/// text and recovering NASR's own spelling from an <see cref="ArtccBoundaryAltitude"/>.
/// </summary>
public static class ArtccBoundaryAltitudes
{
	/// <summary>Attempts to parse an <c>ARB_SEG.ALTITUDE</c> value, ignoring case and surrounding whitespace.</summary>
	/// <param name="value">The raw <c>ALTITUDE</c> value.</param>
	/// <param name="altitude">The parsed altitude, when <paramref name="value"/> is HIGH, LOW or UNLIMITED.</param>
	/// <returns><see langword="true"/> when <paramref name="value"/> is recognized.</returns>
	public static bool TryParse(string? value, out ArtccBoundaryAltitude altitude)
	{
		switch (value?.Trim().ToUpperInvariant())
		{
			case "HIGH":
				altitude = ArtccBoundaryAltitude.High;
				return true;

			case "LOW":
				altitude = ArtccBoundaryAltitude.Low;
				return true;

			case "UNLIMITED":
				altitude = ArtccBoundaryAltitude.Unlimited;
				return true;

			default:
				altitude = default;
				return false;
		}
	}

	/// <summary>
	/// The altitude exactly as NASR writes it - <c>HIGH</c>, <c>LOW</c> or <c>UNLIMITED</c> -
	/// used in the ARTCC Altitude output file names, their CRC class names, and the
	/// <c>feb.altitude</c> property.
	/// </summary>
	/// <param name="altitude">The altitude.</param>
	/// <returns>The NASR spelling.</returns>
	public static string NasrToken(ArtccBoundaryAltitude altitude) => altitude switch
	{
		ArtccBoundaryAltitude.High => "HIGH",
		ArtccBoundaryAltitude.Low => "LOW",
		ArtccBoundaryAltitude.Unlimited => "UNLIMITED",
		_ => throw new ArgumentOutOfRangeException(nameof(altitude), altitude, "Unknown ARTCC boundary altitude."),
	};
}
