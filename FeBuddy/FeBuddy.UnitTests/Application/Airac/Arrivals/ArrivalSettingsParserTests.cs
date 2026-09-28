using FeBuddy.Core.Application.Airac.Arrivals;
using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Application.Airac.Arrivals;

/// <summary>
/// Covers <see cref="ArrivalSettingsParser"/>: defaults, the output guard, the ARTCC filter, the
/// ROI mode, the amendment-date filter, the vNAS file keys, and which CRC defaults are required.
/// </summary>
public sealed class ArrivalSettingsParserTests
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

	private static Dictionary<string, string> WithRoi(string swLat, string swLon, string neLat, string neLon)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["FilterByRoi"] = "Y";
		settings["RoiSwLat"] = swLat;
		settings["RoiSwLon"] = swLon;
		settings["RoiNeLat"] = neLat;
		settings["RoiNeLon"] = neLon;
		return settings;
	}

	[Fact]
	public void missing_output_directory_throws()
	{
		Dictionary<string, string> settings = [];

		Assert.Throws<ArgumentException>(() => ArrivalSettingsParser.Parse(settings));
	}

	[Fact]
	public void minimal_settings_fall_back_to_documented_defaults()
	{
		ArrivalSettingsParseResult result = ArrivalSettingsParser.Parse(MinimalValidSettings());
		ArrivalSettings settings = result.Settings;

		Assert.Equal(@"C:\Output", settings.OutputDirectory);
		Assert.True(settings.GenerateGeojson);
		Assert.True(settings.GenerateAliasFile);
		Assert.True(settings.EmitLines);
		Assert.True(settings.EmitSymbols);
		Assert.True(settings.EmitText);
		Assert.Equal(ArrivalAmendmentFilter.None, settings.AmendmentFilter);
		Assert.Equal(0, settings.AmendedWithinCycles);
		Assert.Equal(0, settings.AmendedWithinDays);
		Assert.Null(settings.AmendedOnOrAfter);
		Assert.Equal(ArrivalRoiMode.Airport, settings.RoiMode);
		Assert.Null(settings.Roi);
		Assert.Empty(settings.ArtccFilter);
		Assert.False(settings.IncludeFebCustomProperties);
		Assert.Empty(settings.FebProperties);
		Assert.Empty(settings.Vnas.UploadFiles);
		Assert.Empty(settings.Vnas.CrcDefaultsFiles);
		Assert.Equal(6, settings.CoordinatePrecision);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void include_obstacle_departures_is_now_an_unknown_key_that_produces_a_warning()
	{
		// STARs have no obstacle-departure concept; the key Departures reads is unrecognized here.
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeObstacleDepartures"] = "Y";

		ArrivalSettingsParseResult result = ArrivalSettingsParser.Parse(settings);

		Assert.Contains(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains("IncludeObstacleDepartures"));
	}

	/// <summary>Marks every kind of Arrivals file for vNAS, and every GeoJSON kind for CRC-ERAM defaults.</summary>
	private static void UploadEverythingWithCrcDefaults(Dictionary<string, string> settings)
	{
		settings["UploadToVnas"] = "Arrivals_Lines,Arrivals_Symbols,Arrivals_Text,Arrivals.txt";
		settings["CrcDefaultsFor"] = "Arrivals_Lines,Arrivals_Symbols,Arrivals_Text";
	}

	[Fact]
	public void every_arrivals_file_key_is_accepted_ignoring_case()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["UploadToVnas"] = "arrivals_lines, ARRIVALS_SYMBOLS, Arrivals_Text, arrivals.txt";

		ArrivalSettings parsed = ArrivalSettingsParser.Parse(settings).Settings;

		Assert.True(parsed.Vnas.IsUploaded(ArrivalOutputFiles.Lines));
		Assert.True(parsed.Vnas.IsUploaded(ArrivalOutputFiles.Symbols));
		Assert.True(parsed.Vnas.IsUploaded(ArrivalOutputFiles.Text));
		Assert.True(parsed.Vnas.IsUploaded(ArrivalOutputFiles.Alias));
	}

	[Fact]
	public void a_file_arrivals_does_not_write_is_rejected_for_vnas()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["UploadToVnas"] = "LAS_BLAID_STAR_Lines";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => ArrivalSettingsParser.Parse(settings));
		Assert.Contains("LAS_BLAID_STAR_Lines", ex.Message);
	}

	[Fact]
	public void turning_off_both_geojson_and_the_alias_file_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["GenerateGeojson"] = "N";
		settings["GenerateAliasFile"] = "N";

		Assert.Throws<ArgumentException>(() => ArrivalSettingsParser.Parse(settings));
	}

	[Fact]
	public void generating_geojson_with_every_file_kind_off_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["GenerateGeojson"] = "Y";
		settings["EmitLines"] = "N";
		settings["EmitSymbols"] = "N";
		settings["EmitText"] = "N";

		Assert.Throws<ArgumentException>(() => ArrivalSettingsParser.Parse(settings));
	}

	[Fact]
	public void the_artcc_filter_is_trimmed_and_upper_cased()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["ArtccFilter"] = "zla, zoa";

		ArrivalSettings parsed = ArrivalSettingsParser.Parse(settings).Settings;

		Assert.Equal(["ZLA", "ZOA"], parsed.ArtccFilter);
	}

	[Theory]
	[InlineData("waypoint", ArrivalRoiMode.Waypoint)]
	[InlineData("Waypoint", ArrivalRoiMode.Waypoint)]
	[InlineData("AIRPORT", ArrivalRoiMode.Airport)]
	public void roi_mode_parses_case_insensitively(string value, ArrivalRoiMode expected)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["RoiMode"] = value;

		ArrivalSettings parsed = ArrivalSettingsParser.Parse(settings).Settings;

		Assert.Equal(expected, parsed.RoiMode);
	}

	[Fact]
	public void an_invalid_roi_mode_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["RoiMode"] = "Sideways";

		Assert.Throws<ArgumentException>(() => ArrivalSettingsParser.Parse(settings));
	}

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("none")]
	[InlineData("None")]
	public void a_blank_or_none_amendment_filter_keeps_every_procedure(string value)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["AmendmentFilter"] = value;

		ArrivalSettings parsed = ArrivalSettingsParser.Parse(settings).Settings;

		Assert.Equal(ArrivalAmendmentFilter.None, parsed.AmendmentFilter);
	}

	[Theory]
	[InlineData("cycles")]
	[InlineData("CYCLES")]
	[InlineData("Cycles")]
	public void cycles_mode_parses_case_insensitively_with_its_value(string value)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["AmendmentFilter"] = value;
		settings["AmendedWithinCycles"] = "4";

		ArrivalSettings parsed = ArrivalSettingsParser.Parse(settings).Settings;

		Assert.Equal(ArrivalAmendmentFilter.Cycles, parsed.AmendmentFilter);
		Assert.Equal(4, parsed.AmendedWithinCycles);
	}

	[Fact]
	public void days_mode_parses_its_value()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["AmendmentFilter"] = "Days";
		settings["AmendedWithinDays"] = "90";

		ArrivalSettings parsed = ArrivalSettingsParser.Parse(settings).Settings;

		Assert.Equal(ArrivalAmendmentFilter.Days, parsed.AmendmentFilter);
		Assert.Equal(90, parsed.AmendedWithinDays);
	}

	[Fact]
	public void date_mode_parses_its_value()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["AmendmentFilter"] = "Date";
		settings["AmendedOnOrAfter"] = "2026-01-01";

		ArrivalSettings parsed = ArrivalSettingsParser.Parse(settings).Settings;

		Assert.Equal(ArrivalAmendmentFilter.Date, parsed.AmendmentFilter);
		Assert.Equal(new DateOnly(2026, 1, 1), parsed.AmendedOnOrAfter);
	}

	[Theory]
	[InlineData("1")]
	[InlineData("cycle")]
	[InlineData("Cycles,Days")]
	[InlineData("Weeks")]
	public void an_invalid_amendment_filter_throws_naming_the_key(string value)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["AmendmentFilter"] = value;
		settings["AmendedWithinCycles"] = "1";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => ArrivalSettingsParser.Parse(settings));
		Assert.Contains("AmendmentFilter", ex.Message);
	}

	[Theory]
	[InlineData("Cycles", "AmendedWithinCycles")]
	[InlineData("Days", "AmendedWithinDays")]
	[InlineData("Date", "AmendedOnOrAfter")]
	public void the_active_modes_value_is_required(string mode, string key)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["AmendmentFilter"] = mode;

		ArgumentException ex = Assert.Throws<ArgumentException>(() => ArrivalSettingsParser.Parse(settings));
		Assert.Contains(key, ex.Message);
	}

	[Theory]
	[InlineData("Cycles", "AmendedWithinCycles", "0")]
	[InlineData("Cycles", "AmendedWithinCycles", "1001")]
	[InlineData("Cycles", "AmendedWithinCycles", "four")]
	[InlineData("Days", "AmendedWithinDays", "0")]
	[InlineData("Days", "AmendedWithinDays", "36501")]
	[InlineData("Days", "AmendedWithinDays", "-5")]
	public void an_out_of_range_or_non_integer_amendment_value_throws_naming_the_key(string mode, string key, string value)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["AmendmentFilter"] = mode;
		settings[key] = value;

		ArgumentException ex = Assert.Throws<ArgumentException>(() => ArrivalSettingsParser.Parse(settings));
		Assert.Contains(key, ex.Message);
	}

	[Theory]
	[InlineData("2026/01/01")]
	[InlineData("01-01-2026")]
	[InlineData("2026-02-30")]
	[InlineData("yesterday")]
	public void a_badly_formatted_amended_on_or_after_throws_naming_the_key(string value)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["AmendmentFilter"] = "Date";
		settings["AmendedOnOrAfter"] = value;

		ArgumentException ex = Assert.Throws<ArgumentException>(() => ArrivalSettingsParser.Parse(settings));
		Assert.Contains("AmendedOnOrAfter", ex.Message);
	}

	[Fact]
	public void keys_for_inactive_amendment_modes_are_ignored_without_a_warning()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["AmendmentFilter"] = "Days";
		settings["AmendedWithinDays"] = "30";
		// Invalid values for the other modes: never read, so they cannot fail the parse.
		settings["AmendedWithinCycles"] = "not a number";
		settings["AmendedOnOrAfter"] = "not a date";

		ArrivalSettingsParseResult result = ArrivalSettingsParser.Parse(settings);

		Assert.Equal(ArrivalAmendmentFilter.Days, result.Settings.AmendmentFilter);
		Assert.Equal(0, result.Settings.AmendedWithinCycles);
		Assert.Null(result.Settings.AmendedOnOrAfter);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void amendment_values_are_ignored_when_the_filter_is_absent()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["AmendedWithinCycles"] = "0";

		ArrivalSettingsParseResult result = ArrivalSettingsParser.Parse(settings);

		Assert.Equal(ArrivalAmendmentFilter.None, result.Settings.AmendmentFilter);
		Assert.Equal(0, result.Settings.AmendedWithinCycles);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void roi_filtering_with_a_blank_corner_throws()
	{
		Dictionary<string, string> settings = WithRoi("33.0", "-119.0", "35.0", " ");

		Assert.Throws<ArgumentException>(() => ArrivalSettingsParser.Parse(settings));
	}

	[Fact]
	public void roi_filtering_builds_a_region_of_interest()
	{
		Dictionary<string, string> settings = WithRoi("33.0", "-119.0", "35.0", "-117.0");

		ArrivalSettings parsed = ArrivalSettingsParser.Parse(settings).Settings;

		Assert.NotNull(parsed.Roi);
		Assert.Equal(33.0, parsed.Roi!.SwLat);
		Assert.Equal(-117.0, parsed.Roi.NeLon);
	}

	[Fact]
	public void an_unknown_feb_property_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeFebCustomProperties"] = "Y";
		settings["FebProperties"] = "arrivalName,notAProperty";

		Assert.Throws<ArgumentException>(() => ArrivalSettingsParser.Parse(settings));
	}

	[Fact]
	public void feb_properties_parse_in_the_order_given()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeFebCustomProperties"] = "Y";
		settings["FebProperties"] = "arrivalName,pointId";

		ArrivalSettings parsed = ArrivalSettingsParser.Parse(settings).Settings;

		Assert.True(parsed.IncludeFebCustomProperties);
		Assert.Equal([ArrivalFebProperty.ArrivalName, ArrivalFebProperty.PointId], parsed.FebProperties);
	}

	[Fact]
	public void crc_defaults_are_only_required_for_the_kinds_being_emitted()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		UploadEverythingWithCrcDefaults(settings);
		settings["EmitSymbols"] = "N";
		settings["EmitText"] = "N";
		AddCrcDefaults(settings, "Arrivals", "Line");

		// No Symbol or Text keys at all: those files are not written, so they are not needed.
		ArrivalSettings parsed = ArrivalSettingsParser.Parse(settings).Settings;

		Assert.Equal(3, Assert.Single(parsed.LineDefaults).Value.Filters[0]);
		Assert.Empty(parsed.SymbolDefaults);
		Assert.Empty(parsed.TextDefaults);
	}

	[Fact]
	public void crc_defaults_for_every_emitted_kind_are_required()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		UploadEverythingWithCrcDefaults(settings);
		// No Crc.* keys supplied at all.

		Assert.Throws<ArgumentException>(() => ArrivalSettingsParser.Parse(settings));
	}

	[Fact]
	public void a_text_default_missing_x_offset_throws_naming_the_key_only_when_text_is_emitted()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		UploadEverythingWithCrcDefaults(settings);
		AddCrcDefaults(settings, "Arrivals", "Line");
		AddCrcDefaults(settings, "Arrivals", "Symbol");
		AddCrcDefaults(settings, "Arrivals", "Text");
		settings.Remove("Crc.Arrivals.Text.xOffset");

		ArgumentException ex = Assert.Throws<ArgumentException>(() => ArrivalSettingsParser.Parse(settings));
		Assert.Contains("Crc.Arrivals.Text.xOffset", ex.Message);

		settings["EmitText"] = "N";
		ArrivalSettings parsed = ArrivalSettingsParser.Parse(settings).Settings;

		Assert.Empty(parsed.TextDefaults);
		Assert.Single(parsed.LineDefaults);
		Assert.Single(parsed.SymbolDefaults);
	}

	[Fact]
	public void an_unrecognized_key_produces_a_warning_rather_than_failing()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["TotallyMadeUpKey"] = "Y";

		ArrivalSettingsParseResult result = ArrivalSettingsParser.Parse(settings);

		Assert.Contains(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains("TotallyMadeUpKey"));
		Assert.Equal("ArrivalSettingsParser", Assert.Single(result.Messages).Source);
	}

	[Fact]
	public void generate_alias_file_is_arrivals_own_key_and_produces_no_unknown_key_warning()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["GenerateAliasFile"] = "Y";

		ArrivalSettingsParseResult result = ArrivalSettingsParser.Parse(settings);

		Assert.DoesNotContain(result.Messages, m => m.Text.Contains("GenerateAliasFile"));
	}

	[Fact]
	public void a_crc_key_for_another_class_produces_a_warning_rather_than_failing()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["Crc.Airports.Line.bcg"] = "1";

		ArrivalSettingsParseResult result = ArrivalSettingsParser.Parse(settings);

		Assert.Contains(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains("Crc.Airports.Line.bcg"));
	}

	[Fact]
	public void only_the_kinds_chosen_for_crc_defaults_have_their_defaults_read()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["UploadToVnas"] = "Arrivals_Lines,Arrivals_Symbols,Arrivals_Text";
		settings["CrcDefaultsFor"] = "Arrivals_Symbols";
		AddCrcDefaults(settings, "Arrivals", "Symbol");
		// No Line or Text keys at all: those kinds go to vNAS without defaults.

		ArrivalSettingsParseResult result = ArrivalSettingsParser.Parse(settings);

		Assert.Equal("vor", Assert.Single(result.Settings.SymbolDefaults).Value.Style);
		Assert.Empty(result.Settings.LineDefaults);
		Assert.Empty(result.Settings.TextDefaults);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void crc_defaults_for_a_kind_that_is_not_emitted_are_not_required()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["UploadToVnas"] = "Arrivals_Lines";
		settings["CrcDefaultsFor"] = "Arrivals_Lines";
		settings["EmitLines"] = "N";
		// No Crc.Arrivals.Line.* keys: the lines files are not written, so their defaults are not needed.

		ArrivalSettings parsed = ArrivalSettingsParser.Parse(settings).Settings;

		Assert.Empty(parsed.LineDefaults);
	}

	[Fact]
	public void crc_defaults_are_not_required_when_geojson_is_off()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["GenerateGeojson"] = "N";
		UploadEverythingWithCrcDefaults(settings);
		// No Crc.* keys at all: no GeoJSON is written, so no defaults are needed.

		ArrivalSettings parsed = ArrivalSettingsParser.Parse(settings).Settings;

		Assert.Empty(parsed.LineDefaults);
		Assert.Empty(parsed.SymbolDefaults);
		Assert.Empty(parsed.TextDefaults);
	}

	[Theory]
	[InlineData("IncludeCrcEramPropertyDefaults")]
	[InlineData("IncludeCrcTextDefaults")]
	[InlineData("AddFeBuddyOutputFolder")]
	public void a_retired_key_produces_a_warning_and_changes_nothing(string key)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings[key] = "Y";

		ArrivalSettingsParseResult result = ArrivalSettingsParser.Parse(settings);

		Assert.Contains(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains(key));
		Assert.Empty(result.Settings.Vnas.CrcDefaultsFiles);
		Assert.Empty(result.Settings.TextDefaults);
	}

	[Fact]
	public void feb_custom_properties_on_with_an_empty_list_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeFebCustomProperties"] = "Y";
		settings["FebProperties"] = " ";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => ArrivalSettingsParser.Parse(settings));
		Assert.Contains("'FebProperties' names none", ex.Message);
	}

	[Theory]
	[InlineData("north", "-85.0", "43.0", "-78.0")]
	[InlineData("43.0", "-85.0", "38.0", "-78.0")]
	public void an_invalid_region_of_interest_is_rejected(string swLat, string swLon, string neLat, string neLon)
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => ArrivalSettingsParser.Parse(WithRoi(swLat, swLon, neLat, neLon)));
		Assert.StartsWith("Invalid Region of Interest", ex.Message);
	}

	[Theory]
	[InlineData("Crc.Arrivals.Text.text", "each point is labelled with its own identifier")]
	[InlineData("Crc.Arrivals.Symbol.madeUp", "Unrecognized setting 'Crc.Arrivals.Symbol.madeUp'")]
	public void a_crc_key_that_cannot_apply_is_a_warning(string key, string expected)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings[key] = "1";

		ArrivalSettingsParseResult result = ArrivalSettingsParser.Parse(settings);

		Assert.Contains(expected, Assert.Single(result.Messages).Text);
	}
}
