using FeBuddy.Core.Application.Airac.Airports;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Airports.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Airports;

/// <summary>
/// Covers the NASR-code-to-display-value translations and the class-airspace sentence builder:
/// every published tower and facility code maps, an unpublished one passes through unchanged
/// rather than being blanked, and one, two, and three flagged classes each read correctly.
/// </summary>
public class AirportFieldMapsTests
{
	[Theory]
	[InlineData("ATCT", "TWR")]
	[InlineData("NON-ATCT", "No-TWR")]
	[InlineData("ATCT-A/C", "TWR provides APP services")]
	[InlineData("ATCT-RAPCON", "AirForce TWR / FAA APP")]
	[InlineData("ATCT-RATCF", "Navy TWR / FAA APP")]
	[InlineData("ATCT-TRACON", "TWR/RADAR APP Up-Down")]
	public void every_tower_type_code_maps_to_its_display_form(string code, string expected) =>
		Assert.Equal(expected, AirportFieldMaps.MapTowerType(code));

	[Fact]
	public void an_unknown_tower_type_code_passes_through_unchanged() =>
		Assert.Equal("ATCT-NEWCODE", AirportFieldMaps.MapTowerType("ATCT-NEWCODE"));

	[Theory]
	[InlineData("A", "AIRPORT")]
	[InlineData("B", "BALLOONPORT")]
	[InlineData("C", "SEAPLANE BASE")]
	[InlineData("G", "GLIDERPORT")]
	[InlineData("H", "HELIPORT")]
	[InlineData("U", "ULTRALIGHT FIELD")]
	public void every_facility_type_code_maps_to_its_display_form(string code, string expected) =>
		Assert.Equal(expected, AirportFieldMaps.MapFacilityType(code));

	[Fact]
	public void an_unknown_facility_type_code_passes_through_unchanged() =>
		Assert.Equal("Z", AirportFieldMaps.MapFacilityType("Z"));

	[Fact]
	public void one_flagged_class_reads_as_that_class_alone()
	{
		ClsArspCsvDataModel.ClsArsp row = AirportTestDataBuilder.ClassAirspaceRow("SEA", classD: "Y");

		Assert.Equal("Delta", AirportFieldMaps.BuildClassAirspace(row));
	}

	[Fact]
	public void two_flagged_classes_are_joined_with_an_ampersand()
	{
		ClsArspCsvDataModel.ClsArsp row = AirportTestDataBuilder.ClassAirspaceRow("SEA", classD: "Y", classE: "Y");

		Assert.Equal("Delta & Echo", AirportFieldMaps.BuildClassAirspace(row));
	}

	[Fact]
	public void three_flagged_classes_are_joined_with_a_serial_ampersand()
	{
		ClsArspCsvDataModel.ClsArsp row =
			AirportTestDataBuilder.ClassAirspaceRow("SEA", classB: "Y", classD: "Y", classE: "Y");

		Assert.Equal("Bravo, Delta, & Echo", AirportFieldMaps.BuildClassAirspace(row));
	}

	[Fact]
	public void a_row_that_flags_nothing_has_no_class_airspace()
	{
		ClsArspCsvDataModel.ClsArsp row = AirportTestDataBuilder.ClassAirspaceRow("SEA");

		Assert.Null(AirportFieldMaps.BuildClassAirspace(row));
	}

	[Fact]
	public void a_null_row_has_no_class_airspace() =>
		Assert.Null(AirportFieldMaps.BuildClassAirspace(null));
}
