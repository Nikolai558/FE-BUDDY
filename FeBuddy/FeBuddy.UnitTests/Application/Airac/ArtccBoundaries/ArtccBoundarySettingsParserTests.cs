using FeBuddy.Core.Application.Airac.ArtccBoundaries;
using FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;

namespace FeBuddy.UnitTests.Application.Airac.ArtccBoundaries;

/// <summary>
/// Covers <see cref="ArtccBoundarySettingsParser"/>: the documented defaults, each
/// <c>OutputBy</c> value, the <c>LocationFilter</c>, the CRC Line defaults <c>CrcDefaultsFor</c>
/// reads (the fixed High/Low/Unlimited classes, or a hyphenated <c>LocationId-ALTITUDE</c> class),
/// and that this sub-service has no alias file and none of <c>GenerateGeojson</c>,
/// <c>GenerateAliasFile</c> or <c>EmitLines</c>.
/// </summary>
public sealed class ArtccBoundarySettingsParserTests
{
	private static Dictionary<string, string> MinimalValidSettings() => new()
	{
		{ "OutputDirectory", @"C:\Output" }
	};

	private static void AddLineDefaults(Dictionary<string, string> settings, string prefix)
	{
		settings[$"{prefix}.bcg"] = "3";
		settings[$"{prefix}.filters"] = "3";
		settings[$"{prefix}.style"] = "solid";
		settings[$"{prefix}.thickness"] = "1";
	}

	[Fact]
	public void missing_output_directory_throws() =>
		Assert.Throws<ArgumentException>(() => ArtccBoundarySettingsParser.Parse(new Dictionary<string, string>()));

	[Fact]
	public void a_fully_default_settings_block_produces_no_messages_and_the_documented_defaults()
	{
		ArtccBoundarySettingsParseResult result = ArtccBoundarySettingsParser.Parse(MinimalValidSettings());

		Assert.Empty(result.Messages);
		Assert.Equal(ArtccBoundaryOutputBy.HighLow, result.Settings.OutputBy);
		Assert.True(result.Settings.SplitAtAntimeridian);
		Assert.Null(result.Settings.Roi);
		Assert.Empty(result.Settings.LocationFilter);
		Assert.False(result.Settings.IncludeFebCustomProperties);
		Assert.Empty(result.Settings.FebProperties);
		Assert.Empty(result.Settings.Vnas.UploadFiles);
		Assert.Empty(result.Settings.LineDefaults);
		Assert.Equal(6, result.Settings.CoordinatePrecision);
	}

	[Theory]
	[InlineData("HighLow", ArtccBoundaryOutputBy.HighLow)]
	[InlineData("highlow", ArtccBoundaryOutputBy.HighLow)]
	[InlineData("HighLowUnlimited", ArtccBoundaryOutputBy.HighLowUnlimited)]
	[InlineData("highlowunlimited", ArtccBoundaryOutputBy.HighLowUnlimited)]
	[InlineData("ArtccAltitude", ArtccBoundaryOutputBy.ArtccAltitude)]
	[InlineData("artccaltitude", ArtccBoundaryOutputBy.ArtccAltitude)]
	public void output_by_parses_case_insensitively(string value, ArtccBoundaryOutputBy expected)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = value;

