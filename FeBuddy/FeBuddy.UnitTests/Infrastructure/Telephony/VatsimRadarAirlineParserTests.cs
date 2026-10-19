using System.Text;
using System.Text.Json;

using FeBuddy.Core.Infrastructure.Telephony.Models;
using FeBuddy.Core.Infrastructure.Telephony.Parsers;

namespace FeBuddy.UnitTests.Infrastructure.Telephony;

/// <summary>
/// Covers <see cref="VatsimRadarAirlineParser"/> against small real files: VATSIM-Radar's GitHub list
/// (one JSON array) - only its virtual airlines, at least one of them. An entry missing a value, or
/// with one that is not text or is blank, is skipped; and a file that is not the list throws, which is
/// what keeps a bad download from replacing FE-Buddy's copy.
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

	/// <summary>Only the virtual airlines - not the RAAF, the US Navy and the like - trimmed, in the list's order.</summary>
	[Fact]
	public void the_github_list_gives_its_virtual_airlines()
	{
		string path = Write("""
			[
			  { "icao": " OCN ", "name": " vOCN ", "callsign": " Ocean ", "virtual": true, "country": "US" },
			  { "icao": "RSF", "name": "Royal Australian Air Force", "callsign": "AUSSIE", "virtual": false },
			  { "icao": "NWR", "name": "NICKS AIR", "callsign": "FABULOUS", "virtual": true }
			]
			""");

		Assert.Equal(
			[new VatsimRadarAirline("OCN", "vOCN", "Ocean"), new VatsimRadarAirline("NWR", "NICKS AIR", "FABULOUS")],
			VatsimRadarAirlineParser.ParseGitHubList(path));
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

		Assert.Equal([new VatsimRadarAirline("ASK", "AIRSKY", "AIRSKY")], VatsimRadarAirlineParser.ParseGitHubList(path));
	}

	/// <summary>A file that isn't the array, or has no virtual airline, isn't the list.</summary>
	[Theory]
	[InlineData("""{ "virtual": [{ "icao": "DAL", "name": "Fly Delta Virtual", "callsign": "Delta", "virtual": true }] }""")]
	[InlineData("""[{ "icao": "RSF", "name": "RAAF", "callsign": "AUSSIE", "virtual": false }]""")]
	[InlineData("[]")]
	[InlineData("42")]
	public void a_file_that_is_not_the_github_list_or_has_no_virtual_airline_is_refused(string json)
	{
		Assert.Throws<InvalidDataException>(() => VatsimRadarAirlineParser.ParseGitHubList(Write(json)));
	}

	[Fact]
	public void the_github_check_refuses_what_is_not_json_and_a_blank_path()
	{
		Assert.ThrowsAny<JsonException>(() => VatsimRadarAirlineParser.ParseGitHubList(Write("<html></html>")));
		Assert.Throws<ArgumentException>(() => VatsimRadarAirlineParser.ParseGitHubList(""));
		Assert.Throws<ArgumentException>(() => VatsimRadarAirlineParser.ParseGitHubList(" "));
	}
}
