using FeBuddy.Core.Domain.Procedures;

namespace FeBuddy.UnitTests.Domain.Procedures;

/// <summary>
/// Covers <see cref="ProcedureNaming"/>: stripping the <c>, CONT.n</c> continuation marker (with
/// its spacing and case variants), collapsing internal whitespace, detecting a continuation page,
/// and numbering a chart's pages.
/// </summary>
public sealed class ProcedureNamingTests
{
	[Fact]
	public void base_name_is_unchanged_without_a_continuation_marker() =>
		Assert.Equal("ILS OR LOC RWY 28C", ProcedureNaming.BaseName("ILS OR LOC RWY 28C"));

	[Theory]
	[InlineData("GRUUB ONE (RNAV), CONT.1", "GRUUB ONE (RNAV)")]
	[InlineData("GRUUB ONE (RNAV), CONT.2", "GRUUB ONE (RNAV)")]
	[InlineData("GRUUB ONE (RNAV),CONT.1", "GRUUB ONE (RNAV)")] // no space after the comma
	[InlineData("GRUUB ONE (RNAV) ,  CONT.1", "GRUUB ONE (RNAV)")] // extra spacing around the comma
	[InlineData("GRUUB ONE (RNAV), cont.1", "GRUUB ONE (RNAV)")] // lower case
	[InlineData("GRUUB ONE (RNAV), Cont.10", "GRUUB ONE (RNAV)")] // multi-digit page number
	public void base_name_strips_the_continuation_marker_in_every_spacing_and_case_variant(string chartName, string expected) =>
		Assert.Equal(expected, ProcedureNaming.BaseName(chartName));

	[Fact]
	public void base_name_trims_and_collapses_internal_whitespace() =>
		Assert.Equal("ILS OR LOC RWY 28C", ProcedureNaming.BaseName("  ILS  OR LOC   RWY 28C  "));

	[Fact]
	public void base_name_rejects_a_null_argument() =>
		Assert.Throws<ArgumentNullException>(() => ProcedureNaming.BaseName(null!));

	[Theory]
	[InlineData("GRUUB ONE (RNAV), CONT.1")]
	[InlineData("GRUUB ONE (RNAV), cont.2")]
	public void is_continuation_is_true_for_a_continuation_page(string chartName) =>
		Assert.True(ProcedureNaming.IsContinuation(chartName));

	[Theory]
	[InlineData("ILS OR LOC RWY 28C")]
	[InlineData("TAKEOFF MINIMUMS")]
	[InlineData("")]
	public void is_continuation_is_false_without_the_marker(string chartName) =>
		Assert.False(ProcedureNaming.IsContinuation(chartName));

	[Fact]
	public void is_continuation_rejects_a_null_argument() =>
		Assert.Throws<ArgumentNullException>(() => ProcedureNaming.IsContinuation(null!));

	[Theory]
	[InlineData("ILS OR LOC RWY 28C", 1)]
	[InlineData("GRUUB ONE (RNAV), CONT.1", 2)]
	[InlineData("GRUUB ONE (RNAV), cont.2", 3)]
	[InlineData("GRUUB ONE (RNAV),CONT.10", 11)]
	[InlineData("", 1)]
	public void page_number_is_one_for_the_first_page_and_n_plus_one_for_cont_n(string chartName, int expected) =>
		Assert.Equal(expected, ProcedureNaming.PageNumber(chartName));

	[Fact]
	public void page_number_rejects_a_null_argument() =>
		Assert.Throws<ArgumentNullException>(() => ProcedureNaming.PageNumber(null!));
}
