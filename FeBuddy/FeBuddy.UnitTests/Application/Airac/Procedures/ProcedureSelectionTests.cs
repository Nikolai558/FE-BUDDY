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

	// ---- Procedures picks ----

	[Fact]
	public void a_procedures_pick_includes_that_procedure_at_any_airport_regardless_of_chart_type()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures:
		[
			ProcedureTestData.BuiltProcedure("HOT SPOTS", chartCode: "HOT"), // not in the default ChartTypes
		]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[airport], Settings(procedures: ["HOT SPOTS"]), []);

		ProcedureAirport included = Assert.Single(result);
		Assert.Equal(["HOT SPOTS"], included.Procedures.Select(p => p.Name));
	}

	[Fact]
	public void a_procedures_pick_matches_ignoring_case()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures: [ProcedureTestData.BuiltProcedure("ILS RWY 1")]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[airport], Settings(procedures: ["ils rwy 1"]), []);

		Assert.Single(result);
	}

	// ---- AirportProcedures picks ----

	[Fact]
	public void an_airport_procedures_pick_includes_that_pair_regardless_of_chart_type()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures:
		[
			ProcedureTestData.BuiltProcedure("HOT SPOTS", chartCode: "HOT"),
		]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[airport], Settings(airportProcedures: [new ProcedureAirportPick("AAA", "HOT SPOTS")]), []);

		Assert.Single(result);
	}

	[Fact]
	public void an_airport_procedures_pick_does_not_apply_to_airports_it_does_not_name()
	{
		ProcedureAirport aaa = ProcedureTestData.BuiltAirport("AAA", procedures: [ProcedureTestData.BuiltProcedure("ONE", chartCode: "HOT")]);
		ProcedureAirport bbb = ProcedureTestData.BuiltAirport("BBB", procedures: [ProcedureTestData.BuiltProcedure("ONE", chartCode: "HOT")]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[aaa, bbb], Settings(airportProcedures: [new ProcedureAirportPick("BBB", "ONE")]), []);

		ProcedureAirport included = Assert.Single(result);
		Assert.Equal("BBB", included.AptIdent);
	}

	[Fact]
	public void an_airport_procedures_pick_matches_the_airport_by_icao_identifier_too()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport(
			"AAA", icaoIdent: "KAAA", procedures: [ProcedureTestData.BuiltProcedure("ILS RWY 1")]);

		IReadOnlyList<ProcedureAirport> result = ProcedureSelection.Select(
			[airport], Settings(airportProcedures: [new ProcedureAirportPick("KAAA", "ILS RWY 1")]), []);

		Assert.Single(result);
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
			"CCC", procedures: [ProcedureTestData.BuiltProcedure("THREE", chartCode: "HOT")]);

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
		Assert.Contains("MISSING PROC", message.Text);
	}

	[Fact]
	public void an_unmatched_airport_procedure_pair_warns_once()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures: [ProcedureTestData.BuiltProcedure("ONE")]);
		List<ServiceMessage> messages = [];

		ProcedureSelection.Select([airport], Settings(airportProcedures: [new ProcedureAirportPick("AAA", "MISSING")]), messages);

		ServiceMessage message = Assert.Single(messages);
		Assert.Contains("AAA", message.Text);
		Assert.Contains("MISSING", message.Text);
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
