using FeBuddy.Core.Infrastructure.WxStations.Models;
using FeBuddy.Core.Infrastructure.WxStations.Parsers;

namespace FeBuddy.UnitTests.Infrastructure.WxStations.Parsers;

/// <summary>
/// Covers <see cref="WxStationXmlParser"/>: every element it reads (trimmed, blank to null),
/// invariant-culture parsing of latitude/longitude/elevation, the &lt;site_type&gt; children,
/// <c>num_results</c>, malformed roots, and the async wrapper.
/// </summary>
public sealed class WxStationXmlParserTests : IDisposable
{
	private readonly string _testRoot =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_WxStationXmlParser_" + Guid.NewGuid().ToString("N"));

	public WxStationXmlParserTests() => Directory.CreateDirectory(_testRoot);

	public void Dispose()
	{
		try
		{
			Directory.Delete(_testRoot, recursive: true);
		}
		catch
		{
			// Best-effort.
		}
	}

	private string WriteXml(string xml)
	{
		string path = Path.Combine(_testRoot, $"stations_{Guid.NewGuid():N}.xml");
		File.WriteAllText(path, xml);
		return path;
	}

	/// <summary>A minimal <c>&lt;response&gt;&lt;data num_results="N"&gt;&lt;Station&gt;...&lt;/Station&gt;&lt;/data&gt;&lt;/response&gt;</c> file.</summary>
	private string WriteStations(string stations, string dataAttributes = "") =>
		WriteXml($"<response><data{dataAttributes}>{stations}</data></response>");

	[Fact]
	public void every_field_is_read_and_trimmed()
	{
		string path = WriteStations(
			"<Station>" +
			"<station_id> KDTW </station_id>" +
			"<icao_id> KDTW </icao_id>" +
			"<iata_id> DTW </iata_id>" +
			"<faa_id> DTW </faa_id>" +
			"<wmo_id> 999999 </wmo_id>" +
			"<latitude> 42.212 </latitude>" +
			"<longitude> -83.353 </longitude>" +
			"<elevation_m> 190.0 </elevation_m>" +
			"<site> Detroit/Metro Wayne Cnty </site>" +
			"<state> MI </state>" +
			"<country> US </country>" +
			"</Station>");

		WxStationXmlDataModel.Station station = Assert.Single(WxStationXmlParser.Parse(path).Stations);

		Assert.Equal("KDTW", station.StationId);
		Assert.Equal("KDTW", station.IcaoId);
		Assert.Equal("DTW", station.IataId);
		Assert.Equal("DTW", station.FaaId);
		Assert.Equal("999999", station.WmoId);
		Assert.Equal(42.212, station.Latitude);
		Assert.Equal(-83.353, station.Longitude);
		Assert.Equal(190.0, station.ElevationM);
		Assert.Equal("Detroit/Metro Wayne Cnty", station.Site);
		Assert.Equal("MI", station.State);
		Assert.Equal("US", station.Country);
	}

	[Fact]
	public void a_blank_string_element_reads_as_null()
	{
		string path = WriteStations("<Station><station_id></station_id><icao_id>   </icao_id></Station>");

		WxStationXmlDataModel.Station station = Assert.Single(WxStationXmlParser.Parse(path).Stations);

		Assert.Null(station.StationId);
		Assert.Null(station.IcaoId);
	}

	[Fact]
	public void a_missing_string_element_reads_as_null()
	{
		string path = WriteStations("<Station></Station>");

		WxStationXmlDataModel.Station station = Assert.Single(WxStationXmlParser.Parse(path).Stations);

		Assert.Null(station.StationId);
		Assert.Null(station.IcaoId);
		Assert.Null(station.IataId);
		Assert.Null(station.FaaId);
		Assert.Null(station.WmoId);
		Assert.Null(station.Site);
		Assert.Null(station.State);
		Assert.Null(station.Country);
	}

	[Theory]
	[InlineData("40.5", 40.5)]
	[InlineData(" 40.5 ", 40.5)]
	[InlineData("-99.99", -99.99)]
	public void latitude_and_longitude_parse_with_invariant_culture(string value, double expected)
	{
		string path = WriteStations($"<Station><latitude>{value}</latitude><longitude>{value}</longitude></Station>");

		WxStationXmlDataModel.Station station = Assert.Single(WxStationXmlParser.Parse(path).Stations);

		Assert.Equal(expected, station.Latitude);
		Assert.Equal(expected, station.Longitude);
	}

