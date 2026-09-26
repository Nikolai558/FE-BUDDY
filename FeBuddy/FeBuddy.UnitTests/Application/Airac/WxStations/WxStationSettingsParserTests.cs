using FeBuddy.Core.Application.Airac.WxStations;
using FeBuddy.Core.Application.Airac.WxStations.Models;

namespace FeBuddy.UnitTests.Application.Airac.WxStations;

/// <summary>
/// Covers <see cref="WxStationSettingsParser"/>: the documented defaults, the "both Emit flags
/// off" guard, the <c>IncludeFebCustomProperties</c> warning (Wx Stations has no <c>feb.*</c>
/// properties), unknown-key warnings for settings only other sub-services have, where CRC-ERAM
/// defaults are read from, and reuse of the shared ROI/precision/vNAS readers.
/// </summary>
public sealed class WxStationSettingsParserTests
{
	private static Dictionary<string, string> MinimalValidSettings() => new()
	{
		{ "OutputDirectory", @"C:\Output" }
	};

	private static void AddSymbolDefaults(Dictionary<string, string> settings, string prefix, bool withStyle = true)
	{
		settings[$"{prefix}.bcg"] = "3";
		settings[$"{prefix}.filters"] = "3";
		settings[$"{prefix}.size"] = "1";

		if (withStyle)
		{
			settings[$"{prefix}.style"] = "otherWaypoints";
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

	private static void MarkForCrcDefaults(Dictionary<string, string> settings, params string[] keys)
	{
		settings["UploadToVnas"] = string.Join(',', keys);
		settings["CrcDefaultsFor"] = string.Join(',', keys);
	}

	// ---- defaults ----

	[Fact]
	public void missing_output_directory_throws() =>
		Assert.Throws<ArgumentException>(() => WxStationSettingsParser.Parse(new Dictionary<string, string>()));

	[Fact]
	public void a_fully_default_settings_block_produces_no_messages_and_the_documented_defaults()
	{
		WxStationSettingsParseResult result = WxStationSettingsParser.Parse(MinimalValidSettings());

		Assert.Empty(result.Messages);
		Assert.True(result.Settings.EmitSymbols);
		Assert.True(result.Settings.EmitText);
		Assert.Empty(result.Settings.Vnas.UploadFiles);
		Assert.Empty(result.Settings.SymbolDefaults);
		Assert.Empty(result.Settings.TextDefaults);
		Assert.Null(result.Settings.Roi);
		Assert.Equal(6, result.Settings.CoordinatePrecision);
	}

	// ---- EmitSymbols / EmitText ----

	[Fact]
	public void turning_off_both_emit_flags_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["EmitSymbols"] = "N";
		settings["EmitText"] = "N";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => WxStationSettingsParser.Parse(settings));
		Assert.Contains("EmitSymbols", ex.Message);
		Assert.Contains("EmitText", ex.Message);
	}

	[Theory]
	[InlineData("N", "Y")]
	[InlineData("Y", "N")]
	public void turning_off_only_one_emit_flag_is_fine(string emitSymbols, string emitText)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["EmitSymbols"] = emitSymbols;
		settings["EmitText"] = emitText;

		WxStationSettings parsed = WxStationSettingsParser.Parse(settings).Settings;

