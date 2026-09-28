using FeBuddy.Core.Domain.WxStations;

namespace FeBuddy.UnitTests.Domain.WxStations;

/// <summary>
/// Covers <see cref="WxStationCountries.IsIncluded"/>: every included territory code, matching
/// ignoring case, and rejecting null, blank, and unrelated codes.
/// </summary>
public sealed class WxStationCountriesTests
{
	[Theory]
	[InlineData("US")]
	[InlineData("PR")]
	[InlineData("VI")]
	[InlineData("GU")]
	[InlineData("MP")]
	[InlineData("AS")]
	[InlineData("UM")]
	public void every_included_territory_code_is_included(string country) =>
		Assert.True(WxStationCountries.IsIncluded(country));

	[Theory]
	[InlineData("us")]
	[InlineData("Us")]
	[InlineData("pr")]
	public void matching_ignores_case(string country) =>
		Assert.True(WxStationCountries.IsIncluded(country));

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("CA")]
	[InlineData("MX")]
	public void null_blank_and_unrelated_countries_are_not_included(string? country) =>
		Assert.False(WxStationCountries.IsIncluded(country));
}
