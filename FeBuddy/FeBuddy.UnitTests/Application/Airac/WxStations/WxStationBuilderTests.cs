using FeBuddy.Core.Application.Airac.WxStations;
using FeBuddy.Core.Application.Airac.WxStations.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.WxStations.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.WxStations.Models;

using FeBuddy.UnitTests.Application.Airac.WxStations.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.WxStations;

/// <summary>
/// Covers <see cref="WxStationBuilder.Read"/>: the missing-data guard, every inclusion rule
/// (country, ICAO, METAR, usable coordinates), the unusable-coordinates Info message (singular
/// and plural, sorted), duplicate identifiers, and the stable ordering by ICAO ID.
/// </summary>
public sealed class WxStationBuilderTests
{
	[Fact]
	public void read_throws_when_data_was_never_downloaded() =>
		Assert.Throws<InvalidOperationException>(() => WxStationBuilder.Read(null));

	// ---- inclusion rules ----

	[Fact]
	public void a_non_us_station_is_excluded()
	{
		WxStationDataCollection data = WxStationTestData.Build([WxStationTestData.CanadianRow()]);

		Assert.Empty(WxStationBuilder.Read(data).Stations);
	}

	[Fact]
	public void a_missing_icao_id_is_excluded()
	{
		WxStationDataCollection data = WxStationTestData.Build([WxStationTestData.NoIcaoRow()]);

		Assert.Empty(WxStationBuilder.Read(data).Stations);
	}

	[Fact]
	public void a_blank_icao_id_is_excluded_the_same_as_a_missing_one()
	{
		WxStationDataCollection data = WxStationTestData.Build([WxStationTestData.BlankIcaoRow()]);

		WxStationBuildResult result = WxStationBuilder.Read(data);

		Assert.Empty(result.Stations);
	}

	[Fact]
	public void a_station_reporting_no_metar_is_excluded()
	{
		WxStationDataCollection data = WxStationTestData.Build([WxStationTestData.TafOnlyRow()]);

		Assert.Empty(WxStationBuilder.Read(data).Stations);
	}

