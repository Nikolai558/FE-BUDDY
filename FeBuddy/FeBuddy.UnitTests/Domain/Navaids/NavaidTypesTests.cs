using FeBuddy.Core.Domain.Navaids;

namespace FeBuddy.UnitTests.Domain.Navaids;

/// <summary>
/// Covers <see cref="NavaidTypes"/>: the file-naming token, the default CRC symbol style per
/// type, the NDB-family frequency-unit test, and frequency formatting.
/// </summary>
public sealed class NavaidTypesTests
{
	[Theory]
	[InlineData("VOR", "VOR")]
	[InlineData("VOR/DME", "VOR-DME")]
	[InlineData("FAN MARKER", "FAN-MARKER")]
	[InlineData("MARINE NDB/DME", "MARINE-NDB-DME")]
	[InlineData("  VOR/DME  ", "VOR-DME")]
	[InlineData("vor/dme", "VOR-DME")]
	public void token_upper_cases_and_collapses_slashes_and_whitespace_to_a_single_dash(string type, string expected) =>
		Assert.Equal(expected, NavaidTypes.Token(type));

	[Fact]
	public void token_rejects_null()
	{
		Assert.Throws<ArgumentNullException>(() => NavaidTypes.Token(null!));
	}

	[Theory]
	[InlineData("VOR", "vor")]
	[InlineData("VORTAC", "vor")]
	[InlineData("VOR/DME", "vor")]
	[InlineData("VOT", "vor")]
	[InlineData("TACAN", "tacan")]
	[InlineData("DME", "tacan")]
	[InlineData("NDB", "ndb")]
	[InlineData("NDB/DME", "ndb")]
	[InlineData("MARINE NDB", "ndb")]
	[InlineData("MARINE NDB/DME", "ndb")]
	[InlineData("UHF/NDB", "ndb")]
	[InlineData("vortac", "vor")] // matched ignoring case
	public void symbol_style_for_maps_every_known_type_to_its_group(string type, string expectedStyle) =>
		Assert.Equal(expectedStyle, NavaidTypes.SymbolStyleFor(type));

	[Theory]
	[InlineData("FAN MARKER")]
	[InlineData("CONSOLAN")]
	[InlineData("SOMETHING FE-BUDDY DOES NOT KNOW")]
	public void symbol_style_for_is_null_for_fan_marker_consolan_and_unknown_types(string type) =>
		Assert.Null(NavaidTypes.SymbolStyleFor(type));

	[Theory]
	[InlineData("NDB", true)]
	[InlineData("NDB/DME", true)]
	[InlineData("MARINE NDB", true)]
	[InlineData("MARINE NDB/DME", true)]
	[InlineData("UHF/NDB", true)]
	[InlineData("CONSOLAN", true)]
	[InlineData("VOR", false)]
	[InlineData("VORTAC", false)]
	[InlineData("TACAN", false)]
	[InlineData("DME", false)]
	[InlineData("FAN MARKER", false)]
	public void is_ndb_family_identifies_the_khz_types(string type, bool expected) =>
		Assert.Equal(expected, NavaidTypes.IsNdbFamily(type));

	[Theory]
	[InlineData("VOR", 114.2, "114.20")]
	[InlineData("VOR", 116.65, "116.65")]
	[InlineData("VORTAC", 114.2, "114.20")]
	[InlineData("NDB", 365.0, "365")]
	[InlineData("NDB", 278.5, "278.5")]
	[InlineData("CONSOLAN", 194.0, "194")]
	public void format_frequency_uses_two_decimals_for_mhz_and_trims_trailing_zeros_for_khz(
		string type, double freq, string expected) =>
		Assert.Equal(expected, NavaidTypes.FormatFrequency(type, freq));

	[Fact]
	public void format_frequency_of_null_is_an_empty_string()
	{
		Assert.Equal(string.Empty, NavaidTypes.FormatFrequency("VOR", null));
		Assert.Equal(string.Empty, NavaidTypes.FormatFrequency("NDB", null));
	}

	[Theory]
	[InlineData("VOR")]
	[InlineData("vor")]
	public void is_known_matches_the_all_list_ignoring_case(string type) =>
		Assert.True(NavaidTypes.IsKnown(type));

	[Fact]
	public void is_known_is_false_for_an_unrecognized_type()
	{
		Assert.False(NavaidTypes.IsKnown("SOMETHING FE-BUDDY DOES NOT KNOW"));
	}
}
