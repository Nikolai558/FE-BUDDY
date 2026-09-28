using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Domain.Procedures.Models;

using FeBuddy.UnitTests.Application.Airac.Procedures.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Procedures;

/// <summary>
/// Covers <see cref="ProcedureOrdering"/>: facility section ordering (primary first, others
/// alphabetical, <c>Other</c> last) and airport ordering within a facility (airspace rank, then
/// no-digit-before-digit identifiers, then ordinal).
/// </summary>
public sealed class ProcedureOrderingTests
{
	// ---- GroupByFacility ----

	[Fact]
	public void the_primary_facility_is_listed_first()
	{
		ProcedureAirport zny = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZNY");
		ProcedureAirport zob = ProcedureTestData.BuiltAirport("BBB", responsibleArtcc: "ZOB");

		var sections = ProcedureOrdering.GroupByFacility([zny, zob], primaryFacility: "ZOB");

		Assert.Equal(["ZOB", "ZNY"], sections.Select(s => s.Facility));
	}

	[Fact]
	public void other_facilities_are_ordered_alphabetically_ignoring_case()
	{
		ProcedureAirport zob = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB");
		ProcedureAirport zab = ProcedureTestData.BuiltAirport("BBB", responsibleArtcc: "zab");
		ProcedureAirport zny = ProcedureTestData.BuiltAirport("CCC", responsibleArtcc: "ZNY");

		var sections = ProcedureOrdering.GroupByFacility([zob, zab, zny], primaryFacility: null);

		Assert.Equal(["zab", "ZNY", "ZOB"], sections.Select(s => s.Facility));
	}

	[Fact]
	public void the_other_facility_is_always_listed_last()
	{
		ProcedureAirport zob = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB");
		ProcedureAirport noFacility = ProcedureTestData.BuiltAirport("BBB", responsibleArtcc: null);

		var sections = ProcedureOrdering.GroupByFacility([noFacility, zob], primaryFacility: "ZOB");

		Assert.Equal(["ZOB", ProcedureOrdering.OtherFacility], sections.Select(s => s.Facility));
	}

	[Fact]
	public void a_primary_facility_with_no_included_airports_is_simply_absent()
	{
		ProcedureAirport zob = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB");

		var sections = ProcedureOrdering.GroupByFacility([zob], primaryFacility: "ZNY");

		Assert.Equal(["ZOB"], sections.Select(s => s.Facility));
	}

	[Fact]
	public void each_facilitys_airports_are_ordered_by_order_airports()
	{
		ProcedureAirport b = ProcedureTestData.BuiltAirport("BBB", responsibleArtcc: "ZOB", airspaceClass: ProcedureAirspaceClass.B);
		ProcedureAirport d = ProcedureTestData.BuiltAirport("DDD", responsibleArtcc: "ZOB", airspaceClass: ProcedureAirspaceClass.D);

		var sections = ProcedureOrdering.GroupByFacility([d, b], primaryFacility: null);

		(_, IReadOnlyList<ProcedureAirport> airports) = Assert.Single(sections);
		Assert.Equal(["BBB", "DDD"], airports.Select(a => a.AptIdent));
	}

	// ---- OrderAirports ----

	[Theory]
	[InlineData(ProcedureAirspaceClass.B, ProcedureAirspaceClass.C)]
	[InlineData(ProcedureAirspaceClass.C, ProcedureAirspaceClass.D)]
	[InlineData(ProcedureAirspaceClass.D, ProcedureAirspaceClass.E)]
	[InlineData(ProcedureAirspaceClass.D, ProcedureAirspaceClass.None)]
	public void airspace_rank_orders_b_then_c_then_d_before_everything_else(ProcedureAirspaceClass higher, ProcedureAirspaceClass lower)
	{
		// Same identifier ordinally so airspace rank is the only thing that could put "lower" first.
		ProcedureAirport first = ProcedureTestData.BuiltAirport("ZZZ", airspaceClass: higher);
		ProcedureAirport second = ProcedureTestData.BuiltAirport("AAA", airspaceClass: lower);

		IReadOnlyList<ProcedureAirport> ordered = ProcedureOrdering.OrderAirports([second, first]);

		Assert.Equal(["ZZZ", "AAA"], ordered.Select(a => a.AptIdent));
	}

	[Fact]
	public void class_e_and_no_class_share_the_same_rank_tier_so_they_interleave_by_identifier()
	{
		ProcedureAirport e = ProcedureTestData.BuiltAirport("AAA", airspaceClass: ProcedureAirspaceClass.E);
		ProcedureAirport none = ProcedureTestData.BuiltAirport("ZZZ", airspaceClass: ProcedureAirspaceClass.None);

		IReadOnlyList<ProcedureAirport> ordered = ProcedureOrdering.OrderAirports([none, e]);

		Assert.Equal(["AAA", "ZZZ"], ordered.Select(a => a.AptIdent));
	}

	[Fact]
	public void identifiers_without_digits_sort_before_identifiers_with_digits()
	{
		ProcedureAirport withDigit = ProcedureTestData.BuiltAirport("A1A");
		ProcedureAirport withoutDigit = ProcedureTestData.BuiltAirport("ZZZ");

		IReadOnlyList<ProcedureAirport> ordered = ProcedureOrdering.OrderAirports([withDigit, withoutDigit]);

		Assert.Equal(["ZZZ", "A1A"], ordered.Select(a => a.AptIdent));
	}

	[Fact]
	public void identifiers_in_the_same_digit_group_are_ordered_ordinally()
	{
		ProcedureAirport lower = ProcedureTestData.BuiltAirport("aaa");
		ProcedureAirport upper = ProcedureTestData.BuiltAirport("AAB");

		IReadOnlyList<ProcedureAirport> ordered = ProcedureOrdering.OrderAirports([lower, upper]);

		// Ordinal comparison: upper-case letters sort before lower-case letters.
		Assert.Equal(["AAB", "aaa"], ordered.Select(a => a.AptIdent));
	}
}
