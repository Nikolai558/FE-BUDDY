using System.Text;
using System.Text.Json;

using FeBuddy.Core.Infrastructure.Telephony.Models;
using FeBuddy.Core.Infrastructure.Telephony.Parsers;

namespace FeBuddy.UnitTests.Infrastructure.Telephony;

/// <summary>
/// Covers <see cref="GngAirlineParser"/> against small real files: each row's <c>icao</c>,
/// <c>airline</c> and <c>callsign</c> read, trimmed and in order, the rest ignored, duplicates kept; a
/// row missing a value skipped; and a reply that isn't the whole list - its rows don't number its
/// records, fewer than 100 rows, not the object - refused, which keeps a cut-off download from
/// replacing FE-Buddy's copy.
/// </summary>
public sealed class GngAirlineParserTests : IDisposable
{
	private readonly string _folder = Path.Combine(Path.GetTempPath(), "FeBuddyTests_Gng_" + Guid.NewGuid().ToString("N"));

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
		string path = Path.Combine(_folder, "gng_fhairlines.json");
		File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		return path;
	}

	/// <summary>GNG's reply with these rows first, then fillers up to <paramref name="rows"/>, saying it has <paramref name="records"/>.</summary>
	private string Gng(string[] first, int rows = 100, int? records = null)
	{
		IEnumerable<string> all = first.Concat(Enumerable.Range(0, rows - first.Length)
			.Select(i => $$"""{ "icao": "Q{{i:000}}", "airline": "FILLER {{i}}", "callsign": "FILLER{{i}}" }"""));

		return Write($$"""{ "records": {{records ?? rows}}, "page": 1, "total": 1, "rows": [{{string.Join(",", all)}}] }""");
	}

	/// <summary>The fields FE-Buddy uses, from a row with every field GNG sends; SKA's three rows are all kept.</summary>
	[Fact]
	public void each_rows_3ld_airline_and_callsign_are_read_trimmed_in_order_with_duplicates_kept()
	{
		string path = Gng(
		[
			"""{ "prim": "AAA§±§ANSETT", "icao": " AAA ", "airline": " ANSETT ", "callsign": " ANSETT ", "country": "AUSTRALIA", "proof": null, "addedbyvacc": "LKAA", "rl_exists": "", "in_use": "false" }""",
			"""{ "icao": "SKA", "airline": "SKY AIR", "callsign": "SKYAIR" }""",
			"""{ "icao": "SKA", "airline": "SKYALLIANCE", "callsign": "SKYALLIANCE" }""",
			"""{ "icao": "SKA", "airline": "SKYJET AIRLINES VIRTUAL", "callsign": "SKYJET" }""",
		]);

		IReadOnlyList<VatsimRadarAirline> airlines = GngAirlineParser.Parse(path);

		Assert.Equal(100, airlines.Count);
		Assert.Equal(
			[
				new VatsimRadarAirline("AAA", "ANSETT", "ANSETT"),
				new VatsimRadarAirline("SKA", "SKY AIR", "SKYAIR"),
				new VatsimRadarAirline("SKA", "SKYALLIANCE", "SKYALLIANCE"),
				new VatsimRadarAirline("SKA", "SKYJET AIRLINES VIRTUAL", "SKYJET"),
			],
			airlines.Take(4));
	}

	[Fact]
	public void a_row_missing_a_value_or_with_one_that_is_not_text_or_is_blank_is_skipped()
	{
		string path = Gng(
		[
			"""{ "airline": "NO DESIGNATOR", "callsign": "NONE" }""",
			"""{ "icao": "NUL", "airline": null, "callsign": "NULL" }""",
			"""{ "icao": "NUM", "airline": "A NUMBER", "callsign": 7 }""",
			"""{ "icao": "BLK", "airline": "BLANK", "callsign": "  " }""",
			"\"not an object\"",
		]);

		Assert.Equal(95, GngAirlineParser.Parse(path).Count);
	}

	/// <summary>A reply whose rows don't number its records - one page of several, or cut off - isn't the whole list.</summary>
	[Theory]
	[InlineData(100, 488)]
	[InlineData(488, 100)]
	public void rows_that_do_not_number_the_records_are_refused(int rows, int records)
	{
		InvalidDataException ex = Assert.Throws<InvalidDataException>(() => GngAirlineParser.Parse(Gng([], rows, records)));

		Assert.Contains($"it has {rows:N0} rows where it says it has {records}", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void fewer_than_100_rows_are_refused()
	{
		Assert.Equal(100, GngAirlineParser.MinimumRows);
		InvalidDataException ex = Assert.Throws<InvalidDataException>(() => GngAirlineParser.Parse(Gng([], rows: 99)));

		Assert.Contains("has only 99 airlines", ex.Message, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("""[{ "icao": "AAA", "airline": "ANSETT", "callsign": "ANSETT" }]""")]
	[InlineData("""{ "records": 1 }""")]
	[InlineData("""{ "records": 1, "rows": { "icao": "AAA" } }""")]
	[InlineData("""{ "rows": [] }""")]
	[InlineData("""{ "records": "100", "rows": [] }""")]
	public void json_that_is_not_the_list_is_refused(string json)
	{
		Assert.Throws<InvalidDataException>(() => GngAirlineParser.Parse(Write(json)));
	}

	[Fact]
	public void a_file_that_is_not_json_or_a_blank_path_is_refused()
	{
		Assert.ThrowsAny<JsonException>(() => GngAirlineParser.Parse(Write("<html></html>")));
		Assert.Throws<ArgumentException>(() => GngAirlineParser.Parse(" "));
	}
}
