using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Telephony;
using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Telephony.Models;

namespace FeBuddy.UnitTests.Application.Airac.Telephony;

/// <summary>
/// Runs the whole Telephony pipeline (<c>TelephonyService.Run</c>): the built/alias counts, the
/// exact Info summary text, the "no data" and "nothing to write" warnings, that settings errors and
/// builder messages both flow through, and the public overload that uses today's date - plus the
/// user's virtual airlines: counted apart from the FAA's operators, named in the summary only when
/// there are some, and never written when there is no telephony data.
/// </summary>
public sealed class TelephonyServiceTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_TelephonyService_" + Guid.NewGuid().ToString("N"));

	private static readonly DateOnly Today = new(2026, 9, 27);

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(_outputDirectory))
			{
				Directory.Delete(_outputDirectory, recursive: true);
			}
		}
		catch
		{
			// Best-effort.
		}
	}

	private Dictionary<string, string> Settings(params (string Key, string Value)[] overrides)
	{
		Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputDirectory"] = _outputDirectory,
		};

		foreach ((string key, string value) in overrides)
		{
			settings[key] = value;
		}

		return settings;
	}

	private static TelephonyHtmlDataModel.Assignment Assignment(string company, string country, string telephony, string designator) => new()
	{
		Company = company,
		Country = country,
		Telephony = telephony,
		ThreeLetterDesignator = designator,
	};

	private static TelephonyHtmlDataModel.SpecialCallSign SpecialCallSign(string telephony, string identifier, string agency, string expiration) => new()
	{
		Telephony = telephony,
		Identifier = identifier,
		Agency = agency,
		ExpirationDate = expiration,
	};

	private static TelephonyDataCollection Data(
		IEnumerable<TelephonyHtmlDataModel.Assignment>? assignments = null,
		IEnumerable<TelephonyHtmlDataModel.SpecialCallSign>? specialCallSigns = null) => new()
		{
			Assignments = [.. assignments ?? []],
			SpecialCallSigns = [.. specialCallSigns ?? []],
		};

	private static TelephonyDataCollection MixedScenario() => Data(
	[
		Assignment("AVIANCA S.A.", "COLOMBIA", "AVIANCA", "AVA"),
		Assignment("SOME COMPANY", "UNITED STATES", "SOME TELEPHONY", "..."), // no designator
		Assignment("OTHER COMPANY", "UNITED STATES", "", "XYZ"), // no telephony
	],
	[
		SpecialCallSign("NASA", "NASA", "NATIONAL AERONAUTICS AND SPACE ADMINISTRATION", "N/A"),
		SpecialCallSign("EXPIRED CALL", "EXP1", "SOME AGENCY", "1-Jan-2020"), // expired
	]);

	[Fact]
	public void run_counts_and_writes_the_alias_file()
	{
		TelephonyServiceResult result = TelephonyService.Run(MixedScenario(), Settings(), Today);

		Assert.Equal(1, result.IcaoAssignmentCount);
		Assert.Equal(1, result.SpecialCallSignCount);
		Assert.Equal(1, result.NoDesignatorCount);
		Assert.Equal(1, result.NoTelephonyCount);
		Assert.Equal(1, result.ExpiredCount);
		Assert.NotNull(result.AliasFilePath);
		Assert.Equal(3, result.AliasCommandCount); // .idAVA, .idAVIANCA, .idNASA
		Assert.Equal(0, result.MergedCommandCount);
		Assert.True(File.Exists(result.AliasFilePath));
	}

	[Fact]
	public void the_info_summary_message_has_the_exact_documented_text()
	{
		TelephonyServiceResult result = TelephonyService.Run(MixedScenario(), Settings(), Today);

		const string expected =
			"Telephony.txt: 3 command(s) for 1 ICAO operator(s) and 1 U.S. special call sign(s); 0 command(s) show more than one operator. " +
			"Left out: 1 row(s) with no designator, 1 with no telephony, 1 expired U.S. special call sign(s).";

		Assert.Contains(result.Messages, m => m.Level == LogLevel.Info && m.Text == expected);
	}

	[Fact]
	public void null_data_warns_and_writes_nothing_with_zero_counts()
	{
		TelephonyServiceResult result = TelephonyService.Run(null, Settings(), Today);

		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Warning, message.Level);
		Assert.Equal("There was no telephony data to build from, so Telephony.txt was not written.", message.Text);

		Assert.Null(result.AliasFilePath);
		Assert.Equal(0, result.IcaoAssignmentCount);
		Assert.Equal(0, result.SpecialCallSignCount);
		Assert.Equal(0, result.NoDesignatorCount);
		Assert.Equal(0, result.NoTelephonyCount);
		Assert.Equal(0, result.ExpiredCount);
		Assert.Equal(0, result.AliasCommandCount);
		Assert.Equal(0, result.MergedCommandCount);
	}

	[Fact]
	public void every_row_skipped_produces_an_advisory_warning_and_writes_nothing()
	{
		TelephonyDataCollection data = Data([Assignment("SOME COMPANY", "UNITED STATES", "SOME TELEPHONY", "...")]);

		TelephonyServiceResult result = TelephonyService.Run(data, Settings(), Today);

		Assert.Null(result.AliasFilePath);
		Assert.Equal(0, result.AliasCommandCount);
		Assert.Contains(result.Messages, m =>
			m.IsAdvisory && m.Text == "The FAA telephony pages had no operator with both a designator and a telephony, so Telephony.txt was not written.");
	}

	[Fact]
	public void invalid_settings_errors_propagate()
	{
		TelephonyDataCollection data = Data([Assignment("SOME COMPANY", "UNITED STATES", "SOME TELEPHONY", "ABC")]);

		Assert.Throws<ArgumentException>(() => TelephonyService.Run(data, new Dictionary<string, string>(), Today));
	}

	[Fact]
	public void builder_warnings_flow_into_the_result()
	{
		TelephonyDataCollection data = Data(specialCallSigns: [SpecialCallSign("AIR SIX", "ARSIX", "SOME AGENCY", "SOMEDAY")]);

		TelephonyServiceResult result = TelephonyService.Run(data, Settings(), Today);

		Assert.Contains(result.Messages, m => m.Text.Contains("SOMEDAY", StringComparison.Ordinal));
	}

	[Fact]
	public void run_rejects_a_null_settings_argument() =>
		Assert.Throws<ArgumentNullException>(() => TelephonyService.Run(Data(), null!));

	[Fact]
	public void the_public_overload_uses_todays_date_and_still_builds_and_writes()
	{
		TelephonyDataCollection data = Data([Assignment("AVIANCA S.A.", "COLOMBIA", "AVIANCA", "AVA")]);

		TelephonyServiceResult result = TelephonyService.Run(data, Settings());

		Assert.Equal(1, result.IcaoAssignmentCount);
		Assert.NotNull(result.AliasFilePath);
		Assert.True(File.Exists(result.AliasFilePath));
	}

	[Fact]
	public void a_renamed_alias_file_is_written_under_its_new_name()
	{
		OutputFileNames fileNames = new(new Dictionary<string, string> { ["Telephony.txt"] = "ZOB Telephony" });

		TelephonyServiceResult result = TelephonyService.Run(MixedScenario(), Settings(), Today, fileNames);

		Assert.Equal(Path.Combine(_outputDirectory, "Aliases", "ZOB Telephony.txt"), result.AliasFilePath);
		Assert.True(File.Exists(result.AliasFilePath!));
		Assert.False(File.Exists(Path.Combine(_outputDirectory, "Aliases", "Telephony.txt")));
		Assert.Contains(result.Messages, m => m.Level == LogLevel.Info && m.Text.StartsWith("ZOB Telephony.txt:", StringComparison.Ordinal));
	}

	// ---- virtual airlines ----

	/// <summary>Two virtual airlines, DVA (telephony DELTA) and DEV (telephony DEVIL AIR), as the settings block carries them.</summary>
	private Dictionary<string, string> SettingsWithTwoVirtualAirlines() => Settings(
		("VirtualAirlines.1.Designator", "DVA"),
		("VirtualAirlines.1.Telephony", "DELTA"),
		("VirtualAirlines.1.Organization", "Delta Virtual"),
		("VirtualAirlines.2.Designator", "DEV"),
		("VirtualAirlines.2.Telephony", "DEVIL AIR"),
		("VirtualAirlines.2.Organization", "Rustic Virtual"));

	/// <summary>Virtual airlines are counted on their own: they are neither ICAO operators nor U.S. special call signs.</summary>
	[Fact]
	public void virtual_airlines_are_counted_apart_from_icao_operators_and_special_call_signs()
	{
		TelephonyServiceResult result = TelephonyService.Run(MixedScenario(), SettingsWithTwoVirtualAirlines(), Today);

		Assert.Equal(1, result.IcaoAssignmentCount);
		Assert.Equal(1, result.SpecialCallSignCount);
		Assert.Equal(2, result.VirtualAirlineCount);
		Assert.Equal(1, result.NoDesignatorCount);
		Assert.Equal(1, result.NoTelephonyCount);
		Assert.Equal(1, result.ExpiredCount);
		Assert.Equal(7, result.AliasCommandCount); // .idAVA, .idAVIANCA, .idNASA, .idDVA, .idDELTA, .idDEV, .idDEVILAIR
		Assert.Equal(0, result.MergedCommandCount);

		string contents = File.ReadAllText(result.AliasFilePath!);
		Assert.Contains(@".idDVA .echo \n--VA--", contents);
		Assert.Contains(@".idDEVILAIR .echo \n--VA--", contents);
	}

	[Fact]
	public void the_info_summary_names_the_virtual_airlines_when_there_are_some()
	{
		TelephonyServiceResult result = TelephonyService.Run(MixedScenario(), SettingsWithTwoVirtualAirlines(), Today);

		const string expected =
			"Telephony.txt: 7 command(s) for 1 ICAO operator(s), 1 U.S. special call sign(s) and 2 virtual airline(s); " +
			"0 command(s) show more than one operator. " +
			"Left out: 1 row(s) with no designator, 1 with no telephony, 1 expired U.S. special call sign(s).";

		Assert.Contains(result.Messages, m => m.Level == LogLevel.Info && m.Text == expected);
	}

	[Fact]
	public void with_no_virtual_airlines_the_count_is_zero_and_the_summary_does_not_mention_them()
	{
		TelephonyServiceResult result = TelephonyService.Run(MixedScenario(), Settings(), Today);

		Assert.Equal(0, result.VirtualAirlineCount);

		ServiceMessage summary = Assert.Single(result.Messages, m => m.Level == LogLevel.Info);
		Assert.StartsWith("Telephony.txt: 3 command(s) for 1 ICAO operator(s) and 1 U.S. special call sign(s); ", summary.Text, StringComparison.Ordinal);
		Assert.DoesNotContain("virtual airline", summary.Text);
	}

	/// <summary>The FAA's data is what the file is built from: without a copy of it, nothing is written - virtual airlines included.</summary>
	[Fact]
	public void with_no_telephony_data_virtual_airlines_are_not_written_either()
	{
		TelephonyServiceResult result = TelephonyService.Run(null, SettingsWithTwoVirtualAirlines(), Today);

		Assert.Null(result.AliasFilePath);
		Assert.Equal(0, result.VirtualAirlineCount);
		Assert.Equal(0, result.AliasCommandCount);
		Assert.False(Directory.Exists(_outputDirectory));

		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Warning, message.Level);
	}

	/// <summary>Virtual airlines are entries too, so a register with nothing usable still gives a file when some are listed.</summary>
	[Fact]
	public void virtual_airlines_alone_still_write_the_alias_file()
	{
		TelephonyServiceResult result = TelephonyService.Run(Data(), SettingsWithTwoVirtualAirlines(), Today);

		Assert.Equal(0, result.IcaoAssignmentCount);
		Assert.Equal(0, result.SpecialCallSignCount);
		Assert.Equal(2, result.VirtualAirlineCount);
		Assert.NotNull(result.AliasFilePath);
		Assert.Equal(4, result.AliasCommandCount); // .idDVA, .idDELTA, .idDEV, .idDEVILAIR
		Assert.DoesNotContain(result.Messages, m => m.IsAdvisory);
	}

	[Fact]
	public void a_virtual_airline_listed_twice_is_counted_and_written_once_with_a_note_in_the_result()
	{
		TelephonyServiceResult result = TelephonyService.Run(
			Data(),
			Settings(
				("VirtualAirlines.1.Designator", "DVA"),
				("VirtualAirlines.1.Telephony", "DELTA"),
				("VirtualAirlines.1.Organization", "Delta Virtual"),
				("VirtualAirlines.2.Designator", "dva"),
				("VirtualAirlines.2.Telephony", "delta"),
				("VirtualAirlines.2.Organization", "DELTA VIRTUAL")),
			Today);

		Assert.Equal(1, result.VirtualAirlineCount);
		Assert.Equal(2, result.AliasCommandCount); // .idDVA, .idDELTA
		Assert.Contains(result.Messages, m => m.Level == LogLevel.Info && m.Text.Contains("written once", StringComparison.Ordinal));
	}

	[Fact]
	public void a_virtual_airline_that_cannot_be_written_stops_the_run_before_anything_is_written()
	{
		Dictionary<string, string> settings = Settings(
			("VirtualAirlines.1.Designator", "D1"),
			("VirtualAirlines.1.Telephony", "DELTA"),
			("VirtualAirlines.1.Organization", "Delta Virtual"));

		ArgumentException ex = Assert.Throws<ArgumentException>(() => TelephonyService.Run(MixedScenario(), settings, Today));

		Assert.StartsWith("Virtual airline 1: ", ex.Message, StringComparison.Ordinal);
		Assert.False(Directory.Exists(_outputDirectory));
	}
}
