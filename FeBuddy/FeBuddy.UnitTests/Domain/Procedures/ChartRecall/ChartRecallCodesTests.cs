using FeBuddy.Core.Domain.Procedures.ChartRecall;
using FeBuddy.Core.Domain.Procedures.ChartRecall.Models;

namespace FeBuddy.UnitTests.Domain.Procedures.ChartRecall;

/// <summary>
/// Covers <see cref="ChartRecallCodes"/> for every chart type: approach codes (type, back course,
/// variant, full runway, circling letter, "OR" parts, several runways, charted visuals), the rules
/// that skip a chart or a part, what is reported as unrecognized, the minimums/diagram/hot spot/LAHSO
/// codes, and departure/STAR codes from the computer code or, without one, from the chart name.
/// </summary>
public sealed class ChartRecallCodesTests
{
	private static ChartRecallCodeResult Approach(string name) => ChartRecallCodes.For("IAP", name, computerCode: null, airportName: "TEST AIRPORT");

	// ---- approaches ----

	[Theory]
	[InlineData("ILS OR LOC RWY 22L", "I22L,L22L")]
	[InlineData("ILS Y RWY 22L", "IY22L")]
	[InlineData("ILS Y OR LOC Y RWY 22L", "IY22L,LY22L")]
	[InlineData("ILS RWY 23", "I23")]
	[InlineData("VOR RWY 01L", "O01L")]
	[InlineData("NDB RWY 5", "N5")]
	[InlineData("RNAV (GPS) Y RWY 22L", "RY22L")]
	[InlineData("RNAV (RNP) Z RWY 19", "RZ19")]
	[InlineData("LOC RWY 19", "L19")]
	[InlineData("LOC/DME RWY 24", "LD24")]
	[InlineData("VOR/DME RWY 03", "OD03")]
	[InlineData("NDB/DME RWY 23", "ND23")]
	[InlineData("LDA X RWY 08", "DX08")]
	[InlineData("LDA/DME RWY 24", "DD24")]
	[InlineData("GPS RWY 18", "G18")]
	[InlineData("TACAN RWY 23", "T23")]
	[InlineData("ILS OR LOC/DME RWY 06", "I06,LD06")]
	[InlineData("ILS OR LOC OR RNAV (GPS) RWY 21L", "I21L,L21L,R21L")]
	public void an_approach_gets_its_type_variant_and_full_runway(string name, string expected) =>
		Assert.Equal(expected.Split(','), Approach(name).Codes);

	[Theory]
	[InlineData("ILS Z OR LOC RWY 23", "IZ23,LZ23")]
	[InlineData("VOR OR TACAN Y RWY 12", "OY12,TY12")]
	public void a_variant_printed_on_one_or_part_applies_to_every_part(string name, string expected) =>
		Assert.Equal(expected.Split(','), Approach(name).Codes);

	[Theory]
	[InlineData("LOC BC RWY 33", "LBC33")]
	[InlineData("LOC/DME BC RWY 18", "LDBC18")]
	[InlineData("LOC BC Y RWY 26", "LBCY26")]
	[InlineData("LOC/DME BC-A", "LDBCA")]
	public void a_back_course_adds_bc_straight_after_the_type(string name, string expected) =>
		Assert.Equal(expected.Split(','), Approach(name).Codes);

	[Theory]
	[InlineData("LOC-A", "LA")]
	[InlineData("LOC/DME-A", "LDA")]
	[InlineData("VOR-A", "OA")]
	[InlineData("VOR/DME-A", "ODA")]
	[InlineData("NDB-A", "NA")]
	[InlineData("NDB/DME-A", "NDA")]
	[InlineData("LDA-A", "DA")]
	[InlineData("LDA/DME-A", "DDA")]
	[InlineData("GPS-A", "GA")]
	[InlineData("RNAV (GPS)-B", "RB")]
	[InlineData("TACAN-A", "TA")]
	[InlineData("VOR OR GPS-A", "OA,GA")]
	[InlineData("VOR OR TACAN OR GPS-A", "OA,TA,GA")]
	public void a_circling_approach_keeps_its_letter(string name, string expected) =>
		Assert.Equal(expected.Split(','), Approach(name).Codes);