	[Theory]
	[InlineData("")]
	[InlineData("not-a-number")]
	[InlineData("N/A")]
	public void unparsable_latitude_longitude_and_elevation_read_as_null(string value)
	{
		string path = WriteStations(
			$"<Station><latitude>{value}</latitude><longitude>{value}</longitude><elevation_m>{value}</elevation_m></Station>");

		WxStationXmlDataModel.Station station = Assert.Single(WxStationXmlParser.Parse(path).Stations);

		Assert.Null(station.Latitude);
		Assert.Null(station.Longitude);
		Assert.Null(station.ElevationM);
	}

	[Fact]
	public void missing_latitude_longitude_and_elevation_elements_read_as_null()
	{
		string path = WriteStations("<Station></Station>");

		WxStationXmlDataModel.Station station = Assert.Single(WxStationXmlParser.Parse(path).Stations);

		Assert.Null(station.Latitude);
		Assert.Null(station.Longitude);
		Assert.Null(station.ElevationM);
	}

	[Fact]
	public void site_type_children_are_read_by_element_name()
	{
		string path = WriteStations("<Station><site_type><METAR/><TAF/></site_type></Station>");

		WxStationXmlDataModel.Station station = Assert.Single(WxStationXmlParser.Parse(path).Stations);

		Assert.Equal(["METAR", "TAF"], station.SiteTypes);
	}

	[Fact]
	public void an_empty_site_type_element_reads_as_an_empty_list()
	{
		string path = WriteStations("<Station><site_type></site_type></Station>");

		WxStationXmlDataModel.Station station = Assert.Single(WxStationXmlParser.Parse(path).Stations);

		Assert.Empty(station.SiteTypes);
	}

	[Fact]
	public void a_missing_site_type_element_reads_as_an_empty_list()
	{
		string path = WriteStations("<Station></Station>");

		WxStationXmlDataModel.Station station = Assert.Single(WxStationXmlParser.Parse(path).Stations);

		Assert.Empty(station.SiteTypes);
	}

	[Fact]
	public void a_present_num_results_is_used_even_when_it_disagrees_with_the_station_count()
	{
		string path = WriteStations("<Station></Station>", dataAttributes: " num_results=\"99\"");

		WxStationDataCollection data = WxStationXmlParser.Parse(path);

		Assert.Equal(99, data.NumResults);
		Assert.Single(data.Stations);
	}

	[Fact]
	public void an_absent_num_results_falls_back_to_the_station_count()
	{
		string path = WriteStations("<Station></Station><Station></Station>");

		WxStationDataCollection data = WxStationXmlParser.Parse(path);

		Assert.Equal(2, data.NumResults);
	}

	[Fact]
	public void a_garbage_num_results_falls_back_to_the_station_count()
	{
		string path = WriteStations("<Station></Station>", dataAttributes: " num_results=\"not-a-number\"");

		WxStationDataCollection data = WxStationXmlParser.Parse(path);

		Assert.Equal(1, data.NumResults);
	}

	[Fact]
	public void a_file_with_no_stations_parses_to_an_empty_list_and_zero_results()
	{
		string path = WriteStations("");

		WxStationDataCollection data = WxStationXmlParser.Parse(path);

		Assert.Empty(data.Stations);
		Assert.Equal(0, data.NumResults);
	}

	[Fact]
	public void a_wrong_root_element_throws()
	{
		string path = WriteXml("<notresponse><data></data></notresponse>");

		InvalidDataException ex = Assert.Throws<InvalidDataException>(() => WxStationXmlParser.Parse(path));
		Assert.Contains("<response>", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void a_response_with_no_data_element_throws()
	{
		string path = WriteXml("<response></response>");

		InvalidDataException ex = Assert.Throws<InvalidDataException>(() => WxStationXmlParser.Parse(path));
		Assert.Contains("<data>", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void parse_rejects_a_blank_path() =>
		Assert.Throws<ArgumentException>(() => WxStationXmlParser.Parse(" "));

	[Fact]
	public async Task parse_async_reads_the_same_data_as_the_sync_parse()
	{
		string path = WriteStations("<Station><icao_id>KDTW</icao_id></Station>");

		WxStationDataCollection data = await WxStationXmlParser.ParseAsync(path);

		Assert.Equal("KDTW", Assert.Single(data.Stations).IcaoId);
	}

	[Fact]
	public async Task parse_async_with_an_already_cancelled_token_throws()
	{
		string path = WriteStations("<Station></Station>");

		await Assert.ThrowsAsync<OperationCanceledException>(
			() => WxStationXmlParser.ParseAsync(path, new CancellationToken(canceled: true)));
	}
}
