using FeBuddy.Core.Application.Airac.Telephony;
using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Application.Airac.Telephony;

/// <summary>
/// Covers <see cref="TelephonySettingsParser"/>: the required <c>OutputDirectory</c>, the
/// "GenerateAliasFile is N" guard (Telephony's only output), <c>UploadToVnas</c>/<c>CrcDefaultsFor</c>
/// (the alias file is the only key Telephony can name, and it has no CRC-ERAM defaults at all), the
/// <c>IncludeFebCustomProperties</c> warning (Telephony writes no GeoJSON), unknown-key warnings, and
/// silent acceptance of the shared ROI/precision/<c>feb.*</c> keys Telephony does not itself read -
/// plus the numbered <c>VirtualAirlines.&lt;n&gt;.*</c> keys (number order, trimming, blank and
/// duplicate numbers, what makes one invalid, which keys count as unknown) and
/// <see cref="TelephonySettingsParser.VirtualAirlineProblem"/>.
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

	// ---- virtual airlines ----

	/// <summary>Adds one virtual airline's numbered keys; a <see langword="null"/> field is left out altogether.</summary>
	private static void AddVirtualAirline(
		Dictionary<string, string> settings, int number, string? designator, string? telephony, string? organization)
	{
		if (designator is not null)
		{
			settings[$"VirtualAirlines.{number}.Designator"] = designator;
		}

		if (telephony is not null)
		{
			settings[$"VirtualAirlines.{number}.Telephony"] = telephony;
		}

		if (organization is not null)
		{
			settings[$"VirtualAirlines.{number}.Organization"] = organization;
		}
	}

	[Fact]
	public void with_no_virtual_airline_keys_the_list_is_empty()
	{
		TelephonySettingsParseResult result = TelephonySettingsParser.Parse(MinimalValidSettings());

		Assert.Empty(result.Settings.VirtualAirlines);
		Assert.Empty(result.Messages);
	}

	/// <summary>Number 10 is listed before number 2 in the settings, but is written after it: the order is numeric, not alphabetical.</summary>
	[Fact]
	public void virtual_airlines_are_read_in_number_order_and_trimmed()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		AddVirtualAirline(settings, 10, " TEN ", " TEN CALL ", " Ten Virtual ");
		AddVirtualAirline(settings, 2, "  DVA", "DELTA  ", "  Delta Virtual  ");

		TelephonySettingsParseResult result = TelephonySettingsParser.Parse(settings);

		Assert.Equal(
			[new VirtualAirline("DVA", "DELTA", "Delta Virtual"), new VirtualAirline("TEN", "TEN CALL", "Ten Virtual")],
			result.Settings.VirtualAirlines);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void a_virtual_airline_number_with_nothing_in_it_is_skipped_without_a_message()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		AddVirtualAirline(settings, 1, "", "   ", "");
		AddVirtualAirline(settings, 2, "DVA", "DELTA", "Delta Virtual");
		AddVirtualAirline(settings, 3, "", null, null);

		TelephonySettingsParseResult result = TelephonySettingsParser.Parse(settings);

		Assert.Equal("DVA", Assert.Single(result.Settings.VirtualAirlines).Designator);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void virtual_airline_keys_match_ignoring_case_and_are_not_unknown_keys()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["virtualairlines.1.designator"] = "DVA";
		settings["VIRTUALAIRLINES.1.TELEPHONY"] = "DELTA";
		settings["VirtualAirlines.1.organization"] = "Delta Virtual";

		TelephonySettingsParseResult result = TelephonySettingsParser.Parse(settings);

		Assert.Equal(new VirtualAirline("DVA", "DELTA", "Delta Virtual"), Assert.Single(result.Settings.VirtualAirlines));
		Assert.Empty(result.Messages);
	}

	// ---- virtual airlines: what makes one invalid ----

	[Theory]
	[InlineData("D1")]
	[InlineData("DVAX")]
	[InlineData("")]
	[InlineData(null)]
	public void a_virtual_airline_with_a_bad_designator_throws_naming_its_number(string? designator)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		AddVirtualAirline(settings, 3, designator, "DELTA", "Delta Virtual");

		ArgumentException ex = Assert.Throws<ArgumentException>(() => TelephonySettingsParser.Parse(settings));

		Assert.StartsWith("Virtual airline 3: ", ex.Message, StringComparison.Ordinal);
		Assert.Contains("3LD", ex.Message);
	}

	[Theory]
	[InlineData("!!")]
	[InlineData("")]
	[InlineData(null)]
	public void a_virtual_airline_with_no_letter_or_digit_in_its_telephony_throws_naming_its_number(string? telephony)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		AddVirtualAirline(settings, 4, "DVA", telephony, "Delta Virtual");

		ArgumentException ex = Assert.Throws<ArgumentException>(() => TelephonySettingsParser.Parse(settings));

		Assert.StartsWith("Virtual airline 4: ", ex.Message, StringComparison.Ordinal);
		Assert.Contains("telephony", ex.Message);
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData(null)]
	public void a_virtual_airline_with_no_organization_throws_naming_its_number(string? organization)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		AddVirtualAirline(settings, 5, "DVA", "DELTA", organization);

		ArgumentException ex = Assert.Throws<ArgumentException>(() => TelephonySettingsParser.Parse(settings));

		Assert.StartsWith("Virtual airline 5: ", ex.Message, StringComparison.Ordinal);
		Assert.Contains("organization", ex.Message);
	}

	/// <summary>A bad number stops the run even when the numbers before it are fine.</summary>
	[Fact]
	public void the_first_invalid_virtual_airline_in_number_order_is_the_one_named()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		AddVirtualAirline(settings, 1, "DVA", "DELTA", "Delta Virtual");
		AddVirtualAirline(settings, 7, "X1", "DELTA", "Late Virtual");
		AddVirtualAirline(settings, 6, "AAA", "!!", "Early Virtual");

		ArgumentException ex = Assert.Throws<ArgumentException>(() => TelephonySettingsParser.Parse(settings));

		Assert.StartsWith("Virtual airline 6: ", ex.Message, StringComparison.Ordinal);
	}

	// ---- virtual airlines: unknown fields and keys ----

	[Fact]
	public void an_unknown_virtual_airline_field_warns_naming_it_and_the_airline_is_still_read()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		AddVirtualAirline(settings, 1, "DVA", "DELTA", "Delta Virtual");
		settings["VirtualAirlines.1.Color"] = "red";

		TelephonySettingsParseResult result = TelephonySettingsParser.Parse(settings);

		string warning = Assert.Single(result.Messages.WarningTexts());
		Assert.Contains("VirtualAirlines.1.Color", warning);
		Assert.Single(result.Settings.VirtualAirlines);
	}

	[Theory]
	[InlineData("VirtualAirlines.x.Designator")]
	[InlineData("VirtualAirlines.0.Designator")]
	[InlineData("VirtualAirlines.-1.Designator")]
	[InlineData("VirtualAirlines.1")]
	[InlineData("VirtualAirlines.1.")]
	[InlineData("VirtualAirlines.1.Designator.Extra")]
	public void a_malformed_virtual_airline_key_is_an_unrecognized_setting_and_never_a_virtual_airline(string key)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings[key] = "DVA";

		TelephonySettingsParseResult result = TelephonySettingsParser.Parse(settings);

		Assert.Contains(result.Messages.WarningTexts(), w => w.Contains($"Unrecognized setting '{key}'"));
		Assert.Empty(result.Settings.VirtualAirlines);
	}

	// ---- virtual airlines: duplicates ----

	/// <summary>The card prints upper case, so a change of case alone is the same airline. The first number's values are kept as typed.</summary>
	[Fact]
	public void a_virtual_airline_listed_twice_ignoring_case_is_written_once_with_a_note_naming_both_numbers()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		AddVirtualAirline(settings, 2, "DVA", "DELTA", "Delta Virtual");
		AddVirtualAirline(settings, 5, "dva", "Delta", "DELTA VIRTUAL");

		TelephonySettingsParseResult result = TelephonySettingsParser.Parse(settings);

		Assert.Equal(new VirtualAirline("DVA", "DELTA", "Delta Virtual"), Assert.Single(result.Settings.VirtualAirlines));

		ServiceMessage note = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Info, note.Level);
		Assert.Contains("Virtual airline 5", note.Text);
		Assert.Contains("virtual airline 2", note.Text);
	}

	/// <summary>Two virtual airlines may share a 3LD and a telephony when their organizations differ - that is two cards under one command.</summary>
	[Fact]
	public void virtual_airlines_that_differ_only_in_organization_are_both_kept()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		AddVirtualAirline(settings, 1, "DVA", "DELTA", "Delta Virtual");
		AddVirtualAirline(settings, 2, "DVA", "DELTA", "Rustic Virtual");

		TelephonySettingsParseResult result = TelephonySettingsParser.Parse(settings);

		Assert.Equal(["Delta Virtual", "Rustic Virtual"], result.Settings.VirtualAirlines.Select(va => va.Organization));
		Assert.Empty(result.Messages);
	}

	// ---- VirtualAirlineProblem ----

	[Theory]
	[InlineData("D1", "DELTA", "Delta Virtual", "its 3LD must be three letters")]
	[InlineData("DVAX", "DELTA", "Delta Virtual", "its 3LD must be three letters")]
	[InlineData("", "DELTA", "Delta Virtual", "its 3LD must be three letters")]
	[InlineData(null, "DELTA", "Delta Virtual", "its 3LD must be three letters")]
	[InlineData("DVA", "!!", "Delta Virtual", "its telephony needs at least one letter or digit")]
	[InlineData("DVA", "", "Delta Virtual", "its telephony needs at least one letter or digit")]
	[InlineData("DVA", null, "Delta Virtual", "its telephony needs at least one letter or digit")]
	[InlineData("DVA", "DELTA", "", "it has no virtual organization")]
	[InlineData("DVA", "DELTA", "   ", "it has no virtual organization")]
	[InlineData("DVA", "DELTA", null, "it has no virtual organization")]
	[InlineData(null, null, null, "its 3LD must be three letters")]
	[InlineData("DVA", "DELTA", "Delta Virtual", null)]
	[InlineData(" dva ", " delta ", " Delta Virtual ", null)]
	public void virtual_airline_problem_says_what_stops_it_being_written_or_nothing(
		string? designator, string? telephony, string? organization, string? expected) =>
		Assert.Equal(expected, TelephonySettingsParser.VirtualAirlineProblem(designator, telephony, organization));
}