	[Theory]
	[InlineData("VOR RWY 26L/R", "O26L,O26R")]
	[InlineData("VOR OR GPS RWY 13L/R", "O13L,O13R,G13L,G13R")]
	[InlineData("LOC/DME RWY 12/30", "LD12,LD30")]
	public void a_chart_for_several_runways_gets_a_code_per_runway(string name, string expected) =>
		Assert.Equal(expected.Split(','), Approach(name).Codes);

	[Theory]
	[InlineData("RIVER VISUAL RWY 19", "vRIVER19")]
	[InlineData("RIVER VISUAL RWY 16R", "vRIVER16R")]
	[InlineData("RIVER VISUAL", "vRIVER")]
	[InlineData("LA RIVER VISUAL RWY 12", "vLARIVER12")]
	[InlineData("ROUTE 80 VISUAL RWY 23", "vROUTE8023")]
	[InlineData("BAY VISUAL RWY 16 R/C/L", "vBAY16R,vBAY16C,vBAY16L")]
	[InlineData("TIPP TOE VISUAL RWY 28L/R", "vTIPPTOE28L,vTIPPTOE28R")]
	public void a_charted_visual_is_v_its_name_and_its_runway(string name, string expected) =>
		Assert.Equal(expected.Split(','), Approach(name).Codes);

	[Fact]
	public void a_continuation_page_gets_the_same_codes_as_its_chart() =>
		Assert.Equal(["I22L", "L22L"], Approach("ILS OR LOC RWY 22L, CONT.1").Codes);

	[Fact]
	public void the_name_and_chart_type_are_read_ignoring_case() =>
		Assert.Equal(["I22L", "L22L"], ChartRecallCodes.For("iap", "ils or loc rwy 22l", null, string.Empty).Codes);

	[Theory]
	[InlineData("HI-ILS OR LOC RWY 15", ChartRecallSkipReason.HighAltitude)]
	[InlineData("HI-VOR OR TACAN RWY 36", ChartRecallSkipReason.HighAltitude)]
	[InlineData("COPTER ILS OR LOC RWY 30", ChartRecallSkipReason.Copter)]
	[InlineData("COPTER NDB 303", ChartRecallSkipReason.Copter)]
	[InlineData("ILS PRM RWY 09R", ChartRecallSkipReason.Prm)]
	[InlineData("RNAV (GPS) PRM RWY 09R", ChartRecallSkipReason.Prm)]
	[InlineData("PRM AAUP", ChartRecallSkipReason.Prm)]
	[InlineData("ILS RWY 04R (SA CAT I)", ChartRecallSkipReason.CategoryApproach)]
	[InlineData("ILS RWY 22L (CAT II - III)", ChartRecallSkipReason.CategoryApproach)]
	[InlineData("CONVERGING ILS RWY 17C", ChartRecallSkipReason.Converging)]
	[InlineData("RNAV (RNP) AAUP", ChartRecallSkipReason.Aaup)]
	[InlineData("VOR-1 RWY 14L", ChartRecallSkipReason.NumberedApproach)]
	[InlineData("GLS RWY 19L", ChartRecallSkipReason.Gls)]
	public void a_rule_skips_the_whole_chart(string name, ChartRecallSkipReason reason)
	{
		ChartRecallCodeResult result = Approach(name);

		Assert.Equal(reason, result.SkipReason);
		Assert.Empty(result.Codes);
		Assert.Empty(result.Unrecognized);
	}

