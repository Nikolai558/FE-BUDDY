using FEBuddyLibrary.Models.Services.Airac.Airways;
using FEBuddyLibrary.Services.Airac.Airways;

namespace UnitTests.Services.Airac.Airways;

public class AirwaySettingsParserTests
{
	private static Dictionary<string, string> MinimalValidSettings() => new()
	{
		{ "OutputDirectory", @"C:\Output" },
		{ "OutputBy", "HighLow" }
	};

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
		Assert.False(result.Settings.IncludeCrcEramPropertyDefaults);
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
		settings["IncludeCrcEramPropertyDefaults"] = "Y";
		// No Crc.* keys supplied at all.

		Assert.Throws<ArgumentException>(() => AirwaySettingsParser.Parse(settings));
	}

	[Fact]
	public void crc_defaults_parse_successfully_when_fully_specified()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcEramPropertyDefaults"] = "Y";

		foreach (string cls in new[] { "High", "Low", "Other" })
		{
			settings[$"Crc.{cls}.Line.filters"] = "3";
			settings[$"Crc.{cls}.Line.style"] = "solid";
			settings[$"Crc.{cls}.Symbol.filters"] = "3";
			settings[$"Crc.{cls}.Text.filters"] = "3";
		}

		AirwaySettingsParseResult result = AirwaySettingsParser.Parse(settings);

		Assert.True(result.Settings.IncludeCrcEramPropertyDefaults);
		Assert.Equal(3, result.Settings.LineDefaults[AirwayAltitudeClass.High].Filters[0]);
		Assert.Equal("solid", result.Settings.LineDefaults[AirwayAltitudeClass.High].Style);
	}

	[Fact]
	public void crc_defaults_reject_an_out_of_range_value_with_a_clear_message()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeCrcEramPropertyDefaults"] = "Y";

		foreach (string cls in new[] { "High", "Low", "Other" })
		{
			settings[$"Crc.{cls}.Line.filters"] = "3";
			settings[$"Crc.{cls}.Symbol.filters"] = "3";
			settings[$"Crc.{cls}.Text.filters"] = "3";
		}

		settings["Crc.High.Line.thickness"] = "99"; // out of range

		ArgumentException ex = Assert.Throws<ArgumentException>(() => AirwaySettingsParser.Parse(settings));
		Assert.Contains("thickness", ex.Message);
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
}
