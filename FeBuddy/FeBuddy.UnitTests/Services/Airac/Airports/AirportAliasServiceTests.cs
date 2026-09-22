using FeBuddy.Core.Models.Services.Airac.Airports;
using FeBuddy.Core.Services.Airac.Airports;

using FeBuddy.UnitTests.Services.Airac.Airports.Fixtures;

namespace FeBuddy.UnitTests.Services.Airac.Airports;

/// <summary>
/// Covers the <c>Airports.txt</c> alias file. The load-bearing assertion is that the body holds
/// literal <c>\n</c> and <c>\t</c> escapes - two characters each - and never real newlines or
/// tabs: CRC collapses a run of real whitespace to a single space, so the escapes are the only
/// thing holding the card's layout together.
/// </summary>
public sealed class AirportAliasServiceTests : IDisposable
{
	/// <summary>The literal two-character escape CRC expands into a line break.</summary>
	private const string NewLine = @"\n";

	/// <summary>The literal two-character escape CRC expands into a tab stop.</summary>
	private const string Tab = @"\t";

	/// <summary>The prime symbol used as the feet marker (U+2032).</summary>
	private const string FeetMark = "\u2032";

	/// <summary>
	/// The literal two-character escape CRC expands into a space. Every space that has to hold a
	/// column is written this way, because CRC collapses runs of real spaces.
	/// </summary>
	private const string Space = @"\s";

	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_AptAlias_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(_outputDirectory))
			{
				Directory.Delete(_outputDirectory, recursive: true);
			}
		}
		catch
		{
			// Best-effort.
		}
	}

	private AirportSettings Settings() => new()
	{
		OutputDirectory = _outputDirectory,
		GenerateGeojson = false,
		GenerateAliasFile = true,
		IncludeFebCustomProperties = false,
		IncludeCrcLineDefaults = false,
		IncludeCrcSymbolDefaults = false,
		IncludeCrcTextDefaults = false,
	};

	[Fact]
	public void the_body_uses_literal_escape_sequences_and_never_real_newlines_or_tabs()
	{
		string body = AirportAliasService.BuildCommandBody(
			AirportTestDataBuilder.BuiltAirport(icaoId: "KSEA", weatherFrequency: "135.075", weatherFrequencyUse: "ASOS"));

		// The escapes are present as two characters each - a backslash followed by n or t.
		Assert.Contains(NewLine, body);
		Assert.Contains(Tab, body);
		Assert.Equal('\\', body[body.IndexOf(NewLine, StringComparison.Ordinal)]);
		Assert.Equal('n', body[body.IndexOf(NewLine, StringComparison.Ordinal) + 1]);
		Assert.Equal('\\', body[body.IndexOf(Tab, StringComparison.Ordinal)]);
		Assert.Equal('t', body[body.IndexOf(Tab, StringComparison.Ordinal) + 1]);

		// And no real whitespace CRC would collapse.
		Assert.DoesNotContain("\n", body);
		Assert.DoesNotContain("\r", body);
		Assert.DoesNotContain("\t", body);
	}

	[Fact]
	public void the_whole_written_file_is_one_physical_line_per_command()
	{
		AirportAliasGenerateResult result = AirportAliasService.Generate(
			new[] { AirportTestDataBuilder.BuiltAirport(faaId: "PDX") }, Settings());

		string[] lines = File.ReadAllLines(result.FilePath!);

		Assert.Single(lines);
		Assert.StartsWith(".aptPDX ", lines[0]);
	}

	[Fact]
	public void an_icao_id_shows_both_identifiers_and_produces_two_commands_with_identical_bodies()
	{
		Airport airport = AirportTestDataBuilder.BuiltAirport(faaId: "SEA", icaoId: "KSEA");
		string body = AirportAliasService.BuildCommandBody(airport);

		Assert.Contains("APT:" + Tab + Tab + Tab + "SEA - KSEA" + NewLine, body);

		AirportAliasGenerateResult result = AirportAliasService.Generate(new[] { airport }, Settings());

		Assert.Equal(2, result.CommandCount);

		string contents = File.ReadAllText(result.FilePath!);
		Assert.Contains(".aptSEA " + body, contents);
		Assert.Contains(".aptKSEA " + body, contents);
	}

	[Fact]
	public void an_airport_without_an_icao_id_shows_one_identifier_and_produces_one_command()
	{
		Airport airport = AirportTestDataBuilder.BuiltAirport(faaId: "PDX", icaoId: null);
		string body = AirportAliasService.BuildCommandBody(airport);

		Assert.Contains("APT:" + Tab + Tab + Tab + "PDX" + NewLine, body);
		Assert.DoesNotContain(" - ", body);

		AirportAliasGenerateResult result = AirportAliasService.Generate(new[] { airport }, Settings());

		Assert.Equal(1, result.CommandCount);
		Assert.DoesNotContain(".aptK", File.ReadAllText(result.FilePath!));
	}

	[Fact]
	public void a_blank_weather_frequency_omits_the_parentheses_entirely()
	{
		string body = AirportAliasService.BuildCommandBody(
			AirportTestDataBuilder.BuiltAirport(weatherFrequency: null, weatherFrequencyUse: null));

		Assert.EndsWith("WX:" + Tab + Tab + Tab + Space, body);
	}

	[Fact]
	public void a_weather_frequency_with_a_use_shows_the_use_in_parentheses()
	{
		string body = AirportAliasService.BuildCommandBody(
			AirportTestDataBuilder.BuiltAirport(weatherFrequency: "135.075", weatherFrequencyUse: "ASOS"));

		Assert.EndsWith("WX:" + Tab + Tab + Tab + Space + "135.075 (ASOS)", body);
	}

	[Fact]
	public void an_unpublished_elevation_prints_nothing_not_a_bare_foot_mark()
	{
		string body = AirportAliasService.BuildCommandBody(
			AirportTestDataBuilder.BuiltAirport(elevation: null));

		// The label is still there; what follows it is empty, not a lone foot mark.
		Assert.Contains("ELEV:", body);
		Assert.DoesNotContain("\u2032", body.Split("PTRN")[0]);
	}

	[Fact]
	public void a_sea_level_elevation_still_prints_its_value()
	{
		string body = AirportAliasService.BuildCommandBody(
			AirportTestDataBuilder.BuiltAirport(elevation: 0));

		Assert.Contains("0\u2032", body);
	}

	[Fact]
	public void a_null_traffic_pattern_altitude_prints_nothing_not_a_bare_foot_mark()
	{
		string body = AirportAliasService.BuildCommandBody(
			AirportTestDataBuilder.BuiltAirport(trafficPatternAltitude: null));

		Assert.Contains("PTRN" + Space + "ALT:" + Tab + Space + Space + Space + NewLine, body);
		Assert.DoesNotContain("PTRN" + Space + "ALT:" + Tab + Space + Space + Space + FeetMark, body);
	}

	[Fact]
	public void a_published_traffic_pattern_altitude_is_printed_with_the_foot_mark()
	{
		string body = AirportAliasService.BuildCommandBody(
			AirportTestDataBuilder.BuiltAirport(trafficPatternAltitude: 1500));

		Assert.Contains("PTRN" + Space + "ALT:" + Tab + Space + Space + Space + "1500" + FeetMark + NewLine, body);
	}

	[Fact]
	public void an_airport_with_no_runways_prints_an_empty_longest_runway_with_no_empty_parentheses()
	{
		string body = AirportAliasService.BuildCommandBody(
			AirportTestDataBuilder.BuiltAirport(runways: Array.Empty<AirportRunway>()));

		Assert.Contains("LONGEST" + Space + "RWY:" + Tab + NewLine, body);
		Assert.DoesNotContain("()", body);
	}

	[Fact]
	public void the_foot_mark_is_the_prime_symbol_not_an_apostrophe()
	{
		string body = AirportAliasService.BuildCommandBody(AirportTestDataBuilder.BuiltAirport(
			elevation: 433,
			trafficPatternAltitude: 1500,
			runways: new[] { AirportTestDataBuilder.BuiltRunway("16L/34R", 11901, "ASPH-G") }));

		Assert.Contains("LONGEST" + Space + "RWY:" + Tab + "16L/34R (11901" + FeetMark + ")" + NewLine, body);
		Assert.Contains("ELEV:" + Tab + Tab + Space + Space + Space + "433" + FeetMark + NewLine, body);

		Assert.Equal('\u2032', FeetMark[0]);
		Assert.DoesNotContain("433'", body);
		Assert.DoesNotContain("433\u2019", body);
		Assert.DoesNotContain("433\u00B4", body);
	}

	[Fact]
	public void the_surface_type_of_the_longest_runway_follows_it_on_its_own_line()
	{
		string body = AirportAliasService.BuildCommandBody(AirportTestDataBuilder.BuiltAirport(
			runways: new[] { AirportTestDataBuilder.BuiltRunway("16L/34R", 11901, "ASPH-G") }));

		Assert.Contains(Tab + Tab + Tab + Tab + "ASPH-G" + NewLine, body);
	}

	[Fact]
	public void nothing_is_written_when_there_are_no_airports()
	{
		AirportAliasGenerateResult result = AirportAliasService.Generate(Array.Empty<Airport>(), Settings());

		Assert.Null(result.FilePath);
		Assert.Equal(0, result.CommandCount);
	}
}