	[Fact]
	public void a_loc_ndb_part_is_skipped_but_the_rest_of_the_chart_gets_its_command()
	{
		ChartRecallCodeResult result = Approach("ILS OR LOC/NDB RWY 10");

		Assert.Equal(["I10"], result.Codes);
		Assert.Equal([ChartRecallSkipReason.LocNdb], result.SkippedParts);
		Assert.Null(result.SkipReason);
	}

	[Fact]
	public void an_unknown_approach_type_is_unrecognized()
	{
		ChartRecallCodeResult result = Approach("SDF RWY 36");

		Assert.Empty(result.Codes);
		Assert.Null(result.SkipReason);
		Assert.Equal(["the approach type 'SDF'"], result.Unrecognized);
	}

	[Fact]
	public void an_unknown_or_part_is_unrecognized_while_the_known_parts_get_their_commands()
	{
		ChartRecallCodeResult result = Approach("ILS OR SDF RWY 36");

		Assert.Equal(["I36"], result.Codes);
		Assert.Equal(["the approach type 'SDF'"], result.Unrecognized);
	}

	[Fact]
	public void a_part_that_does_not_read_as_an_approach_type_is_unrecognized() =>
		Assert.Equal(["the approach type 'ILS Z (SPECIAL)'"], Approach("ILS Z (SPECIAL) RWY 4").Unrecognized);

	[Theory]
	[InlineData("ILS RWY 22L/")]
	[InlineData("ILS RWY R/L")]
	[InlineData("ILS RWY 4-22")]
	public void an_unreadable_runway_is_unrecognized(string name)
	{
		ChartRecallCodeResult result = Approach(name);

		Assert.Empty(result.Codes);
		Assert.Equal([$"the runway in '{name}'"], result.Unrecognized);
	}

	[Fact]
	public void an_unreadable_visual_runway_is_unrecognized() =>
		Assert.Equal(["the runway in 'RIVER VISUAL RWY 4-22'"], Approach("RIVER VISUAL RWY 4-22").Unrecognized);

	[Fact]
	public void a_visual_whose_name_has_no_letters_or_digits_is_unrecognized() =>
		Assert.Equal(["the charted visual approach name in '- VISUAL RWY 1'"], Approach("- VISUAL RWY 1").Unrecognized);

	[Fact]
	public void an_approach_with_no_runway_and_no_circling_letter_is_unrecognized()
	{
		ChartRecallCodeResult result = Approach("RNAV (GPS)");

		Assert.Empty(result.Codes);
		Assert.Equal(["an approach with no runway or circling letter, 'RNAV (GPS)'"], result.Unrecognized);
	}

	[Theory]
	[InlineData("22L", "22L")]
	[InlineData("30L/R", "30L,30R")]
	[InlineData("16 R/C/L", "16R,16C,16L")]
	[InlineData("12/30", "12,30")]
	[InlineData("", "")]
	public void runways_expand_one_per_code_exactly_as_printed(string runwayText, string expected) =>
		Assert.Equal(expected.Split(','), ApproachCodes.ExpandRunways(runwayText));

	// ---- other chart types ----

	[Theory]
	[InlineData("APD", "AIRPORT DIAGRAM", "APD")]
	[InlineData("APD", "AIRPORT DIAGRAM (ROGERS LAKEBED)", "APD")]
	[InlineData("HOT", "HOT SPOT", "HS")]
	[InlineData("LAH", "LAHSO", "LAHSO")]
	public void diagrams_hot_spots_and_lahso_get_their_fixed_code(string chartCode, string name, string expected)
	{
		ChartRecallCodeResult result = ChartRecallCodes.For(chartCode, name, null, string.Empty);

		Assert.Equal([expected], result.Codes);
		Assert.False(result.OpensAtAirportPage);
	}

	[Theory]
	[InlineData("TAKEOFF MINIMUMS", "TM")]
	[InlineData("DIVERSE VECTOR AREA", "DVA")]
	[InlineData("RADAR MINIMUMS", "RM")]
	public void a_minimums_sheet_opens_at_the_airports_own_page(string name, string expected)
	{
		ChartRecallCodeResult result = ChartRecallCodes.For("MIN", name, null, string.Empty);

		Assert.Equal([expected], result.Codes);
		Assert.True(result.OpensAtAirportPage);
	}

