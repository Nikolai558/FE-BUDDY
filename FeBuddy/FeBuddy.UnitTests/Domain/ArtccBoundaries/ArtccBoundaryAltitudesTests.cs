using FeBuddy.Core.Domain.ArtccBoundaries;
using FeBuddy.Core.Domain.ArtccBoundaries.Models;

namespace FeBuddy.UnitTests.Domain.ArtccBoundaries;

/// <summary>
/// Covers <see cref="ArtccBoundaryAltitudes"/>: parsing <c>ARB_SEG.ALTITUDE</c> and recovering
/// NASR's own spelling from an <see cref="ArtccBoundaryAltitude"/>.
/// </summary>
public sealed class ArtccBoundaryAltitudesTests
{
	[Theory]
	[InlineData("HIGH", ArtccBoundaryAltitude.High)]
	[InlineData("high", ArtccBoundaryAltitude.High)]
	[InlineData("  High  ", ArtccBoundaryAltitude.High)]
	[InlineData("LOW", ArtccBoundaryAltitude.Low)]
	[InlineData("low", ArtccBoundaryAltitude.Low)]
	[InlineData("  Low  ", ArtccBoundaryAltitude.Low)]
	[InlineData("UNLIMITED", ArtccBoundaryAltitude.Unlimited)]
	[InlineData("unlimited", ArtccBoundaryAltitude.Unlimited)]
	[InlineData("  Unlimited  ", ArtccBoundaryAltitude.Unlimited)]
	public void try_parse_accepts_the_nasr_spellings_ignoring_case_and_surrounding_whitespace(string value, ArtccBoundaryAltitude expected)
	{
		Assert.True(ArtccBoundaryAltitudes.TryParse(value, out ArtccBoundaryAltitude altitude));
		Assert.Equal(expected, altitude);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("MEDIUM")]
	[InlineData("HIGH ALTITUDE")]
	public void try_parse_rejects_anything_else(string? value)
	{
		Assert.False(ArtccBoundaryAltitudes.TryParse(value, out ArtccBoundaryAltitude altitude));
		Assert.Equal(default, altitude);
	}

	[Theory]
	[InlineData(ArtccBoundaryAltitude.High, "HIGH")]
	[InlineData(ArtccBoundaryAltitude.Low, "LOW")]
	[InlineData(ArtccBoundaryAltitude.Unlimited, "UNLIMITED")]
	public void nasr_token_returns_nasrs_own_spelling(ArtccBoundaryAltitude altitude, string expected) =>
		Assert.Equal(expected, ArtccBoundaryAltitudes.NasrToken(altitude));

	[Fact]
	public void nasr_token_rejects_an_undefined_altitude() =>
		Assert.Throws<ArgumentOutOfRangeException>(() => ArtccBoundaryAltitudes.NasrToken((ArtccBoundaryAltitude)99));
}
