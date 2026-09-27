using System.Xml;

using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Dtpp.Parsers;

namespace FeBuddy.UnitTests.Infrastructure.Dtpp.Parsers;

/// <summary>
/// Covers <see cref="DtppMetafileXmlParser"/>: the root/state/city/airport attributes flowing
/// into <see cref="DtppMetafileXmlDataModel.Airport"/> and <see cref="DtppMetafileXmlDataModel.Record"/>
/// rows, every record field (trim, blank to null/empty, ints), the effective-date parsing,
/// malformed input, and the async wrapper.
/// </summary>
public sealed class DtppMetafileXmlParserTests : IDisposable
{
	private readonly string _testRoot =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_DtppMetafileXmlParser_" + Guid.NewGuid().ToString("N"));

	public DtppMetafileXmlParserTests() => Directory.CreateDirectory(_testRoot);

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
		string path = Path.Combine(_testRoot, $"dtpp_{Guid.NewGuid():N}.xml");
		File.WriteAllText(path, xml);
		return path;
	}

	/// <summary>Wraps <paramref name="body"/> (the <c>state_code</c> elements) in a <c>digital_tpp</c> root with the given attributes.</summary>
	private string WriteDoc(string body, string rootAttributes = "cycle=\"2609\" from_edate=\"0901Z  09/03/26\" to_edate=\"0901Z  10/01/26\"") =>
		WriteXml($"<?xml version=\"1.0\" encoding=\"utf-8\"?><digital_tpp {rootAttributes}>{body}</digital_tpp>");

	/// <summary>One state/city/airport wrapping a set of <c>record</c> elements.</summary>
	private static string WrapAirport(string records, string airportAttributes = "ID=\"ADAK\" military=\"N\" apt_ident=\"ADK\" icao_ident=\"PADK\" alnum=\"1244\"") =>
		$"<state_code ID=\"AK\" state_fullname=\"ALASKA\"><city_name ID=\"ADAK ISLAND\" volume=\"AK-1\">" +
		$"<airport_name {airportAttributes}>{records}</airport_name></city_name></state_code>";

	private const string FullRecord =
		"<record>" +
		"<chartseq>50750</chartseq>" +
		"<chart_code>IAP</chart_code>" +
		"<chart_name>ILS Y OR LOC Y RWY 23</chart_name>" +
		"<useraction>C</useraction>" +
		"<pdf_name>01244IYLY23.PDF</pdf_name>" +
		"<cn_flg>N</cn_flg>" +
		"<cnsection></cnsection>" +
		"<cnpage></cnpage>" +
		"<bvsection></bvsection>" +
		"<bvpage>1</bvpage>" +
		"<procuid>39683</procuid>" +
		"<two_colored>Y</two_colored>" +
		"<civil>C</civil>" +
		"<faanfd18></faanfd18>" +
		"<copter></copter>" +
		"<amdtnum>0</amdtnum>" +
		"<amdtdate>12/05/2019</amdtdate>" +
		"</record>";

	// ---- root attributes and effective-date parsing ----

	[Fact]
	public void root_attributes_are_read()
	{
		string path = WriteDoc("");

		DtppMetafileDataCollection data = DtppMetafileXmlParser.Parse(path);

		Assert.Equal("2609", data.Cycle);
		Assert.Equal("0901Z  09/03/26", data.FromEdate);
		Assert.Equal("0901Z  10/01/26", data.ToEdate);
	}

	[Fact]
	public void an_effective_date_with_two_spaces_parses_to_utc()
	{
		string path = WriteDoc("", "cycle=\"2609\" from_edate=\"0901Z  09/03/26\" to_edate=\"0901Z  10/01/26\"");

		DtppMetafileDataCollection data = DtppMetafileXmlParser.Parse(path);

		Assert.Equal(new DateTime(2026, 9, 3, 9, 1, 0, DateTimeKind.Utc), data.FromEffectiveUtc);
		Assert.Equal(new DateTime(2026, 10, 1, 9, 1, 0, DateTimeKind.Utc), data.ToEffectiveUtc);
	}

	[Fact]
	public void an_effective_date_with_one_space_still_parses()
	{
		string path = WriteDoc("", "cycle=\"2609\" from_edate=\"0901Z 09/03/26\" to_edate=\"\"");

		DtppMetafileDataCollection data = DtppMetafileXmlParser.Parse(path);

		Assert.Equal(new DateTime(2026, 9, 3, 9, 1, 0, DateTimeKind.Utc), data.FromEffectiveUtc);
	}

	[Theory]
	[InlineData("")]
	[InlineData("garbage")]
	[InlineData("0901Z  13/03/26")]
	[InlineData("0901Z  09/32/26")]
	public void an_unparsable_effective_date_reads_as_null(string value)
	{
		string path = WriteDoc("", $"cycle=\"2609\" from_edate=\"{value}\" to_edate=\"\"");

		DtppMetafileDataCollection data = DtppMetafileXmlParser.Parse(path);

		Assert.Null(data.FromEffectiveUtc);
		Assert.Null(data.ToEffectiveUtc);
	}

	[Fact]
	public void missing_root_attributes_read_as_empty()
	{
		string path = WriteXml("<digital_tpp></digital_tpp>");

		DtppMetafileDataCollection data = DtppMetafileXmlParser.Parse(path);

		Assert.Equal(string.Empty, data.Cycle);
		Assert.Equal(string.Empty, data.FromEdate);
		Assert.Equal(string.Empty, data.ToEdate);
		Assert.Null(data.FromEffectiveUtc);
		Assert.Null(data.ToEffectiveUtc);
		Assert.Empty(data.Airports);
		Assert.Empty(data.Records);
	}

	[Fact]
	public void a_self_closing_root_element_reads_as_empty()
	{
		string path = WriteXml("<digital_tpp cycle=\"2609\" from_edate=\"\" to_edate=\"\" />");

		DtppMetafileDataCollection data = DtppMetafileXmlParser.Parse(path);

		Assert.Equal("2609", data.Cycle);
		Assert.Empty(data.Airports);
		Assert.Empty(data.Records);
	}

	// ---- state/city/airport attributes flow into both Airport and Record rows ----

	[Fact]
	public void state_city_and_airport_attributes_flow_into_the_airport_row()
	{
		string path = WriteDoc(WrapAirport(""));

		DtppMetafileXmlDataModel.Airport airport = Assert.Single(DtppMetafileXmlParser.Parse(path).Airports);

		Assert.Equal("AK", airport.StateCode);
		Assert.Equal("ALASKA", airport.StateFullName);
		Assert.Equal("ADAK ISLAND", airport.CityName);
		Assert.Equal("AK-1", airport.Volume);
		Assert.Equal("ADAK", airport.AirportName);
		Assert.Equal("N", airport.Military);
		Assert.Equal("ADK", airport.AptIdent);
		Assert.Equal("PADK", airport.IcaoIdent);
		Assert.Equal(1244, airport.Alnum);
	}

	[Fact]
	public void state_city_and_airport_attributes_flow_into_each_record_row_too()
	{
		string path = WriteDoc(WrapAirport(FullRecord));

		DtppMetafileXmlDataModel.Record record = Assert.Single(DtppMetafileXmlParser.Parse(path).Records);

		Assert.Equal("AK", record.StateCode);
		Assert.Equal("ALASKA", record.StateFullName);
		Assert.Equal("ADAK ISLAND", record.CityName);
		Assert.Equal("AK-1", record.Volume);
		Assert.Equal("ADAK", record.AirportName);
		Assert.Equal("N", record.Military);
		Assert.Equal("ADK", record.AptIdent);
		Assert.Equal("PADK", record.IcaoIdent);
		Assert.Equal(1244, record.Alnum);
	}

	[Fact]
	public void a_self_closing_state_with_no_cities_is_skipped_and_the_next_state_still_reads()
	{
		string body =
			"<state_code ID=\"AK\" state_fullname=\"ALASKA\" />" +
			"<state_code ID=\"AL\" state_fullname=\"ALABAMA\">" +
			"<city_name ID=\"MOBILE\" volume=\"SC-1\">" +
			"<airport_name ID=\"MOBILE RGNL\" military=\"N\" apt_ident=\"MOB\" icao_ident=\"KMOB\" alnum=\"5000\"></airport_name>" +
			"</city_name>" +
			"</state_code>";
		string path = WriteDoc(body);

		DtppMetafileDataCollection data = DtppMetafileXmlParser.Parse(path);

		DtppMetafileXmlDataModel.Airport airport = Assert.Single(data.Airports);
		Assert.Equal("MOB", airport.AptIdent);
	}

	[Fact]
	public void a_self_closing_city_with_no_airports_is_skipped_and_the_next_city_still_reads()
	{
		string body =
			"<state_code ID=\"AK\" state_fullname=\"ALASKA\">" +
			"<city_name ID=\"EMPTY CITY\" volume=\"AK-1\" />" +
			"<city_name ID=\"ADAK ISLAND\" volume=\"AK-1\">" +
			"<airport_name ID=\"ADAK\" military=\"N\" apt_ident=\"ADK\" icao_ident=\"PADK\" alnum=\"1244\"></airport_name>" +
			"</city_name>" +
			"</state_code>";
		string path = WriteDoc(body);

		DtppMetafileDataCollection data = DtppMetafileXmlParser.Parse(path);

		DtppMetafileXmlDataModel.Airport airport = Assert.Single(data.Airports);
		Assert.Equal("ADAK ISLAND", airport.CityName);
		Assert.Equal("ADK", airport.AptIdent);
	}

	[Fact]
	public void an_unknown_element_at_any_level_is_skipped_and_the_rest_still_reads()
	{
		string body =
			"<notice><state_code ID=\"ZZ\" state_fullname=\"NOT A STATE\" /></notice>" +
			"<state_code ID=\"AK\" state_fullname=\"ALASKA\">" +
			"<volume_note>AK-1</volume_note>" +
			"<city_name ID=\"ADAK ISLAND\" volume=\"AK-1\">" +
			"<heliport_name ID=\"ADAK HELIPAD\" apt_ident=\"XXX\" alnum=\"garbage\"><record /></heliport_name>" +
			"<airport_name ID=\"ADAK\" military=\"N\" apt_ident=\"ADK\" icao_ident=\"PADK\" alnum=\"1244\">" + FullRecord + "</airport_name>" +
			"</city_name>" +
			"</state_code>";
		string path = WriteDoc(body);

		DtppMetafileDataCollection data = DtppMetafileXmlParser.Parse(path);

		DtppMetafileXmlDataModel.Airport airport = Assert.Single(data.Airports);
		Assert.Equal("ADK", airport.AptIdent);
		Assert.Equal("AK", airport.StateCode);
		Assert.Equal("ADAK ISLAND", airport.CityName);
		Assert.Equal("ADK", Assert.Single(data.Records).AptIdent);
	}

	[Fact]
	public void an_airport_with_no_records_still_produces_an_airport_row()
	{
		string path = WriteDoc(WrapAirport(""));

		DtppMetafileDataCollection data = DtppMetafileXmlParser.Parse(path);

		Assert.Single(data.Airports);
		Assert.Empty(data.Records);
	}

	[Theory]
	[InlineData("N", "N")]
	[InlineData("M", "M")]
	public void military_reads_n_or_m(string value, string expected)
	{
		string path = WriteDoc(WrapAirport("", $"ID=\"ADAK\" military=\"{value}\" apt_ident=\"ADK\" icao_ident=\"PADK\" alnum=\"1244\""));

		DtppMetafileXmlDataModel.Airport airport = Assert.Single(DtppMetafileXmlParser.Parse(path).Airports);

		Assert.Equal(expected, airport.Military);
	}

	[Fact]
	public void a_blank_icao_ident_reads_as_null()
	{
		string path = WriteDoc(WrapAirport("", "ID=\"ADAK\" military=\"N\" apt_ident=\"ADK\" icao_ident=\"\" alnum=\"1244\""));

		DtppMetafileXmlDataModel.Airport airport = Assert.Single(DtppMetafileXmlParser.Parse(path).Airports);

		Assert.Null(airport.IcaoIdent);
	}

	[Fact]
	public void a_missing_icao_ident_attribute_reads_as_null()
	{
		string path = WriteDoc(WrapAirport("", "ID=\"ADAK\" military=\"N\" apt_ident=\"ADK\" alnum=\"1244\""));

		DtppMetafileXmlDataModel.Airport airport = Assert.Single(DtppMetafileXmlParser.Parse(path).Airports);

		Assert.Null(airport.IcaoIdent);
	}

	[Fact]
	public void a_missing_state_or_city_attribute_reads_as_empty()
	{
		string path = WriteDoc("<state_code><city_name><airport_name ID=\"ADAK\" apt_ident=\"ADK\" alnum=\"1\"></airport_name></city_name></state_code>");

		DtppMetafileXmlDataModel.Airport airport = Assert.Single(DtppMetafileXmlParser.Parse(path).Airports);

		Assert.Equal(string.Empty, airport.StateCode);
		Assert.Equal(string.Empty, airport.StateFullName);
		Assert.Equal(string.Empty, airport.CityName);
		Assert.Equal(string.Empty, airport.Volume);
		Assert.Equal(string.Empty, airport.Military);
	}

	// ---- multiple states/cities/airports/records, in file order ----

	[Fact]
	public void multiple_states_cities_airports_and_records_are_read_in_file_order()
	{
		string body =
			"<state_code ID=\"AK\" state_fullname=\"ALASKA\">" +
			"<city_name ID=\"ADAK ISLAND\" volume=\"AK-1\">" +
			"<airport_name ID=\"ADAK\" military=\"N\" apt_ident=\"ADK\" icao_ident=\"PADK\" alnum=\"1244\">" +
			"<record><chartseq>10100</chartseq><chart_code>MIN</chart_code><chart_name>TAKEOFF MINIMUMS</chart_name>" +
			"<useraction></useraction><pdf_name>AKTO.PDF</pdf_name><cn_flg>N</cn_flg><cnsection></cnsection>" +
			"<cnpage></cnpage><bvsection>L</bvsection><bvpage></bvpage><procuid></procuid><two_colored>N</two_colored>" +
			"<civil></civil><faanfd18></faanfd18><copter></copter><amdtnum></amdtnum><amdtdate></amdtdate></record>" +
			"</airport_name>" +
			"<airport_name ID=\"ATKA\" military=\"N\" apt_ident=\"ATK\" icao_ident=\"PAAK\" alnum=\"1245\">" +
			"</airport_name>" +
			"</city_name>" +
			"</state_code>" +
			"<state_code ID=\"AL\" state_fullname=\"ALABAMA\">" +
			"<city_name ID=\"MOBILE\" volume=\"SC-1\">" +
			"<airport_name ID=\"MOBILE RGNL\" military=\"N\" apt_ident=\"MOB\" icao_ident=\"KMOB\" alnum=\"5000\">" +
			"<record><chartseq>90100</chartseq><chart_code>DP</chart_code><chart_name>TEST ONE</chart_name>" +
			"<useraction>A</useraction><pdf_name>TEST1.PDF</pdf_name><cn_flg>N</cn_flg><cnsection></cnsection>" +
			"<cnpage></cnpage><bvsection></bvsection><bvpage></bvpage><procuid>100</procuid><two_colored>N</two_colored>" +
			"<civil></civil><faanfd18>TEST1.TEST</faanfd18><copter>N</copter><amdtnum></amdtnum><amdtdate></amdtdate></record>" +
			"</airport_name>" +
			"</city_name>" +
			"</state_code>";

		string path = WriteDoc(body);

		DtppMetafileDataCollection data = DtppMetafileXmlParser.Parse(path);

		Assert.Equal(3, data.Airports.Count);
		Assert.Equal(["ADK", "ATK", "MOB"], data.Airports.Select(a => a.AptIdent));

		Assert.Equal(2, data.Records.Count);
		Assert.Equal("AKTO.PDF", data.Records[0].PdfName);
		Assert.Equal("MOB", data.Records[1].AptIdent);
		Assert.Equal("TEST1.PDF", data.Records[1].PdfName);
	}

	// ---- every record field ----

	[Fact]
	public void every_record_field_is_read()
	{
		string path = WriteDoc(WrapAirport(FullRecord));

		DtppMetafileXmlDataModel.Record record = Assert.Single(DtppMetafileXmlParser.Parse(path).Records);

		Assert.Equal(50750, record.ChartSeq);
		Assert.Equal("IAP", record.ChartCode);
		Assert.Equal("ILS Y OR LOC Y RWY 23", record.ChartName);
		Assert.Equal("C", record.UserAction);
		Assert.Equal("01244IYLY23.PDF", record.PdfName);
		Assert.Equal("N", record.CnFlg);
		Assert.Null(record.CnSection);
		Assert.Null(record.CnPage);
		Assert.Null(record.BvSection);
		Assert.Equal(1, record.BvPage);
		Assert.Equal(39683, record.ProcUid);
		Assert.Equal("Y", record.TwoColored);
		Assert.Equal("C", record.Civil);
		Assert.Null(record.Faanfd18);
		Assert.Null(record.Copter);
		Assert.Equal("0", record.AmdtNum);
		Assert.Equal("12/05/2019", record.AmdtDate);
	}

	[Fact]
	public void whitespace_padded_field_values_are_trimmed()
	{
		string record =
			"<record><chartseq> 50750 </chartseq><chart_code> IAP </chart_code><chart_name> ILS RWY 23 </chart_name>" +
			"<useraction> C </useraction><pdf_name> X.PDF </pdf_name><cn_flg> N </cn_flg><cnsection></cnsection>" +
			"<cnpage></cnpage><bvsection></bvsection><bvpage> 1 </bvpage><procuid> 5 </procuid><two_colored> Y </two_colored>" +
			"<civil> C </civil><faanfd18></faanfd18><copter></copter><amdtnum> 0 </amdtnum><amdtdate> 12/05/2019 </amdtdate></record>";
		string path = WriteDoc(WrapAirport(record));

		DtppMetafileXmlDataModel.Record data = Assert.Single(DtppMetafileXmlParser.Parse(path).Records);

		Assert.Equal(50750, data.ChartSeq);
		Assert.Equal("IAP", data.ChartCode);
		Assert.Equal("ILS RWY 23", data.ChartName);
		Assert.Equal("C", data.UserAction);
		Assert.Equal("X.PDF", data.PdfName);
		Assert.Equal("N", data.CnFlg);
		Assert.Equal(1, data.BvPage);
		Assert.Equal(5, data.ProcUid);
		Assert.Equal("Y", data.TwoColored);
		Assert.Equal("C", data.Civil);
		Assert.Equal("0", data.AmdtNum);
		Assert.Equal("12/05/2019", data.AmdtDate);
	}

	[Theory]
	[InlineData("A")]
	[InlineData("C")]
	[InlineData("D")]
	public void useraction_reads_a_c_or_d(string value)
	{
		string record = $"<record><chartseq>1</chartseq><useraction>{value}</useraction></record>";
		string path = WriteDoc(WrapAirport(record));

		DtppMetafileXmlDataModel.Record data = Assert.Single(DtppMetafileXmlParser.Parse(path).Records);

		Assert.Equal(value, data.UserAction);
	}

	[Fact]
	public void an_empty_useraction_reads_as_null()
	{
		string path = WriteDoc(WrapAirport("<record><chartseq>1</chartseq><useraction></useraction></record>"));

		DtppMetafileXmlDataModel.Record data = Assert.Single(DtppMetafileXmlParser.Parse(path).Records);

		Assert.Null(data.UserAction);
	}

	[Fact]
	public void missing_record_elements_read_as_empty_or_null_and_ints_as_null()
	{
		string path = WriteDoc(WrapAirport("<record><chartseq>1</chartseq></record>"));

		DtppMetafileXmlDataModel.Record data = Assert.Single(DtppMetafileXmlParser.Parse(path).Records);

		Assert.Equal(1, data.ChartSeq);
		Assert.Equal(string.Empty, data.ChartCode);
		Assert.Equal(string.Empty, data.ChartName);
		Assert.Null(data.UserAction);
		Assert.Equal(string.Empty, data.PdfName);
		Assert.Equal(string.Empty, data.CnFlg);
		Assert.Null(data.CnSection);
		Assert.Null(data.CnPage);
		Assert.Null(data.BvSection);
		Assert.Null(data.BvPage);
		Assert.Null(data.ProcUid);
		Assert.Equal(string.Empty, data.TwoColored);
		Assert.Null(data.Civil);
		Assert.Null(data.Faanfd18);
		Assert.Null(data.Copter);
		Assert.Null(data.AmdtNum);
		Assert.Null(data.AmdtDate);
	}

	[Theory]
	[InlineData("")]
	[InlineData("not-a-number")]
	public void a_blank_or_garbage_cnpage_bvpage_or_procuid_reads_as_null(string value)
	{
		string record =
			$"<record><chartseq>1</chartseq><cnpage>{value}</cnpage><bvpage>{value}</bvpage><procuid>{value}</procuid></record>";
		string path = WriteDoc(WrapAirport(record));

		DtppMetafileXmlDataModel.Record data = Assert.Single(DtppMetafileXmlParser.Parse(path).Records);

		Assert.Null(data.CnPage);
		Assert.Null(data.BvPage);
		Assert.Null(data.ProcUid);
	}

	[Fact]
	public void multiple_records_under_one_airport_are_read_in_order()
	{
		string body = WrapAirport(
			"<record><chartseq>10100</chartseq><chart_name>TAKEOFF MINIMUMS</chart_name></record>" +
			"<record><chartseq>10200</chartseq><chart_name>ALTERNATE MINIMUMS</chart_name></record>" +
			"<record><chartseq>50750</chartseq><chart_name>ILS RWY 23</chart_name></record>");
		string path = WriteDoc(body);

		DtppMetafileDataCollection data = DtppMetafileXmlParser.Parse(path);

		Assert.Equal(3, data.Records.Count);
		Assert.Equal(["TAKEOFF MINIMUMS", "ALTERNATE MINIMUMS", "ILS RWY 23"], data.Records.Select(r => r.ChartName));
	}

	// ---- bad required ints ----

	[Theory]
	[InlineData("")]
	[InlineData("abc")]
	public void a_blank_or_non_numeric_alnum_throws_naming_the_airport(string value)
	{
		string path = WriteDoc(WrapAirport("", $"ID=\"ADAK\" military=\"N\" apt_ident=\"ADK\" icao_ident=\"PADK\" alnum=\"{value}\""));

		InvalidDataException ex = Assert.Throws<InvalidDataException>(() => DtppMetafileXmlParser.Parse(path));
		Assert.Contains("alnum", ex.Message, StringComparison.Ordinal);
		Assert.Contains("ADK", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void a_missing_alnum_attribute_throws_naming_the_airport()
	{
		string path = WriteDoc(WrapAirport("", "ID=\"ADAK\" military=\"N\" apt_ident=\"ADK\" icao_ident=\"PADK\""));

		InvalidDataException ex = Assert.Throws<InvalidDataException>(() => DtppMetafileXmlParser.Parse(path));
		Assert.Contains("alnum", ex.Message, StringComparison.Ordinal);
		Assert.Contains("ADK", ex.Message, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("")]
	[InlineData("abc")]
	public void a_blank_or_non_numeric_chartseq_throws_naming_the_airport_and_the_record(string value)
	{
		string record = $"<record><chartseq>{value}</chartseq><pdf_name>X.PDF</pdf_name><chart_name>SOME CHART</chart_name></record>";
		string path = WriteDoc(WrapAirport(record));

		InvalidDataException ex = Assert.Throws<InvalidDataException>(() => DtppMetafileXmlParser.Parse(path));
		Assert.Contains("chartseq", ex.Message, StringComparison.Ordinal);
		Assert.Contains("ADK", ex.Message, StringComparison.Ordinal);
		Assert.Contains("X.PDF", ex.Message, StringComparison.Ordinal);
		Assert.Contains("SOME CHART", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void a_missing_chartseq_element_throws_naming_the_airport_and_the_record()
	{
		string record = "<record><pdf_name>X.PDF</pdf_name><chart_name>SOME CHART</chart_name></record>";
		string path = WriteDoc(WrapAirport(record));

		InvalidDataException ex = Assert.Throws<InvalidDataException>(() => DtppMetafileXmlParser.Parse(path));
		Assert.Contains("chartseq", ex.Message, StringComparison.Ordinal);
		Assert.Contains("ADK", ex.Message, StringComparison.Ordinal);
		Assert.Contains("X.PDF", ex.Message, StringComparison.Ordinal);
	}

	// ---- malformed input ----

	[Fact]
	public void a_wrong_root_element_throws()
	{
		string path = WriteXml("<not_digital_tpp></not_digital_tpp>");

		InvalidDataException ex = Assert.Throws<InvalidDataException>(() => DtppMetafileXmlParser.Parse(path));
		Assert.Contains("<digital_tpp>", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void a_dtd_in_the_file_is_rejected()
	{
		string path = WriteXml("<?xml version=\"1.0\"?><!DOCTYPE digital_tpp [<!ENTITY x \"y\">]><digital_tpp cycle=\"2609\"></digital_tpp>");

		Assert.ThrowsAny<XmlException>(() => DtppMetafileXmlParser.Parse(path));
	}

	[Fact]
	public void parse_rejects_a_blank_path() =>
		Assert.Throws<ArgumentException>(() => DtppMetafileXmlParser.Parse(" "));

	// ---- async wrapper ----

	[Fact]
	public async Task parse_async_reads_the_same_data_as_the_sync_parse()
	{
		string path = WriteDoc(WrapAirport(""));

		DtppMetafileDataCollection data = await DtppMetafileXmlParser.ParseAsync(path);

		Assert.Single(data.Airports);
	}

	[Fact]
	public async Task parse_async_with_an_already_cancelled_token_throws()
	{
		string path = WriteDoc("");

		await Assert.ThrowsAsync<OperationCanceledException>(
			() => DtppMetafileXmlParser.ParseAsync(path, new CancellationToken(canceled: true)));
	}

	// ---- real-shape sanity check ----

	/// <summary>
	/// The example file's first airport (ADAK): one state, one city, one airport, and its first
	/// two MIN records plus one IAP record - copied verbatim from the real 2609 file's shape.
	/// </summary>
	[Fact]
	public void a_real_shaped_adak_sample_parses_correctly()
	{
		string xml =
			"<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>" +
			"<digital_tpp cycle=\"2609\" from_edate=\"0901Z  09/03/26\" to_edate=\"0901Z  10/01/26\">" +
			"<state_code ID=\"AK\" state_fullname=\"ALASKA\">" +
			"<city_name ID=\"ADAK ISLAND\" volume=\"AK-1\">" +
			"<airport_name ID=\"ADAK\" military=\"N\" apt_ident=\"ADK\" icao_ident=\"PADK\" alnum=\"1244\">" +
			"<record><chartseq>10100</chartseq><chart_code>MIN</chart_code><chart_name>TAKEOFF MINIMUMS</chart_name>" +
			"<useraction></useraction><pdf_name>AKTO.PDF</pdf_name><cn_flg>N</cn_flg><cnsection></cnsection>" +
			"<cnpage></cnpage><bvsection>L</bvsection><bvpage></bvpage><procuid></procuid><two_colored>N</two_colored>" +
			"<civil></civil><faanfd18></faanfd18><copter></copter><amdtnum></amdtnum><amdtdate></amdtdate></record>" +
			"<record><chartseq>10200</chartseq><chart_code>MIN</chart_code><chart_name>ALTERNATE MINIMUMS</chart_name>" +
			"<useraction></useraction><pdf_name>AKALT.PDF</pdf_name><cn_flg>N</cn_flg><cnsection></cnsection>" +
			"<cnpage></cnpage><bvsection>M</bvsection><bvpage></bvpage><procuid></procuid><two_colored>N</two_colored>" +
			"<civil></civil><faanfd18></faanfd18><copter></copter><amdtnum></amdtnum><amdtdate></amdtdate></record>" +
			"<record><chartseq>50750</chartseq><chart_code>IAP</chart_code><chart_name>ILS Y OR LOC Y RWY 23</chart_name>" +
			"<useraction></useraction><pdf_name>01244IYLY23.PDF</pdf_name><cn_flg>N</cn_flg><cnsection></cnsection>" +
			"<cnpage></cnpage><bvsection></bvsection><bvpage>1</bvpage><procuid>39683</procuid><two_colored>Y</two_colored>" +
			"<civil>C</civil><faanfd18></faanfd18><copter></copter><amdtnum>0</amdtnum><amdtdate>12/05/2019</amdtdate></record>" +
			"</airport_name>" +
			"</city_name>" +
			"</state_code>" +
			"</digital_tpp>";
		string path = WriteXml(xml);

		DtppMetafileDataCollection data = DtppMetafileXmlParser.Parse(path);

		Assert.Equal("2609", data.Cycle);
		Assert.Equal(new DateTime(2026, 9, 3, 9, 1, 0, DateTimeKind.Utc), data.FromEffectiveUtc);
		Assert.Equal(new DateTime(2026, 10, 1, 9, 1, 0, DateTimeKind.Utc), data.ToEffectiveUtc);

		DtppMetafileXmlDataModel.Airport airport = Assert.Single(data.Airports);
		Assert.Equal("ADK", airport.AptIdent);
		Assert.Equal("PADK", airport.IcaoIdent);
		Assert.Equal(1244, airport.Alnum);

		Assert.Equal(3, data.Records.Count);
		Assert.Equal("AKTO.PDF", data.Records[0].PdfName);
		Assert.Equal("L", data.Records[0].BvSection);
		Assert.Equal("AKALT.PDF", data.Records[1].PdfName);
		Assert.Equal("M", data.Records[1].BvSection);
		Assert.Equal("01244IYLY23.PDF", data.Records[2].PdfName);
		Assert.Equal(39683, data.Records[2].ProcUid);
		Assert.Equal("12/05/2019", data.Records[2].AmdtDate);
		Assert.All(data.Records, r => Assert.Equal("ADK", r.AptIdent));
	}
}
