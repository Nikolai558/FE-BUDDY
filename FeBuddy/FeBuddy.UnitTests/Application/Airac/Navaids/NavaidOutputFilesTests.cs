using FeBuddy.Core.Application.Airac.Navaids;
using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.UnitTests.Application.Airac.Navaids;

/// <summary>
/// Covers <see cref="NavaidOutputFiles"/>: the per-type file key format, which keys count as
/// NAVAIDs GeoJSON files, and reading a per-type key back into its token and kind.
/// </summary>
public sealed class NavaidOutputFilesTests
{
	[Theory]
	[InlineData("VORTAC", CrcFeatureKind.Symbol, "NAVAIDs_VORTACs_Symbols")]
	[InlineData("VOR/DME", CrcFeatureKind.Text, "NAVAIDs_VOR-DMEs_Text")]
	[InlineData("FAN MARKER", CrcFeatureKind.Symbol, "NAVAIDs_FAN-MARKERs_Symbols")]
	public void type_key_combines_the_type_group_and_the_feature_kind(string navType, CrcFeatureKind kind, string expected) =>
		Assert.Equal(expected, NavaidOutputFiles.TypeKey(navType, kind));

	[Theory]
	[InlineData("VORTAC", "VORTACs")]
	[InlineData("VOR/DME", "VOR-DMEs")]
	public void type_group_is_the_token_plus_a_trailing_s(string navType, string expected) =>
		Assert.Equal(expected, NavaidOutputFiles.TypeGroup(navType));

	[Theory]
	[InlineData("NAVAIDs_Symbols")]
	[InlineData("NAVAIDs_Text")]
	[InlineData("navaids_symbols")] // ignoring case
	[InlineData("NAVAIDs_VORTACs_Symbols")]
	[InlineData("NAVAIDs_VOR-DMEs_Text")]
	public void is_geojson_key_accepts_the_all_mode_and_type_mode_keys(string key) =>
		Assert.True(NavaidOutputFiles.IsGeojsonKey(key));

	[Theory]
	[InlineData("NAVAIDs.txt")]
	[InlineData("Airports_Symbols")]
	[InlineData("NAVAIDs_VORTACs_Lines")] // NAVAIDs has no Lines file
	[InlineData("junk")]
	public void is_geojson_key_rejects_everything_else(string key) =>
		Assert.False(NavaidOutputFiles.IsGeojsonKey(key));

	[Theory]
	[InlineData("NAVAIDs_VORTACs_Symbols", "VORTAC", CrcFeatureKind.Symbol)]
	[InlineData("NAVAIDs_VOR-DMEs_Text", "VOR-DME", CrcFeatureKind.Text)]
	[InlineData("navaids_vortacs_symbols", "VORTAC", CrcFeatureKind.Symbol)]
	public void try_parse_type_key_reads_the_token_upper_cased_and_the_kind(string key, string expectedToken, CrcFeatureKind expectedKind)
	{
		Assert.True(NavaidOutputFiles.TryParseTypeKey(key, out string token, out CrcFeatureKind kind));
		Assert.Equal(expectedToken, token);
		Assert.Equal(expectedKind, kind);
	}

	[Theory]
	[InlineData("NAVAIDs_Symbols")]
	[InlineData("junk")]
	[InlineData("NAVAIDs.txt")]
	public void try_parse_type_key_fails_for_the_all_mode_keys_and_junk(string key)
	{
		Assert.False(NavaidOutputFiles.TryParseTypeKey(key, out string token, out _));
		Assert.Equal(string.Empty, token);
	}
}
