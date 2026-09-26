using FeBuddy.Core.Domain.Fixes;

namespace FeBuddy.UnitTests.Domain.Fixes;

/// <summary>
/// Covers <see cref="FixUses"/>: mapping every known <c>FIX_USE_CODE</c> to its display name
/// (case/whitespace-insensitive), an unrecognized code kept literally (only sanitized for use as a
/// file name), the blank-code fallback, <see cref="FixUses.All"/>'s order, <see cref="FixUses.IsKnown"/>
/// and <see cref="FixUses.Token"/>.
/// </summary>
public sealed class FixUsesTests
{
	[Theory]
	[InlineData("CN", "COMPUTER-NAV")]
	[InlineData("cn", "COMPUTER-NAV")]
	[InlineData(" CN ", "COMPUTER-NAV")]
	[InlineData("MR", "MIL-RPRTNG-PNT")]
	[InlineData("mr", "MIL-RPRTNG-PNT")]
	[InlineData("MW", "MIL-WYPNT")]
	[InlineData("mw", "MIL-WYPNT")]
	[InlineData("NRS", "NRS-WYPNT")]
	[InlineData("nrs", "NRS-WYPNT")]
	[InlineData("RADAR", "RADAR")]
	[InlineData("radar", "RADAR")]
	[InlineData("RP", "RPRTNG-PNT")]
	[InlineData("rp", "RPRTNG-PNT")]
	[InlineData("VFR", "VFR-WYPNT")]
	[InlineData("vfr", "VFR-WYPNT")]
	[InlineData("WP", "WYPNT")]
	[InlineData("  wp  ", "WYPNT")]
	public void name_maps_every_known_code_ignoring_case_and_surrounding_whitespace(string code, string expected) =>
		Assert.Equal(expected, FixUses.Name(code));

	[Theory]
	[InlineData("ZQ", "ZQ")]
	[InlineData("zq", "zq")]
	public void name_keeps_an_unrecognized_code_literally(string code, string expected) =>
		Assert.Equal(expected, FixUses.Name(code));

	[Theory]
	[InlineData("AB/CD", "AB CD")]
	[InlineData("/AB/", "AB")]
	[InlineData("A//B", "A B")]
	public void name_replaces_invalid_file_name_characters_with_spaces_collapsed_and_trimmed(string code, string expected) =>
		Assert.Equal(expected, FixUses.Name(code));

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void name_of_a_blank_or_null_code_is_unknown(string? code) =>
		Assert.Equal("UNKNOWN", FixUses.Name(code));

	[Fact]
	public void all_lists_the_known_names_in_gui_order() =>
		Assert.Equal(
			["COMPUTER-NAV", "MIL-RPRTNG-PNT", "MIL-WYPNT", "NRS-WYPNT", "RADAR", "RPRTNG-PNT", "VFR-WYPNT", "WYPNT"],
			FixUses.All);

	[Theory]
	[InlineData("WYPNT")]
	[InlineData("wypnt")]
	[InlineData("Computer-Nav")]
	public void is_known_matches_the_all_list_ignoring_case(string name) =>
		Assert.True(FixUses.IsKnown(name));

	[Fact]
	public void is_known_is_false_for_an_unrecognized_name() =>
		Assert.False(FixUses.IsKnown("ZQ"));

	[Fact]
	public void token_of_an_already_known_name_is_unchanged() =>
		Assert.Equal("VFR-WYPNT", FixUses.Token("VFR-WYPNT"));

	[Fact]
	public void token_tokenizes_an_unrecognized_name() =>
		Assert.Equal("MYSTERY-TYPE", FixUses.Token("mystery type"));
}