	[Fact]
	public void the_alternate_minimums_sheet_is_skipped() =>
		Assert.Equal(ChartRecallSkipReason.AlternateMinimums, ChartRecallCodes.For("MIN", "ALTERNATE MINIMUMS", null, string.Empty).SkipReason);

	[Fact]
	public void an_unknown_minimums_sheet_is_unrecognized() =>
		Assert.Equal(["the minimums sheet 'NIGHT MINIMUMS'"], ChartRecallCodes.For("MIN", "NIGHT MINIMUMS", null, string.Empty).Unrecognized);

	[Fact]
	public void an_rnav_departure_aaup_is_skipped() =>
		Assert.Equal(ChartRecallSkipReason.Aaup, ChartRecallCodes.For("DAU", "RNAV DP AAUP", null, string.Empty).SkipReason);

	[Fact]
	public void an_unknown_chart_type_is_unrecognized() =>
		Assert.Equal(["the chart type 'XYZ'"], ChartRecallCodes.For("xyz", "SOMETHING NEW", null, string.Empty).Unrecognized);

	// ---- departures, obstacle departures and STARs ----

	[Theory]
	[InlineData("DP", "JALEX THREE (RNAV)", "JALEX3.JALEX", "JALEX")]
	[InlineData("ODP", "BEAR LAKE ONE (OBSTACLE) (RNAV)", "BEARL1.BEARL", "BEARL")]
	[InlineData("STR", "GRUUB ONE (RNAV)", "BRODE.GRUUB1", "GRUUB")]
	[InlineData("DP", "ABCDE TEN", "ABCDE10.ABCDE", "ABCDE")]
	[InlineData("DP", "DEVLN 1", "DEVLN1.DEVLN", "DEVLN")]
	public void a_procedure_with_a_computer_code_uses_the_right_half_less_its_version(string chartCode, string name, string computerCode, string expected)
	{
		ChartRecallCodeResult result = ChartRecallCodes.For(chartCode, name, computerCode, "TEST AIRPORT");

		Assert.Equal([expected], result.Codes);
		Assert.False(result.IsNamedFromChartName);
		Assert.False(result.NamesItsAirport);
	}

	[Theory]
	[InlineData("CALIFORNIA CITY ONE (OBSTACLE) (RNAV)", "L711.LHS", "L71")]
	[InlineData("NEPHI TWO (OBSTACLE) (RNAV)", "U142.DTA", "U14")]
	public void only_the_version_the_name_spells_is_dropped_so_an_airport_ending_in_a_digit_keeps_it(string name, string computerCode, string expected) =>
		Assert.Equal([expected], ChartRecallCodes.For("ODP", name, computerCode, "TEST AIRPORT").Codes);

