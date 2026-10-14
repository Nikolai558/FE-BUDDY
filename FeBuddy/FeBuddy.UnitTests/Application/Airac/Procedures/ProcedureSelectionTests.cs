using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Procedures;
using FeBuddy.Core.Domain.Procedures.Models;

using FeBuddy.UnitTests.Application.Airac.Procedures.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Procedures;

/// <summary>
/// Covers <see cref="ProcedureSelection.Select"/>: whole-airport inclusion by the area (facility, ROI or everything),
/// explicit airport/procedure/pair picks, the chart-type filter applying only to whole-airport
/// inclusion, the union of every source, dropping airports left with nothing included, and the
/// unmatched-pick warnings.
/// </summary>
public sealed class ProcedureSelectionTests
{
	private static ProcedureSettings Settings(
		IReadOnlyCollection<string>? facilities = null,
		string? primaryFacility = null,
		SubServiceArea? area = null,
		RegionOfInterest? roi = null,
		IReadOnlyCollection<string>? airports = null,
		IReadOnlyCollection<string>? procedures = null,
		IReadOnlyList<ProcedureAirportPick>? airportProcedures = null,
		IReadOnlyCollection<string>? chartTypes = null) =>
		new()
		{
			OutputDirectory = @"C:\unused",
			Facilities = facilities ?? [],
			PrimaryFacility = primaryFacility,
			// The facilities count only as the area, as the parser sees to; without either, none.
			Area = area ?? (facilities is { Count: > 0 } ? SubServiceArea.Artccs : SubServiceArea.None),
			Roi = roi,
			Airports = airports ?? [],
			Procedures = procedures ?? [],
			AirportProcedures = airportProcedures ?? [],
			ChartTypes = chartTypes ?? ProcedureChartTypes.Default,
		};

	[Fact]
	public void select_rejects_null_arguments()
	{
		List<ServiceMessage> messages = [];
		ProcedureSettings settings = Settings();

		Assert.Throws<ArgumentNullException>(() => ProcedureSelection.Select(null!, settings, messages));
		Assert.Throws<ArgumentNullException>(() => ProcedureSelection.Select([], null!, messages));
		Assert.Throws<ArgumentNullException>(() => ProcedureSelection.Select([], settings, null!));
	}

	// ---- facilities ----

