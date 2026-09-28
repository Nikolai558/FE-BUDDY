using FeBuddy.Core.Application.Airac.Telephony;
using FeBuddy.Core.Application.Airac.Telephony.Models;

namespace FeBuddy.UnitTests.Application.Airac.Telephony;

/// <summary>
/// Covers <see cref="TelephonySettingsParser"/>: the required <c>OutputDirectory</c>, the
/// "GenerateAliasFile is N" guard (Telephony's only output), <c>UploadToVnas</c>/<c>CrcDefaultsFor</c>
/// (the alias file is the only key Telephony can name, and it has no CRC-ERAM defaults at all), the
/// <c>IncludeFebCustomProperties</c> warning (Telephony writes no GeoJSON), unknown-key warnings, and
/// silent acceptance of the shared ROI/precision/<c>feb.*</c> keys Telephony does not itself read.
/// </summary>
public sealed class TelephonySettingsParserTests
{
	private static Dictionary<string, string> MinimalValidSettings() => new(StringComparer.OrdinalIgnoreCase)
	{
		["OutputDirectory"] = @"C:\Output",
	};

	[Fact]
	public void parse_rejects_a_null_argument() =>
		Assert.Throws<ArgumentNullException>(() => TelephonySettingsParser.Parse(null!));

	[Fact]
	public void missing_output_directory_throws() =>
		Assert.Throws<ArgumentException>(() => TelephonySettingsParser.Parse(new Dictionary<string, string>()));

	[Fact]
	public void a_minimal_settings_block_produces_no_messages_and_the_documented_defaults()
	{
		TelephonySettingsParseResult result = TelephonySettingsParser.Parse(MinimalValidSettings());

		Assert.Empty(result.Messages);
		Assert.Equal(@"C:\Output", result.Settings.OutputDirectory);
		Assert.Empty(result.Settings.Vnas.UploadFiles);
		Assert.False(result.Settings.Vnas.IsUploaded(TelephonyOutputFiles.Alias));
	}

	// ---- GenerateAliasFile ----

	[Fact]
	public void generate_alias_file_n_throws_because_it_is_telephonys_only_output()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["GenerateAliasFile"] = "N";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => TelephonySettingsParser.Parse(settings));
		Assert.Contains("GenerateAliasFile", ex.Message);
	}

	[Fact]
	public void generate_alias_file_y_is_the_default_and_produces_no_warning()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["GenerateAliasFile"] = "Y";

		TelephonySettingsParseResult result = TelephonySettingsParser.Parse(settings);

		Assert.Empty(result.Messages);
	}

	// ---- UploadToVnas / CrcDefaultsFor ----

	[Fact]
	public void upload_to_vnas_may_name_the_alias_file()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["UploadToVnas"] = "Telephony.txt";

		TelephonySettings parsed = TelephonySettingsParser.Parse(settings).Settings;

		Assert.True(parsed.Vnas.IsUploaded(TelephonyOutputFiles.Alias));
	}

	[Fact]
	public void upload_to_vnas_naming_any_other_key_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["UploadToVnas"] = "SomeOtherFile.txt";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => TelephonySettingsParser.Parse(settings));
		Assert.Contains("SomeOtherFile.txt", ex.Message);
	}

	[Fact]
	public void crc_defaults_for_the_alias_file_throws_because_it_has_no_crc_eram_defaults()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["CrcDefaultsFor"] = "Telephony.txt";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => TelephonySettingsParser.Parse(settings));
		Assert.Contains("Telephony.txt", ex.Message);
	}

	// ---- IncludeFebCustomProperties ----

	[Fact]
	public void include_feb_custom_properties_warns_that_telephony_writes_no_geojson()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeFebCustomProperties"] = "Y";

		TelephonySettingsParseResult result = TelephonySettingsParser.Parse(settings);

		Assert.Contains(
			result.Messages.WarningTexts(),
			w => w.Contains("IncludeFebCustomProperties") && w.Contains("no FE-Buddy properties"));
	}

	// ---- unknown keys ----

	[Fact]
	public void an_unrecognized_key_warns_and_does_not_throw()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["TotallyMadeUpKey"] = "x";

		TelephonySettingsParseResult result = TelephonySettingsParser.Parse(settings);

		Assert.Contains(result.Messages.WarningTexts(), w => w.Contains("TotallyMadeUpKey"));
	}

	[Theory]
	[InlineData("EmitSymbols")]
	[InlineData("OutputBy")]
	public void settings_only_other_sub_services_have_are_unknown_key_warnings(string key)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings[key] = "N";

		TelephonySettingsParseResult result = TelephonySettingsParser.Parse(settings);

		Assert.Contains(result.Messages.WarningTexts(), w => w.Contains(key));
	}

	// ---- shared keys accepted silently ----

	[Fact]
	public void shared_roi_precision_and_feb_properties_keys_are_accepted_without_warnings()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["FilterByRoi"] = "Y";
		settings["RoiSwLat"] = "40.0";
		settings["RoiSwLon"] = "-89.0";
		settings["RoiNeLat"] = "43.0";
		settings["RoiNeLon"] = "-86.0";
		settings["CoordinatePrecision"] = "3";
		settings["FebProperties"] = "someProperty";

		TelephonySettingsParseResult result = TelephonySettingsParser.Parse(settings);

		Assert.Empty(result.Messages);
	}
}
