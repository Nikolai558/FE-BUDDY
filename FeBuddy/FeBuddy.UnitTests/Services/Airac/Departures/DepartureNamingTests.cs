using FeBuddy.Core.Services.Airac.Departures;

namespace FeBuddy.UnitTests.Services.Airac.Departures;

public class DepartureNamingTests
{
	[Theory]
	[InlineData("DOTSS2.DOTSS", "TWO", "DOTSS", "DOTSS")]
	[InlineData("SLC4.TCH", "FOUR", "SALT LAKE", "SLC")]
	[InlineData("1U71.LUNDI", "ONE", "LUNDI", "1U7")]
	[InlineData("L711.LHS", "ONE", "LAKE HAVASU", "L71")]
	[InlineData("77S2.EUG", "TWO", "EUGENE", "77S")]
	[InlineData("HUDSN1.YOMAN", "ONE", "HUDSN (COPTER)", "HUDSN")]
	public void the_code_id_comes_from_the_computer_code_with_the_amendment_digit_removed(
		string computerCode, string amendmentNo, string dpName, string expected)
	{
		string codeId = DepartureNaming.CodeIdFor(computerCode, amendmentNo, dpName, out bool usedFallback);

		Assert.Equal(expected, codeId);
		Assert.False(usedFallback);
	}

	[Theory]
	[InlineData("NOT ASSIGNED", "ONE", "O'HARE", "OHARE")]
	[InlineData("NOT ASSIGNED", "ONE", "TURN-AGAIN", "TURNAGAIN")]
	[InlineData("ABC3.X", "TWO", "MY NAME", "MYNAME")]
	[InlineData("", "ONE", "SALT LAKE", "SALTLAKE")]
	public void the_code_id_falls_back_to_the_cleaned_name_when_the_code_is_unusable(
		string computerCode, string amendmentNo, string dpName, string expected)
	{
		string codeId = DepartureNaming.CodeIdFor(computerCode, amendmentNo, dpName, out bool usedFallback);

		Assert.Equal(expected, codeId);
		Assert.True(usedFallback);
	}

	[Fact]
	public void a_null_computer_code_falls_back_to_the_name()
	{
		string codeId = DepartureNaming.CodeIdFor(null, "ONE", "PORTLAND", out bool usedFallback);

		Assert.Equal("PORTLAND", codeId);
		Assert.True(usedFallback);
	}
}
