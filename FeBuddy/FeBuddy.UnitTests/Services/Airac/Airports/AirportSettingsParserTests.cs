using FeBuddy.Core.Models.Services.Airac.Airports;
using FeBuddy.Core.Services.Airac.Airports;
using FeBuddy.Core.Services.General;

namespace FeBuddy.UnitTests.Services.Airac.Airports;

/// <summary>
/// Covers the Airports settings parser: the combinations of output choices that would produce
/// nothing at all are rejected up front, the FE-Buddy custom-property list is validated against
/// the known names, an unrecognized key is a warning rather than a failure, and CRC property
/// defaults are demanded only for the files actually being emitted.
/// </summary>
public class AirportSettingsParserTests
{
	private static Dictionary<string, string> MinimalValidSettings() => new()
	{
		{ "OutputDirectory", @"C:\Output" }
	};

	[Fact]
	public void missing_output_directory_throws()
	{
		Assert.Throws<ArgumentException>(() => AirportSettingsParser.Parse(new Dictionary<string, string>()));
	}

	[Fact]
	public void turning_off_both_geojson_and_the_alias_file_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["GenerateGeojson"] = "N";
		settings["GenerateAliasFile"] = "N";

		Assert.Throws<ArgumentException>(() => AirportSettingsParser.Parse(settings));
	}

	[Fact]
	public void generating_geojson_with_every_emit_flag_off_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["GenerateGeojson"] = "Y";
		settings["EmitAirportSymbols"] = "N";
		settings["EmitAirportText"] = "N";
		settings["EmitRunwayLines"] = "N";

		Assert.Throws<ArgumentException>(() => AirportSettingsParser.Parse(settings));

		// The same three flags are harmless once GeoJSON itself is off - the alias file is the output.
		settings["GenerateGeojson"] = "N";
		AirportSettings parsed = AirportSettingsParser.Parse(settings).Settings;

		Assert.False(parsed.GenerateGeojson);
		Assert.True(parsed.GenerateAliasFile);
	}

	[Fact]
	public void including_feb_custom_properties_without_naming_any_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeFebCustomProperties"] = "Y";
		settings["FebProperties"] = string.Empty;

		Assert.Throws<ArgumentException>(() => AirportSettingsParser.Parse(settings));
	}

	[Fact]
	public void including_feb_custom_properties_with_the_key_absent_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeFebCustomProperties"] = "Y";

		Assert.Throws<ArgumentException>(() => AirportSettingsParser.Parse(settings));
	}

	[Fact]
	public void an_unknown_feb_property_name_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeFebCustomProperties"] = "Y";
		settings["FebProperties"] = "faaId,notAProperty";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => AirportSettingsParser.Parse(settings));
		Assert.Contains("notAProperty", ex.Message);
	}

	[Theory]
	[InlineData("lat")]
	[InlineData("LON")]
	public void a_retired_coordinate_feb_property_throws_explaining_why(string name)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeFebCustomProperties"] = "Y";
		settings["FebProperties"] = $"faaId,{name}";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => AirportSettingsParser.Parse(settings));
		Assert.Contains("geometry already carries", ex.Message);
	}

	[Fact]
	public void known_feb_property_names_parse_to_their_enum_values()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeFebCustomProperties"] = "Y";
		settings["FebProperties"] = "faaId, icaoId ,elev";

		AirportSettings parsed = AirportSettingsParser.Parse(settings).Settings;

		Assert.Equal(
			new[] { AirportFebProperty.FaaId, AirportFebProperty.IcaoId, AirportFebProperty.Elev },
			parsed.FebProperties);
	}

	[Fact]
	public void an_unrecognized_key_produces_a_warning_and_does_not_throw()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["TotallyMadeUpKey"] = "x";

		AirportSettingsParseResult result = AirportSettingsParser.Parse(settings);

		Assert.Contains(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains("TotallyMadeUpKey"));
		Assert.Equal("AirportSettingsParser", Assert.Single(result.Messages).Source);
	}

	[Fact]
	public void a_fully_default_settings_block_produces_no_messages()
	{
		AirportSettingsParseResult result = AirportSettingsParser.Parse(MinimalValidSettings());

		Assert.Empty(result.Messages);
		Assert.True(result.Settings.GenerateGeojson);
		Assert.True(result.Settings.GenerateAliasFile);
		Assert.True(result.Settings.EmitAirportSymbols);
		Assert.True(result.Settings.EmitAirportText);
		Assert.True(result.Settings.EmitRunwayLines);
		Assert.False(result.Settings.IncludeFebCustomProperties);
		Assert.False(result.Settings.IncludeCrcEramPropertyDefaults);
		Assert.Null(result.Settings.Roi);
		Assert.Equal(6, result.Settings.CoordinatePrecision);
		Assert.True(result.Settings.AddFeBuddyOutputFolder);
	}

	[Fact]
	public void crc_defaults_are_only_required_for_the_files_actually_being_emitted()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcEramPropertyDefaults"] = "Y";
		settings["EmitAirportText"] = "N";
		settings["EmitRunwayLines"] = "N";
		settings["Crc.Airports.Symbol.filters"] = "3";

		AirportSettingsParseResult result = AirportSettingsParser.Parse(settings);

		Assert.Equal(3, result.Settings.SymbolDefaults[AirportCrcClass.Airports].Filters[0]);
		Assert.Empty(result.Settings.TextDefaults);
		Assert.Empty(result.Settings.LineDefaults);
		Assert.Empty(result.Messages);

		// Turning the runway lines back on makes Crc.Runways.Line.* required again.
		settings["EmitRunwayLines"] = "Y";

		Assert.Throws<ArgumentException>(() => AirportSettingsParser.Parse(settings));
	}

	[Fact]
	public void crc_defaults_parse_for_every_emitted_file_when_fully_specified()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcEramPropertyDefaults"] = "Y";
		settings["Crc.Airports.Symbol.filters"] = "3";
		settings["Crc.Airports.Symbol.style"] = "airport";
		settings["Crc.Airports.Text.filters"] = "3";
		settings["Crc.Runways.Line.filters"] = "4";
		settings["Crc.Runways.Line.style"] = "solid";

		AirportSettings parsed = AirportSettingsParser.Parse(settings).Settings;

		Assert.Equal("airport", parsed.SymbolDefaults[AirportCrcClass.Airports].Style);
		Assert.Equal(3, parsed.TextDefaults[AirportCrcClass.Airports].Filters[0]);
		Assert.Equal("solid", parsed.LineDefaults[AirportCrcClass.Runways].Style);
		Assert.Equal(4, parsed.LineDefaults[AirportCrcClass.Runways].Filters[0]);
	}
}
