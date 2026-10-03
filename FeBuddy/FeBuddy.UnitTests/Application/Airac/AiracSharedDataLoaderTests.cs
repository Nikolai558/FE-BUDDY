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
/// signs and the VATSIM-Radar Virtual Airline List optional - the list only when included), and
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

	// ---- Telephony: the VATSIM-Radar Virtual Airline List ----

	private const string ValidVatsimRadarJson =
		"""[{ "icao": "DAL", "name": "Fly Delta Virtual", "callsign": "Delta", "virtual": true }]""";

	private TelephonyRefreshResult FreshPages() => new(
		new SharedDataRefreshResult(WriteFile("register.html", ValidRegisterHtml), NowUtc, FailureReason: null),
		new SharedDataRefreshResult(WriteFile("special.html", ValidSpecialCallSignsHtml), NowUtc, FailureReason: null));

	[Fact]
	public async Task the_vatsim_radar_list_when_included_and_fresh_is_added_with_an_info_message()
	{
		string listPath = WriteFile("airlines.json", ValidVatsimRadarJson);

		AiracSharedDataLoadResult<TelephonyDataCollection> result = await AiracSharedDataLoader.LoadTelephonyAsync(
			Refresh(FreshPages()), Refresh(new SharedDataRefreshResult(listPath, NowUtc, FailureReason: null)), NowUtc, CancellationToken.None);

		Assert.Equal(new VatsimRadarAirline("DAL", "Fly Delta Virtual", "Delta"), Assert.Single(result.Data!.VatsimRadarAirlines));
		Assert.Equal(3, result.Messages.Count);
		Assert.Equal("Downloaded the latest VATSIM-Radar Virtual Airline List.", result.Messages[2].Text);
		Assert.Equal(LogLevel.Info, result.Messages[2].Level);
	}

	[Fact]
	public async Task the_vatsim_radar_list_when_not_included_is_not_there_and_says_nothing()
	{
		AiracSharedDataLoadResult<TelephonyDataCollection> result = await AiracSharedDataLoader.LoadTelephonyAsync(
			Refresh(FreshPages()), refreshVatsimRadar: null, NowUtc, CancellationToken.None);

		Assert.Empty(result.Data!.VatsimRadarAirlines);
		Assert.Equal(2, result.Messages.Count);
	}

	/// <summary>The list is optional, like the U.S. special call signs: without it the run goes on, with an advisory warning.</summary>
	[Fact]
	public async Task the_vatsim_radar_list_with_no_copy_is_left_out_with_an_advisory_warning_and_the_run_goes_on()
	{
		AiracSharedDataLoadResult<TelephonyDataCollection> result = await AiracSharedDataLoader.LoadTelephonyAsync(
			Refresh(FreshPages()), Refresh(new SharedDataRefreshResult(null, null, "list download failed")), NowUtc, CancellationToken.None);

		Assert.NotNull(result.Data);
		Assert.Single(result.Data!.Assignments);
		Assert.Empty(result.Data.VatsimRadarAirlines);

		ServiceMessage warning = result.Messages[2];
		Assert.Equal(LogLevel.Warning, warning.Level);
		Assert.True(warning.IsAdvisory);
		Assert.Contains("list download failed", warning.Text, StringComparison.Ordinal);
		Assert.Contains("Telephony.txt leaves it out", warning.Text, StringComparison.Ordinal);
	}

	[Fact]
	public async Task a_vatsim_radar_copy_that_cannot_be_read_is_left_out_with_an_error()
	{
		string listPath = WriteFile("airlines.json", UnparsableHtml);

		AiracSharedDataLoadResult<TelephonyDataCollection> result = await AiracSharedDataLoader.LoadTelephonyAsync(
			Refresh(FreshPages()), Refresh(new SharedDataRefreshResult(listPath, NowUtc.AddDays(-2), "list download failed")), NowUtc, CancellationToken.None);

		Assert.Empty(result.Data!.VatsimRadarAirlines);
		Assert.Equal(4, result.Messages.Count);
		Assert.True(result.Messages[2].IsAdvisory);
		Assert.Equal(LogLevel.Error, result.Messages[3].Level);
		Assert.Contains("VATSIM-Radar Virtual Airline List can't be read", result.Messages[3].Text, StringComparison.Ordinal);
	}

	[Fact]
	public async Task with_no_register_the_vatsim_radar_list_is_not_downloaded_at_all()
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
				return Task.FromResult(new SharedDataRefreshResult(null, null, "unused"));
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
