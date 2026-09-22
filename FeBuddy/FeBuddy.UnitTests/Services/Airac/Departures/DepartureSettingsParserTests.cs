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
		Assert.False(settings.IncludeCrcEramPropertyDefaults);
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
		settings["FebProperties"] = "dpName,waypoints";

		DepartureSettings parsed = DepartureSettingsParser.Parse(settings).Settings;

		Assert.True(parsed.IncludeFebCustomProperties);
		Assert.Equal(new[] { DepartureFebProperty.DpName, DepartureFebProperty.Waypoints }, parsed.FebProperties);
	}

	[Fact]
	public void crc_defaults_are_only_required_for_the_kinds_being_emitted()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcEramPropertyDefaults"] = "Y";
		settings["EmitSymbols"] = "N";
		settings["EmitText"] = "N";
		settings["Crc.Departures.Line.filters"] = "3";

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
		settings["IncludeCrcEramPropertyDefaults"] = "Y";
		// No Crc.* keys supplied at all.

		Assert.Throws<ArgumentException>(() => DepartureSettingsParser.Parse(settings));
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
}
