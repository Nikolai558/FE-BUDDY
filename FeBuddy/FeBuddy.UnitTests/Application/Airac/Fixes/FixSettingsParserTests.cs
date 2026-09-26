using FeBuddy.Core.Application.Airac.Fixes;
using FeBuddy.Core.Application.Airac.Fixes.Models;
using FeBuddy.Core.Domain.Fixes;

namespace FeBuddy.UnitTests.Application.Airac.Fixes;

/// <summary>
/// Covers <see cref="FixSettingsParser"/>: the documented defaults, the "both Emit flags off"
/// guard, <c>OutputBy</c> parsing, <c>ExcludedFixUses</c>/<c>ExcludedCharts</c> tokenizing,
/// <c>Combinations</c> parsing and validation, that this sub-service has no
/// <c>GenerateAliasFile</c>/<c>GenerateGeojson</c>, and where each file layout reads its CRC-ERAM
/// defaults from.
/// </summary>
public sealed class FixSettingsParserTests
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

	// ---- defaults ----

	[Fact]
	public void missing_output_directory_throws() =>
		Assert.Throws<ArgumentException>(() => FixSettingsParser.Parse(new Dictionary<string, string>()));

	[Fact]
	public void a_fully_default_settings_block_produces_no_messages_and_the_documented_defaults()
	{
		FixSettingsParseResult result = FixSettingsParser.Parse(MinimalValidSettings());

		Assert.Empty(result.Messages);
		Assert.True(result.Settings.EmitSymbols);
		Assert.True(result.Settings.EmitText);
		Assert.Equal(FixOutputBy.All, result.Settings.OutputBy);
		Assert.Empty(result.Settings.ExcludedFixUses);
		Assert.Empty(result.Settings.ExcludedCharts);
		Assert.Empty(result.Settings.Combinations);
		Assert.False(result.Settings.IncludeFebCustomProperties);
		Assert.Empty(result.Settings.FebProperties);
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

		ArgumentException ex = Assert.Throws<ArgumentException>(() => FixSettingsParser.Parse(settings));
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

		FixSettings parsed = FixSettingsParser.Parse(settings).Settings;

		Assert.Equal(emitSymbols == "Y", parsed.EmitSymbols);
		Assert.Equal(emitText == "Y", parsed.EmitText);
	}

	// ---- OutputBy ----

	[Theory]
	[InlineData("All", FixOutputBy.All)]
	[InlineData("all", FixOutputBy.All)]
	[InlineData("FixUse", FixOutputBy.FixUse)]
	[InlineData("fixuse", FixOutputBy.FixUse)]
	[InlineData("Chart", FixOutputBy.Chart)]
	[InlineData("chart", FixOutputBy.Chart)]
	public void output_by_parses_case_insensitively(string value, FixOutputBy expected)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = value;

		Assert.Equal(expected, FixSettingsParser.Parse(settings).Settings.OutputBy);
	}

	[Fact]
	public void an_invalid_output_by_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "Sideways";

		Assert.Throws<ArgumentException>(() => FixSettingsParser.Parse(settings));
	}

	[Fact]
	public void chart_and_fix_use_with_no_combinations_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "ChartAndFixUse";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => FixSettingsParser.Parse(settings));
		Assert.Contains("ChartAndFixUse", ex.Message);
		Assert.Contains("Combinations", ex.Message);
	}

	[Fact]
	public void chart_and_fix_use_with_at_least_one_combination_parses()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "ChartAndFixUse";
		settings["Combinations"] = "ENROUTE-LOW+WYPNT";

		FixSettings parsed = FixSettingsParser.Parse(settings).Settings;

		Assert.Equal(FixOutputBy.ChartAndFixUse, parsed.OutputBy);
		Assert.Single(parsed.Combinations);
	}

	// ---- ExcludedFixUses ----

	[Fact]
	public void an_unknown_excluded_fix_use_warns_listing_known_uses_and_is_still_excluded()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["ExcludedFixUses"] = "WYPNT,BOGUS";

		FixSettingsParseResult result = FixSettingsParser.Parse(settings);

		string warning = Assert.Single(result.Messages.WarningTexts());
		Assert.Contains("BOGUS", warning);
		Assert.Contains("WYPNT", warning); // one of FixUses.All, listed as a known use
		Assert.Contains("BOGUS", result.Settings.ExcludedFixUses);
	}

	[Fact]
	public void excluded_fix_uses_are_tokenized_and_matched_ignoring_case()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["ExcludedFixUses"] = " wypnt , computer-nav ";

		FixSettings parsed = FixSettingsParser.Parse(settings).Settings;

		Assert.Equal(["COMPUTER-NAV", "WYPNT"], parsed.ExcludedFixUses.OrderBy(x => x, StringComparer.Ordinal));
	}

	[Fact]
	public void excluding_every_known_fix_use_in_fix_use_layout_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "FixUse";
		settings["ExcludedFixUses"] = string.Join(',', FixUses.All);

		ArgumentException ex = Assert.Throws<ArgumentException>(() => FixSettingsParser.Parse(settings));
		Assert.Contains("ExcludedFixUses", ex.Message);
	}

	[Fact]
	public void excluding_every_known_fix_use_outside_fix_use_layout_does_not_throw()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["ExcludedFixUses"] = string.Join(',', FixUses.All);

		FixSettings parsed = FixSettingsParser.Parse(settings).Settings;

		Assert.Equal(FixUses.All.Count, parsed.ExcludedFixUses.Count);
	}

	// ---- ExcludedCharts ----

	[Theory]
	[InlineData("ENROUTE LOW")]
	[InlineData("ENROUTE-LOW")]
	[InlineData("enroute low")]
	public void excluded_charts_tokenize_so_spaced_and_hyphenated_spellings_are_equal(string value)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["ExcludedCharts"] = value;

		FixSettings parsed = FixSettingsParser.Parse(settings).Settings;

		Assert.Equal(["ENROUTE-LOW"], parsed.ExcludedCharts);
	}

	// ---- Combinations ----

	[Fact]
	public void combinations_tokenize_both_sides()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["Combinations"] = "enroute low+wypnt";

		FixCombination combination = Assert.Single(FixSettingsParser.Parse(settings).Settings.Combinations);

		Assert.Equal("ENROUTE-LOW", combination.Chart);
		Assert.Equal("WYPNT", combination.FixUse);
	}

	[Fact]
	public void duplicate_combinations_are_deduped_keeping_the_first_occurrences_order()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["Combinations"] = "enroute low+wypnt,ENROUTE-LOW+WYPNT,VFR+COMPUTER-NAV";

		IReadOnlyList<FixCombination> combinations = FixSettingsParser.Parse(settings).Settings.Combinations;

		Assert.Equal(
			[("ENROUTE-LOW", "WYPNT"), ("VFR", "COMPUTER-NAV")],
			combinations.Select(c => (c.Chart, c.FixUse)));
	}

	[Fact]
	public void an_unknown_fix_use_in_a_combination_warns_listing_known_uses_but_is_still_used()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["Combinations"] = "ENROUTE-LOW+BOGUS";

		FixSettingsParseResult result = FixSettingsParser.Parse(settings);

		string warning = Assert.Single(result.Messages.WarningTexts());
		Assert.Contains("BOGUS", warning);
		Assert.Contains("WYPNT", warning);

		FixCombination combination = Assert.Single(result.Settings.Combinations);
		Assert.Equal("BOGUS", combination.FixUse);
	}

	[Theory]
	[InlineData("ENROUTELOWWYPNT")] // no '+'
	[InlineData("ENROUTE-LOW+WYPNT+EXTRA")] // two '+'
	[InlineData("+WYPNT")] // empty chart side
	[InlineData("ENROUTE-LOW+")] // empty fix use side
	[InlineData("***+WYPNT")] // chart side is only punctuation
	[InlineData("WYPNT+***")] // fix use side is only punctuation
	public void a_malformed_combination_entry_throws(string entry)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["Combinations"] = entry;

		ArgumentException ex = Assert.Throws<ArgumentException>(() => FixSettingsParser.Parse(settings));
		Assert.Contains(entry, ex.Message);
	}

	// ---- FebProperties (shared reader) ----

	[Fact]
	public void feb_properties_parse_via_the_shared_reader()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeFebCustomProperties"] = "Y";
		settings["FebProperties"] = "fixId,fixUseCode,charts";

		FixSettings parsed = FixSettingsParser.Parse(settings).Settings;

		Assert.True(parsed.IncludeFebCustomProperties);
		Assert.Equal([FixFebProperty.FixId, FixFebProperty.FixUseCode, FixFebProperty.Charts], parsed.FebProperties);
	}

	[Fact]
	public void an_empty_feb_properties_list_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeFebCustomProperties"] = "Y";

		Assert.Throws<ArgumentException>(() => FixSettingsParser.Parse(settings));
	}

	[Fact]
	public void an_unknown_feb_property_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeFebCustomProperties"] = "Y";
		settings["FebProperties"] = "bogus";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => FixSettingsParser.Parse(settings));
		Assert.Contains("bogus", ex.Message);
	}

	// ---- no GenerateAliasFile, no GenerateGeojson ----

	[Theory]
	[InlineData("GenerateAliasFile")]
	[InlineData("GenerateGeojson")]
	public void settings_only_other_sub_services_have_are_unknown_key_warnings(string key)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings[key] = "N";

		FixSettingsParseResult result = FixSettingsParser.Parse(settings);

		Assert.Contains(result.Messages.WarningTexts(), w => w.Contains(key));
	}

	[Fact]
	public void upload_to_vnas_naming_an_alias_looking_key_throws_because_there_is_no_alias_file()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["UploadToVnas"] = "Fix.txt";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => FixSettingsParser.Parse(settings));
		Assert.Contains("Fix.txt", ex.Message);
	}

	[Fact]
	public void crc_defaults_for_entry_not_in_upload_to_vnas_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["CrcDefaultsFor"] = FixOutputFiles.Symbols;

		ArgumentException ex = Assert.Throws<ArgumentException>(() => FixSettingsParser.Parse(settings));
		Assert.Contains(FixOutputFiles.Symbols, ex.Message);
	}

	// ---- CRC defaults: All layout ----

	private static void MarkForCrcDefaults(Dictionary<string, string> settings, params string[] keys)
	{
		settings["UploadToVnas"] = string.Join(',', keys);
		settings["CrcDefaultsFor"] = string.Join(',', keys);
	}

	[Fact]
	public void all_layout_reads_symbol_and_text_defaults_when_chosen_for_crc_defaults()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		MarkForCrcDefaults(settings, FixOutputFiles.Symbols, FixOutputFiles.Text);
		AddSymbolDefaults(settings, $"Crc.{FixOutputFiles.AllClass}.Symbol");
		AddTextDefaults(settings, $"Crc.{FixOutputFiles.AllClass}.Text");

		FixSettingsParseResult result = FixSettingsParser.Parse(settings);

		Assert.Equal("otherWaypoints", result.Settings.SymbolDefaults[FixOutputFiles.AllClass].Style);
		Assert.Equal(3, result.Settings.TextDefaults[FixOutputFiles.AllClass].Filters[0]);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void all_layout_symbol_defaults_require_a_style_value()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		MarkForCrcDefaults(settings, FixOutputFiles.Symbols);
		AddSymbolDefaults(settings, $"Crc.{FixOutputFiles.AllClass}.Symbol", withStyle: false);

		ArgumentException ex = Assert.Throws<ArgumentException>(() => FixSettingsParser.Parse(settings));
		Assert.Contains($"Crc.{FixOutputFiles.AllClass}.Symbol.style", ex.Message);
	}

	[Fact]
	public void crc_defaults_are_only_read_for_the_files_chosen_in_crc_defaults_for()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["UploadToVnas"] = $"{FixOutputFiles.Symbols},{FixOutputFiles.Text}";
		settings["CrcDefaultsFor"] = FixOutputFiles.Symbols;
		AddSymbolDefaults(settings, $"Crc.{FixOutputFiles.AllClass}.Symbol");
		// No Crc.Fix.Text.* keys supplied - if the parser tried to read them, this would throw.

		FixSettings parsed = FixSettingsParser.Parse(settings).Settings;

		Assert.Single(parsed.SymbolDefaults);
		Assert.Empty(parsed.TextDefaults);
	}

	[Fact]
	public void crc_symbol_defaults_are_skipped_when_emit_symbols_is_off()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["EmitSymbols"] = "N";
		MarkForCrcDefaults(settings, FixOutputFiles.Symbols);
		// No Crc.Fix.Symbol.* keys supplied - if the parser tried to read them, this would throw.

		FixSettings parsed = FixSettingsParser.Parse(settings).Settings;

		Assert.Empty(parsed.SymbolDefaults);
	}

	[Fact]
	public void crc_text_defaults_are_skipped_when_emit_text_is_off()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["EmitText"] = "N";
		MarkForCrcDefaults(settings, FixOutputFiles.Text);
		// No Crc.Fix.Text.* keys supplied - if the parser tried to read them, this would throw.

		FixSettings parsed = FixSettingsParser.Parse(settings).Settings;

		Assert.Empty(parsed.TextDefaults);
	}

	// ---- CRC defaults: other layouts ----

	[Fact]
	public void fix_use_layout_reads_defaults_from_a_present_groups_key()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "FixUse";
		MarkForCrcDefaults(settings, "Fix_WYPNT_Symbols");
		AddSymbolDefaults(settings, "Crc.WYPNT.Symbol");

		FixSettingsParseResult result = FixSettingsParser.Parse(settings);

		Assert.Equal("otherWaypoints", result.Settings.SymbolDefaults["WYPNT"].Style);
		Assert.Empty(result.Messages.WarningTexts());
	}

	[Fact]
	public void chart_layout_reads_a_hyphenated_class_without_an_unknown_key_warning()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "Chart";
		MarkForCrcDefaults(settings, "Fix_ENROUTE-LOW_Text");
		AddTextDefaults(settings, "Crc.ENROUTE-LOW.Text");

		FixSettingsParseResult result = FixSettingsParser.Parse(settings);

		Assert.Equal(3, result.Settings.TextDefaults["ENROUTE-LOW"].Filters[0]);
		Assert.Empty(result.Messages.WarningTexts());
	}

	[Fact]
	public void chart_layout_reads_the_no_chart_groups_defaults_without_an_unknown_key_warning()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "Chart";
		MarkForCrcDefaults(settings, $"Fix_{FixCharts.NoChart}_Symbols");
		AddSymbolDefaults(settings, $"Crc.{FixCharts.NoChart}.Symbol");

		FixSettingsParseResult result = FixSettingsParser.Parse(settings);

		Assert.Contains(FixCharts.NoChart, result.Settings.SymbolDefaults.Keys);
		Assert.Empty(result.Messages.WarningTexts());
	}

	[Fact]
	public void chart_and_fix_use_layout_reads_a_combination_groups_defaults_without_an_unknown_key_warning()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "ChartAndFixUse";
		settings["Combinations"] = "ENROUTE-LOW+WYPNT";
		MarkForCrcDefaults(settings, "Fix_ENROUTE-LOW-WYPNT_Symbols");
		AddSymbolDefaults(settings, "Crc.ENROUTE-LOW-WYPNT.Symbol");

		FixSettingsParseResult result = FixSettingsParser.Parse(settings);

		Assert.Equal("otherWaypoints", result.Settings.SymbolDefaults["ENROUTE-LOW-WYPNT"].Style);
		Assert.Empty(result.Messages.WarningTexts());
	}

	[Fact]
	public void an_excluded_fix_uses_defaults_are_skipped_in_fix_use_layout()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "FixUse";
		settings["ExcludedFixUses"] = "WYPNT";
		MarkForCrcDefaults(settings, "Fix_WYPNT_Symbols");
		// No Crc.WYPNT.Symbol.* keys supplied - if the parser tried to read them, this would throw.

		FixSettings parsed = FixSettingsParser.Parse(settings).Settings;

		Assert.Empty(parsed.SymbolDefaults);
	}

	[Fact]
	public void an_excluded_charts_defaults_are_skipped_in_chart_layout()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "Chart";
		settings["ExcludedCharts"] = "ENROUTE-LOW";
		MarkForCrcDefaults(settings, "Fix_ENROUTE-LOW_Symbols");
		// No Crc.ENROUTE-LOW.Symbol.* keys supplied - if the parser tried to read them, this would throw.

		FixSettings parsed = FixSettingsParser.Parse(settings).Settings;

		Assert.Empty(parsed.SymbolDefaults);
	}

	[Fact]
	public void an_all_mode_key_named_in_crc_defaults_for_outside_all_layout_is_silently_skipped()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "FixUse";
		MarkForCrcDefaults(settings, FixOutputFiles.Symbols);
		// Fix_Symbols is not a per-group key in FixUse layout - if the parser tried to read
		// Crc.Fix.Symbol.* as though it were a group, this would throw.

		FixSettingsParseResult result = FixSettingsParser.Parse(settings);

		Assert.Empty(result.Settings.SymbolDefaults);
		Assert.Empty(result.Messages.WarningTexts());
	}

	[Fact]
	public void a_missing_crc_value_throws_naming_the_key()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "FixUse";
		MarkForCrcDefaults(settings, "Fix_WYPNT_Symbols");
		settings["Crc.WYPNT.Symbol.bcg"] = "3";
		settings["Crc.WYPNT.Symbol.filters"] = "3";
		settings["Crc.WYPNT.Symbol.style"] = "otherWaypoints";
		// Crc.WYPNT.Symbol.size is deliberately missing.

		ArgumentException ex = Assert.Throws<ArgumentException>(() => FixSettingsParser.Parse(settings));
		Assert.Contains("Crc.WYPNT.Symbol.size", ex.Message);
	}

	// ---- a per-fix text default is meaningless: a fix's label is always its own identifier ----

	[Fact]
	public void a_per_fix_text_default_is_ignored_with_an_explanatory_warning()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings[$"Crc.{FixOutputFiles.AllClass}.Text.text"] = "SHOULD_BE_IGNORED";

		FixSettingsParseResult result = FixSettingsParser.Parse(settings);

		Assert.Contains(result.Messages.WarningTexts(), w => w.Contains($"Crc.{FixOutputFiles.AllClass}.Text.text"));
	}

	[Fact]
	public void an_unrecognized_key_produces_a_warning_and_does_not_throw()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["TotallyMadeUpKey"] = "x";

		FixSettingsParseResult result = FixSettingsParser.Parse(settings);

		Assert.Contains(result.Messages.WarningTexts(), w => w.Contains("TotallyMadeUpKey"));
	}

	// ---- ROI reuse ----

	[Fact]
	public void roi_is_parsed_when_filter_by_roi_is_set()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["FilterByRoi"] = "Y";
		settings["RoiSwLat"] = "40.0";
		settings["RoiSwLon"] = "-89.0";
		settings["RoiNeLat"] = "43.0";
		settings["RoiNeLon"] = "-86.0";

		FixSettings parsed = FixSettingsParser.Parse(settings).Settings;

		Assert.NotNull(parsed.Roi);
		Assert.Equal(40.0, parsed.Roi!.SwLat);
	}

	[Fact]
	public void parse_rejects_a_null_argument() =>
		Assert.Throws<ArgumentNullException>(() => FixSettingsParser.Parse(null!));
}
