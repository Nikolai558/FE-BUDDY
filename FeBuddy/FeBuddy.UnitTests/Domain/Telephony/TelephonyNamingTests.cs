using FeBuddy.Core.Domain.Telephony;

namespace FeBuddy.UnitTests.Domain.Telephony;

/// <summary>
/// Covers <see cref="TelephonyNaming"/>: building a command name from an identifier or telephony
/// (letters and digits only, upper-cased, <see langword="null"/> when there is nothing to build
/// from), and recognizing a three-letter ICAO designator versus the register's "none" placeholders.
/// </summary>
public sealed class TelephonyNamingTests
{
	// ---- CommandName ----

	[Theory]
	[InlineData("AVA", ".idAVA")]
	[InlineData("RYAN AIR", ".idRYANAIR")]
	[InlineData("COTE D'IVOIRE", ".idCOTEDIVOIRE")]
	[InlineData("ava", ".idAVA")]
	[InlineData("Ryan Air", ".idRYANAIR")]
	public void command_name_keeps_only_letters_and_digits_upper_cased(string text, string expected) =>
		Assert.Equal(expected, TelephonyNaming.CommandName(text));

	[Theory]
	[InlineData("-- ")]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("...")]
	public void command_name_is_null_when_there_is_no_letter_or_digit(string text) =>
		Assert.Null(TelephonyNaming.CommandName(text));

	[Fact]
	public void command_name_rejects_a_null_argument() =>
		Assert.Throws<ArgumentNullException>(() => TelephonyNaming.CommandName(null!));

	// ---- IsThreeLetterDesignator ----

	[Theory]
	[InlineData("AVA")]
	[InlineData(" ava ")]
	[InlineData("aVa")]
	public void is_three_letter_designator_is_true_for_three_letters_ignoring_case_and_surrounding_spaces(string code) =>
		Assert.True(TelephonyNaming.IsThreeLetterDesignator(code));

	[Theory]
	[InlineData("...")]
	[InlineData("--")]
	[InlineData("AV")]
	[InlineData("AVAX")]
	[InlineData("A1B")]
	[InlineData("")]
	public void is_three_letter_designator_is_false_for_anything_else(string code) =>
		Assert.False(TelephonyNaming.IsThreeLetterDesignator(code));

	[Fact]
	public void is_three_letter_designator_rejects_a_null_argument() =>
		Assert.Throws<ArgumentNullException>(() => TelephonyNaming.IsThreeLetterDesignator(null!));
}
