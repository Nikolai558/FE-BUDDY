using System.Text;
using System.Text.Json;

using FeBuddy.Core.Infrastructure.Telephony.Models;
using FeBuddy.Core.Infrastructure.Telephony.Parsers;

namespace FeBuddy.UnitTests.Infrastructure.Telephony;

/// <summary>
/// Covers <see cref="VatsimRadarAirlineParser"/> against small real files: VATSIM-Radar's GitHub list
/// (one JSON array), as a fresh download is checked - only its virtual airlines, at least one of them -
/// and the older single copy beta.4 and earlier kept (beta.4's object, whose <c>virtual</c> array is
/// read; beta.3's array). An entry missing a value, or with one that is not text or is blank, is
/// skipped; and a file that is not the list throws, which is what keeps a bad download from replacing
/// FE-Buddy's copy.
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

	// ---- VATSIM-Radar's GitHub list, as a download is checked ----

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

	/// <summary>A download that isn't the array - beta.4's object included - or has no virtual airline isn't the list.</summary>
	[Theory]
	[InlineData("""{ "virtual": [{ "icao": "DAL", "name": "Fly Delta Virtual", "callsign": "Delta", "virtual": true }] }""")]
	[InlineData("""[{ "icao": "RSF", "name": "RAAF", "callsign": "AUSSIE", "virtual": false }]""")]
	[InlineData("[]")]
	public void a_download_that_is_not_the_github_list_or_has_no_virtual_airline_is_refused(string json)
	{
		Assert.Throws<InvalidDataException>(() => VatsimRadarAirlineParser.ParseGitHubList(Write(json)));
	}

	[Fact]
	public void the_github_check_refuses_what_is_not_json_and_a_blank_path()
	{
		Assert.ThrowsAny<JsonException>(() => VatsimRadarAirlineParser.ParseGitHubList(Write("<html></html>")));
		Assert.Throws<ArgumentException>(() => VatsimRadarAirlineParser.ParseGitHubList(" "));
	}

	// ---- a kept copy, the older single copy included ----

	/// <summary>The shape beta.4's older single copy has (<c>data.vatsim-radar.com/airlines/all</c>); NWR is issue #317's test case.</summary>
	[Fact]
	public void only_the_virtual_array_is_read_trimmed_and_in_the_lists_order()
	{
		string path = Write("""
			{
			  "airlines": [
			    { "icao": "AAB", "name": "ABELAG AVIATION", "callsign": "ABG", "virtual": false },
			    { "icao": "TALN", "name": "Royal Australian Air Force", "callsign": "Talon", "virtual": false }
			  ],
			  "virtual": [
			    { "icao": " OCN ", "name": " vOCN ", "callsign": " Ocean ", "virtual": true },
			    { "icao": "NWR", "name": "NICKS AIR", "callsign": "FABULOUS", "virtual": true },
			    { "icao": "DAL", "name": "Fly Delta Virtual", "callsign": "Delta", "virtual": true }
			  ]
			}
			""");

		Assert.Equal(
			[
				new VatsimRadarAirline("OCN", "vOCN", "Ocean"),
				new VatsimRadarAirline("NWR", "NICKS AIR", "FABULOUS"),
				new VatsimRadarAirline("DAL", "Fly Delta Virtual", "Delta"),
			],
			VatsimRadarAirlineParser.Parse(path));
	}

	[Fact]
	public void an_entry_missing_a_value_or_with_one_that_is_not_text_or_is_blank_is_skipped()
	{
		string path = Write("""
			{
			  "virtual": [
			    { "name": "No Designator", "callsign": "NONE", "virtual": true },
			    { "icao": "NUL", "name": null, "callsign": "NULL", "virtual": true },
			    { "icao": "NUM", "name": "A Number", "callsign": 7, "virtual": true },
			    { "icao": "BLK", "name": "Blank Callsign", "callsign": "   ", "virtual": true },
			    { "icao": "STR", "name": "String Virtual", "callsign": "STRING", "virtual": "true" },
			    { "icao": "NOV", "name": "No Virtual", "callsign": "NOVIRTUAL" },
			    "not an object",
			    { "icao": "ASK", "name": "AIRSKY", "callsign": "AIRSKY", "virtual": true }
			  ]
			}
			""");

		Assert.Equal([new VatsimRadarAirline("ASK", "AIRSKY", "AIRSKY")], VatsimRadarAirlineParser.Parse(path));
	}

	[Fact]
	public void an_empty_list_has_no_virtual_airlines()
	{
		Assert.Empty(VatsimRadarAirlineParser.Parse(Write("""{ "airlines": [], "virtual": [] }""")));
	}

	/// <summary>
	/// The GitHub list FE-Buddy downloaded up to beta.3 is one array, real and virtual airlines mixed.
	/// A copy of it is read until the next download replaces it.
	/// </summary>
	[Fact]
	public void a_copy_of_the_list_kept_by_beta_3_is_still_read()
	{
		string path = Write("""
			[
			  { "icao": "OCN", "name": "vOCN", "callsign": "Ocean", "virtual": true, "country": "US" },
			  { "icao": "SBI", "name": "S7 AIRLINES", "callsign": "SIBERIAN AIRLINES", "virtual": false }
			]
			""");

		Assert.Equal([new VatsimRadarAirline("OCN", "vOCN", "Ocean")], VatsimRadarAirlineParser.Parse(path));
	}

	[Theory]
	[InlineData("""{ "icao": "DAL" }""")]
	[InlineData("""{ "airlines": [] }""")]
	[InlineData("""{ "virtual": { "icao": "DAL" } }""")]
	[InlineData("42")]
	public void json_with_no_array_of_virtual_airlines_is_not_the_list(string json)
	{
		Assert.Throws<InvalidDataException>(() => VatsimRadarAirlineParser.Parse(Write(json)));
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
