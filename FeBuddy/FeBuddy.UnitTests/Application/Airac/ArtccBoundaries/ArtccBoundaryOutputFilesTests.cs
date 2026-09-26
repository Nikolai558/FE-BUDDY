using FeBuddy.Core.Application.Airac.ArtccBoundaries;

namespace FeBuddy.UnitTests.Application.Airac.ArtccBoundaries;

/// <summary>
/// Covers <see cref="ArtccBoundaryOutputFiles"/>: the High/Low/Unlimited-mode file keys, the
/// ArtccAltitude-mode per-location-and-altitude keys, which keys count as ARTCC Boundaries GeoJSON
/// files, and reading a key back into its CRC class.
/// </summary>
public sealed class ArtccBoundaryOutputFilesTests
{
	[Theory]
	[InlineData("High", "ARTCC-Boundary_High_Lines")]
	[InlineData("Low", "ARTCC-Boundary_Low_Lines")]
	[InlineData("Unlimited", "ARTCC-Boundary_Unlimited_Lines")]
	public void key_for_a_group_combines_the_fixed_prefix_and_suffix(string group, string expected) =>
		Assert.Equal(expected, ArtccBoundaryOutputFiles.KeyFor(group));

	[Fact]
	public void key_for_location_and_altitude_combines_the_location_id_and_altitude_token() =>
		Assert.Equal("ARTCC-Boundary_ZOB-HIGH_Lines", ArtccBoundaryOutputFiles.KeyFor("ZOB", "HIGH"));

	[Fact]
	public void class_for_a_group_is_the_group_unchanged() =>
		Assert.Equal(ArtccBoundaryOutputFiles.HighClass, ArtccBoundaryOutputFiles.ClassFor(ArtccBoundaryOutputFiles.HighClass));

	[Fact]
	public void class_for_location_and_altitude_hyphenates_them() =>
		Assert.Equal("ZOB-HIGH", ArtccBoundaryOutputFiles.ClassFor("ZOB", "HIGH"));

	[Theory]
	[InlineData("ARTCC-Boundary_High_Lines")]
	[InlineData("ARTCC-Boundary_Low_Lines")]
	[InlineData("ARTCC-Boundary_Unlimited_Lines")]
	[InlineData("artcc-boundary_high_lines")] // ignoring case
	[InlineData("ARTCC-Boundary_ZOB-HIGH_Lines")]
	[InlineData("ARTCC-Boundary_ZOB-LOW_Lines")]
	[InlineData("artcc-boundary_zob-high_lines")]
	public void is_geojson_key_accepts_every_recognized_form(string key) =>
		Assert.True(ArtccBoundaryOutputFiles.IsGeojsonKey(key));

	[Theory]
	[InlineData("ARTCC-Boundary_High_Symbols")] // no Symbols file
	[InlineData("ARTCC-Boundary_Medium_Lines")] // not a real altitude, and no hyphen
	[InlineData("ArtccBoundaries.txt")]
	[InlineData("junk")]
	public void is_geojson_key_rejects_everything_else(string key) =>
		Assert.False(ArtccBoundaryOutputFiles.IsGeojsonKey(key));

	[Theory]
	[InlineData("ARTCC-Boundary_High_Lines", "High")]
	[InlineData("ARTCC-Boundary_Low_Lines", "Low")]
	[InlineData("ARTCC-Boundary_Unlimited_Lines", "Unlimited")]
	[InlineData("ARTCC-Boundary_ZOB-HIGH_Lines", "ZOB-HIGH")]
	public void try_parse_key_returns_the_files_crc_class(string key, string expectedClass)
	{
		Assert.True(ArtccBoundaryOutputFiles.TryParseKey(key, out string className));
		Assert.Equal(expectedClass, className);
	}

	[Theory]
	[InlineData("junk")]
	[InlineData("ArtccBoundaries.txt")]
	[InlineData("ARTCC-Boundary_High_Symbols")]
	public void try_parse_key_fails_for_junk(string key)
	{
		Assert.False(ArtccBoundaryOutputFiles.TryParseKey(key, out string className));
		Assert.Equal(string.Empty, className);
	}
}