	[Fact]
	public void a_station_with_missing_latitude_is_excluded_silently()
	{
		WxStationDataCollection data = WxStationTestData.Build([WxStationTestData.NoLatitudeRow()]);

		WxStationBuildResult result = WxStationBuilder.Read(data);

		Assert.Empty(result.Stations);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void a_station_with_missing_longitude_is_excluded_silently()
	{
		WxStationDataCollection data = WxStationTestData.Build([WxStationTestData.NoLongitudeRow()]);

		WxStationBuildResult result = WxStationBuilder.Read(data);

		Assert.Empty(result.Stations);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void placeholder_coordinates_are_excluded_with_an_info_message_naming_it()
	{
		WxStationDataCollection data = WxStationTestData.Build([WxStationTestData.PlaceholderCoordinatesRow("KPLHD")]);

		WxStationBuildResult result = WxStationBuilder.Read(data);

		Assert.Empty(result.Stations);
		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Info, message.Level);
		Assert.Contains("KPLHD", message.Text, StringComparison.Ordinal);
		Assert.Contains("1 METAR station has", message.Text, StringComparison.Ordinal);
		Assert.Contains("was left out", message.Text, StringComparison.Ordinal);
	}

	[Fact]
	public void an_out_of_range_latitude_is_excluded_with_an_info_message()
	{
		WxStationDataCollection data = WxStationTestData.Build([WxStationTestData.OutOfRangeLatitudeRow("KOORL")]);

		WxStationBuildResult result = WxStationBuilder.Read(data);

		Assert.Empty(result.Stations);
		Assert.Contains("KOORL", Assert.Single(result.Messages).Text, StringComparison.Ordinal);
	}

	[Fact]
	public void an_out_of_range_longitude_is_excluded_with_an_info_message()
	{
		WxStationDataCollection data = WxStationTestData.Build([WxStationTestData.OutOfRangeLongitudeRow("KOORG")]);

		WxStationBuildResult result = WxStationBuilder.Read(data);

		Assert.Empty(result.Stations);
		Assert.Contains("KOORG", Assert.Single(result.Messages).Text, StringComparison.Ordinal);
	}

	[Fact]
	public void nan_coordinates_are_excluded_with_an_info_message()
	{
		WxStationDataCollection data = WxStationTestData.Build([WxStationTestData.NanCoordinatesRow("KNANX")]);

		WxStationBuildResult result = WxStationBuilder.Read(data);

		Assert.Empty(result.Stations);
		Assert.Contains("KNANX", Assert.Single(result.Messages).Text, StringComparison.Ordinal);
	}

	[Fact]
	public void infinite_coordinates_are_excluded_with_an_info_message()
	{
		WxStationDataCollection data = WxStationTestData.Build([WxStationTestData.InfiniteLatitudeRow("KINFX")]);

		WxStationBuildResult result = WxStationBuilder.Read(data);

		Assert.Empty(result.Stations);
		Assert.Contains("KINFX", Assert.Single(result.Messages).Text, StringComparison.Ordinal);
	}

	[Fact]
	public void multiple_unusable_coordinate_stations_produce_one_plural_message_sorted_by_icao_id()
	{
		WxStationDataCollection data = WxStationTestData.Build(
		[
			WxStationTestData.PlaceholderCoordinatesRow("KZULU"),
			WxStationTestData.OutOfRangeLatitudeRow("KALFA"),
		]);

		WxStationBuildResult result = WxStationBuilder.Read(data);

		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Contains("2 METAR stations have", message.Text, StringComparison.Ordinal);
		Assert.Contains("were left out", message.Text, StringComparison.Ordinal);
		Assert.Contains("KALFA, KZULU", message.Text, StringComparison.Ordinal);
	}

	// ---- a fully-included station carries every field ----

	[Fact]
	public void an_included_station_carries_every_field_onto_the_built_station()
	{
		WxStationDataCollection data = WxStationTestData.Build([WxStationTestData.DtwRow()]);

		WxStation station = Assert.Single(WxStationBuilder.Read(data).Stations);

		Assert.Equal("KDTW", station.IcaoId);
		Assert.Equal("DTW", station.IataId);
		Assert.Equal("Detroit/Metro Wayne Cnty", station.Site);
		Assert.Equal(42.212, station.Latitude);
		Assert.Equal(-83.353, station.Longitude);
		Assert.Equal("US", station.Country);
	}

	[Fact]
	public void a_station_with_no_iata_id_or_site_gets_empty_strings_not_null()
	{
		WxStationDataCollection data = WxStationTestData.Build([WxStationTestData.Row("KBARE")]);

		WxStation station = Assert.Single(WxStationBuilder.Read(data).Stations);

		Assert.Equal(string.Empty, station.IataId);
		Assert.Equal(string.Empty, station.Site);
	}

	// ---- ordering and duplicates ----

	[Fact]
	public void stations_are_ordered_by_icao_id_ignoring_case_stably()
	{
		WxStationDataCollection data = WxStationTestData.Build(
		[
			WxStationTestData.Row("bbb"),
			WxStationTestData.Row("AAA"),
			WxStationTestData.Row("aaa"),
		]);

		WxStationBuildResult result = WxStationBuilder.Read(data);

		Assert.Equal(["AAA", "aaa", "bbb"], result.Stations.Select(s => s.IcaoId));
	}

	[Fact]
	public void duplicate_icao_ids_are_never_merged()
	{
		WxStationDataCollection data = WxStationTestData.Build(
		[
			WxStationTestData.Row("DUP", site: "First"),
			WxStationTestData.Row("DUP", site: "Second"),
		]);

		WxStationBuildResult result = WxStationBuilder.Read(data);

		Assert.Equal(["DUP", "DUP"], result.Stations.Select(s => s.IcaoId));
		Assert.Contains(result.Stations, s => s.Site == "First");
		Assert.Contains(result.Stations, s => s.Site == "Second");
	}

	// ---- total count ----

	[Fact]
	public void total_station_count_is_every_row_regardless_of_inclusion()
	{
		WxStationDataCollection data = WxStationTestData.Build(
		[
			WxStationTestData.DtwRow(),
			WxStationTestData.CanadianRow(),
			WxStationTestData.NoIcaoRow(),
		]);

		WxStationBuildResult result = WxStationBuilder.Read(data);

		Assert.Single(result.Stations);
		Assert.Equal(3, result.TotalStationCount);
	}
}
