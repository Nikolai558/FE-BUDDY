using System.Text;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.SharedData.Models;
using FeBuddy.Core.Infrastructure.Telephony.Models;
using FeBuddy.Core.Infrastructure.WxStations.Models;

namespace FeBuddy.UnitTests.Application.Airac;

/// <summary>
/// Exercises <see cref="AiracSharedDataLoader"/>'s internal overloads with fake refresh functions,
/// a fixed clock and small real files on disk: the fresh/stale/no-copy/unreadable-copy message and
/// data shapes for both Wx Stations (required) and Telephony (register required, special call
/// signs and the virtual airline list optional - the list only when included, each of its two parts
/// falling back on its own, the older single copy only while neither has one), and
/// <see cref="AiracSharedDataLoader.DescribeAge"/>'s boundaries.
/// </summary>
public sealed class AiracSharedDataLoaderTests : IDisposable
{
	private static readonly DateTime NowUtc = new(2026, 9, 27, 12, 0, 0, DateTimeKind.Utc);

	private const string ValidStationsXml =
		"<response><data num_results=\"1\"><Station><icao_id>KDTW</icao_id></Station></data></response>";

	private const string UnparsableXml = "<foo></foo>";

	private const string ValidRegisterHtml =
		"<table><thead><tr><th>Company</th><th>Country</th><th>Telephony</th><th>3&#x2010;Ltr</th></tr></thead>" +
		"<tbody><tr><td>AVIANCA</td><td>COLOMBIA</td><td>AVIANCA</td><td>AVA</td></tr></tbody></table>";

	private const string ValidSpecialCallSignsHtml =
		"<table><thead><tr><th>Telephony/Call Sign</th><th>Identifier</th><th>Company or Operating Agency</th><th>Expiration Date</th></tr></thead>" +
		"<tbody><tr><td>AIR SIX</td><td>ARSIX</td><td>Some Agency</td><td>N/A</td></tr></tbody></table>";

	private const string UnparsableHtml = "<html><body>no tables here</body></html>";

