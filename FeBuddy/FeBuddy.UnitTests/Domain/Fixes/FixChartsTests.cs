using FeBuddy.Core.Domain.Fixes;

namespace FeBuddy.UnitTests.Domain.Fixes;

/// <summary>
/// Covers <see cref="FixCharts"/>: parsing <c>FIX_BASE.CHARTS</c> into its chart names, the
/// file-naming/CRC-class token for a chart, and <see cref="FixCharts.TokensFor"/>'s fallback for a
/// chart-less fix.
/// </summary>
public sealed class FixChartsTests
{
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData(",,")]
	public void parse_of_null_blank_or_only_commas_is_empty(string? charts) =>
		Assert.Empty(FixCharts.Parse(charts));

	[Fact]
	public void parse_trims_each_entry()
	{
		IReadOnlyList<string> charts = FixCharts.Parse(" ENROUTE LOW , VFR ");

		Assert.Equal(["ENROUTE LOW", "VFR"], charts);
	}

	[Fact]
	public void parse_preserves_nasrs_own_order()
	{
		IReadOnlyList<string> charts = FixCharts.Parse("VFR,ENROUTE LOW,ENROUTE HIGH");

		Assert.Equal(["VFR", "ENROUTE LOW", "ENROUTE HIGH"], charts);
	}

	[Fact]
	public void parse_drops_duplicates_ignoring_case_keeping_the_first_spelling()
	{
		IReadOnlyList<string> charts = FixCharts.Parse("Enroute Low,ENROUTE LOW,VFR,enroute low");

		Assert.Equal(["Enroute Low", "VFR"], charts);
	}

	[Fact]
	public void token_tokenizes_a_chart_name() =>
		Assert.Equal("ENROUTE-LOW", FixCharts.Token("ENROUTE LOW"));

	[Fact]
	public void tokens_for_an_empty_chart_list_is_the_no_chart_token() =>
		Assert.Equal([FixCharts.NoChart], FixCharts.TokensFor([]));

	[Fact]
	public void tokens_for_tokenizes_every_chart_in_order() =>
		Assert.Equal(["ENROUTE-LOW", "ENROUTE-HIGH"], FixCharts.TokensFor(["ENROUTE LOW", "ENROUTE HIGH"]));

	[Fact]
	public void tokens_for_rejects_null() =>
		Assert.Throws<ArgumentNullException>(() => FixCharts.TokensFor(null!));

	[Fact]
	public void no_chart_is_the_documented_token() =>
		Assert.Equal("NO-CHART", FixCharts.NoChart);
}