	[Fact]
	public void a_facility_match_includes_every_procedure_of_a_chart_type_in_chart_types()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", procedures:
		[
			ProcedureTestData.BuiltProcedure("ONE", chartCode: "IAP"),
			ProcedureTestData.BuiltProcedure("TWO", chartCode: "STR"),
		]);
		List<ServiceMessage> messages = [];

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select([airport], Settings(facilities: ["ZOB"]), messages);

		ProcedureAirport included = Assert.Single(result);
		Assert.Equal(["ONE", "TWO"], included.Procedures.Select(p => p.Name));
		Assert.Empty(messages);
	}

	[Fact]
	public void a_facility_match_excludes_procedures_outside_the_chosen_chart_types()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", procedures:
		[
			ProcedureTestData.BuiltProcedure("ONE", chartCode: "IAP"),
			ProcedureTestData.BuiltProcedure("TWO", chartCode: "MIN"),
		]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[airport], Settings(facilities: ["ZOB"], chartTypes: ["IAP"]), []);

		ProcedureAirport included = Assert.Single(result);
		Assert.Equal(["ONE"], included.Procedures.Select(p => p.Name));
	}

	[Fact]
	public void a_facility_that_does_not_match_excludes_the_airport()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZNY", procedures:
		[
			ProcedureTestData.BuiltProcedure("ONE"),
		]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select([airport], Settings(facilities: ["ZOB"]), []);

		Assert.Empty(result);
	}

	// ---- ROI ----

	[Fact]
	public void roi_airports_are_included_only_when_the_area_is_the_roi()
	{
		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0);
		ProcedureAirport airport = ProcedureTestData.BuiltAirport(
			"AAA", latitude: 40.0, longitude: -80.0, procedures: [ProcedureTestData.BuiltProcedure("ONE")]);

		IReadOnlyList<ProcedureAirport> notIncluded = ProcedureSelection.Select(
			[airport], Settings(area: SubServiceArea.None, roi: roi), []);
		Assert.Empty(notIncluded);

		IReadOnlyList<ProcedureAirport> included = ProcedureSelection.Select(
			[airport], Settings(area: SubServiceArea.Roi, roi: roi), []);
		Assert.Single(included);
	}

	[Fact]
	public void an_airport_without_coordinates_is_skipped_by_the_roi_even_when_the_area_is_the_roi()
	{
		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0);
		ProcedureAirport airport = ProcedureTestData.BuiltAirport(
			"AAA", latitude: null, longitude: null, procedures: [ProcedureTestData.BuiltProcedure("ONE")]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[airport], Settings(area: SubServiceArea.Roi, roi: roi), []);

		Assert.Empty(result);
	}

	[Fact]
	public void the_roi_area_with_no_region_of_interest_set_includes_nothing_by_roi()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport(
			"AAA", latitude: 40.0, longitude: -80.0, procedures: [ProcedureTestData.BuiltProcedure("ONE")]);

		// Select() itself does not validate the combination (the settings parser does) - passing
		// the ROI area with no Roi must simply skip the ROI check rather than throw.
		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[airport], Settings(area: SubServiceArea.Roi, roi: null), []);

		Assert.Empty(result);
	}

	[Fact]
	public void an_airport_with_only_one_coordinate_is_skipped_by_the_roi()
	{
		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0);
		ProcedureAirport airport = ProcedureTestData.BuiltAirport(
			"AAA", latitude: 40.0, longitude: null, procedures: [ProcedureTestData.BuiltProcedure("ONE")]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[airport], Settings(area: SubServiceArea.Roi, roi: roi), []);

		Assert.Empty(result);
	}

	[Fact]
	public void an_airport_outside_the_roi_is_not_included()
	{
		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0);
		ProcedureAirport airport = ProcedureTestData.BuiltAirport(
			"AAA", latitude: 10.0, longitude: 10.0, procedures: [ProcedureTestData.BuiltProcedure("ONE")]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[airport], Settings(area: SubServiceArea.Roi, roi: roi), []);

		Assert.Empty(result);
	}

	/// <summary>Only the area chosen applies: with the ROI, a facility listed brings nothing in.</summary>
	[Fact]
	public void with_the_roi_area_the_facilities_are_not_used()
	{
		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0);
		ProcedureAirport outside = ProcedureTestData.BuiltAirport(
			"AAA", responsibleArtcc: "ZOB", latitude: 30.0, longitude: -90.0, procedures: [ProcedureTestData.BuiltProcedure("ONE")]);

		Assert.Empty(ProcedureSelection.Select([outside], Settings(facilities: ["ZOB"], area: SubServiceArea.Roi, roi: roi), []));
	}

	// ---- everything ----

	[Fact]
	public void everything_includes_every_airport_with_or_without_coordinates_and_still_narrows_by_chart_type()
	{
		ProcedureAirport located = ProcedureTestData.BuiltAirport("AAA", latitude: 40.0, longitude: -80.0, procedures:
		[
			ProcedureTestData.BuiltProcedure("ONE", chartCode: "IAP"),
			ProcedureTestData.BuiltProcedure("TWO", chartCode: "MIN"),
		]);
		ProcedureAirport unlocated = ProcedureTestData.BuiltAirport(
			"BBB", latitude: null, longitude: null, procedures: [ProcedureTestData.BuiltProcedure("THREE", chartCode: "IAP")]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[located, unlocated], Settings(area: SubServiceArea.Everything, chartTypes: ["IAP"]), []);

		Assert.Equal(["AAA", "BBB"], result.Select(airport => airport.AptIdent));
		Assert.Equal(["ONE"], result[0].Procedures.Select(p => p.Name));
	}

	// ---- explicit airports ----

	[Fact]
	public void an_airports_pick_matches_the_faa_identifier()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures: [ProcedureTestData.BuiltProcedure("ONE")]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select([airport], Settings(airports: ["AAA"]), []);

		Assert.Single(result);
	}

	[Fact]
	public void an_airports_pick_matches_the_icao_identifier_ignoring_case()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport(
			"AAA", icaoIdent: "KAAA", procedures: [ProcedureTestData.BuiltProcedure("ONE")]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select([airport], Settings(airports: ["kaaa"]), []);

		Assert.Single(result);
	}

	// ---- Procedures picks: departures and arrivals, by root name ----

	[Fact]
	public void a_procedures_pick_includes_that_departure_or_arrival_at_every_airport_regardless_of_chart_type()
	{
		ProcedureAirport aaa = ProcedureTestData.BuiltAirport("AAA", procedures: [ProcedureTestData.BuiltProcedure("BRWNZ FOUR (RNAV)", chartCode: "STR")]);
		ProcedureAirport bbb = ProcedureTestData.BuiltAirport("BBB", procedures: [ProcedureTestData.BuiltProcedure("BRWNZ FOUR (RNAV)", chartCode: "STR")]);
		List<ServiceMessage> messages = [];

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[aaa, bbb], Settings(procedures: ["BRWNZ"], chartTypes: ["IAP"]), messages);

		Assert.Equal(["AAA", "BBB"], result.Select(airport => airport.AptIdent));
		Assert.All(result, airport => Assert.Equal(["BRWNZ FOUR (RNAV)"], airport.Procedures.Select(p => p.Name)));
		Assert.Empty(messages);
	}

	[Fact]
	public void a_procedures_pick_matches_ignoring_case()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures: [ProcedureTestData.BuiltProcedure("DOTSS TWO (RNAV)", chartCode: "DP")]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[airport], Settings(procedures: ["dotss"]), []);

		Assert.Single(result);
	}

	/// <summary>
	/// The FAA renumbers a departure or arrival at each amendment, and can add or drop its (RNAV): a
	/// pick by root name - or the full name an earlier FE-Buddy saved - still finds it.
	/// </summary>
	[Theory]
	[InlineData("BRWNZ", "BRWNZ FIVE (RNAV)")]
	[InlineData("BRWNZ FOUR (RNAV)", "BRWNZ FIVE (RNAV)")]
	[InlineData("BRWNZ FOUR (RNAV)", "BRWNZ FIVE")]
	[InlineData("BRWNZ FOUR", "BRWNZ ONE (RNAV)")]
	public void a_procedures_pick_follows_the_procedure_through_renumbering(string pick, string renumbered)
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures: [ProcedureTestData.BuiltProcedure(renumbered, chartCode: "STR")]);
		List<ServiceMessage> messages = [];

		ProcedureAirport included = Assert.Single(ProcedureSelection.Select([airport], Settings(procedures: [pick]), messages));

		Assert.Equal([renumbered], included.Procedures.Select(p => p.Name));
		Assert.Empty(messages);
	}

	[Fact]
	public void a_procedures_pick_takes_sids_odps_and_stars()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures:
		[
			ProcedureTestData.BuiltProcedure("DELTA EIGHT", chartCode: "DP"),
			ProcedureTestData.BuiltProcedure("CAPTAIN ONE (OBSTACLE)", chartCode: "ODP"),
			ProcedureTestData.BuiltProcedure("JAYSN THREE (RNAV)", chartCode: "STR"),
		]);

		ProcedureAirport included = Assert.Single(ProcedureSelection.Select(
			[airport], Settings(procedures: ["DELTA", "CAPTAIN", "JAYSN"]), []));

		Assert.Equal(3, included.Procedures.Count);
	}

	/// <summary>
	/// Two procedures can share a root name - a few do, like OCEAN FOUR (a departure) and OCEAN THREE
	/// (an arrival): a pick of it brings in both.
	/// </summary>
	[Fact]
	public void a_procedures_pick_brings_in_every_departure_and_arrival_sharing_its_root()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures:
		[
			ProcedureTestData.BuiltProcedure("OCEAN FOUR", chartCode: "DP"),
			ProcedureTestData.BuiltProcedure("OCEAN THREE", chartCode: "STR"),
			ProcedureTestData.BuiltProcedure("OCEANA TWO", chartCode: "STR"),
		]);

		ProcedureAirport included = Assert.Single(ProcedureSelection.Select([airport], Settings(procedures: ["OCEAN"]), []));

		Assert.Equal(["OCEAN FOUR", "OCEAN THREE"], included.Procedures.Select(p => p.Name));
	}

	/// <summary>An approach, or any chart but a departure or arrival, is never brought in by a pick, and the run says why.</summary>
	[Fact]
	public void a_procedures_pick_naming_an_approach_is_left_out_and_says_why()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures:
		[
			ProcedureTestData.BuiltProcedure("ILS RWY 1", chartCode: "IAP"),
			ProcedureTestData.BuiltProcedure("HOT SPOTS", chartCode: "HOT"),
		]);
		List<ServiceMessage> messages = [];

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select([airport], Settings(procedures: ["ILS RWY 1"]), messages);

		Assert.Empty(result);
		Assert.Equal(
			"'ILS RWY 1' isn't a departure or arrival, so it's left out: Additional Procedures to Include takes departures and arrivals only.",
			Assert.Single(messages).Text);
	}

	// ---- AirportProcedures picks ----

	[Fact]
	public void an_airport_procedures_pick_includes_that_pair_regardless_of_chart_type()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures:
		[
			ProcedureTestData.BuiltProcedure("DOTSS TWO (RNAV)", chartCode: "DP"),
		]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[airport], Settings(airportProcedures: [new ProcedureAirportPick("AAA", "DOTSS")], chartTypes: ["IAP"]), []);

		Assert.Single(result);
	}

	[Fact]
	public void an_airport_procedures_pick_does_not_apply_to_airports_it_does_not_name()
	{
		ProcedureAirport aaa = ProcedureTestData.BuiltAirport("AAA", procedures: [ProcedureTestData.BuiltProcedure("GRUUB ONE (RNAV)", chartCode: "STR")]);
		ProcedureAirport bbb = ProcedureTestData.BuiltAirport("BBB", procedures: [ProcedureTestData.BuiltProcedure("GRUUB ONE (RNAV)", chartCode: "STR")]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[aaa, bbb], Settings(airportProcedures: [new ProcedureAirportPick("BBB", "GRUUB")]), []);

		ProcedureAirport included = Assert.Single(result);
		Assert.Equal("BBB", included.AptIdent);
	}

	[Fact]
	public void an_airport_procedures_pick_matches_the_airport_by_icao_identifier_too()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport(
			"AAA", icaoIdent: "KAAA", procedures: [ProcedureTestData.BuiltProcedure("GRUUB ONE (RNAV)", chartCode: "STR")]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[airport], Settings(airportProcedures: [new ProcedureAirportPick("KAAA", "GRUUB")]), []);

		Assert.Single(result);
	}

	[Fact]
	public void an_airport_procedures_pick_follows_the_procedure_through_renumbering()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("LAS", procedures: [ProcedureTestData.BuiltProcedure("BRWNZ FIVE", chartCode: "STR")]);
		List<ServiceMessage> messages = [];

		ProcedureAirport included = Assert.Single(ProcedureSelection.Select(
			[airport], Settings(airportProcedures: [new ProcedureAirportPick("LAS", "BRWNZ FOUR (RNAV)")]), messages));

		Assert.Equal(["BRWNZ FIVE"], included.Procedures.Select(p => p.Name));
		Assert.Empty(messages);
	}

	[Fact]
	public void an_airport_procedures_pick_naming_an_approach_is_left_out_and_says_why()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("PIT", procedures: [ProcedureTestData.BuiltProcedure("ILS OR LOC RWY 28C", chartCode: "IAP")]);
		List<ServiceMessage> messages = [];

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[airport], Settings(airportProcedures: [new ProcedureAirportPick("PIT", "ILS OR LOC RWY 28C")]), messages);

		Assert.Empty(result);
		Assert.Equal(
			"'ILS OR LOC RWY 28C' at PIT isn't a departure or arrival, so it's left out: Additional Airport-Specific Procedures to Include takes departures and arrivals only.",
			Assert.Single(messages).Text);
	}

	// ---- procedures with a fix in the facility (Feature #367) ----

	private static readonly FacilityFixMatch InZdc = new("ZDC", ["DUCKY", "OTTTO"]);

	[Fact]
	public void a_chart_whose_route_has_a_fix_in_the_facility_is_added_and_labeled()
	{
		Procedure star = ProcedureTestData.BuiltProcedure("JIIMS FOUR (RNAV)", chartCode: "STR", change: ProcedureChange.Changed);
		Procedure other = ProcedureTestData.BuiltProcedure("ILS RWY 9R", chartCode: "IAP");
		ProcedureAirport phl = ProcedureTestData.BuiltAirport("PHL", responsibleArtcc: "ZNY", procedures: [star, other]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[phl], Settings(facilities: ["ZDC"]), [], new Dictionary<Procedure, FacilityFixMatch>(ReferenceEqualityComparer.Instance) { [star] = InZdc });

		Procedure included = Assert.Single(Assert.Single(result).Procedures);
		Assert.Equal("JIIMS FOUR (RNAV)", included.Name);
		Assert.Same(InZdc, included.IncludedForFacilityFixes);
		Assert.Null(star.IncludedForFacilityFixes); // the built procedure itself is left as it was
	}

	/// <summary>
	/// At one of the user's own facility's airports the label says nothing new, so the chart is added
	/// without it; at another facility's airport it keeps it (Feature #371).
	/// </summary>
	[Fact]
	public void a_chart_at_an_airport_in_the_users_facility_is_added_without_the_label()
	{
		Procedure ownStar = ProcedureTestData.BuiltProcedure("TRISH FIVE (RNAV)", chartCode: "STR");
		Procedure otherStar = ProcedureTestData.BuiltProcedure("JIIMS FOUR (RNAV)", chartCode: "STR");
		ProcedureAirport iad = ProcedureTestData.BuiltAirport("IAD", responsibleArtcc: "ZDC", procedures: [ownStar]);
		ProcedureAirport phl = ProcedureTestData.BuiltAirport("PHL", responsibleArtcc: "ZNY", procedures: [otherStar]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[iad, phl], Settings(primaryFacility: "zdc"), [],
			new Dictionary<Procedure, FacilityFixMatch>(ReferenceEqualityComparer.Instance) { [ownStar] = InZdc, [otherStar] = InZdc });

		Assert.Equal(["IAD", "PHL"], result.Select(airport => airport.AptIdent));
		Assert.Null(Assert.Single(result[0].Procedures).IncludedForFacilityFixes);
		Assert.Same(InZdc, Assert.Single(result[1].Procedures).IncludedForFacilityFixes);
	}

	[Fact]
	public void a_chart_the_area_or_a_pick_already_includes_gets_no_label()
	{
		Procedure byArea = ProcedureTestData.BuiltProcedure("TRISH FIVE (RNAV)", chartCode: "STR");
		Procedure byPick = ProcedureTestData.BuiltProcedure("JIIMS FOUR (RNAV)", chartCode: "STR");
		ProcedureAirport iad = ProcedureTestData.BuiltAirport("IAD", responsibleArtcc: "ZDC", procedures: [byArea]);
		ProcedureAirport phl = ProcedureTestData.BuiltAirport("PHL", responsibleArtcc: "ZNY", procedures: [byPick]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[iad, phl], Settings(facilities: ["ZDC"], procedures: ["JIIMS"]), [],
			new Dictionary<Procedure, FacilityFixMatch>(ReferenceEqualityComparer.Instance) { [byArea] = InZdc, [byPick] = InZdc });

		Assert.All(result.SelectMany(airport => airport.Procedures), procedure => Assert.Null(procedure.IncludedForFacilityFixes));
		Assert.Equal(2, result.Sum(airport => airport.Procedures.Count));
	}

	[Fact]
	public void a_chart_type_not_ticked_is_never_added_for_its_route()
	{
		Procedure odp = ProcedureTestData.BuiltProcedure("BWI ONE (OBSTACLE)", chartCode: "ODP");
		ProcedureAirport bwi = ProcedureTestData.BuiltAirport("BWI", responsibleArtcc: "ZNY", procedures: [odp]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[bwi], Settings(facilities: ["ZDC"], chartTypes: ["IAP", "STR", "DP"]), [],
			new Dictionary<Procedure, FacilityFixMatch>(ReferenceEqualityComparer.Instance) { [odp] = InZdc });

		Assert.Empty(result);
	}

	// ---- union and dropping ----

	[Fact]
	public void selection_is_the_union_of_every_source()
	{
		ProcedureAirport facilityAirport = ProcedureTestData.BuiltAirport(
			"AAA", responsibleArtcc: "ZOB", procedures: [ProcedureTestData.BuiltProcedure("ONE")]);
		ProcedureAirport pickedAirport = ProcedureTestData.BuiltAirport(
			"BBB", procedures: [ProcedureTestData.BuiltProcedure("TWO")]);
		ProcedureAirport procedurePickAirport = ProcedureTestData.BuiltAirport(
			"CCC", procedures: [ProcedureTestData.BuiltProcedure("THREE", chartCode: "STR")]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[facilityAirport, pickedAirport, procedurePickAirport],
			Settings(facilities: ["ZOB"], airports: ["BBB"], procedures: ["THREE"]),
			[]);

		Assert.Equal(["AAA", "BBB", "CCC"], result.Select(a => a.AptIdent));
	}

	[Fact]
	public void an_airport_with_nothing_included_is_dropped()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZNY", procedures:
		[
			ProcedureTestData.BuiltProcedure("ONE", chartCode: "MIN"), // not in the default ChartTypes and not picked
		]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select([airport], Settings(facilities: ["ZNY"], chartTypes: ["IAP"]), []);

		Assert.Empty(result);
	}

	[Fact]
	public void included_procedures_keep_their_original_chartseq_order()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", procedures:
		[
			ProcedureTestData.BuiltProcedure("ONE", chartSeq: 10),
			ProcedureTestData.BuiltProcedure("TWO", chartSeq: 20),
			ProcedureTestData.BuiltProcedure("THREE", chartSeq: 30),
		]);

		ProcedureAirport included = Assert.Single(ProcedureSelection.Select([airport], Settings(facilities: ["ZOB"]), []));

		Assert.Equal(["ONE", "TWO", "THREE"], included.Procedures.Select(p => p.Name));
	}

	// ---- warnings ----

	[Fact]
	public void an_unmatched_airport_pick_warns_once()
	{
		List<ServiceMessage> messages = [];

		ProcedureSelection.Select([], Settings(airports: ["ZZZ"]), messages);

		ServiceMessage message = Assert.Single(messages);
		Assert.Equal("ProcedureSelection", message.Source);
		Assert.Contains("ZZZ", message.Text);
	}

	[Fact]
	public void an_unmatched_procedure_pick_warns_once()
	{
		List<ServiceMessage> messages = [];

		ProcedureSelection.Select([], Settings(procedures: ["MISSING PROC"]), messages);

		ServiceMessage message = Assert.Single(messages);
		Assert.Equal("No departure or arrival named 'MISSING PROC' is in this cycle's d-TPP metafile.", message.Text);
	}

	[Fact]
	public void an_unmatched_airport_procedure_pair_warns_once()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures: [ProcedureTestData.BuiltProcedure("ONE")]);
		List<ServiceMessage> messages = [];

		ProcedureSelection.Select([airport], Settings(airportProcedures: [new ProcedureAirportPick("AAA", "MISSING")]), messages);

		ServiceMessage message = Assert.Single(messages);
		Assert.Equal("Airport 'AAA' has no departure or arrival named 'MISSING' in this cycle's d-TPP metafile.", message.Text);
	}

	[Fact]
	public void a_pick_that_also_matches_an_already_included_airport_does_not_warn()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport(
			"AAA", responsibleArtcc: "ZOB", procedures: [ProcedureTestData.BuiltProcedure("ONE")]);
		List<ServiceMessage> messages = [];

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[airport], Settings(facilities: ["ZOB"], airports: ["AAA"]), messages);

		Assert.Single(result);
		Assert.Empty(messages);
	}
}
