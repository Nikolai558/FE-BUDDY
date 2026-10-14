using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Domain.Procedures.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Procedures.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Procedures;

/// <summary>
/// Covers <see cref="ProceduresWithFixInFacility"/> (Feature #367): a chart is matched to its route at
/// each airport the route serves, by computer code or, when either has none, by root name without
/// punctuation; and the route that counts depends on the chart's change - the old one for a deleted
/// chart, either for a changed one.
/// </summary>
public sealed class ProceduresWithFixInFacilityTests
{
	private const string Facility = "ZDC";

	/// <summary>DUCKY and OTTTO are in ZDC, WEVVE in ZNY; DCA is a ZDC VOR/DME.</summary>
	private static readonly FixCsvDataModel.FixBase[] Fixes =
	[
		ProcedureTestData.FixRow("DUCKY", "ZDC", "ZDC"),
		ProcedureTestData.FixRow("OTTTO", null, "ZDC"),
		ProcedureTestData.FixRow("WEVVE", "ZNY", "ZNY"),
	];

	private static readonly NavCsvDataModel.NavBase[] Navaids = [ProcedureTestData.NavRow("DCA", "VOR/DME", "ZDC", "ZDC")];

	private static ProcedureRouteSet Routes(
		IEnumerable<(DpCsvDataModel.DpBase, IEnumerable<DpCsvDataModel.DpRte>)>? departures = null,
		IEnumerable<(StarCsvDataModel.StarBase, IEnumerable<StarCsvDataModel.StarRte>)>? arrivals = null) =>
		ProcedureRouteSet.FromNasr(ProcedureTestData.RouteNasr(departures: departures, arrivals: arrivals, fixes: Fixes, navaids: Navaids));

	private static ProcedureAirport Airport(string id, params Procedure[] procedures) =>
		ProcedureTestData.BuiltAirport(id, icaoIdent: "K" + id, responsibleArtcc: "ZNY", procedures: procedures);

	private static Procedure Star(string name, string? code, ProcedureChange change = ProcedureChange.None) =>
		ProcedureTestData.BuiltProcedure(name, chartCode: "STR", change: change, computerCode: code);

	private static Procedure Departure(string name, string? code, ProcedureChange change = ProcedureChange.None) =>
		ProcedureTestData.BuiltProcedure(name, chartCode: "DP", change: change, computerCode: code);

	private static IReadOnlyDictionary<Procedure, FacilityFixMatch> Find(ProcedureRouteSet current, ProcedureRouteSet? previous, params ProcedureAirport[] airports) =>
		ProceduresWithFixInFacility.Find(airports, Facility, current, previous, useAirports: false);

	[Fact]
	public void a_star_serving_several_airports_is_found_at_each_with_its_fixes_in_route_order()
	{
		ProcedureRouteSet routes = Routes(arrivals:
		[
			ProcedureTestData.Arrival("WEVVE.JIIMS4", "JIIMS FOUR (RNAV)", "PHL, TTN",
				("WEVVE", "WP", "K6"), ("OTTTO", "WP", "K6"), ("DCA", "VOR/DME", null), ("OTTTO", "WP", "K6"), ("DUCKY", "WP", "K6")),
		]);
		Procedure phl = Star("JIIMS FOUR (RNAV)", "WEVVE.JIIMS4");
		Procedure ttn = Star("JIIMS FOUR (RNAV)", "weVVe.jiims4");

		IReadOnlyDictionary<Procedure, FacilityFixMatch> found = Find(routes, null, Airport("PHL", phl), Airport("TTN", ttn));

		Assert.Equal(Facility, found[phl].Facility);
		Assert.Equal(["OTTTO", "DCA", "DUCKY"], found[phl].Fixes);
		Assert.Equal(["OTTTO", "DCA", "DUCKY"], found[ttn].Fixes);
	}

	[Fact]
	public void a_route_with_no_fix_in_the_facility_finds_nothing()
	{
		ProcedureRouteSet routes = Routes(arrivals: [ProcedureTestData.Arrival("WEVVE.WEVVE1", "WEVVE ONE", "PHL", ("WEVVE", "WP", "K6"))]);

		Assert.Empty(Find(routes, null, Airport("PHL", Star("WEVVE ONE", "WEVVE.WEVVE1"))));
	}

	[Fact]
	public void a_route_is_found_by_the_airports_icao_id_too()
	{
		ProcedureRouteSet routes = Routes(arrivals: [ProcedureTestData.Arrival("DUCKY.DUCKY1", "DUCKY ONE", "KPHL", ("DUCKY", "WP", "K6"))]);
		Procedure chart = Star("DUCKY ONE", "DUCKY.DUCKY1");

		Assert.True(Find(routes, null, Airport("PHL", chart)).ContainsKey(chart));
	}

	// ---- matching a chart to its route ----

	[Fact]
	public void a_different_code_is_a_different_procedure_whatever_the_name()
	{
		ProcedureRouteSet routes = Routes(arrivals: [ProcedureTestData.Arrival("DUCKY.DUCKY1", "DUCKY ONE", "PHL", ("DUCKY", "WP", "K6"))]);

		Assert.Empty(Find(routes, null, Airport("PHL", Star("DUCKY ONE", "DUCKY.DUCKY2"))));
	}

	/// <summary>NASR's obstacle departures have no code (NOT ASSIGNED): the name decides, punctuation left out.</summary>
	[Fact]
	public void a_route_with_no_code_is_matched_by_root_name_without_punctuation()
	{
		ProcedureRouteSet routes = Routes(departures: [ProcedureTestData.Departure("NOT ASSIGNED", "O'HARE", "ORD", ("DCA", "VOR/DME", null))]);
		Procedure chart = Departure("OHARE NINE", "OHARE9.OHARE");

		Assert.Equal(["DCA"], Find(routes, null, Airport("ORD", chart))[chart].Fixes);
	}