	private readonly string _testRoot =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_SharedDataLoader_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(_testRoot))
			{
				Directory.Delete(_testRoot, recursive: true);
			}
		}
		catch
		{
			// Best-effort.
		}
	}

	private string WriteFile(string fileName, string content)
	{
		Directory.CreateDirectory(_testRoot);
		string path = Path.Combine(_testRoot, fileName);
		File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		return path;
	}

	private static Func<CancellationToken, Task<SharedDataRefreshResult>> Refresh(SharedDataRefreshResult result) =>
		_ => Task.FromResult(result);

	private static Func<CancellationToken, Task<TelephonyRefreshResult>> Refresh(TelephonyRefreshResult result) =>
		_ => Task.FromResult(result);

	// ---- Wx Stations: fresh ----

	[Fact]
	public async Task load_wx_stations_async_when_fresh_returns_the_parsed_data_and_an_info_message()
	{
		string path = WriteFile("stations.xml", ValidStationsXml);

		AiracSharedDataLoadResult<WxStationDataCollection> result = await AiracSharedDataLoader.LoadWxStationsAsync(
			Refresh(new SharedDataRefreshResult(path, NowUtc, FailureReason: null)), NowUtc, CancellationToken.None);

		Assert.NotNull(result.Data);
		Assert.Equal("KDTW", Assert.Single(result.Data!.Stations).IcaoId);

		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Info, message.Level);
		Assert.False(message.IsAdvisory);
		Assert.Equal("Downloaded the latest Wx station data.", message.Text);
	}

	// ---- Wx Stations: stale ----

	[Fact]
	public async Task load_wx_stations_async_when_stale_returns_the_parsed_data_and_an_advisory_warning_with_the_reason_date_and_age()
	{
		string path = WriteFile("stations.xml", ValidStationsXml);
		DateTime downloadedUtc = NowUtc.AddDays(-3);

		AiracSharedDataLoadResult<WxStationDataCollection> result = await AiracSharedDataLoader.LoadWxStationsAsync(
			Refresh(new SharedDataRefreshResult(path, downloadedUtc, "network down")), NowUtc, CancellationToken.None);

		Assert.NotNull(result.Data);

		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Warning, message.Level);
		Assert.True(message.IsAdvisory);
		Assert.Contains("network down", message.Text, StringComparison.Ordinal);
		Assert.Contains(downloadedUtc.ToLocalTime().ToString("d MMM yyyy"), message.Text, StringComparison.Ordinal);
		Assert.Contains("3 days old", message.Text, StringComparison.Ordinal);
	}

	// ---- Wx Stations: no copy ----

	[Fact]
	public async Task load_wx_stations_async_with_no_copy_returns_null_and_an_error()
	{
		AiracSharedDataLoadResult<WxStationDataCollection> result = await AiracSharedDataLoader.LoadWxStationsAsync(
			Refresh(new SharedDataRefreshResult(FilePath: null, DownloadedUtc: null, "network down")), NowUtc, CancellationToken.None);

		Assert.Null(result.Data);

		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Error, message.Level);
		Assert.Contains("Wx Stations wrote nothing", message.Text, StringComparison.Ordinal);
	}

	// ---- Wx Stations: unreadable kept copy ----

	[Fact]
	public async Task load_wx_stations_async_with_an_unreadable_kept_copy_returns_null_and_an_error()
	{
		string path = WriteFile("stations.xml", UnparsableXml);

		AiracSharedDataLoadResult<WxStationDataCollection> result = await AiracSharedDataLoader.LoadWxStationsAsync(
			Refresh(new SharedDataRefreshResult(path, NowUtc, FailureReason: null)), NowUtc, CancellationToken.None);

		Assert.Null(result.Data);
		Assert.Equal(2, result.Messages.Count);
		Assert.Equal(LogLevel.Info, result.Messages[0].Level);

		ServiceMessage error = result.Messages[1];
		Assert.Equal(LogLevel.Error, error.Level);
		Assert.Contains("can't be read", error.Text, StringComparison.Ordinal);
		Assert.Contains("Wx Stations wrote nothing", error.Text, StringComparison.Ordinal);
	}

	// ---- DescribeAge boundaries ----

	[Theory]
	[InlineData(0.0, "less than a day old")]
	[InlineData(23.0, "less than a day old")]
	[InlineData(24.0, "1 day old")]
	[InlineData(47.0, "1 day old")]
	[InlineData(15.0 * 24, "15 days old")]
	public void describe_age_returns_the_expected_text_at_each_boundary(double hours, string expected) =>
		Assert.Equal(expected, AiracSharedDataLoader.DescribeAge(TimeSpan.FromHours(hours)));

	// ---- Telephony: both pages fresh ----

	[Fact]
	public async Task load_telephony_async_when_both_pages_are_fresh_returns_data_and_two_info_messages()
	{
		string registerPath = WriteFile("register.html", ValidRegisterHtml);
		string specialPath = WriteFile("special.html", ValidSpecialCallSignsHtml);

		TelephonyRefreshResult refreshed = new(
			new SharedDataRefreshResult(registerPath, NowUtc, FailureReason: null),
			new SharedDataRefreshResult(specialPath, NowUtc, FailureReason: null));

		AiracSharedDataLoadResult<TelephonyDataCollection> result = await AiracSharedDataLoader.LoadTelephonyAsync(
			Refresh(refreshed), NowUtc, CancellationToken.None);

		Assert.NotNull(result.Data);
		Assert.Single(result.Data!.Assignments);
		Assert.Single(result.Data.SpecialCallSigns);

		Assert.Equal(2, result.Messages.Count);
		Assert.All(result.Messages, m => Assert.Equal(LogLevel.Info, m.Level));
		Assert.Equal("Downloaded the latest FAA telephony register.", result.Messages[0].Text);
		Assert.Equal("Downloaded the latest FAA U.S. special call signs.", result.Messages[1].Text);
	}

	// ---- Telephony: the virtual airline list (GNG + VATSIM-Radar) ----

	/// <summary>GNG's list: 100 rows (the fewest a copy may have), the first two DAL and SKA, then fillers.</summary>
	private static string GngJson()
	{
		IEnumerable<string> rows = new[]
			{
				"""{ "icao": "DAL", "airline": "GNG DELTA", "callsign": "GNGDELTA" }""",
				"""{ "icao": "SKA", "airline": "SKY AIR", "callsign": "SKYAIR" }""",
			}
			.Concat(Enumerable.Range(0, 98).Select(i => $$"""{ "icao": "Q{{i:00}}", "airline": "FILLER {{i}}", "callsign": "FILLER{{i}}" }"""));

		return $$"""{ "records": 100, "page": 1, "total": 1, "rows": [{{string.Join(",", rows)}}] }""";
	}

	private const string VatsimRadarJson =
		"""[{ "icao": "DAL", "name": "Fly Delta Virtual", "callsign": "Delta", "virtual": true }, { "icao": "OCN", "name": "vOCN", "callsign": "Ocean", "virtual": true }]""";

	private const string OlderCopyJson =
		"""{ "airlines": [], "virtual": [{ "icao": "WAT", "name": "Walker Air", "callsign": "Walker", "virtual": true }] }""";

	private TelephonyRefreshResult FreshPages() => new(
		new SharedDataRefreshResult(WriteFile("register.html", ValidRegisterHtml), NowUtc, FailureReason: null),
		new SharedDataRefreshResult(WriteFile("special.html", ValidSpecialCallSignsHtml), NowUtc, FailureReason: null));

	private static Func<CancellationToken, Task<VirtualAirlineListRefreshResult>> Refresh(VirtualAirlineListRefreshResult result) =>
		_ => Task.FromResult(result);

	private SharedDataRefreshResult FreshGng() => new(WriteFile("gng.json", GngJson()), NowUtc, FailureReason: null);

	private SharedDataRefreshResult FreshVatsimRadar() => new(WriteFile("vatsim_radar.json", VatsimRadarJson), NowUtc, FailureReason: null);

	/// <summary>Both parts fresh: merged - VATSIM-Radar's DAL takes over GNG's, OCN is added - with an Info message for each.</summary>
	[Fact]
	public async Task the_list_when_included_and_fresh_is_both_parts_merged_with_an_info_message_each()
	{
		AiracSharedDataLoadResult<TelephonyDataCollection> result = await AiracSharedDataLoader.LoadTelephonyAsync(
			Refresh(FreshPages()), Refresh(new VirtualAirlineListRefreshResult(FreshGng(), FreshVatsimRadar())), NowUtc, CancellationToken.None);

		List<VatsimRadarAirline> list = result.Data!.VatsimRadarAirlines;
		Assert.Equal(101, list.Count);
		Assert.Equal(new VatsimRadarAirline("DAL", "Fly Delta Virtual", "Delta"), list[0]);
		Assert.Equal(new VatsimRadarAirline("OCN", "vOCN", "Ocean"), list[^1]);

		Assert.Equal(4, result.Messages.Count);
		Assert.Equal("Downloaded the latest GNG fictional airline list.", result.Messages[2].Text);
		Assert.Equal("Downloaded the latest VATSIM-Radar airline list.", result.Messages[3].Text);
		Assert.All(result.Messages, m => Assert.Equal(LogLevel.Info, m.Level));
	}

	/// <summary>A copy under a day old is used without downloading, and the run says so - not "Downloaded the latest".</summary>
	[Fact]
	public async Task a_part_used_without_downloading_says_so()
	{
		DateTime keptUtc = NowUtc.AddHours(-3);
		SharedDataRefreshResult reused = new(WriteFile("gng.json", GngJson()), keptUtc, FailureReason: null) { Reused = true };

		AiracSharedDataLoadResult<TelephonyDataCollection> result = await AiracSharedDataLoader.LoadTelephonyAsync(
			Refresh(FreshPages()), Refresh(new VirtualAirlineListRefreshResult(reused, FreshVatsimRadar())), NowUtc, CancellationToken.None);

		Assert.Equal(
			$"Used FE-Buddy's copy of the GNG fictional airline list from {keptUtc.ToLocalTime():d MMM yyyy HH:mm}: it's less than a day old, so it wasn't downloaded again.",
			result.Messages[2].Text);
		Assert.Equal(LogLevel.Info, result.Messages[2].Level);
		Assert.Equal(101, result.Data!.VatsimRadarAirlines.Count);
	}

	[Fact]
	public async Task the_list_when_not_included_is_not_there_and_says_nothing()
	{
		AiracSharedDataLoadResult<TelephonyDataCollection> result = await AiracSharedDataLoader.LoadTelephonyAsync(
			Refresh(FreshPages()), refreshVirtualAirlineList: null, NowUtc, CancellationToken.None);

		Assert.Empty(result.Data!.VatsimRadarAirlines);
		Assert.Equal(2, result.Messages.Count);
	}

	/// <summary>Each part falls back on its own last good copy: a failed download keeps the other part's fresh one.</summary>
	[Fact]
	public async Task a_part_that_fails_uses_its_last_good_copy_beside_the_others_fresh_one()
	{
		SharedDataRefreshResult stale = new(WriteFile("vatsim_radar.json", VatsimRadarJson), NowUtc.AddDays(-4), "The SSL connection could not be established: TLS alert");

		AiracSharedDataLoadResult<TelephonyDataCollection> result = await AiracSharedDataLoader.LoadTelephonyAsync(
			Refresh(FreshPages()), Refresh(new VirtualAirlineListRefreshResult(FreshGng(), stale)), NowUtc, CancellationToken.None);

		Assert.Equal(101, result.Data!.VatsimRadarAirlines.Count);
		ServiceMessage warning = result.Messages[3];
		Assert.Equal(LogLevel.Warning, warning.Level);
		Assert.True(warning.IsAdvisory);
		Assert.Contains("VATSIM-Radar airline list (The SSL connection could not be established: TLS alert)", warning.Text, StringComparison.Ordinal);
		Assert.Contains("4 days old", warning.Text, StringComparison.Ordinal);
	}

	/// <summary>A part with no copy at all leaves the list to the other part alone, with a warning saying so.</summary>
	[Fact]
	public async Task a_part_with_no_copy_leaves_the_other_alone_with_a_warning()
	{
		AiracSharedDataLoadResult<TelephonyDataCollection> result = await AiracSharedDataLoader.LoadTelephonyAsync(
			Refresh(FreshPages()),
			Refresh(new VirtualAirlineListRefreshResult(new SharedDataRefreshResult(null, null, "GNG is down"), FreshVatsimRadar())),
			NowUtc,
			CancellationToken.None);

		Assert.Equal(["DAL", "OCN"], result.Data!.VatsimRadarAirlines.Select(a => a.Icao));

		ServiceMessage warning = result.Messages[2];
		Assert.Equal(LogLevel.Warning, warning.Level);
		Assert.True(warning.IsAdvisory);
		Assert.Contains("GNG is down", warning.Text, StringComparison.Ordinal);
		Assert.Contains("the virtual airline list has only the VATSIM-Radar airline list's airlines", warning.Text, StringComparison.Ordinal);
	}

	/// <summary>The list is optional, like the U.S. special call signs: with no copy of anything the run goes on, with advisory warnings.</summary>
	[Fact]
	public async Task the_list_with_no_copy_of_either_part_is_left_out_and_the_run_goes_on()
	{
		AiracSharedDataLoadResult<TelephonyDataCollection> result = await AiracSharedDataLoader.LoadTelephonyAsync(
			Refresh(FreshPages()),
			Refresh(new VirtualAirlineListRefreshResult(new SharedDataRefreshResult(null, null, "GNG is down"), new SharedDataRefreshResult(null, null, "GitHub is down"))),
			NowUtc,
			CancellationToken.None);

		Assert.NotNull(result.Data);
		Assert.Single(result.Data!.Assignments);
		Assert.Empty(result.Data.VatsimRadarAirlines);
		Assert.Equal(4, result.Messages.Count);
		Assert.All(result.Messages.Skip(2), m => Assert.True(m.IsAdvisory));
		Assert.Contains("Telephony.txt leaves the virtual airline list out", result.Messages[3].Text, StringComparison.Ordinal);
	}

	/// <summary>The older single copy is the last resort: used only while neither part has a copy.</summary>
	[Fact]
	public async Task the_older_copy_is_used_only_while_neither_part_has_a_copy()
	{
		string olderPath = WriteFile("vatsim_radar_airlines.json", OlderCopyJson);
		SharedDataRefreshResult noGng = new(null, null, "GNG is down");
		SharedDataRefreshResult noVatsimRadar = new(null, null, "GitHub is down");

		AiracSharedDataLoadResult<TelephonyDataCollection> result = await AiracSharedDataLoader.LoadTelephonyAsync(
			Refresh(FreshPages()),
			Refresh(new VirtualAirlineListRefreshResult(noGng, noVatsimRadar, olderPath, NowUtc.AddDays(-2))),
			NowUtc,
			CancellationToken.None);

		Assert.Equal(new VatsimRadarAirline("WAT", "Walker Air", "Walker"), Assert.Single(result.Data!.VatsimRadarAirlines));
		Assert.Contains("this run used FE-Buddy's older copy of the whole list", result.Messages[2].Text, StringComparison.Ordinal);

		ServiceMessage older = result.Messages[^1];
		Assert.Equal(LogLevel.Warning, older.Level);
		Assert.True(older.IsAdvisory);
		Assert.StartsWith("FE-Buddy has no copy of either part of the virtual airline list yet, so this run used its older copy of the whole list from ", older.Text, StringComparison.Ordinal);
		Assert.Contains("(2 days old)", older.Text, StringComparison.Ordinal);
	}

	[Fact]
	public async Task a_part_whose_copy_cannot_be_read_is_left_out_with_an_error()
	{
		string unreadable = WriteFile("gng.json", UnparsableHtml);

		AiracSharedDataLoadResult<TelephonyDataCollection> result = await AiracSharedDataLoader.LoadTelephonyAsync(
			Refresh(FreshPages()),
			Refresh(new VirtualAirlineListRefreshResult(new SharedDataRefreshResult(unreadable, NowUtc.AddDays(-2), "GNG is down"), FreshVatsimRadar())),
			NowUtc,
			CancellationToken.None);

		Assert.Equal(2, result.Data!.VatsimRadarAirlines.Count);
		Assert.Equal(5, result.Messages.Count);
		ServiceMessage error = result.Messages[4];
		Assert.Equal(LogLevel.Error, error.Level);
		Assert.StartsWith("FE-Buddy's copy of the GNG fictional airline list can't be read", error.Text, StringComparison.Ordinal);
		Assert.EndsWith("so the virtual airline list leaves its airlines out.", error.Text, StringComparison.Ordinal);
	}

	[Fact]
	public async Task an_older_copy_that_cannot_be_read_is_left_out_with_an_error()
	{
		string olderPath = WriteFile("vatsim_radar_airlines.json", UnparsableHtml);

		AiracSharedDataLoadResult<TelephonyDataCollection> result = await AiracSharedDataLoader.LoadTelephonyAsync(
			Refresh(FreshPages()),
			Refresh(new VirtualAirlineListRefreshResult(new SharedDataRefreshResult(null, null, "down"), new SharedDataRefreshResult(null, null, "down"), olderPath)),
			NowUtc,
			CancellationToken.None);

		Assert.Empty(result.Data!.VatsimRadarAirlines);
		Assert.Equal(LogLevel.Error, result.Messages[^1].Level);
		Assert.Contains("Telephony.txt leaves the virtual airline list out", result.Messages[^1].Text, StringComparison.Ordinal);
	}

	[Fact]
	public async Task with_no_register_the_list_is_not_downloaded_at_all()
	{
		bool asked = false;
		TelephonyRefreshResult refreshed = new(
			new SharedDataRefreshResult(null, null, "register download failed"),
			new SharedDataRefreshResult(null, null, "special call signs download failed"));

		AiracSharedDataLoadResult<TelephonyDataCollection> result = await AiracSharedDataLoader.LoadTelephonyAsync(
			Refresh(refreshed),
			_ =>
			{
				asked = true;
				return Task.FromResult(new VirtualAirlineListRefreshResult(new SharedDataRefreshResult(null, null, "unused"), new SharedDataRefreshResult(null, null, "unused")));
			},
			NowUtc,
			CancellationToken.None);

		Assert.Null(result.Data);
		Assert.False(asked);
	}

	// ---- Telephony: both pages stale ----

	[Fact]
	public async Task load_telephony_async_when_both_pages_are_stale_returns_data_and_two_advisory_warnings()
	{
		string registerPath = WriteFile("register.html", ValidRegisterHtml);
		string specialPath = WriteFile("special.html", ValidSpecialCallSignsHtml);
		DateTime downloadedUtc = NowUtc.AddDays(-1);

		TelephonyRefreshResult refreshed = new(
			new SharedDataRefreshResult(registerPath, downloadedUtc, "register download failed"),
			new SharedDataRefreshResult(specialPath, downloadedUtc, "special call signs download failed"));

		AiracSharedDataLoadResult<TelephonyDataCollection> result = await AiracSharedDataLoader.LoadTelephonyAsync(
			Refresh(refreshed), NowUtc, CancellationToken.None);

		Assert.NotNull(result.Data);
		Assert.Single(result.Data!.Assignments);
		Assert.Single(result.Data.SpecialCallSigns);

		Assert.Equal(2, result.Messages.Count);
		Assert.All(result.Messages, m => Assert.Equal(LogLevel.Warning, m.Level));
		Assert.All(result.Messages, m => Assert.True(m.IsAdvisory));
		Assert.Contains("register download failed", result.Messages[0].Text, StringComparison.Ordinal);
		Assert.Contains("special call signs download failed", result.Messages[1].Text, StringComparison.Ordinal);
		Assert.Contains("1 day old", result.Messages[0].Text, StringComparison.Ordinal);
	}

	// ---- Telephony: register has no copy (required) ----

	[Fact]
	public async Task load_telephony_async_when_the_register_has_no_copy_returns_null_and_an_error()
	{
		string specialPath = WriteFile("special.html", ValidSpecialCallSignsHtml);

		TelephonyRefreshResult refreshed = new(
			new SharedDataRefreshResult(FilePath: null, DownloadedUtc: null, "network down"),
			new SharedDataRefreshResult(specialPath, NowUtc, FailureReason: null));

		AiracSharedDataLoadResult<TelephonyDataCollection> result = await AiracSharedDataLoader.LoadTelephonyAsync(
			Refresh(refreshed), NowUtc, CancellationToken.None);

		Assert.Null(result.Data);

		ServiceMessage registerMessage = result.Messages[0];
		Assert.Equal(LogLevel.Error, registerMessage.Level);
		Assert.Contains("Telephony wrote nothing", registerMessage.Text, StringComparison.Ordinal);
	}

	// ---- Telephony: special call signs have no copy (optional) ----

	[Fact]
	public async Task load_telephony_async_when_the_special_call_signs_have_no_copy_returns_data_without_them_and_an_advisory_warning()
	{
		string registerPath = WriteFile("register.html", ValidRegisterHtml);

		TelephonyRefreshResult refreshed = new(
			new SharedDataRefreshResult(registerPath, NowUtc, FailureReason: null),
			new SharedDataRefreshResult(FilePath: null, DownloadedUtc: null, "network down"));

		AiracSharedDataLoadResult<TelephonyDataCollection> result = await AiracSharedDataLoader.LoadTelephonyAsync(
			Refresh(refreshed), NowUtc, CancellationToken.None);

		Assert.NotNull(result.Data);
		Assert.Single(result.Data!.Assignments);
		Assert.Empty(result.Data.SpecialCallSigns);

		ServiceMessage specialMessage = result.Messages[1];
		Assert.Equal(LogLevel.Warning, specialMessage.Level);
		Assert.True(specialMessage.IsAdvisory);
		Assert.Contains("Telephony.txt leaves them out", specialMessage.Text, StringComparison.Ordinal);
	}

	// ---- Telephony: unreadable kept copies ----

	[Fact]
	public async Task load_telephony_async_when_the_register_copy_is_unreadable_returns_null_and_an_error()
	{
		string registerPath = WriteFile("register.html", UnparsableHtml);
		string specialPath = WriteFile("special.html", ValidSpecialCallSignsHtml);

		TelephonyRefreshResult refreshed = new(
			new SharedDataRefreshResult(registerPath, NowUtc, FailureReason: null),
			new SharedDataRefreshResult(specialPath, NowUtc, FailureReason: null));

		AiracSharedDataLoadResult<TelephonyDataCollection> result = await AiracSharedDataLoader.LoadTelephonyAsync(
			Refresh(refreshed), NowUtc, CancellationToken.None);

		Assert.Null(result.Data);
		Assert.Equal(3, result.Messages.Count);

		ServiceMessage error = result.Messages[2];
		Assert.Equal(LogLevel.Error, error.Level);
		Assert.Contains("can't be read", error.Text, StringComparison.Ordinal);
		Assert.Contains("Telephony wrote nothing", error.Text, StringComparison.Ordinal);
	}

	[Fact]
	public async Task load_telephony_async_when_the_special_call_signs_copy_is_unreadable_keeps_the_register_data_and_adds_an_error()
	{
		string registerPath = WriteFile("register.html", ValidRegisterHtml);
		string specialPath = WriteFile("special.html", UnparsableHtml);

		TelephonyRefreshResult refreshed = new(
			new SharedDataRefreshResult(registerPath, NowUtc, FailureReason: null),
			new SharedDataRefreshResult(specialPath, NowUtc, FailureReason: null));

		AiracSharedDataLoadResult<TelephonyDataCollection> result = await AiracSharedDataLoader.LoadTelephonyAsync(
			Refresh(refreshed), NowUtc, CancellationToken.None);

		Assert.NotNull(result.Data);
		Assert.Single(result.Data!.Assignments);
		Assert.Empty(result.Data.SpecialCallSigns);

		Assert.Equal(3, result.Messages.Count);
		ServiceMessage error = result.Messages[2];
		Assert.Equal(LogLevel.Error, error.Level);
		Assert.Contains("can't be read", error.Text, StringComparison.Ordinal);
		Assert.Contains("Telephony.txt leaves them out", error.Text, StringComparison.Ordinal);
	}
}
