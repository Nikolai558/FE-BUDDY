using FeBuddy.Core.Application.Airac.Airports;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Airports.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Airports;

/// <summary>
/// Covers the NASR-code-to-display-value translations, the class-airspace sentence builder and the
/// airspace-hours splitter: every published tower and facility code maps, an unpublished one
/// passes through unchanged rather than being blanked, one, two, and three flagged classes each
/// read correctly, and airspace hours break into the lines the alias file shows.
/// </summary>
public sealed class AirportFieldMapsTests
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

	/// <summary>
	/// A break after the first <c>SVC</c>, then at every comma and semicolon; lines trimmed, blank
	/// ones dropped. The cases are from real <c>AIRSPACE_HRS</c> values. <paramref name="expected"/>
	/// is the lines joined with <c>|</c>.
	/// </summary>
	[Theory]
	[InlineData("CLASS D SVC 0700-2200 1 APR- 31 OCT; 0700-2000 1 NOV-31 MAR; OTHER TIMES CLASS E",
		"CLASS D SVC|0700-2200 1 APR- 31 OCT|0700-2000 1 NOV-31 MAR|OTHER TIMES CLASS E")]
	[InlineData("CLASS D SVC 0600-2100 MON-FRI, 0800-2000 SAT-SUN; OTHER TIMES CLASS E",
		"CLASS D SVC|0600-2100 MON-FRI|0800-2000 SAT-SUN|OTHER TIMES CLASS E")]
	[InlineData("CLASS D SVC 1500-2400Z++ FRI-MON;  1500-0600Z++ TUE-THU, CLSD ALL FED HOL EXP CLOSURE OR RDCD SVC PER NOTAM; OTHER TIMES CLASS G",
		"CLASS D SVC|1500-2400Z++ FRI-MON|1500-0600Z++ TUE-THU|CLSD ALL FED HOL EXP CLOSURE OR RDCD SVC PER NOTAM|OTHER TIMES CLASS G")]
	[InlineData("CLASS D 0700-2000; OTHER TIMES CLASS E", "CLASS D 0700-2000|OTHER TIMES CLASS E")]
	[InlineData("CLASS D SVC; OTR TIMES CLASS E BY NOTAM.", "CLASS D SVC|OTR TIMES CLASS E BY NOTAM.")]
	[InlineData(" 1100-0200Z UTC-4 ", "1100-0200Z UTC-4")]
	[InlineData("CLASS D SVC 0600-2400;", "CLASS D SVC|0600-2400")]
	[InlineData(" ; , ", "")]
	[InlineData("", "")]
	[InlineData(null, "")]
	public void airspace_hours_break_after_svc_and_at_every_comma_and_semicolon(string? airspaceHours, string expected) =>
		Assert.Equal(expected.Length == 0 ? [] : expected.Split('|'), AirportFieldMaps.SplitAirspaceHours(airspaceHours));
}
