using FeBuddy.Core.Domain.Fixes;

namespace FeBuddy.UnitTests.Domain.Fixes;

/// <summary>
/// Covers <see cref="FixTokens.Token"/>: upper-casing, collapsing runs of non-alphanumeric
/// characters to a single <c>-</c>, and stripping any leading or trailing <c>-</c>.
/// </summary>
public sealed class FixTokensTests
{
	[Theory]
	[InlineData("wypnt", "WYPNT")]
	[InlineData("VFR TERMINAL AREA", "VFR-TERMINAL-AREA")]
	[InlineData("a/b c", "A-B-C")]
	[InlineData("  vfr terminal area  ", "VFR-TERMINAL-AREA")]
	[InlineData("ENROUTE LOW", "ENROUTE-LOW")]
	public void token_upper_cases_and_collapses_non_alphanumeric_runs_to_a_single_dash(string value, string expected) =>
		Assert.Equal(expected, FixTokens.Token(value));

	[Theory]
	[InlineData("--abc--", "ABC")]
	[InlineData("///abc///", "ABC")]
	[InlineData("-abc", "ABC")]
	[InlineData("abc-", "ABC")]
	public void leading_and_trailing_non_alphanumerics_are_stripped(string value, string expected) =>
		Assert.Equal(expected, FixTokens.Token(value));

	[Theory]
	[InlineData("ENROUTE-LOW")]
	[InlineData("WYPNT")]
	[InlineData("ABC-DEF-123")]
	public void an_already_tokenized_value_is_unchanged(string value) =>
		Assert.Equal(value, FixTokens.Token(value));

	[Theory]
	[InlineData("---")]
	[InlineData("///")]
	[InlineData("   ")]
	[InlineData("")]
	public void a_value_with_no_letters_or_digits_tokenizes_to_an_empty_string(string value) =>
		Assert.Equal(string.Empty, FixTokens.Token(value));

	[Fact]
	public void token_rejects_null() =>
		Assert.Throws<ArgumentNullException>(() => FixTokens.Token(null!));
}
