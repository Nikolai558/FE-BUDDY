using FeBuddy.Core.Application.Airac.Airports;
using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Airports.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Airports.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Airports;

/// <summary>
/// Covers the assembly of an <see cref="Airport"/> from the NASR tables: the permanently-closed
/// exclusion, what counts as a runway, the deterministic longest-runway and runway-end ordering,
/// the routine "no end coordinates" gap being Info rather than Warning, the CTAF and weather
/// frequency picks, and the stable output ordering.
/// </summary>
public sealed class AirportBuilderTests
{
	[Fact]
	public void build_all_throws_when_apt_data_was_never_parsed()
	{
		NasrCsvDataCollection data = new(); // Apt is null

		Assert.Throws<InvalidOperationException>(() => AirportBuilder.BuildAll(data));
	}

	[Fact]
	public void a_permanently_closed_airport_is_excluded_entirely()
	{
		NasrCsvDataCollection data = AirportTestDataBuilder.Build(
			airports:
			[
				AirportTestDataBuilder.Base("CLS", arptStatus: "CP"),
				AirportTestDataBuilder.Base("OPN", arptStatus: "O"),
			]);

		AirportBuildAllResult result = AirportBuilder.BuildAll(data);

		Airport airport = Assert.Single(result.Airports);
		Assert.Equal("OPN", airport.FaaId);
	}

	[Theory]
	[InlineData("16L/34R", true)]
	[InlineData("09/27", true)]
	[InlineData("H1", false)]
	[InlineData("", false)]
	[InlineData(null, false)]
	public void only_a_runway_id_containing_a_slash_is_a_true_runway(string? runwayId, bool expected) =>
		Assert.Equal(expected, AirportBuilder.IsTrueRunway(runwayId));

	[Fact]
	public void a_helipad_row_does_not_become_a_runway()
	{
		NasrCsvDataCollection data = AirportTestDataBuilder.Build(
			airports: [AirportTestDataBuilder.Base("SEA")],
			runways:
			[
				AirportTestDataBuilder.Runway("SEA", "H1", 60),
				AirportTestDataBuilder.Runway("SEA", "16L/34R", 11901),
			],
			runwayEnds:
			[
				AirportTestDataBuilder.RunwayEnd("SEA", "16L/34R", "16L", 47.463, -122.308),
				AirportTestDataBuilder.RunwayEnd("SEA", "16L/34R", "34R", 47.430, -122.308),
			]);

		Airport airport = Assert.Single(AirportBuilder.BuildAll(data).Airports);

		AirportRunway runway = Assert.Single(airport.Runways);
		Assert.Equal("16L/34R", runway.RunwayId);
	}

	[Fact]
	public void the_longest_runway_is_the_one_with_the_greatest_length()
	{
		NasrCsvDataCollection data = AirportTestDataBuilder.Build(
			airports: [AirportTestDataBuilder.Base("SEA")],
			runways:
			[
				AirportTestDataBuilder.Runway("SEA", "09/27", 5000),
				AirportTestDataBuilder.Runway("SEA", "18/36", 9000),
				AirportTestDataBuilder.Runway("SEA", "04/22", 7000),
			]);

		Airport airport = Assert.Single(AirportBuilder.BuildAll(data).Airports);

		Assert.NotNull(airport.LongestRunway);
		Assert.Equal("18/36", airport.LongestRunway!.RunwayId);
		Assert.Equal(9000, airport.LongestRunway.Length);
	}

	[Fact]
	public void equally_long_runways_break_the_tie_on_runway_id_ordinal()
	{
		NasrCsvDataCollection data = AirportTestDataBuilder.Build(
			airports: [AirportTestDataBuilder.Base("SEA")],
			runways:
			[
				AirportTestDataBuilder.Runway("SEA", "09/27", 5000),
				AirportTestDataBuilder.Runway("SEA", "04/22", 5000),
			]);

		Airport airport = Assert.Single(AirportBuilder.BuildAll(data).Airports);

		Assert.Equal("04/22", airport.LongestRunway!.RunwayId);
	}

