using FeBuddy.Core.Domain.Arrivals;

namespace FeBuddy.UnitTests.Domain.Arrivals;

/// <summary>
/// Covers <see cref="ArrivalNaming"/>: the procedure code id (read as <c>TRANSITION.PROCEDURE</c>,
/// the reverse of a departure's code) and its fallback to the arrival name.
/// </summary>
public sealed class ArrivalNamingTests
{
	[Theory]
	[InlineData("AALAN.BLAID2", "TWO", "BLAID", "BLAID")]
	[InlineData("FIM.FERN7", "SEVEN", "FERNANDO", "FERN")]
	[InlineData("MRB.EMI7", "SEVEN", "WESTMINSTER", "EMI")]
	[InlineData("BLAID2", "TWO", "BLAID", "BLAID")]
	public void the_code_id_comes_from_the_transition_side_of_the_computer_code_with_the_amendment_digit_removed(
		string computerCode, string amendmentNo, string arrivalName, string expected)
	{
		string codeId = ArrivalNaming.CodeIdFor(computerCode, amendmentNo, arrivalName, out bool usedFallback);

		Assert.Equal(expected, codeId);
		Assert.False(usedFallback);
	}

	[Theory]
	[InlineData("AALAN.BLAID3", "TWO", "WILKES-BARRE", "WILKESBARRE")]
	[InlineData("NOT ASSIGNED", "ONE", "GOLDEN GATE", "GOLDENGATE")]
	[InlineData("AALAN.BLAID2", "ORIGINAL", "MY NAME", "MYNAME")]
	[InlineData("", "ONE", "SALT LAKE", "SALTLAKE")]
	public void the_code_id_falls_back_to_the_cleaned_arrival_name_when_the_code_is_unusable(
		string computerCode, string amendmentNo, string arrivalName, string expected)
	{
		string codeId = ArrivalNaming.CodeIdFor(computerCode, amendmentNo, arrivalName, out bool usedFallback);

		Assert.Equal(expected, codeId);
		Assert.True(usedFallback);
	}

	[Fact]
	public void a_null_computer_code_falls_back_to_the_name()
	{
		string codeId = ArrivalNaming.CodeIdFor(null, "ONE", "PORTLAND", out bool usedFallback);

		Assert.Equal("PORTLAND", codeId);
		Assert.True(usedFallback);
	}
}
