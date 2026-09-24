using System.Text.Json;

using FeBuddy.Core.Application.Airac.Airports;
using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Domain.Airports.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Airports.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Airports;

/// <summary>
/// Runs the whole Airports pipeline (<see cref="AirportService.Run"/>) and checks what lands on
/// disk - the Symbols, Text and Runways files, the alias file - and the advisory when the region
/// of interest leaves no airports for the GeoJSON.
/// </summary>
public sealed class AirportServiceTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_Airports_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		if (Directory.Exists(_outputDirectory))
		{
			Directory.Delete(_outputDirectory, recursive: true);
		}
	}

	/// <summary>SEA with one runway whose two ends are published, and PAE with no runways.</summary>
	private static NasrCsvDataCollection SeattleData(params AptCsvDataModel.AptBase[] extraAirports) =>
		AirportTestDataBuilder.Build(
			airports: new[]
			{
				AirportTestDataBuilder.Base("SEA", icaoId: "KSEA", name: "SEATTLE-TACOMA INTL", trafficPatternAltitude: 1433, fssId: "SEA"),
				AirportTestDataBuilder.Base("PAE", icaoId: "KPAE", name: "SNOHOMISH COUNTY", latitude: 47.906, longitude: -122.282),
			}.Concat(extraAirports),
			runways: [AirportTestDataBuilder.Runway("SEA", "16L/34R", 11901, "CONC")],
			runwayEnds:
			[
				AirportTestDataBuilder.RunwayEnd("SEA", "16L/34R", "16L", 47.4638, -122.3079),
				AirportTestDataBuilder.RunwayEnd("SEA", "16L/34R", "34R", 47.4312, -122.3080),
			]);

	private Dictionary<string, string> Settings(params (string Key, string Value)[] overrides)
	{
		Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputDirectory"] = _outputDirectory,
		};

		foreach ((string key, string value) in overrides)
		{
			settings[key] = value;
		}

		return settings;
	}

	private string GeojsonPath(string fileName) =>
		Path.Combine(_outputDirectory, "FE-Buddy_Output", "Airports", "Geojson", fileName);

	[Fact]
	public void run_writes_symbols_text_runways_and_the_alias_file()
	{
		AirportServiceResult result = AirportService.Run(SeattleData(), Settings());

		Assert.Equal(2, result.AirportCount);
		Assert.Equal(2, result.AirportsInRoiCount);
		Assert.Empty(result.Warnings);

		Assert.Equal(
			[GeojsonPath("Airports_Symbols.geojson"), GeojsonPath("Airports_Text.geojson"), GeojsonPath("Runways_Lines.geojson")],
			result.GeojsonFilesWritten);
		Assert.Equal(2, result.GeojsonFeatureCountsByFile[GeojsonPath("Airports_Symbols.geojson")]);
		Assert.Equal(1, result.GeojsonFeatureCountsByFile[GeojsonPath("Runways_Lines.geojson")]);

		// One .apt command per identifier: SEA, KSEA, PAE, KPAE.
		Assert.Equal(4, result.AliasCommandCount);
		Assert.Equal(Path.Combine(_outputDirectory, "FE-Buddy_Output", "Airports", "Alias", "Airports.txt"), result.AliasFilePath);
		Assert.Contains(".aptKSEA ", File.ReadAllText(result.AliasFilePath!), StringComparison.Ordinal);
	}

	[Fact]
	public void run_puts_crc_defaults_first_and_every_feb_property_on_each_feature()
	{
		AirportServiceResult result = AirportService.Run(SeattleData(), Settings(
			("IncludeCrcSymbolDefaults", "Y"),
			("IncludeCrcTextDefaults", "Y"),
			("IncludeCrcLineDefaults", "Y"),
			("Crc.Airports.Symbol.bcg", "3"), ("Crc.Airports.Symbol.filters", "3"),
			("Crc.Airports.Symbol.style", "airport"), ("Crc.Airports.Symbol.size", "1"),
			("Crc.Airports.Text.bcg", "3"), ("Crc.Airports.Text.filters", "3"),
			("Crc.Airports.Text.size", "1"), ("Crc.Airports.Text.underline", "N"),
			("Crc.Airports.Text.opaque", "N"), ("Crc.Airports.Text.xOffset", "0"),
			("Crc.Airports.Text.yOffset", "0"),
			("Crc.Runways.Line.bcg", "3"), ("Crc.Runways.Line.filters", "3"),
			("Crc.Runways.Line.style", "solid"), ("Crc.Runways.Line.thickness", "1"),
			("IncludeFebCustomProperties", "Y"),
			("FebProperties", "faaId,icaoId,name,elev,respArtcc,tfcPtrnAlt,fssId,twrType,rwyId"),
			("GenerateAliasFile", "N")));

		Assert.Null(result.AliasFilePath);
		Assert.Equal(3, result.GeojsonFilesWritten.Count);

		using JsonDocument symbols = JsonDocument.Parse(File.ReadAllText(GeojsonPath("Airports_Symbols.geojson")));
		JsonElement[] features = [.. symbols.RootElement.GetProperty("features").EnumerateArray()];
		Assert.True(features[0].GetProperty("properties").GetProperty("isSymbolDefaults").GetBoolean());

		JsonElement sea = features.Skip(1)
			.Select(f => f.GetProperty("properties"))
			.Single(p => p.GetProperty("feb.faaId").GetString() == "SEA");
		Assert.Equal("KSEA", sea.GetProperty("feb.icaoId").GetString());
		Assert.Equal("SEATTLE-TACOMA INTL", sea.GetProperty("feb.name").GetString());
		Assert.Equal(433, sea.GetProperty("feb.elev").GetDouble());
		Assert.Equal("ZSE", sea.GetProperty("feb.respArtcc").GetString());
		Assert.Equal(1433, sea.GetProperty("feb.tfcPtrnAlt").GetInt32());
		Assert.Equal("SEA", sea.GetProperty("feb.fssId").GetString());
		Assert.Equal("TWR", sea.GetProperty("feb.twrType").GetString());
		Assert.False(sea.TryGetProperty("feb.rwyId", out _));

		using JsonDocument runways = JsonDocument.Parse(File.ReadAllText(GeojsonPath("Runways_Lines.geojson")));
		JsonElement runway = runways.RootElement.GetProperty("features")[1].GetProperty("properties");
		Assert.Equal("16L/34R", Assert.Single(runway.GetProperty("feb.rwyId").EnumerateArray()).GetString());
	}

	[Fact]
	public void run_with_no_airports_in_the_roi_says_why_and_still_writes_every_alias()
	{
		AirportServiceResult result = AirportService.Run(SeattleData(), Settings(
			("FilterByRoi", "Y"),
			("RoiSwLat", "38.0"), ("RoiSwLon", "-85.0"),
			("RoiNeLat", "43.0"), ("RoiNeLon", "-78.0")));

		Assert.Equal(2, result.AirportCount);
		Assert.Equal(0, result.AirportsInRoiCount);
		Assert.Empty(result.GeojsonFilesWritten);
		Assert.Equal(4, result.AliasCommandCount);

		string warning = Assert.Single(result.Warnings);
		Assert.Contains("No airports are inside the region of interest", warning, StringComparison.Ordinal);
		Assert.Contains("The alias file still covers every airport.", warning, StringComparison.Ordinal);
	}

	[Fact]
	public void run_with_no_airports_at_all_says_none_were_found()
	{
		AirportServiceResult result = AirportService.Run(AirportTestDataBuilder.Build(), Settings(("GenerateAliasFile", "N")));

		Assert.Equal(0, result.AirportCount);
		Assert.Null(result.AliasFilePath);
		Assert.Contains("No airports were found", Assert.Single(result.Warnings), StringComparison.Ordinal);
	}

	[Fact]
	public void run_with_geojson_off_writes_only_the_alias_file_without_the_output_folder()
	{
		AirportServiceResult result = AirportService.Run(SeattleData(), Settings(
			("GenerateGeojson", "N"),
			("AddFeBuddyOutputFolder", "N")));

		Assert.Empty(result.GeojsonFilesWritten);
		Assert.Equal(Path.Combine(_outputDirectory, "Airports", "Alias", "Airports.txt"), result.AliasFilePath);
		Assert.Empty(result.Warnings);
	}

	[Fact]
	public void alias_warns_when_two_airports_would_write_the_same_command()
	{
		// A made-up airport whose FAA ID equals SEA's ICAO ID.
		AirportServiceResult result = AirportService.Run(
			SeattleData(AirportTestDataBuilder.Base("KSEA", name: "CLASHING FIELD")),
			Settings(("GenerateGeojson", "N")));

		Assert.Equal(4, result.AliasCommandCount);
		Assert.Contains(result.Warnings, w => w.Contains("'.aptKSEA' was already written", StringComparison.Ordinal));
	}

	[Fact]
	public void run_rejects_null_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => AirportService.Run(null!, Settings()));
		Assert.Throws<ArgumentNullException>(() => AirportService.Run(new NasrCsvDataCollection(), null!));
	}

	[Theory]
	[InlineData("38.0", "-85.0", "43.0", "not-a-number")]
	[InlineData("43.0", "-85.0", "38.0", "-78.0")]
	public void an_invalid_region_of_interest_is_rejected(string swLat, string swLon, string neLat, string neLon)
	{
		Dictionary<string, string> settings = Settings(
			("FilterByRoi", "Y"),
			("RoiSwLat", swLat), ("RoiSwLon", swLon),
			("RoiNeLat", neLat), ("RoiNeLon", neLon));

		ArgumentException ex = Assert.Throws<ArgumentException>(() => AirportSettingsParser.Parse(settings));
		Assert.StartsWith("Invalid Region of Interest", ex.Message, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("Crc.Airports.Line.bcg", "'Airports' has no Line output")]
	[InlineData("Crc.Runways.Symbol.bcg", "'Runways' has no Symbol output")]
	[InlineData("Crc.Airports.Text.text", "label is always built from its identifier and name")]
	[InlineData("Crc.Airports.Symbol.madeUp", "Unrecognized setting 'Crc.Airports.Symbol.madeUp'")]
	public void a_crc_key_that_cannot_apply_is_a_warning(string key, string expected)
	{
		Dictionary<string, string> settings = Settings((key, "1"));

		AirportSettingsParseResult result = AirportSettingsParser.Parse(settings);

		Assert.Contains(expected, Assert.Single(result.Messages).Text, StringComparison.Ordinal);
	}

	[Fact]
	public void alias_with_no_airports_writes_no_file()
	{
		AirportSettings settings = AirportSettingsParser.Parse(Settings()).Settings;

		AirportAliasGenerateResult result = AirportAliasWriter.Generate([], settings);

		Assert.Null(result.FilePath);
		Assert.Equal(0, result.CommandCount);
	}
}