	[Fact]
	public void runway_ends_are_ordered_by_end_id_regardless_of_row_order()
	{
		NasrCsvDataCollection data = AirportTestDataBuilder.Build(
			airports: [AirportTestDataBuilder.Base("SEA")],
			runways: [AirportTestDataBuilder.Runway("SEA", "16L/34R", 11901)],
			runwayEnds:
			[
				// Deliberately the "second" end first, so file order cannot be what decides.
				AirportTestDataBuilder.RunwayEnd("SEA", "16L/34R", "34R", 47.430, -122.308),
				AirportTestDataBuilder.RunwayEnd("SEA", "16L/34R", "16L", 47.463, -122.307),
			]);

		Airport airport = Assert.Single(AirportBuilder.BuildAll(data).Airports);
		AirportRunway runway = Assert.Single(airport.Runways);

		Assert.True(runway.HasGeometry);
		Assert.Equal("16L", runway.FirstEnd!.EndId);
		Assert.Equal(47.463, runway.FirstEnd.Latitude);
		Assert.Equal("34R", runway.SecondEnd!.EndId);
		Assert.Equal(47.430, runway.SecondEnd.Latitude);
	}

	[Fact]
	public void a_runway_with_no_end_coordinates_keeps_its_place_and_is_reported_as_info()
	{
		NasrCsvDataCollection data = AirportTestDataBuilder.Build(
			airports: [AirportTestDataBuilder.Base("SEA")],
			runways: [AirportTestDataBuilder.Runway("SEA", "09/27", 8000)],
			runwayEnds:
			[
				AirportTestDataBuilder.RunwayEnd("SEA", "09/27", "09", null, null),
				AirportTestDataBuilder.RunwayEnd("SEA", "09/27", "27", null, null),
			]);

		AirportBuildAllResult result = AirportBuilder.BuildAll(data);
		Airport airport = Assert.Single(result.Airports);

		AirportRunway runway = Assert.Single(airport.Runways);
		Assert.False(runway.HasGeometry);
		Assert.Null(runway.FirstEnd);
		Assert.Null(runway.SecondEnd);

		// It still counts for the alias file's longest-runway line.
		Assert.Equal("09/27", airport.LongestRunway!.RunwayId);

		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Info, message.Level);
		Assert.NotEqual(LogLevel.Warning, message.Level);
		Assert.Equal("AirportBuilder", message.Source);
		Assert.Contains("09/27", message.Text);
	}

	[Fact]
	public void the_ctaf_is_the_first_frequency_row_whose_use_mentions_ctaf()
	{
		NasrCsvDataCollection data = AirportTestDataBuilder.Build(
			airports: [AirportTestDataBuilder.Base("SEA")],
			frequencies:
			[
				AirportTestDataBuilder.Frequency("SEA", "119.900", "LCL/P"),
				AirportTestDataBuilder.Frequency("SEA", "122.800", "CTAF"),
				AirportTestDataBuilder.Frequency("SEA", "123.000", "CTAF"),
			]);

		Airport airport = Assert.Single(AirportBuilder.BuildAll(data).Airports);

		Assert.Equal("122.800", airport.CtafFrequency);
	}

	[Fact]
	public void the_ctaf_is_found_on_a_row_whose_facility_is_null_as_nasr_publishes_it()
	{
		// NASR leaves FACILITY null for CTAF, UNICOM, GCO and AFIS rows and puts the airport in
		// SERVICED_FACILITY instead. Joining on FACILITY finds no CTAF for any airport in the
		// real database, which is exactly the bug this asserts against.
		NasrCsvDataCollection data = AirportTestDataBuilder.Build(
			airports: [AirportTestDataBuilder.Base("SEA")],
			frequencies:
			[
				AirportTestDataBuilder.Frequency(null, "122.800", "CTAF", servicedFacility: "SEA"),
			]);

		Airport airport = Assert.Single(AirportBuilder.BuildAll(data).Airports);

		Assert.Equal("122.800", airport.CtafFrequency);
	}

	[Fact]
	public void the_weather_frequency_is_the_first_awos_or_asos_row_and_carries_its_use()
	{
		NasrCsvDataCollection data = AirportTestDataBuilder.Build(
			airports: [AirportTestDataBuilder.Base("SEA"), AirportTestDataBuilder.Base("PDX")],
			frequencies:
			[
				AirportTestDataBuilder.Frequency("PDX", "128.350", "ATIS"),
				AirportTestDataBuilder.Frequency("SEA", "119.900", "LCL/P"),
				AirportTestDataBuilder.Frequency("SEA", "135.075", "ASOS"),
				AirportTestDataBuilder.Frequency("SEA", "118.000", "AWOS-3"),
			]);

		Airport sea = Assert.Single(AirportBuilder.BuildAll(data).Airports, a => a.FaaId == "SEA");

		Assert.Equal("135.075", sea.WeatherFrequency);
		Assert.Equal("ASOS", sea.WeatherFrequencyUse);
	}

	[Fact]
	public void an_airport_with_no_matching_frequency_rows_reports_none()
	{
		NasrCsvDataCollection data = AirportTestDataBuilder.Build(
			airports: [AirportTestDataBuilder.Base("SEA")],
			frequencies: [AirportTestDataBuilder.Frequency("SEA", "119.900", "LCL/P")]);

		Airport airport = Assert.Single(AirportBuilder.BuildAll(data).Airports);

		Assert.Null(airport.CtafFrequency);
		Assert.Null(airport.WeatherFrequency);
		Assert.Null(airport.WeatherFrequencyUse);
	}

	[Fact]
	public void airports_come_back_ordered_by_faa_identifier()
	{
		NasrCsvDataCollection data = AirportTestDataBuilder.Build(
			airports:
			[
				AirportTestDataBuilder.Base("ZZZ"),
				AirportTestDataBuilder.Base("AAA"),
				AirportTestDataBuilder.Base("MMM"),
			]);

		AirportBuildAllResult result = AirportBuilder.BuildAll(data);

		Assert.Equal(["AAA", "MMM", "ZZZ"], result.Airports.Select(a => a.FaaId));
	}

	[Fact]
	public void the_mapped_and_joined_fields_are_carried_onto_the_built_airport()
	{
		NasrCsvDataCollection data = AirportTestDataBuilder.Build(
			airports:
			[
				AirportTestDataBuilder.Base(
					"SEA",
					icaoId: "KSEA",
					name: "SEATTLE TACOMA INTL",
					trafficPatternAltitude: 1500,
					fssId: "SEA",
					towerTypeCode: "ATCT-TRACON",
					siteTypeCode: "A")
			],
			classAirspace: [AirportTestDataBuilder.ClassAirspaceRow("SEA", classB: "Y", classE: "Y")]);

		Airport airport = Assert.Single(AirportBuilder.BuildAll(data).Airports);

		Assert.Equal("KSEA", airport.IcaoId);
		Assert.Equal("SEATTLE TACOMA INTL", airport.Name);
		Assert.Equal(1500, airport.TrafficPatternAltitude);
		Assert.Equal("SEA", airport.FssId);
		Assert.Equal("TWR/RADAR APP Up-Down", airport.TowerType);
		Assert.Equal("AIRPORT", airport.FacilityType);
		Assert.Equal("Bravo & Echo", airport.ClassAirspace);
	}

	[Fact]
	public void rows_without_an_identifier_are_ignored_and_a_duplicate_identifier_keeps_the_first()
	{
		NasrCsvDataCollection data = AirportTestDataBuilder.Build(
			airports:
			[
				AirportTestDataBuilder.Base(" "),
				AirportTestDataBuilder.Base("SEA", name: "FIRST"),
				AirportTestDataBuilder.Base("SEA", name: "SECOND"),
			],
			runways: [AirportTestDataBuilder.Runway("SEA", " ", 5000)]);

		AirportBuildAllResult result = AirportBuilder.BuildAll(data);

		Airport airport = Assert.Single(result.Airports);
		Assert.Equal("FIRST", airport.Name);
		Assert.Empty(airport.Runways);
		Assert.Contains(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains("more than one APT_BASE record"));
	}

	[Fact]
	public void a_frequency_row_counts_for_both_its_facility_and_the_facility_it_serves()
	{
		NasrCsvDataCollection data = AirportTestDataBuilder.Build(
			airports: [AirportTestDataBuilder.Base("BFI"), AirportTestDataBuilder.Base("SEA")],
			frequencies: [AirportTestDataBuilder.Frequency("SEA", "118.575", "ASOS", servicedFacility: "BFI")]);

		IReadOnlyList<Airport> airports = AirportBuilder.BuildAll(data).Airports;

		Assert.All(airports, airport => Assert.Equal("118.575", airport.WeatherFrequency));
	}
}
