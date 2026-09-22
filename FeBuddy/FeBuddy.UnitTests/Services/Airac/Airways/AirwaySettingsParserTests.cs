using FeBuddy.Core.Models.Services.Airac.Airways;
using FeBuddy.Core.Services.Airac.Airways;

namespace FeBuddy.UnitTests.Services.Airac.Airways;

public class AirwaySettingsParserTests
{
	private static Dictionary<string, string> MinimalValidSettings() => new()
	{
		{ "OutputDirectory", @"C:\Output" },
		{ "OutputBy", "HighLow" }
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
		Dictionary<string, string> settings = new() { { "OutputBy", "HighLow" } };

		Assert.Throws<ArgumentException>(() => AirwaySettingsParser.Parse(settings));
	}

	[Fact]
	public void missing_output_by_throws()
	{
		Dictionary<string, string> settings = new() { { "OutputDirectory", @"C:\Output" } };

		Assert.Throws<ArgumentException>(() => AirwaySettingsParser.Parse(settings));
	}

	[Theory]
	[InlineData("HighLow", AirwayGeojsonOutputBy.HighLow)]
	[InlineData("highlow", AirwayGeojsonOutputBy.HighLow)]
	[InlineData("Designation", AirwayGeojsonOutputBy.Designation)]
	[InlineData("None", AirwayGeojsonOutputBy.None)]
	public void output_by_parses_case_insensitively(string value, AirwayGeojsonOutputBy expected)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = value;

		AirwaySettingsParseResult result = AirwaySettingsParser.Parse(settings);

