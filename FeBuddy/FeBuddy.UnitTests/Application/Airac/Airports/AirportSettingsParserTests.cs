using FeBuddy.Core.Application.Airac.Airports;
using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Application.Airac.Airports;

/// <summary>
/// Covers the Airports settings parser: the combinations of output choices that would produce
/// nothing at all are rejected up front, the FE-Buddy custom-property list is validated against
/// the known names, the CrcDefaultsFor file keys are Airports' own, an unrecognized key is a warning
/// rather than a failure, and CRC property defaults are demanded only for the files that get them
/// and are actually being emitted.
/// </summary>
public sealed class AirportSettingsParserTests
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

	// ---- the area: the ROI or everything (issue #335) ----

	private static Dictionary<string, string> WithRoi()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["FilterByRoi"] = "Y";
		settings["RoiSwLat"] = "32.5";
		settings["RoiSwLon"] = "-120";
		settings["RoiNeLat"] = "37";
		settings["RoiNeLon"] = "-114";
		return settings;
	}

	[Fact]
	public void the_roi_area_reads_the_roi_and_everything_leaves_it_out()
	{
		Dictionary<string, string> settings = WithRoi();
		settings["Area"] = "Roi";

		Assert.Equal(32.5, AirportSettingsParser.Parse(settings).Settings.Roi!.SwLat);

		settings["Area"] = "Everything";

		Assert.Null(AirportSettingsParser.Parse(settings).Settings.Roi);
	}

	/// <summary>Airports has no ARTCCs to pick, so the ARTCCs area isn't one it offers.</summary>
	[Fact]
	public void the_artccs_area_is_not_one_airports_offers()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["Area"] = "Artccs";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => AirportSettingsParser.Parse(settings));
		Assert.Contains("\"Roi\", \"Everything\"", ex.Message, StringComparison.Ordinal);
	}

	/// <summary>A block without <c>Area</c> means what it did before: the ROI when FilterByRoi is Y.</summary>
	[Fact]
	public void a_block_without_an_area_reads_the_roi_when_filter_by_roi_is_set()
	{
		Assert.NotNull(AirportSettingsParser.Parse(WithRoi()).Settings.Roi);
		Assert.Null(AirportSettingsParser.Parse(MinimalValidSettings()).Settings.Roi);
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
	public void generate_alias_file_is_airports_own_key_and_produces_no_unknown_key_warning()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["GenerateAliasFile"] = "Y";

		AirportSettingsParseResult result = AirportSettingsParser.Parse(settings);

		Assert.DoesNotContain(result.Messages, m => m.Text.Contains("GenerateAliasFile"));
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
		Assert.Empty(result.Settings.CrcDefaultsFiles.Files);
		Assert.Empty(result.Settings.LineDefaults);
		Assert.Empty(result.Settings.SymbolDefaults);
		Assert.Empty(result.Settings.TextDefaults);
		Assert.Null(result.Settings.Roi);
		Assert.Equal(6, result.Settings.CoordinatePrecision);
	}

	/// <summary>Chooses every Airports GeoJSON file for CRC-ERAM defaults.</summary>
	private static void ChooseEveryFileForCrcDefaults(Dictionary<string, string> settings)
	{
		settings["CrcDefaultsFor"] = "Runways_Lines,Airports_Symbols,Airports_Text";
	}

	[Fact]
	public void every_airports_geojson_file_key_is_accepted_ignoring_case()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["CrcDefaultsFor"] = "runways_lines, AIRPORTS_SYMBOLS, Airports_Text";
		AddCrcDefaults(settings, "Airports", "Symbol");
		AddCrcDefaults(settings, "Airports", "Text");
		AddCrcDefaults(settings, "Runways", "Line");

		AirportSettings parsed = AirportSettingsParser.Parse(settings).Settings;

		Assert.True(parsed.CrcDefaultsFiles.HasCrcDefaults(AirportOutputFiles.RunwaysLines));
		Assert.True(parsed.CrcDefaultsFiles.HasCrcDefaults(AirportOutputFiles.AirportsSymbols));
		Assert.True(parsed.CrcDefaultsFiles.HasCrcDefaults(AirportOutputFiles.AirportsText));
	}

	/// <summary>Only a GeoJSON file Airports writes can get CRC-ERAM defaults: not another sub-service's, not its alias file.</summary>
	[Theory]
	[InlineData("Airways_High_Lines")]
	[InlineData("Airports.txt")]
	public void a_file_that_is_not_an_airports_geojson_file_is_rejected_for_crc_defaults(string key)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["CrcDefaultsFor"] = key;

		ArgumentException ex = Assert.Throws<ArgumentException>(() => AirportSettingsParser.Parse(settings));
		Assert.Contains(key, ex.Message);
	}

	[Fact]
	public void crc_defaults_are_only_required_for_the_files_actually_being_emitted()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		ChooseEveryFileForCrcDefaults(settings);
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
		ChooseEveryFileForCrcDefaults(settings);
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
		ChooseEveryFileForCrcDefaults(settings);
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
	public void only_the_files_chosen_for_crc_defaults_have_their_defaults_read()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["CrcDefaultsFor"] = "Airports_Symbols";
		AddCrcDefaults(settings, "Airports", "Symbol");
		// No Crc.Airports.Text.* or Crc.Runways.Line.* keys: those files are written without defaults.

		AirportSettingsParseResult result = AirportSettingsParser.Parse(settings);

		Assert.Equal("vor", Assert.Single(result.Settings.SymbolDefaults).Value.Style);
		Assert.Empty(result.Settings.LineDefaults);
		Assert.Empty(result.Settings.TextDefaults);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void crc_defaults_for_a_file_that_is_not_emitted_are_not_required()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["CrcDefaultsFor"] = "Runways_Lines";
		settings["EmitRunwayLines"] = "N";
		// No Crc.Runways.Line.* keys: runway lines are not written, so their defaults are not needed.

		AirportSettings parsed = AirportSettingsParser.Parse(settings).Settings;

		Assert.Empty(parsed.LineDefaults);
	}

	[Fact]
	public void crc_defaults_are_not_required_when_geojson_is_off()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["GenerateGeojson"] = "N";
		ChooseEveryFileForCrcDefaults(settings);
		// No Crc.* keys at all: no GeoJSON is written, so no defaults are needed.

		AirportSettings parsed = AirportSettingsParser.Parse(settings).Settings;

		Assert.Empty(parsed.LineDefaults);
		Assert.Empty(parsed.SymbolDefaults);
		Assert.Empty(parsed.TextDefaults);
	}
}