		Assert.Equal(emitSymbols == "Y", parsed.EmitSymbols);
		Assert.Equal(emitText == "Y", parsed.EmitText);
	}

	// ---- IncludeFebCustomProperties ----

	[Fact]
	public void include_feb_custom_properties_warns_that_wx_stations_has_none()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeFebCustomProperties"] = "Y";

		WxStationSettingsParseResult result = WxStationSettingsParser.Parse(settings);

		Assert.Contains(
			result.Messages.WarningTexts(),
			w => w.Contains("IncludeFebCustomProperties") && w.Contains("no FE-Buddy properties"));
	}

	// ---- settings only other sub-services have ----

	[Theory]
	[InlineData("GenerateAliasFile")]
	[InlineData("OutputBy")]
	public void settings_only_other_sub_services_have_are_unknown_key_warnings(string key)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings[key] = "N";

		WxStationSettingsParseResult result = WxStationSettingsParser.Parse(settings);

		Assert.Contains(result.Messages.WarningTexts(), w => w.Contains(key));
	}

	[Fact]
	public void an_unrecognized_key_produces_a_warning_and_does_not_throw()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["TotallyMadeUpKey"] = "x";

		WxStationSettingsParseResult result = WxStationSettingsParser.Parse(settings);

		Assert.Contains(result.Messages.WarningTexts(), w => w.Contains("TotallyMadeUpKey"));
	}

	[Fact]
	public void upload_to_vnas_naming_an_alias_looking_key_throws_because_there_is_no_alias_file()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["UploadToVnas"] = "WxStations.txt";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => WxStationSettingsParser.Parse(settings));
		Assert.Contains("WxStations.txt", ex.Message);
	}

	[Fact]
	public void crc_defaults_for_entry_not_in_upload_to_vnas_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["CrcDefaultsFor"] = WxStationOutputFiles.Symbols;

		ArgumentException ex = Assert.Throws<ArgumentException>(() => WxStationSettingsParser.Parse(settings));
		Assert.Contains(WxStationOutputFiles.Symbols, ex.Message);
	}

	// ---- CRC defaults ----

	[Fact]
	public void crc_defaults_are_read_for_symbols_and_text_when_chosen_for_crc_defaults()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		MarkForCrcDefaults(settings, WxStationOutputFiles.Symbols, WxStationOutputFiles.Text);
		AddSymbolDefaults(settings, $"Crc.{WxStationOutputFiles.AllClass}.Symbol");
		AddTextDefaults(settings, $"Crc.{WxStationOutputFiles.AllClass}.Text");

		WxStationSettingsParseResult result = WxStationSettingsParser.Parse(settings);

		Assert.Equal("otherWaypoints", result.Settings.SymbolDefaults[WxStationOutputFiles.AllClass].Style);
		Assert.Equal(3, result.Settings.TextDefaults[WxStationOutputFiles.AllClass].Filters[0]);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void symbol_defaults_require_a_style_value()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		MarkForCrcDefaults(settings, WxStationOutputFiles.Symbols);
		AddSymbolDefaults(settings, $"Crc.{WxStationOutputFiles.AllClass}.Symbol", withStyle: false);

		ArgumentException ex = Assert.Throws<ArgumentException>(() => WxStationSettingsParser.Parse(settings));
		Assert.Contains($"Crc.{WxStationOutputFiles.AllClass}.Symbol.style", ex.Message);
	}

	[Fact]
	public void crc_defaults_are_only_read_for_the_files_chosen_in_crc_defaults_for()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["UploadToVnas"] = $"{WxStationOutputFiles.Symbols},{WxStationOutputFiles.Text}";
		settings["CrcDefaultsFor"] = WxStationOutputFiles.Symbols;
		AddSymbolDefaults(settings, $"Crc.{WxStationOutputFiles.AllClass}.Symbol");
		// No Crc.Wx.Text.* keys supplied - if the parser tried to read them, this would throw.

		WxStationSettings parsed = WxStationSettingsParser.Parse(settings).Settings;

		Assert.Single(parsed.SymbolDefaults);
		Assert.Empty(parsed.TextDefaults);
	}

	[Fact]
	public void crc_symbol_defaults_are_skipped_when_emit_symbols_is_off()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["EmitSymbols"] = "N";
		MarkForCrcDefaults(settings, WxStationOutputFiles.Symbols);
		// No Crc.Wx.Symbol.* keys supplied - if the parser tried to read them, this would throw.

		WxStationSettings parsed = WxStationSettingsParser.Parse(settings).Settings;

		Assert.Empty(parsed.SymbolDefaults);
	}

	[Fact]
	public void crc_text_defaults_are_skipped_when_emit_text_is_off()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["EmitText"] = "N";
		MarkForCrcDefaults(settings, WxStationOutputFiles.Text);
		// No Crc.Wx.Text.* keys supplied - if the parser tried to read them, this would throw.

		WxStationSettings parsed = WxStationSettingsParser.Parse(settings).Settings;

		Assert.Empty(parsed.TextDefaults);
	}

	[Fact]
	public void a_missing_crc_value_throws_naming_the_key()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		MarkForCrcDefaults(settings, WxStationOutputFiles.Symbols);
		settings[$"Crc.{WxStationOutputFiles.AllClass}.Symbol.bcg"] = "3";
		settings[$"Crc.{WxStationOutputFiles.AllClass}.Symbol.filters"] = "3";
		settings[$"Crc.{WxStationOutputFiles.AllClass}.Symbol.style"] = "otherWaypoints";
		// Crc.Wx.Symbol.size is deliberately missing.

		ArgumentException ex = Assert.Throws<ArgumentException>(() => WxStationSettingsParser.Parse(settings));
		Assert.Contains($"Crc.{WxStationOutputFiles.AllClass}.Symbol.size", ex.Message);
	}

	// ---- a per-station text default is meaningless: a station's label is always its own fields ----

	[Fact]
	public void a_per_station_text_default_is_ignored_with_an_explanatory_warning()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings[$"Crc.{WxStationOutputFiles.AllClass}.Text.text"] = "SHOULD_BE_IGNORED";

		WxStationSettingsParseResult result = WxStationSettingsParser.Parse(settings);

		Assert.Contains(result.Messages.WarningTexts(), w => w.Contains($"Crc.{WxStationOutputFiles.AllClass}.Text.text"));
	}

	// ---- ROI / precision / vNAS reuse ----

	[Fact]
	public void roi_is_parsed_when_filter_by_roi_is_set()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["FilterByRoi"] = "Y";
		settings["RoiSwLat"] = "40.0";
		settings["RoiSwLon"] = "-89.0";
		settings["RoiNeLat"] = "43.0";
		settings["RoiNeLon"] = "-86.0";

		WxStationSettings parsed = WxStationSettingsParser.Parse(settings).Settings;

		Assert.NotNull(parsed.Roi);
		Assert.Equal(40.0, parsed.Roi!.SwLat);
	}

	[Fact]
	public void coordinate_precision_is_read_via_the_shared_reader()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["CoordinatePrecision"] = "3";

		WxStationSettings parsed = WxStationSettingsParser.Parse(settings).Settings;

		Assert.Equal(3, parsed.CoordinatePrecision);
	}

	[Fact]
	public void vnas_files_are_read_via_the_shared_reader()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["UploadToVnas"] = WxStationOutputFiles.Symbols;

		WxStationSettings parsed = WxStationSettingsParser.Parse(settings).Settings;

		Assert.True(parsed.Vnas.IsUploaded(WxStationOutputFiles.Symbols));
		Assert.False(parsed.Vnas.IsUploaded(WxStationOutputFiles.Text));
	}

	[Fact]
	public void parse_rejects_a_null_argument() =>
		Assert.Throws<ArgumentNullException>(() => WxStationSettingsParser.Parse(null!));
}