		Assert.Equal(expected, result.Settings.OutputBy);
	}

	[Fact]
	public void invalid_output_by_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "Sideways";

		Assert.Throws<ArgumentException>(() => AirwaySettingsParser.Parse(settings));
	}

	[Fact]
	public void missing_optional_yes_no_settings_fall_back_to_documented_defaults()
	{
		AirwaySettingsParseResult result = AirwaySettingsParser.Parse(MinimalValidSettings());

		Assert.False(result.Settings.BufferAirwayWaypoints);
		Assert.False(result.Settings.IncludeFebCustomProperties);
		Assert.False(result.Settings.IncludeAirwayWaypointIds);
		Assert.True(result.Settings.GenerateAliasFile);
		Assert.True(result.Settings.SplitAtAntimeridian);
		Assert.False(result.Settings.IncludeCrcLineDefaults);
		Assert.False(result.Settings.IncludeCrcSymbolDefaults);
		Assert.False(result.Settings.IncludeCrcTextDefaults);
		Assert.Null(result.Settings.Roi);
	}

	[Theory]
	[InlineData("Y", true)]
	[InlineData("y", true)]
	[InlineData("N", false)]
	[InlineData("n", false)]
	public void yes_no_settings_are_case_insensitive(string value, bool expected)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["BufferAirwayWaypoints"] = value;

		AirwaySettingsParseResult result = AirwaySettingsParser.Parse(settings);

		Assert.Equal(expected, result.Settings.BufferAirwayWaypoints);
	}

	[Fact]
	public void invalid_yes_no_value_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["BufferAirwayWaypoints"] = "Maybe";

		Assert.Throws<ArgumentException>(() => AirwaySettingsParser.Parse(settings));
	}

	[Fact]
	public void roi_filtering_requires_all_four_coordinates()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["FilterByRoi"] = "Y";
		settings["RoiSwLat"] = "38.0";
		// RoiSwLon, RoiNeLat, RoiNeLon intentionally omitted.

		Assert.Throws<ArgumentException>(() => AirwaySettingsParser.Parse(settings));
	}

	[Fact]
	public void roi_filtering_rejects_an_invalid_relative_position()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["FilterByRoi"] = "Y";
		settings["RoiSwLat"] = "43.0"; // north of NE - invalid box
		settings["RoiSwLon"] = "-85.0";
		settings["RoiNeLat"] = "38.0";
		settings["RoiNeLon"] = "-78.0";

		Assert.Throws<ArgumentException>(() => AirwaySettingsParser.Parse(settings));
	}

	[Fact]
	public void roi_filtering_builds_a_valid_region_of_interest()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["FilterByRoi"] = "Y";
		settings["RoiSwLat"] = "38.0";
		settings["RoiSwLon"] = "-85.0";
		settings["RoiNeLat"] = "43.0";
		settings["RoiNeLon"] = "-78.0";

		AirwaySettingsParseResult result = AirwaySettingsParser.Parse(settings);

		Assert.NotNull(result.Settings.Roi);
		Assert.Equal(38.0, result.Settings.Roi!.SwLat);
		Assert.Equal(-78.0, result.Settings.Roi.NeLon);
	}

	[Fact]
	public void crc_defaults_require_filters_for_every_class_and_kind()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcLineDefaults"] = "Y";
		settings["IncludeCrcSymbolDefaults"] = "Y";
		settings["IncludeCrcTextDefaults"] = "Y";
		// No Crc.* keys supplied at all.

		Assert.Throws<ArgumentException>(() => AirwaySettingsParser.Parse(settings));
	}

	[Fact]
	public void crc_defaults_parse_successfully_when_fully_specified()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcLineDefaults"] = "Y";
		settings["IncludeCrcSymbolDefaults"] = "Y";
		settings["IncludeCrcTextDefaults"] = "Y";

		foreach (string cls in new[] { "High", "Low", "Other" })
		{
			AddCrcDefaults(settings, cls, "Line");
			AddCrcDefaults(settings, cls, "Symbol");
			AddCrcDefaults(settings, cls, "Text");
		}

		AirwaySettingsParseResult result = AirwaySettingsParser.Parse(settings);

		Assert.True(result.Settings.IncludeCrcLineDefaults);
		Assert.True(result.Settings.IncludeCrcSymbolDefaults);
		Assert.True(result.Settings.IncludeCrcTextDefaults);
		Assert.Equal(3, result.Settings.LineDefaults[AirwayAltitudeClass.High].Filters[0]);
		Assert.Equal("solid", result.Settings.LineDefaults[AirwayAltitudeClass.High].Style);
	}

	[Fact]
	public void crc_defaults_are_only_required_for_the_kinds_being_emitted()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcLineDefaults"] = "Y";
		settings["IncludeCrcSymbolDefaults"] = "Y";
		settings["IncludeCrcTextDefaults"] = "Y";
		settings["EmitSymbols"] = "N";
		settings["EmitText"] = "N";

		foreach (string cls in new[] { "High", "Low", "Other" })
		{
			AddCrcDefaults(settings, cls, "Line");
		}

		// No Symbol or Text keys at all: those files are not written, so they are not needed.
		AirwaySettings parsed = AirwaySettingsParser.Parse(settings).Settings;

		Assert.Equal(3, parsed.LineDefaults.Count);
		Assert.Empty(parsed.SymbolDefaults);
		Assert.Empty(parsed.TextDefaults);
	}

	[Fact]
	public void crc_defaults_reject_an_out_of_range_value_with_a_clear_message()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcLineDefaults"] = "Y";
		settings["IncludeCrcSymbolDefaults"] = "Y";
		settings["IncludeCrcTextDefaults"] = "Y";

		foreach (string cls in new[] { "High", "Low", "Other" })
		{
			AddCrcDefaults(settings, cls, "Line");
			AddCrcDefaults(settings, cls, "Symbol");
			AddCrcDefaults(settings, cls, "Text");
		}

		settings["Crc.High.Line.thickness"] = "99"; // out of range

		ArgumentException ex = Assert.Throws<ArgumentException>(() => AirwaySettingsParser.Parse(settings));
		Assert.Contains("thickness", ex.Message);
	}

	[Fact]
	public void a_text_default_missing_x_offset_throws_naming_the_key_only_when_text_is_emitted()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcLineDefaults"] = "Y";
		settings["IncludeCrcSymbolDefaults"] = "Y";
		settings["IncludeCrcTextDefaults"] = "Y";

		foreach (string cls in new[] { "High", "Low", "Other" })
		{
			AddCrcDefaults(settings, cls, "Line");
			AddCrcDefaults(settings, cls, "Symbol");
			AddCrcDefaults(settings, cls, "Text");
		}

		settings.Remove("Crc.Low.Text.xOffset");

		ArgumentException ex = Assert.Throws<ArgumentException>(() => AirwaySettingsParser.Parse(settings));
		Assert.Contains("Crc.Low.Text.xOffset", ex.Message);

		settings["EmitText"] = "N";
		AirwaySettings parsed = AirwaySettingsParser.Parse(settings).Settings;

		Assert.Empty(parsed.TextDefaults);
		Assert.Equal(3, parsed.LineDefaults.Count);
		Assert.Equal(3, parsed.SymbolDefaults.Count);
	}

	[Fact]
	public void unrecognized_key_produces_a_warning_instead_of_failing()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["SomeTypo"] = "value";

		AirwaySettingsParseResult result = AirwaySettingsParser.Parse(settings);

		Assert.Contains(result.Warnings, w => w.Contains("SomeTypo"));
	}

	[Fact]
	public void per_waypoint_text_key_is_ignored_with_an_explanatory_warning()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["Crc.High.Text.text"] = "SHOULD_BE_IGNORED";

		AirwaySettingsParseResult result = AirwaySettingsParser.Parse(settings);

		Assert.Contains(result.Warnings, w => w.Contains("Crc.High.Text.text"));
	}

	// ---- Phase 3.3-3.7 settings ---------------------------------------

	[Fact]
	public void excluded_designations_parse_as_a_trimmed_upper_cased_set()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["ExcludedDesignations"] = " rn , sl ,V";

		AirwaySettings parsed = AirwaySettingsParser.Parse(settings).Settings;

		Assert.Equal(new[] { "RN", "SL", "V" }, parsed.ExcludedDesignations.OrderBy(x => x));
		Assert.Contains("rn", parsed.ExcludedDesignations); // case-insensitive membership
	}

	[Fact]
	public void excluded_designations_default_to_empty()
	{
		Assert.Empty(AirwaySettingsParser.Parse(MinimalValidSettings()).Settings.ExcludedDesignations);
	}

	[Fact]
	public void emit_flags_default_to_true()
	{
		AirwaySettings parsed = AirwaySettingsParser.Parse(MinimalValidSettings()).Settings;

		Assert.True(parsed.EmitLines);
		Assert.True(parsed.EmitSymbols);
		Assert.True(parsed.EmitText);
	}

	[Fact]
	public void all_three_emit_flags_off_throws_unless_output_by_is_none()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["EmitLines"] = "N";
		settings["EmitSymbols"] = "N";
		settings["EmitText"] = "N";

		Assert.Throws<ArgumentException>(() => AirwaySettingsParser.Parse(settings));

		settings["OutputBy"] = "None";
		AirwaySettings parsed = AirwaySettingsParser.Parse(settings).Settings;
		Assert.False(parsed.EmitLines);
	}

	[Theory]
	[InlineData("All", AliasRoiScope.All)]
	[InlineData("RoiAirways", AliasRoiScope.RoiAirways)]
	[InlineData("roiairways", AliasRoiScope.RoiAirways)]
	public void alias_roi_scope_parses_case_insensitively(string value, AliasRoiScope expected)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["AliasRoiScope"] = value;

		Assert.Equal(expected, AirwaySettingsParser.Parse(settings).Settings.AliasRoiScope);
	}

	[Fact]
	public void alias_roi_scope_defaults_to_all_and_rejects_junk()
	{
		Assert.Equal(AliasRoiScope.All, AirwaySettingsParser.Parse(MinimalValidSettings()).Settings.AliasRoiScope);

		Dictionary<string, string> settings = MinimalValidSettings();
		settings["AliasRoiScope"] = "Nonsense";
		Assert.Throws<ArgumentException>(() => AirwaySettingsParser.Parse(settings));
	}

	[Theory]
	[InlineData("5", 5)]
	[InlineData("7", 7)]
	public void coordinate_precision_parses_and_defaults_to_6(string value, int expected)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["CoordinatePrecision"] = value;

		Assert.Equal(expected, AirwaySettingsParser.Parse(settings).Settings.CoordinatePrecision);
		Assert.Equal(6, AirwaySettingsParser.Parse(MinimalValidSettings()).Settings.CoordinatePrecision);
	}

	[Theory]
	[InlineData("-1")]
	[InlineData("16")]
	[InlineData("abc")]
	public void coordinate_precision_rejects_out_of_range_or_non_numeric(string value)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["CoordinatePrecision"] = value;

		Assert.Throws<ArgumentException>(() => AirwaySettingsParser.Parse(settings));
	}

	[Fact]
	public void add_febuddy_output_folder_defaults_to_true()
	{
		Assert.True(AirwaySettingsParser.Parse(MinimalValidSettings()).Settings.AddFeBuddyOutputFolder);

		Dictionary<string, string> settings = MinimalValidSettings();
		settings["AddFeBuddyOutputFolder"] = "N";
		Assert.False(AirwaySettingsParser.Parse(settings).Settings.AddFeBuddyOutputFolder);
	}

	[Fact]
	public void only_the_crc_kinds_asked_for_have_their_defaults_read()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcLineDefaults"] = "N";
		settings["IncludeCrcSymbolDefaults"] = "Y";
		settings["IncludeCrcTextDefaults"] = "N";

		foreach (string cls in new[] { "High", "Low", "Other" })
		{
			AddCrcDefaults(settings, cls, "Symbol");
		}

		// No Line or Text keys at all: those defaults were not asked for.
		AirwaySettingsParseResult result = AirwaySettingsParser.Parse(settings);

		Assert.False(result.Settings.IncludeCrcLineDefaults);
		Assert.True(result.Settings.IncludeCrcSymbolDefaults);
		Assert.False(result.Settings.IncludeCrcTextDefaults);
		Assert.Equal(3, result.Settings.SymbolDefaults.Count);
		Assert.Empty(result.Settings.LineDefaults);
		Assert.Empty(result.Settings.TextDefaults);
		Assert.Empty(result.Warnings);
	}

	[Fact]
	public void asking_for_crc_defaults_on_a_kind_that_is_not_emitted_has_no_effect()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcTextDefaults"] = "Y";
		settings["EmitText"] = "N";
		// No Crc.*.Text.* keys: text files are not written, so their defaults are not needed.

		AirwaySettings parsed = AirwaySettingsParser.Parse(settings).Settings;

		Assert.False(parsed.IncludeCrcTextDefaults);
		Assert.Empty(parsed.TextDefaults);
	}

	[Fact]
	public void crc_defaults_are_not_required_when_output_by_is_none()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "None";
		settings["IncludeCrcLineDefaults"] = "Y";
		settings["IncludeCrcSymbolDefaults"] = "Y";
		settings["IncludeCrcTextDefaults"] = "Y";
		// No Crc.* keys at all: no GeoJSON is written, so no defaults are needed.

		AirwaySettings parsed = AirwaySettingsParser.Parse(settings).Settings;

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

		AirwaySettingsParseResult result = AirwaySettingsParser.Parse(settings);

		Assert.Contains(result.Warnings, w => w.Contains("IncludeCrcEramPropertyDefaults"));
		Assert.False(result.Settings.IncludeCrcLineDefaults);
		Assert.False(result.Settings.IncludeCrcSymbolDefaults);
		Assert.False(result.Settings.IncludeCrcTextDefaults);
	}
}