		Assert.Equal(expected, ArtccBoundarySettingsParser.Parse(settings).Settings.OutputBy);
	}

	[Fact]
	public void an_invalid_output_by_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "Sideways";

		Assert.Throws<ArgumentException>(() => ArtccBoundarySettingsParser.Parse(settings));
	}

	[Fact]
	public void location_filter_parses_as_a_trimmed_upper_cased_set()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["LocationFilter"] = " zob , zak";

		ArtccBoundarySettings parsed = ArtccBoundarySettingsParser.Parse(settings).Settings;

		Assert.Equal(["ZAK", "ZOB"], parsed.LocationFilter.OrderBy(x => x, StringComparer.Ordinal));
	}

	[Theory]
	[InlineData("Y", true)]
	[InlineData("N", false)]
	public void split_at_antimeridian_reads_the_yes_no_flag(string value, bool expected)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["SplitAtAntimeridian"] = value;

		Assert.Equal(expected, ArtccBoundarySettingsParser.Parse(settings).Settings.SplitAtAntimeridian);
	}

	// ---- CRC Line defaults ----

	[Fact]
	public void high_low_mode_reads_line_defaults_for_the_high_class_when_chosen_for_crc_defaults()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		string highKey = ArtccBoundaryOutputFiles.KeyFor(ArtccBoundaryOutputFiles.HighClass);
		settings["UploadToVnas"] = highKey;
		settings["CrcDefaultsFor"] = highKey;
		AddLineDefaults(settings, $"Crc.{ArtccBoundaryOutputFiles.HighClass}.Line");

		ArtccBoundarySettingsParseResult result = ArtccBoundarySettingsParser.Parse(settings);

		Assert.Equal("solid", result.Settings.LineDefaults[ArtccBoundaryOutputFiles.HighClass].Style);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void artcc_altitude_mode_reads_line_defaults_for_a_hyphenated_location_altitude_class_with_no_unknown_key_warning()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["OutputBy"] = "ArtccAltitude";
		string key = ArtccBoundaryOutputFiles.KeyFor("ZOB", "HIGH");
		settings["UploadToVnas"] = key;
		settings["CrcDefaultsFor"] = key;
		AddLineDefaults(settings, "Crc.ZOB-HIGH.Line");

		ArtccBoundarySettingsParseResult result = ArtccBoundarySettingsParser.Parse(settings);

		Assert.Equal("solid", result.Settings.LineDefaults["ZOB-HIGH"].Style);
		Assert.Empty(result.Messages.WarningTexts());
	}

	[Fact]
	public void a_missing_crc_value_throws_naming_the_key()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		string highKey = ArtccBoundaryOutputFiles.KeyFor(ArtccBoundaryOutputFiles.HighClass);
		settings["UploadToVnas"] = highKey;
		settings["CrcDefaultsFor"] = highKey;
		settings["Crc.High.Line.bcg"] = "3";
		settings["Crc.High.Line.filters"] = "3";
		settings["Crc.High.Line.style"] = "solid";
		// Crc.High.Line.thickness is deliberately missing.

		ArgumentException ex = Assert.Throws<ArgumentException>(() => ArtccBoundarySettingsParser.Parse(settings));

		Assert.Contains("Crc.High.Line.thickness", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void crc_defaults_are_only_read_for_the_keys_chosen_in_crc_defaults_for()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		string highKey = ArtccBoundaryOutputFiles.KeyFor(ArtccBoundaryOutputFiles.HighClass);
		string lowKey = ArtccBoundaryOutputFiles.KeyFor(ArtccBoundaryOutputFiles.LowClass);
		settings["UploadToVnas"] = $"{highKey},{lowKey}";
		settings["CrcDefaultsFor"] = highKey;
		AddLineDefaults(settings, $"Crc.{ArtccBoundaryOutputFiles.HighClass}.Line");
		// No Crc.Low.Line.* keys supplied - if the parser tried to read them, this would throw.

		ArtccBoundarySettings parsed = ArtccBoundarySettingsParser.Parse(settings).Settings;

		Assert.Single(parsed.LineDefaults);
		Assert.True(parsed.LineDefaults.ContainsKey(ArtccBoundaryOutputFiles.HighClass));
	}

	// ---- no GenerateAliasFile, no GenerateGeojson, no EmitLines, no alias file ----

	[Theory]
	[InlineData("GenerateAliasFile")]
	[InlineData("GenerateGeojson")]
	[InlineData("EmitLines")]
	public void settings_only_other_sub_services_have_are_unknown_key_warnings(string key)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings[key] = "N";

		ArtccBoundarySettingsParseResult result = ArtccBoundarySettingsParser.Parse(settings);

		Assert.Contains(result.Messages.WarningTexts(), w => w.Contains(key));
	}

	[Fact]
	public void upload_to_vnas_naming_an_alias_looking_key_throws_because_there_is_no_alias_file()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["UploadToVnas"] = "ArtccBoundaries.txt";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => ArtccBoundarySettingsParser.Parse(settings));

		Assert.Contains("ArtccBoundaries.txt", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void an_unrecognized_key_produces_a_warning_rather_than_failing()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["TotallyMadeUpKey"] = "x";

		ArtccBoundarySettingsParseResult result = ArtccBoundarySettingsParser.Parse(settings);

		Assert.Contains(result.Messages.WarningTexts(), w => w.Contains("TotallyMadeUpKey"));
	}
}