	[Theory]
	[InlineData("WEIRD", "WEIRD4.WEIRD", "WEIRD")]
	[InlineData("FOO TWO", "FOO3.FOO", "FOO")]
	public void without_a_matching_version_word_one_trailing_digit_is_dropped(string name, string computerCode, string expected) =>
		Assert.Equal([expected], ChartRecallCodes.For("DP", name, computerCode, "TEST AIRPORT").Codes);

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("JALEX3")]
	[InlineData(".JALEX")]
	[InlineData("A.B.C")]
	public void a_missing_or_unusable_computer_code_falls_back_to_the_chart_name(string? computerCode)
	{
		ChartRecallCodeResult result = ChartRecallCodes.For("DP", "JALEX THREE (RNAV)", computerCode, "TEST AIRPORT");

		Assert.Equal(["JALEX"], result.Codes);
		Assert.True(result.IsNamedFromChartName);
	}

	[Theory]
	[InlineData("KNIK THREE", "KNIK")]
	[InlineData("CAPE LISBURNE EIGHT (OBSTACLE)", "CAPELISBURNE")]
	[InlineData("CABNN THREE (OBSTACLE) (RNAV)", "CABNN")]
	[InlineData("HUDSN ONE (COPTER) (RNAV)", "HUDSN")]
	[InlineData("COPTER PRIDE TWO", "PRIDE")]
	[InlineData("FREMONT 1 (VECTOR)", "FREMONT")]
	[InlineData("PAL-WAUKEE SEVEN", "PALWAUKEE")]
	[InlineData("TIN CITY FIVE RWY 17", "TINCITY17")]
	public void without_a_computer_code_the_name_is_spelled_out_without_its_version_brackets_or_descriptions(string name, string expected)
	{
		ChartRecallCodeResult result = ChartRecallCodes.For("DP", name, null, "SOMEWHERE ELSE");

		Assert.Equal([expected], result.Codes);
		Assert.True(result.IsNamedFromChartName);
		Assert.False(result.NamesItsAirport);
	}

	[Theory]
	[InlineData("TATALINA FOUR (OBSTACLE) (RNAV)", "TATALINA LRRS")]
	[InlineData("OHARE NINE", "CHICAGO O'HARE INTL")]
	[InlineData("TRI-CITIES EIGHT", "TRI-CITIES")]
	[InlineData("COPTER MUIR FOUR", "MUIR AHP")]
	[InlineData("TIN CITY FIVE RWY 17", "TIN CITY LRRS")]
	[InlineData("MUNN FOUR (OBSTACLE)", "CAMP PENDLETON MCAS (MUNN FLD)")]
	public void a_name_that_names_the_airport_it_serves_is_flagged(string name, string airportName)
	{
		ChartRecallCodeResult result = ChartRecallCodes.For("ODP", name, null, airportName);

		Assert.True(result.NamesItsAirport);
		Assert.True(result.IsNamedFromChartName);
	}

	[Theory]
	[InlineData("JOHN TUNE ONE", "JOHN C TUNE")]
	[InlineData("DENVER THREE", "CENTENNIAL")]
	[InlineData("CITY ONE", "TIN CIT LRRS")]
	public void a_name_that_does_not_name_its_airport_as_consecutive_words_is_not_flagged(string name, string airportName) =>
		Assert.False(ChartRecallCodes.For("DP", name, null, airportName).NamesItsAirport);

	[Fact]
	public void a_procedure_with_a_computer_code_is_never_flagged_as_naming_its_airport() =>
		Assert.False(ChartRecallCodes.For("DP", "DENVER THREE", "DENVR3.DENVR", "DENVER INTL").NamesItsAirport);

	[Fact]
	public void a_name_that_is_only_a_runway_keeps_the_runway_and_never_names_its_airport()
	{
		ChartRecallCodeResult result = ChartRecallCodes.For("DP", "RWY 17", null, "TEST AIRPORT");

		Assert.Equal(["17"], result.Codes);
		Assert.False(result.NamesItsAirport);
	}

	[Fact]
	public void a_name_with_nothing_left_to_make_a_command_from_is_unrecognized()
	{
		ChartRecallCodeResult result = ChartRecallCodes.For("STR", "(RNAV)", null, "TEST AIRPORT");

		Assert.Empty(result.Codes);
		Assert.Equal(["a name to make a command from in '(RNAV)'"], result.Unrecognized);
	}

	[Fact]
	public void null_arguments_are_rejected()
	{
		Assert.Throws<ArgumentNullException>(() => ChartRecallCodes.For(null!, "ILS RWY 1", null, string.Empty));
		Assert.Throws<ArgumentNullException>(() => ChartRecallCodes.For("IAP", null!, null, string.Empty));
		Assert.Throws<ArgumentNullException>(() => ChartRecallCodes.For("IAP", "ILS RWY 1", null, null!));
	}
}