	/// <summary>The d-TPP has no code for HUDSN ONE (COPTER) at JRB; NASR does.</summary>
	[Fact]
	public void a_chart_with_no_code_is_matched_by_root_name()
	{
		ProcedureRouteSet routes = Routes(departures: [ProcedureTestData.Departure("HUDSN1.YOMAN", "HUDSN (COPTER)", "JRB", ("DUCKY", "WP", "K6"))]);
		Procedure chart = Departure("HUDSN ONE (COPTER) (RNAV)", null);

		Assert.True(Find(routes, null, Airport("JRB", chart)).ContainsKey(chart));
	}

	[Fact]
	public void a_route_is_never_another_chart_types()
	{
		ProcedureRouteSet routes = Routes(arrivals: [ProcedureTestData.Arrival("DUCKY.DUCKY1", "DUCKY ONE", "PHL", ("DUCKY", "WP", "K6"))]);

		Assert.Empty(Find(routes, null, Airport("PHL", Departure("DUCKY ONE", "DUCKY.DUCKY1"))));
	}

	[Theory]
	[InlineData("DUCKY1.DUCKY", "DUCKY", "DUCKY ONE (RNAV)", "DP", true)]
	[InlineData("DUCKY1.DUCKY", "DUCKY", "DUCKY ONE (RNAV)", "ODP", true)]
	[InlineData("DUCKY1.DUCKY", "DUCKY", "DUCKY ONE (RNAV)", "STR", false)]
	[InlineData(null, "DUCKY", "DUCKY TWO", "DP", true)] // a renumbered name is the same root
	[InlineData(null, "DUCKY", "OTTTO ONE", "DP", false)]
	[InlineData(null, "--", "--", "DP", false)] // nothing of either name to match on
	public void matches_compares_the_chart_type_then_the_code_or_the_name(string? code, string routeName, string chartName, string chartType, bool matches)
	{
		ProcedureRoute route = new(["DP", "ODP"], code, routeName, ["AAA"], []);

		Assert.Equal(matches, ProceduresWithFixInFacility.Matches(route, ProcedureTestData.BuiltProcedure(chartName, chartCode: chartType, computerCode: code)));
	}

	// ---- which cycle's route ----

	/// <summary>The old route goes through ZDC; the new one doesn't.</summary>
	private static readonly ProcedureRouteSet Old = Routes(arrivals: [ProcedureTestData.Arrival("WEVVE.JIIMS4", "JIIMS FOUR", "PHL", ("DUCKY", "WP", "K6"))]);

	private static readonly ProcedureRouteSet New = Routes(arrivals: [ProcedureTestData.Arrival("WEVVE.JIIMS4", "JIIMS FOUR", "PHL", ("WEVVE", "WP", "K6"))]);

	[Fact]
	public void a_deleted_chart_is_checked_against_its_route_last_cycle()
	{
		Procedure chart = Star("JIIMS FOUR", "WEVVE.JIIMS4", ProcedureChange.Deleted);

		Assert.Equal(["DUCKY"], Find(New, Old, Airport("PHL", chart))[chart].Fixes);
		Assert.Empty(Find(Old, New, Airport("PHL", chart)));
	}

	[Fact]
	public void without_last_cycles_routes_a_deleted_chart_is_checked_against_this_cycles()
	{
		Procedure chart = Star("JIIMS FOUR", "WEVVE.JIIMS4", ProcedureChange.Deleted);

		Assert.Empty(Find(New, null, Airport("PHL", chart)));
		Assert.True(Find(Old, null, Airport("PHL", chart)).ContainsKey(chart));
	}

	[Theory]
	[InlineData(ProcedureChange.Changed)]
	[InlineData(ProcedureChange.ReAdded)]
	public void a_changed_chart_is_included_when_either_route_has_a_fix_in_the_facility(ProcedureChange change)
	{
		Procedure chart = Star("JIIMS FOUR", "WEVVE.JIIMS4", change);

		Assert.Equal(["DUCKY"], Find(New, Old, Airport("PHL", chart))[chart].Fixes); // only the old route did: it left ZDC
		Assert.Equal(["DUCKY"], Find(Old, New, Airport("PHL", chart))[chart].Fixes); // only the new route does
		Assert.Empty(Find(New, New, Airport("PHL", chart)));
		Assert.Empty(Find(New, null, Airport("PHL", chart)));
	}

	[Theory]
	[InlineData(ProcedureChange.New)]
	[InlineData(ProcedureChange.None)]
	public void a_current_or_new_chart_is_checked_against_this_cycles_route_only(ProcedureChange change)
	{
		Procedure chart = Star("JIIMS FOUR", "WEVVE.JIIMS4", change);

		Assert.Empty(Find(New, Old, Airport("PHL", chart)));
		Assert.True(Find(Old, New, Airport("PHL", chart)).ContainsKey(chart));
	}

	[Fact]
	public void find_and_matches_reject_missing_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => ProceduresWithFixInFacility.Find(null!, Facility, New, null, false));
		Assert.Throws<ArgumentException>(() => ProceduresWithFixInFacility.Find([], " ", New, null, false));
		Assert.Throws<ArgumentNullException>(() => ProceduresWithFixInFacility.Find([], Facility, null!, null, false));
		Assert.Throws<ArgumentNullException>(() => ProceduresWithFixInFacility.Matches(null!, Star("A", null)));
		Assert.Throws<ArgumentNullException>(() => ProceduresWithFixInFacility.Matches(New.Routes[0], null!));
	}
}
