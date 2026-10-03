using System.Text;
using System.Text.Json;

using FeBuddy.Core.Infrastructure.Telephony.Models;
using FeBuddy.Core.Infrastructure.Telephony.Parsers;

namespace FeBuddy.UnitTests.Infrastructure.Telephony;

/// <summary>
/// Covers <see cref="VatsimRadarAirlineParser"/> against small real files: only the virtual airlines
/// are read, trimmed and in the list's order; an entry missing a value, or with one that is not text
/// or is blank, is skipped; and a file that is not the list - not JSON, or JSON but not an array -
/// throws, which is what keeps a bad download from replacing FE-Buddy's copy.
/// </summary>
public sealed class VatsimRadarAirlineParserTests : IDisposable
{
	private readonly string _folder = Path.Combine(Path.GetTempPath(), "FeBuddyTests_VatsimRadar_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		if (Directory.Exists(_folder))
		{
			Directory.Delete(_folder, recursive: true);
		}
	}

	private string Write(string json)
	{
		Directory.CreateDirectory(_folder);
		string path = Path.Combine(_folder, "airlines.json");
		File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		return path;
	}

	[Fact]
	public void only_the_virtual_airlines_are_read_trimmed_and_in_the_lists_order()
	{
		string path = Write("""
			[
			  { "icao": " OCN ", "name": " vOCN ", "callsign": " Ocean ", "virtual": true, "country": "US" },
			  { "icao": "SBI", "name": "S7 AIRLINES", "callsign": "SIBERIAN AIRLINES", "virtual": false },
			  { "icao": "DAL", "name": "Fly Delta Virtual", "callsign": "Delta", "virtual": true }
			]
			""");

		Assert.Equal(
			[new VatsimRadarAirline("OCN", "vOCN", "Ocean"), new VatsimRadarAirline("DAL", "Fly Delta Virtual", "Delta")],
			VatsimRadarAirlineParser.Parse(path));
	}

	[Fact]
	public void an_entry_missing_a_value_or_with_one_that_is_not_text_or_is_blank_is_skipped()
	{
		string path = Write("""
			[
			  { "name": "No Designator", "callsign": "NONE", "virtual": true },
			  { "icao": "NUL", "name": null, "callsign": "NULL", "virtual": true },
			  { "icao": "NUM", "name": "A Number", "callsign": 7, "virtual": true },
			  { "icao": "BLK", "name": "Blank Callsign", "callsign": "   ", "virtual": true },
			  { "icao": "STR", "name": "String Virtual", "callsign": "STRING", "virtual": "true" },
			  { "icao": "NOV", "name": "No Virtual", "callsign": "NOVIRTUAL" },
			  "not an object",
			  { "icao": "ASK", "name": "AIRSKY", "callsign": "AIRSKY", "virtual": true }
			]
			""");

		Assert.Equal([new VatsimRadarAirline("ASK", "AIRSKY", "AIRSKY")], VatsimRadarAirlineParser.Parse(path));
	}

	[Fact]
	public void an_empty_list_has_no_virtual_airlines()
	{
		Assert.Empty(VatsimRadarAirlineParser.Parse(Write("[]")));
	}

	[Fact]
	public void json_that_is_not_an_array_is_not_the_list()
	{
		Assert.Throws<InvalidDataException>(() => VatsimRadarAirlineParser.Parse(Write("""{ "icao": "DAL" }""")));
	}

	[Fact]
	public void a_file_that_is_not_json_is_not_the_list()
	{
		Assert.ThrowsAny<JsonException>(() => VatsimRadarAirlineParser.Parse(Write("<html></html>")));
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void a_blank_path_is_rejected(string path)
	{
		Assert.Throws<ArgumentException>(() => VatsimRadarAirlineParser.Parse(path));
	}
}
