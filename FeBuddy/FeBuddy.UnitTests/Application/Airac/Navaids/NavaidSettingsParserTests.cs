using FeBuddy.Core.Application.Airac.Navaids;
using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Domain.Navaids;

namespace FeBuddy.UnitTests.Application.Airac.Navaids;

/// <summary>
/// Covers <see cref="NavaidSettingsParser"/>: the combinations of output choices that would
/// produce nothing at all are rejected up front, <c>ExcludedTypes</c> is validated against the
/// known types, <c>SymbolStyleBy</c> and <c>FanMarkerStyle</c> are required only when a merged
/// All-mode Symbols file will actually need a per-Feature style, and Type mode reads CRC defaults
/// from whichever type keys were chosen for <c>CrcDefaultsFor</c> - including a type FE-Buddy does
/// not itself recognize.
/// </summary>
public sealed class NavaidSettingsParserTests
{
	private static Dictionary<string, string> MinimalValidSettings() => new()
	{
		{ "OutputDirectory", @"C:\Output" }
	};

	private static void AddSymbolDefaults(Dictionary<string, string> settings, string prefix, bool withStyle)
	{
		settings[$"{prefix}.bcg"] = "3";
		settings[$"{prefix}.filters"] = "3";
		settings[$"{prefix}.size"] = "1";

		if (withStyle)
		{
			settings[$"{prefix}.style"] = "vor";
		}
	}

	private static void AddTextDefaults(Dictionary<string, string> settings, string prefix)
	{
		settings[$"{prefix}.bcg"] = "3";
		settings[$"{prefix}.filters"] = "3";
		settings[$"{prefix}.size"] = "1";
		settings[$"{prefix}.underline"] = "N";
		settings[$"{prefix}.opaque"] = "N";
		settings[$"{prefix}.xOffset"] = "0";
		settings[$"{prefix}.yOffset"] = "0";
	}

	[Fact]
	public void missing_output_directory_throws()
	{
		Assert.Throws<ArgumentException>(() => NavaidSettingsParser.Parse(new Dictionary<string, string>()));
	}

	[Fact]
	public void turning_off_both_geojson_and_the_alias_file_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["GenerateGeojson"] = "N";
		settings["GenerateAliasFile"] = "N";

