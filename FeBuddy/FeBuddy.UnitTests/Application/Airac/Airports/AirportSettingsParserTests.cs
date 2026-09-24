using FeBuddy.Core.Application.Airac.Airports;
using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Application.Airac.Airports;

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

	/// <summary>Adds a complete, valid set of CRC defaults for one class and kind.</summary>
	private static void AddCrcDefaults(Dictionary<string, string> settings, string crcClass, string kind)
	{
		string prefix = $"Crc.{crcClass}.{kind}";
		settings[$"{prefix}.bcg"] = "3";
		settings[$"{prefix}.filters"] = "3";

		switch (kind)
		{
			case "Line":
				settings[$"{prefix}.style"] = "solid";
				settings[$"{prefix}.thickness"] = "1";
				break;

			case "Symbol":
				settings[$"{prefix}.style"] = "vor";
				settings[$"{prefix}.size"] = "1";
				break;

			case "Text":
				settings[$"{prefix}.size"] = "1";
				settings[$"{prefix}.underline"] = "N";
				settings[$"{prefix}.opaque"] = "N";
				settings[$"{prefix}.xOffset"] = "0";
				settings[$"{prefix}.yOffset"] = "0";
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown CRC feature kind.");
		}
	}

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
			[AirportFebProperty.FaaId, AirportFebProperty.IcaoId, AirportFebProperty.Elev],
			parsed.FebProperties);
	}

	[Theory]
	[InlineData("rwyId")]
	[InlineData("RWYID")]
	public void the_runway_id_feb_property_name_parses_case_insensitively(string name)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeFebCustomProperties"] = "Y";
		settings["FebProperties"] = name;

		AirportSettings parsed = AirportSettingsParser.Parse(settings).Settings;

		Assert.Equal(AirportFebProperty.RwyId, Assert.Single(parsed.FebProperties));
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
		Assert.False(result.Settings.IncludeCrcLineDefaults);
		Assert.False(result.Settings.IncludeCrcSymbolDefaults);
		Assert.False(result.Settings.IncludeCrcTextDefaults);
		Assert.Null(result.Settings.Roi);
		Assert.Equal(6, result.Settings.CoordinatePrecision);
		Assert.True(result.Settings.AddFeBuddyOutputFolder);
	}

	[Fact]
	public void crc_defaults_are_only_required_for_the_files_actually_being_emitted()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcLineDefaults"] = "Y";
		settings["IncludeCrcSymbolDefaults"] = "Y";
		settings["IncludeCrcTextDefaults"] = "Y";
		settings["EmitAirportText"] = "N";
		settings["EmitRunwayLines"] = "N";
		AddCrcDefaults(settings, "Airports", "Symbol");

		AirportSettingsParseResult result = AirportSettingsParser.Parse(settings);

		Assert.Equal(3, result.Settings.SymbolDefaults[AirportCrcClass.Airports].Filters[0]);
		Assert.Empty(result.Settings.TextDefaults);
		Assert.Empty(result.Settings.LineDefaults);
		Assert.Empty(result.Messages);

		// Turning the runway lines back on makes Crc.Runways.Line.* required again.
		settings["EmitRunwayLines"] = "Y";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => AirportSettingsParser.Parse(settings));
		Assert.Contains("Crc.Runways.Line.bcg", ex.Message);
	}

	[Fact]
	public void crc_defaults_parse_for_every_emitted_file_when_fully_specified()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcLineDefaults"] = "Y";
		settings["IncludeCrcSymbolDefaults"] = "Y";
		settings["IncludeCrcTextDefaults"] = "Y";
		AddCrcDefaults(settings, "Airports", "Symbol");
		AddCrcDefaults(settings, "Airports", "Text");
		AddCrcDefaults(settings, "Runways", "Line");
		settings["Crc.Airports.Symbol.style"] = "airport";
		settings["Crc.Runways.Line.filters"] = "4";

		AirportSettings parsed = AirportSettingsParser.Parse(settings).Settings;

		Assert.Equal("airport", parsed.SymbolDefaults[AirportCrcClass.Airports].Style);
		Assert.Equal(3, parsed.TextDefaults[AirportCrcClass.Airports].Filters[0]);
		Assert.Equal("solid", parsed.LineDefaults[AirportCrcClass.Runways].Style);
		Assert.Equal(4, parsed.LineDefaults[AirportCrcClass.Runways].Filters[0]);
	}

	[Fact]
	public void a_text_default_missing_x_offset_throws_naming_the_key_only_when_text_is_emitted()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcLineDefaults"] = "Y";
		settings["IncludeCrcSymbolDefaults"] = "Y";
		settings["IncludeCrcTextDefaults"] = "Y";
		AddCrcDefaults(settings, "Airports", "Symbol");
		AddCrcDefaults(settings, "Airports", "Text");
		AddCrcDefaults(settings, "Runways", "Line");
		settings.Remove("Crc.Airports.Text.xOffset");

		ArgumentException ex = Assert.Throws<ArgumentException>(() => AirportSettingsParser.Parse(settings));
		Assert.Contains("Crc.Airports.Text.xOffset", ex.Message);

		settings["EmitAirportText"] = "N";
		AirportSettings parsed = AirportSettingsParser.Parse(settings).Settings;

		Assert.Empty(parsed.TextDefaults);
		Assert.Single(parsed.SymbolDefaults);
		Assert.Single(parsed.LineDefaults);
	}

	[Fact]
	public void only_the_crc_kinds_asked_for_have_their_defaults_read()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcLineDefaults"] = "N";
		settings["IncludeCrcSymbolDefaults"] = "Y";
		settings["IncludeCrcTextDefaults"] = "N";
		AddCrcDefaults(settings, "Airports", "Symbol");
		// No Crc.Airports.Text.* or Crc.Runways.Line.* keys: those defaults were not asked for.

		AirportSettingsParseResult result = AirportSettingsParser.Parse(settings);

		Assert.False(result.Settings.IncludeCrcLineDefaults);
		Assert.True(result.Settings.IncludeCrcSymbolDefaults);
		Assert.False(result.Settings.IncludeCrcTextDefaults);
		Assert.Equal("vor", Assert.Single(result.Settings.SymbolDefaults).Value.Style);
		Assert.Empty(result.Settings.LineDefaults);
		Assert.Empty(result.Settings.TextDefaults);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void asking_for_crc_defaults_on_a_file_that_is_not_emitted_has_no_effect()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcLineDefaults"] = "Y";
		settings["EmitRunwayLines"] = "N";
		// No Crc.Runways.Line.* keys: runway lines are not written, so their defaults are not needed.

		AirportSettings parsed = AirportSettingsParser.Parse(settings).Settings;

		Assert.False(parsed.IncludeCrcLineDefaults);
		Assert.Empty(parsed.LineDefaults);
	}

	[Fact]
	public void crc_defaults_are_not_required_when_geojson_is_off()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["GenerateGeojson"] = "N";
		settings["IncludeCrcLineDefaults"] = "Y";
		settings["IncludeCrcSymbolDefaults"] = "Y";
		settings["IncludeCrcTextDefaults"] = "Y";
		// No Crc.* keys at all: no GeoJSON is written, so no defaults are needed.

		AirportSettings parsed = AirportSettingsParser.Parse(settings).Settings;

		Assert.False(parsed.IncludeCrcLineDefaults);
		Assert.False(parsed.IncludeCrcSymbolDefaults);
		Assert.False(parsed.IncludeCrcTextDefaults);
		Assert.Empty(parsed.LineDefaults);
		Assert.Empty(parsed.SymbolDefaults);
		Assert.Empty(parsed.TextDefaults);
	}

	[Fact]
	public void the_retired_combined_crc_include_key_produces_a_warning_and_includes_nothing()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcEramPropertyDefaults"] = "Y";

		AirportSettingsParseResult result = AirportSettingsParser.Parse(settings);

		Assert.Contains(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains("IncludeCrcEramPropertyDefaults"));
		Assert.False(result.Settings.IncludeCrcLineDefaults);
		Assert.False(result.Settings.IncludeCrcSymbolDefaults);
		Assert.False(result.Settings.IncludeCrcTextDefaults);
	}
}
