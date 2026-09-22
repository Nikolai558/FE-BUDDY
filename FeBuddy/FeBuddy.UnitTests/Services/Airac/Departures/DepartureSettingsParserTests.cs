using FeBuddy.Core.Models.Services.Airac.Departures;
using FeBuddy.Core.Services.Airac.Departures;
using FeBuddy.Core.Services.General;

namespace FeBuddy.UnitTests.Services.Airac.Departures;

public class DepartureSettingsParserTests
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
		Dictionary<string, string> settings = new();

		Assert.Throws<ArgumentException>(() => DepartureSettingsParser.Parse(settings));
	}

	[Fact]
	public void minimal_settings_fall_back_to_documented_defaults()
	{
		DepartureSettingsParseResult result = DepartureSettingsParser.Parse(MinimalValidSettings());
		DepartureSettings settings = result.Settings;

		Assert.Equal(@"C:\Output", settings.OutputDirectory);
		Assert.True(settings.GenerateGeojson);
		Assert.True(settings.GenerateAliasFile);
		Assert.True(settings.EmitLines);
		Assert.True(settings.EmitSymbols);
		Assert.True(settings.EmitText);
		Assert.True(settings.IncludeObstacleDepartures);
		Assert.Equal(0, settings.AmendedWithinCycles);
		Assert.Equal(DepartureRoiMode.Airport, settings.RoiMode);
		Assert.Null(settings.Roi);
		Assert.Empty(settings.ArtccFilter);
		Assert.False(settings.IncludeFebCustomProperties);
		Assert.Empty(settings.FebProperties);
		Assert.False(settings.IncludeCrcLineDefaults);
		Assert.False(settings.IncludeCrcSymbolDefaults);
		Assert.False(settings.IncludeCrcTextDefaults);
		Assert.Equal(6, settings.CoordinatePrecision);
		Assert.True(settings.AddFeBuddyOutputFolder);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void turning_off_both_geojson_and_the_alias_file_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["GenerateGeojson"] = "N";
		settings["GenerateAliasFile"] = "N";

		Assert.Throws<ArgumentException>(() => DepartureSettingsParser.Parse(settings));
	}

	[Fact]
	public void generating_geojson_with_every_file_kind_off_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["GenerateGeojson"] = "Y";
		settings["EmitLines"] = "N";
		settings["EmitSymbols"] = "N";
		settings["EmitText"] = "N";

		Assert.Throws<ArgumentException>(() => DepartureSettingsParser.Parse(settings));
	}

	[Fact]
	public void the_artcc_filter_is_trimmed_and_upper_cased()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["ArtccFilter"] = "zla, zoa";

		DepartureSettings parsed = DepartureSettingsParser.Parse(settings).Settings;

		Assert.Equal(new[] { "ZLA", "ZOA" }, parsed.ArtccFilter);
	}

	[Theory]
	[InlineData("waypoint", DepartureRoiMode.Waypoint)]
	[InlineData("Waypoint", DepartureRoiMode.Waypoint)]
	[InlineData("AIRPORT", DepartureRoiMode.Airport)]
	public void roi_mode_parses_case_insensitively(string value, DepartureRoiMode expected)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["RoiMode"] = value;

		DepartureSettings parsed = DepartureSettingsParser.Parse(settings).Settings;

		Assert.Equal(expected, parsed.RoiMode);
	}

	[Fact]
	public void an_invalid_roi_mode_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["RoiMode"] = "Sideways";

		Assert.Throws<ArgumentException>(() => DepartureSettingsParser.Parse(settings));
	}

	[Fact]
	public void roi_filtering_with_a_blank_corner_throws()
	{
		Dictionary<string, string> settings = WithRoi("33.0", "-119.0", "35.0", " ");

		Assert.Throws<ArgumentException>(() => DepartureSettingsParser.Parse(settings));
	}

	[Fact]
	public void roi_filtering_builds_a_region_of_interest()
	{
		Dictionary<string, string> settings = WithRoi("33.0", "-119.0", "35.0", "-117.0");

		DepartureSettings parsed = DepartureSettingsParser.Parse(settings).Settings;

		Assert.NotNull(parsed.Roi);
		Assert.Equal(33.0, parsed.Roi!.SwLat);
		Assert.Equal(-117.0, parsed.Roi.NeLon);
	}

	[Fact]
	public void an_unknown_feb_property_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeFebCustomProperties"] = "Y";
		settings["FebProperties"] = "dpName,notAProperty";

		Assert.Throws<ArgumentException>(() => DepartureSettingsParser.Parse(settings));
	}

	[Fact]
	public void feb_properties_parse_in_the_order_given()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeFebCustomProperties"] = "Y";
		settings["FebProperties"] = "dpName,pointId";

		DepartureSettings parsed = DepartureSettingsParser.Parse(settings).Settings;

		Assert.True(parsed.IncludeFebCustomProperties);
		Assert.Equal(new[] { DepartureFebProperty.DpName, DepartureFebProperty.PointId }, parsed.FebProperties);
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
		AddCrcDefaults(settings, "Departures", "Line");

		// No Symbol or Text keys at all: those files are not written, so they are not needed.
		DepartureSettings parsed = DepartureSettingsParser.Parse(settings).Settings;

		Assert.Equal(3, Assert.Single(parsed.LineDefaults).Value.Filters[0]);
		Assert.Empty(parsed.SymbolDefaults);
		Assert.Empty(parsed.TextDefaults);
	}

	[Fact]
	public void crc_defaults_for_every_emitted_kind_are_required()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcLineDefaults"] = "Y";
		settings["IncludeCrcSymbolDefaults"] = "Y";
		settings["IncludeCrcTextDefaults"] = "Y";
		// No Crc.* keys supplied at all.

		Assert.Throws<ArgumentException>(() => DepartureSettingsParser.Parse(settings));
	}

	[Fact]
	public void a_text_default_missing_x_offset_throws_naming_the_key_only_when_text_is_emitted()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcLineDefaults"] = "Y";
		settings["IncludeCrcSymbolDefaults"] = "Y";
		settings["IncludeCrcTextDefaults"] = "Y";
		AddCrcDefaults(settings, "Departures", "Line");
		AddCrcDefaults(settings, "Departures", "Symbol");
		AddCrcDefaults(settings, "Departures", "Text");
		settings.Remove("Crc.Departures.Text.xOffset");

		ArgumentException ex = Assert.Throws<ArgumentException>(() => DepartureSettingsParser.Parse(settings));
		Assert.Contains("Crc.Departures.Text.xOffset", ex.Message);

		settings["EmitText"] = "N";
		DepartureSettings parsed = DepartureSettingsParser.Parse(settings).Settings;

		Assert.Empty(parsed.TextDefaults);
		Assert.Single(parsed.LineDefaults);
		Assert.Single(parsed.SymbolDefaults);
	}

	[Fact]
	public void an_unrecognized_key_produces_a_warning_rather_than_failing()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["TotallyMadeUpKey"] = "Y";

		DepartureSettingsParseResult result = DepartureSettingsParser.Parse(settings);

		Assert.Contains(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains("TotallyMadeUpKey"));
		Assert.Equal("DepartureSettingsParser", Assert.Single(result.Messages).Source);
	}

	[Fact]
	public void a_crc_key_for_another_class_produces_a_warning_rather_than_failing()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["Crc.Airports.Line.bcg"] = "1";

		DepartureSettingsParseResult result = DepartureSettingsParser.Parse(settings);

		Assert.Contains(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains("Crc.Airports.Line.bcg"));
	}

	[Fact]
	public void only_the_crc_kinds_asked_for_have_their_defaults_read()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcLineDefaults"] = "N";
		settings["IncludeCrcSymbolDefaults"] = "Y";
		settings["IncludeCrcTextDefaults"] = "N";
		AddCrcDefaults(settings, "Departures", "Symbol");
		// No Line or Text keys at all: those defaults were not asked for.

		DepartureSettingsParseResult result = DepartureSettingsParser.Parse(settings);

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
		settings["EmitLines"] = "N";
		// No Crc.Departures.Line.* keys: the lines file is not written, so its defaults are not needed.

		DepartureSettings parsed = DepartureSettingsParser.Parse(settings).Settings;

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

		DepartureSettings parsed = DepartureSettingsParser.Parse(settings).Settings;

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

		DepartureSettingsParseResult result = DepartureSettingsParser.Parse(settings);

		Assert.Contains(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains("IncludeCrcEramPropertyDefaults"));
		Assert.False(result.Settings.IncludeCrcLineDefaults);
		Assert.False(result.Settings.IncludeCrcSymbolDefaults);
		Assert.False(result.Settings.IncludeCrcTextDefaults);
	}
}