		Assert.Throws<ArgumentException>(() => NavaidSettingsParser.Parse(settings));
	}

	[Fact]
	public void generating_geojson_with_both_emit_flags_off_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["EmitSymbols"] = "N";
		settings["EmitText"] = "N";

		Assert.Throws<ArgumentException>(() => NavaidSettingsParser.Parse(settings));

		// Harmless once GeoJSON itself is off - the alias file is the output.
		settings["GenerateGeojson"] = "N";
		NavaidSettings parsed = NavaidSettingsParser.Parse(settings).Settings;

		Assert.False(parsed.GenerateGeojson);
		Assert.True(parsed.GenerateAliasFile);
	}

	[Fact]
	public void there_is_no_emit_lines_setting_so_it_is_only_an_unknown_key_warning()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["EmitLines"] = "N";

		NavaidSettingsParseResult result = NavaidSettingsParser.Parse(settings);

		Assert.Contains(result.Messages.WarningTexts(), w => w.Contains("EmitLines"));
		Assert.True(result.Settings.EmitSymbols);
		Assert.True(result.Settings.EmitText);
	}

	[Fact]
	public void a_fully_default_settings_block_produces_no_messages()
	{
		NavaidSettingsParseResult result = NavaidSettingsParser.Parse(MinimalValidSettings());

		Assert.Empty(result.Messages);
		Assert.True(result.Settings.GenerateGeojson);
		Assert.True(result.Settings.GenerateAliasFile);
		Assert.True(result.Settings.EmitSymbols);
		Assert.True(result.Settings.EmitText);
		Assert.Equal(NavaidOutputBy.All, result.Settings.OutputBy);
		Assert.Equal(NavaidSymbolStyleBy.Type, result.Settings.SymbolStyleBy);
		Assert.Null(result.Settings.FanMarkerStyle);
		Assert.Empty(result.Settings.ExcludedTypes);
		Assert.Empty(result.Settings.SymbolDefaults);
		Assert.Empty(result.Settings.TextDefaults);
		Assert.Null(result.Settings.Roi);
		Assert.Equal(6, result.Settings.CoordinatePrecision);
	}

	[Theory]
	[InlineData("All", NavaidOutputBy.All)]
	[InlineData("all", NavaidOutputBy.All)]
	[InlineData("Type", NavaidOutputBy.Type)]
	public void output_by_parses_case_insensitively(string value, NavaidOutputBy expected)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = value;

		Assert.Equal(expected, NavaidSettingsParser.Parse(settings).Settings.OutputBy);
	}

	[Fact]
	public void an_invalid_output_by_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "Sideways";

		Assert.Throws<ArgumentException>(() => NavaidSettingsParser.Parse(settings));
	}

	[Fact]
	public void an_unknown_excluded_type_warns_listing_the_known_types_and_is_still_excluded()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["ExcludedTypes"] = "VOR,BOGUS";

		NavaidSettingsParseResult result = NavaidSettingsParser.Parse(settings);

		string warning = Assert.Single(result.Messages.WarningTexts());
		Assert.Contains("BOGUS", warning);
		Assert.Contains("VORTAC", warning); // one of NavaidTypes.All, listed as a known type
		Assert.Contains("BOGUS", result.Settings.ExcludedTypes);
	}

	[Fact]
	public void excluding_every_known_type_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["ExcludedTypes"] = string.Join(',', NavaidTypes.All);

		Assert.Throws<ArgumentException>(() => NavaidSettingsParser.Parse(settings));
	}

	[Fact]
	public void excluded_types_parse_as_a_trimmed_upper_cased_set()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["ExcludedTypes"] = " vor , consolan";

		NavaidSettings parsed = NavaidSettingsParser.Parse(settings).Settings;

		Assert.Equal(["CONSOLAN", "VOR"], parsed.ExcludedTypes.OrderBy(x => x, StringComparer.Ordinal));
	}

	// ---- All mode: SymbolStyleBy and FanMarkerStyle ----

	private static void MarkSymbolsForCrcDefaults(Dictionary<string, string> settings)
	{
		settings["UploadToVnas"] = "NAVAIDs_Symbols";
		settings["CrcDefaultsFor"] = "NAVAIDs_Symbols";
	}

	[Fact]
	public void all_mode_symbol_style_by_type_reads_the_class_defaults_without_a_style_key()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		MarkSymbolsForCrcDefaults(settings);
		AddSymbolDefaults(settings, "Crc.NAVAIDs.Symbol", withStyle: false);
		settings["FanMarkerStyle"] = "vor";

		NavaidSettings parsed = NavaidSettingsParser.Parse(settings).Settings;

		Assert.Null(parsed.SymbolDefaults[NavaidOutputFiles.AllClass].Style);
		Assert.Equal("vor", parsed.FanMarkerStyle);
	}

	[Fact]
	public void all_mode_symbol_style_by_type_ignores_a_style_key_even_when_one_is_present()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		MarkSymbolsForCrcDefaults(settings);
		AddSymbolDefaults(settings, "Crc.NAVAIDs.Symbol", withStyle: true);
		settings["FanMarkerStyle"] = "vor";

		NavaidSettings parsed = NavaidSettingsParser.Parse(settings).Settings;

		// readStyle: false means the style key, even when present, is never read back.
		Assert.Null(parsed.SymbolDefaults[NavaidOutputFiles.AllClass].Style);
	}

	[Fact]
	public void fan_marker_style_is_optional_even_when_symbols_get_crc_defaults_and_style_is_by_type()
	{
		// The cycle may hold no fan markers, so the GUI does not always ask; a fan marker written
		// without one is the GeoJSON writer's warning to give, not a parse error.
		Dictionary<string, string> settings = MinimalValidSettings();
		MarkSymbolsForCrcDefaults(settings);
		AddSymbolDefaults(settings, "Crc.NAVAIDs.Symbol", withStyle: false);

		NavaidSettingsParseResult result = NavaidSettingsParser.Parse(settings);

		Assert.Null(result.Settings.FanMarkerStyle);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void an_invalid_fan_marker_style_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		MarkSymbolsForCrcDefaults(settings);
		AddSymbolDefaults(settings, "Crc.NAVAIDs.Symbol", withStyle: false);
		settings["FanMarkerStyle"] = "wavy";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => NavaidSettingsParser.Parse(settings));
		Assert.Contains("FanMarkerStyle", ex.Message);
		Assert.Contains("wavy", ex.Message);
	}

	[Fact]
	public void fan_marker_style_is_normalised_to_its_canonical_case()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		MarkSymbolsForCrcDefaults(settings);
		AddSymbolDefaults(settings, "Crc.NAVAIDs.Symbol", withStyle: false);
		settings["FanMarkerStyle"] = "OTHERWAYPOINTS";

		NavaidSettings parsed = NavaidSettingsParser.Parse(settings).Settings;

		Assert.Equal("otherWaypoints", parsed.FanMarkerStyle);
	}

	[Fact]
	public void fan_marker_style_is_not_required_when_fan_marker_is_excluded()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		MarkSymbolsForCrcDefaults(settings);
		AddSymbolDefaults(settings, "Crc.NAVAIDs.Symbol", withStyle: false);
		settings["ExcludedTypes"] = "FAN MARKER";

		NavaidSettings parsed = NavaidSettingsParser.Parse(settings).Settings;

		Assert.Null(parsed.FanMarkerStyle);
	}

	[Fact]
	public void fan_marker_style_is_not_required_when_symbol_style_is_by_file_and_the_style_key_is_required_instead()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		MarkSymbolsForCrcDefaults(settings);
		settings["SymbolStyleBy"] = "File";
		AddSymbolDefaults(settings, "Crc.NAVAIDs.Symbol", withStyle: false);

		// SymbolStyleBy=File requires the class's own style key.
		ArgumentException ex = Assert.Throws<ArgumentException>(() => NavaidSettingsParser.Parse(settings));
		Assert.Contains("Crc.NAVAIDs.Symbol.style", ex.Message);

		AddSymbolDefaults(settings, "Crc.NAVAIDs.Symbol", withStyle: true);
		NavaidSettings parsed = NavaidSettingsParser.Parse(settings).Settings;

		Assert.Null(parsed.FanMarkerStyle);
		Assert.Equal("vor", parsed.SymbolDefaults[NavaidOutputFiles.AllClass].Style);
	}

	[Fact]
	public void fan_marker_style_is_not_required_when_symbols_are_not_chosen_for_crc_defaults()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		// Symbols never marked for CrcDefaultsFor.

		NavaidSettings parsed = NavaidSettingsParser.Parse(settings).Settings;

		Assert.Null(parsed.FanMarkerStyle);
		Assert.Empty(parsed.SymbolDefaults);
	}

	[Fact]
	public void fan_marker_style_is_not_required_in_type_mode()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "Type";
		settings["UploadToVnas"] = "NAVAIDs_VORTACs_Symbols";
		settings["CrcDefaultsFor"] = "NAVAIDs_VORTACs_Symbols";
		AddSymbolDefaults(settings, "Crc.VORTAC.Symbol", withStyle: true);

		NavaidSettings parsed = NavaidSettingsParser.Parse(settings).Settings;

		Assert.Null(parsed.FanMarkerStyle);
	}

	// ---- Type mode ----

	[Fact]
	public void type_mode_reads_symbol_and_text_defaults_from_the_crc_defaults_for_keys()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "Type";
		settings["UploadToVnas"] = "NAVAIDs_VORs_Symbols,NAVAIDs_VORs_Text";
		settings["CrcDefaultsFor"] = "NAVAIDs_VORs_Symbols,NAVAIDs_VORs_Text";
		AddSymbolDefaults(settings, "Crc.VOR.Symbol", withStyle: true);
		AddTextDefaults(settings, "Crc.VOR.Text");

		NavaidSettingsParseResult result = NavaidSettingsParser.Parse(settings);

		Assert.Equal("vor", result.Settings.SymbolDefaults["VOR"].Style);
		Assert.Equal(3, result.Settings.TextDefaults["VOR"].Filters[0]);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void type_mode_reads_defaults_for_a_token_fe_buddy_does_not_itself_recognize_without_an_unknown_key_warning()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "Type";
		settings["UploadToVnas"] = "NAVAIDs_FOOs_Symbols";
		settings["CrcDefaultsFor"] = "NAVAIDs_FOOs_Symbols";
		AddSymbolDefaults(settings, "Crc.FOO.Symbol", withStyle: true);

		NavaidSettingsParseResult result = NavaidSettingsParser.Parse(settings);

		Assert.Equal("vor", result.Settings.SymbolDefaults["FOO"].Style);
		Assert.Empty(result.Messages.WarningTexts());
	}

	[Fact]
	public void type_mode_skips_reading_defaults_for_an_excluded_types_keys()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "Type";
		settings["ExcludedTypes"] = "VOR";
		settings["UploadToVnas"] = "NAVAIDs_VORs_Symbols";
		settings["CrcDefaultsFor"] = "NAVAIDs_VORs_Symbols";
		// No Crc.VOR.Symbol.* keys supplied - if the parser tried to read them, this would throw.

		NavaidSettings parsed = NavaidSettingsParser.Parse(settings).Settings;

		Assert.Empty(parsed.SymbolDefaults);
	}

	[Fact]
	public void type_mode_reads_a_hyphenated_token_class_without_a_spurious_unknown_key_warning()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "Type";
		settings["UploadToVnas"] = "NAVAIDs_VOR-DMEs_Symbols";
		settings["CrcDefaultsFor"] = "NAVAIDs_VOR-DMEs_Symbols";
		AddSymbolDefaults(settings, "Crc.VOR-DME.Symbol", withStyle: true);

		NavaidSettingsParseResult result = NavaidSettingsParser.Parse(settings);

		Assert.Equal("vor", result.Settings.SymbolDefaults["VOR-DME"].Style);
		Assert.Empty(result.Messages.WarningTexts());
	}

	[Fact]
	public void crc_defaults_are_not_required_when_geojson_is_off()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["GenerateGeojson"] = "N";
		settings["UploadToVnas"] = "NAVAIDs_Symbols,NAVAIDs_Text";
		settings["CrcDefaultsFor"] = "NAVAIDs_Symbols,NAVAIDs_Text";
		// No Crc.* keys at all.

		NavaidSettings parsed = NavaidSettingsParser.Parse(settings).Settings;

		Assert.Empty(parsed.SymbolDefaults);
		Assert.Empty(parsed.TextDefaults);
	}

	[Fact]
	public void an_unrecognized_key_produces_a_warning_and_does_not_throw()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["TotallyMadeUpKey"] = "x";

		NavaidSettingsParseResult result = NavaidSettingsParser.Parse(settings);

		Assert.Contains(result.Messages.WarningTexts(), w => w.Contains("TotallyMadeUpKey"));
	}

	[Fact]
	public void generate_alias_file_is_navaids_own_key_and_produces_no_unknown_key_warning()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["GenerateAliasFile"] = "Y";

		NavaidSettingsParseResult result = NavaidSettingsParser.Parse(settings);

		Assert.DoesNotContain(result.Messages.WarningTexts(), w => w.Contains("GenerateAliasFile"));
	}

	[Fact]
	public void a_per_navaid_text_default_is_ignored_with_an_explanatory_warning()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["Crc.NAVAIDs.Text.text"] = "SHOULD_BE_IGNORED";

		NavaidSettingsParseResult result = NavaidSettingsParser.Parse(settings);

		Assert.Contains(result.Messages.WarningTexts(), w => w.Contains("Crc.NAVAIDs.Text.text"));
	}
}
