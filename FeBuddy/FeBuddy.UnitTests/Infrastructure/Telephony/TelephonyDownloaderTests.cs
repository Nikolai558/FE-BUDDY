using System.Net;
using System.Text;

using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Telephony;
using FeBuddy.Core.Infrastructure.Telephony.Models;

namespace FeBuddy.UnitTests.Infrastructure.Telephony;

/// <summary>
/// Exercises the real download -&gt; validate -&gt; place mechanics of <see cref="TelephonyDownloader"/>
/// against local <see cref="HttpListener"/>s serving synthetic payloads, using
/// <see cref="TelephonyDownloader.RefreshFromUrlsAsync"/> - the same code path the real
/// <see cref="TelephonyDownloader.RefreshAsync"/> uses, with only the URLs and kept-copy paths
/// swapped out. The register and U.S. special call signs pages are independent: one page's success
/// or failure never affects the other's kept copy. The virtual airline list's two parts go the same
/// way through <see cref="TelephonyDownloader.RefreshVirtualAirlineListFromUrlsAsync"/>, each checked,
/// kept and fallen back on by itself, downloaded at most once a day during a run.
/// </summary>
[Collection("AppLog")]
public sealed class TelephonyDownloaderTests : IDisposable
{
	private readonly string _testRoot =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_TelephonyDl_" + Guid.NewGuid().ToString("N"));

	public TelephonyDownloaderTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_testRoot, "logs"));
		TempWorkspace.ConfigureForTesting(Path.Combine(_testRoot, "temp"));
	}

	public void Dispose()
	{
		TempWorkspace.ConfigureForTesting(null);
		AppLog.ConfigureForTesting(null);

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

	private const string ValidRegisterHtml =
		"<table><thead><tr><th>Company</th><th>Country</th><th>Telephony</th><th>3-Ltr</th></tr></thead>" +
		"<tbody><tr><td>AVIANCA S.A.</td><td>COLOMBIA</td><td>AVIANCA</td><td>AVA</td></tr></tbody></table>";

	private const string ValidSpecialCallSignsHtml =
		"<table><thead><tr><th>Telephony/Call Sign</th><th>Identifier</th>" +
		"<th>Company or Operating Agency</th><th>Expiration Date</th></tr></thead>" +
		"<tbody><tr><td>AIR SIX</td><td>ARSIX</td><td>NYC Environmental Protection (New Windsor, NY)</td>" +
		"<td>24-Feb-2027</td></tr></tbody></table>";

	private const string NotThePage = "<html></html>";

	/// <summary>Starts a minimal local HTTP server that serves <paramref name="payload"/> with <paramref name="statusCode"/> (default 200).</summary>
	private static (HttpListener Listener, string Url, Func<int> RequestCount) StartServer(
		string payload, HttpStatusCode statusCode = HttpStatusCode.OK)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(payload);
		HttpListener listener = new();
		string url;

		while (true)
		{
			int port = GetFreeTcpPort();
			url = $"http://127.0.0.1:{port}/page.html";
			listener.Prefixes.Clear();
			listener.Prefixes.Add($"http://127.0.0.1:{port}/");

			try
			{
				listener.Start();
				break;
			}
			catch (HttpListenerException)
			{
				// Port was taken between GetFreeTcpPort() and Start(); try another.
			}
		}

		int requestCount = 0;

		_ = Task.Run(async () =>
		{
			try
			{
				while (listener.IsListening)
				{
					HttpListenerContext context = await listener.GetContextAsync();
					Interlocked.Increment(ref requestCount);

					context.Response.StatusCode = (int)statusCode;
					context.Response.ContentLength64 = bytes.Length;
					await context.Response.OutputStream.WriteAsync(bytes);
					context.Response.OutputStream.Close();
				}
			}
			catch (Exception) when (!listener.IsListening)
			{
				// Expected once the listener is stopped mid-GetContextAsync.
			}
		});

		return (listener, url, () => requestCount);
	}

	private static int GetFreeTcpPort()
	{
		System.Net.Sockets.TcpListener tcpListener = new(IPAddress.Loopback, 0);
		tcpListener.Start();
		int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
		tcpListener.Stop();
		return port;
	}

	[Fact]
	public async Task both_pages_download_fresh_when_both_servers_succeed()
	{
		(HttpListener registerListener, string registerUrl, _) = StartServer(ValidRegisterHtml);
		(HttpListener specialListener, string specialUrl, _) = StartServer(ValidSpecialCallSignsHtml);

		try
		{
			string registerPath = Path.Combine(_testRoot, "telephony_register.html");
			string specialPath = Path.Combine(_testRoot, "us_special_call_signs.html");

			TelephonyRefreshResult result = await TelephonyDownloader.RefreshFromUrlsAsync(
				registerUrl, registerPath, specialUrl, specialPath, CancellationToken.None);

			Assert.Equal(registerPath, result.Register.FilePath);
			Assert.NotNull(result.Register.DownloadedUtc);
			Assert.Null(result.Register.FailureReason);
			Assert.Equal(ValidRegisterHtml, File.ReadAllText(registerPath));

			Assert.Equal(specialPath, result.SpecialCallSigns.FilePath);
			Assert.NotNull(result.SpecialCallSigns.DownloadedUtc);
			Assert.Null(result.SpecialCallSigns.FailureReason);
			Assert.Equal(ValidSpecialCallSignsHtml, File.ReadAllText(specialPath));
		}
		finally
		{
			registerListener.Stop();
			registerListener.Close();
			specialListener.Stop();
			specialListener.Close();
		}
	}

	[Fact]
	public async Task the_two_pages_succeed_or_fail_independently()
	{
		(HttpListener registerListener, string registerUrl, _) = StartServer(ValidRegisterHtml);
		(HttpListener specialListener, string specialUrl, _) = StartServer(string.Empty, HttpStatusCode.InternalServerError);

		try
		{
			string registerPath = Path.Combine(_testRoot, "telephony_register.html");
			string specialPath = Path.Combine(_testRoot, "us_special_call_signs.html"); // no prior copy

			TelephonyRefreshResult result = await TelephonyDownloader.RefreshFromUrlsAsync(
				registerUrl, registerPath, specialUrl, specialPath, CancellationToken.None);

			Assert.Equal(registerPath, result.Register.FilePath);
			Assert.Null(result.Register.FailureReason);

			Assert.Null(result.SpecialCallSigns.FilePath);
			Assert.Null(result.SpecialCallSigns.DownloadedUtc);
			Assert.NotNull(result.SpecialCallSigns.FailureReason);
			Assert.False(File.Exists(specialPath));
		}
		finally
		{
			registerListener.Stop();
			registerListener.Close();
			specialListener.Stop();
			specialListener.Close();
		}
	}

	[Fact]
	public async Task a_register_payload_that_is_not_the_page_fails_validation_and_keeps_the_existing_copy()
	{
		(HttpListener registerListener, string registerUrl, _) = StartServer(NotThePage);
		(HttpListener specialListener, string specialUrl, _) = StartServer(ValidSpecialCallSignsHtml);

		try
		{
			string registerPath = Path.Combine(_testRoot, "telephony_register.html");
			string specialPath = Path.Combine(_testRoot, "us_special_call_signs.html");
			Directory.CreateDirectory(_testRoot);
			File.WriteAllText(registerPath, "an existing kept copy");

			TelephonyRefreshResult result = await TelephonyDownloader.RefreshFromUrlsAsync(
				registerUrl, registerPath, specialUrl, specialPath, CancellationToken.None);

			Assert.Equal(registerPath, result.Register.FilePath);
			Assert.NotNull(result.Register.FailureReason);
			Assert.Equal("an existing kept copy", File.ReadAllText(registerPath));

			Assert.Equal(specialPath, result.SpecialCallSigns.FilePath);
			Assert.Null(result.SpecialCallSigns.FailureReason);
		}
		finally
		{
			registerListener.Stop();
			registerListener.Close();
			specialListener.Stop();
			specialListener.Close();
		}
	}

	[Fact]
	public async Task a_special_call_signs_payload_that_is_not_the_page_fails_validation_and_keeps_the_existing_copy()
	{
		(HttpListener registerListener, string registerUrl, _) = StartServer(ValidRegisterHtml);
		(HttpListener specialListener, string specialUrl, _) = StartServer(NotThePage);

		try
		{
			string registerPath = Path.Combine(_testRoot, "telephony_register.html");
			string specialPath = Path.Combine(_testRoot, "us_special_call_signs.html");
			Directory.CreateDirectory(_testRoot);
			File.WriteAllText(specialPath, "an existing kept copy");

			TelephonyRefreshResult result = await TelephonyDownloader.RefreshFromUrlsAsync(
				registerUrl, registerPath, specialUrl, specialPath, CancellationToken.None);

			Assert.Null(result.Register.FailureReason);

			Assert.Equal(specialPath, result.SpecialCallSigns.FilePath);
			Assert.NotNull(result.SpecialCallSigns.FailureReason);
			Assert.Equal("an existing kept copy", File.ReadAllText(specialPath));
		}
		finally
		{
			registerListener.Stop();
			registerListener.Close();
			specialListener.Stop();
			specialListener.Close();
		}
	}

	// ---- the virtual airline list: GNG + VATSIM-Radar ----

	/// <summary>GNG's list with <paramref name="rows"/> rows, saying it has <paramref name="records"/> (the same, unless a test says otherwise).</summary>
	private static string GngJson(int rows = 100, int? records = null)
	{
		string items = string.Join(",", Enumerable.Range(0, rows)
			.Select(i => $$"""{ "icao": "Q{{i:000}}", "airline": "FILLER {{i}}", "callsign": "FILLER{{i}}", "country": "", "in_use": "false" }"""));

		return $$"""{ "records": {{records ?? rows}}, "page": 1, "total": 1, "rows": [{{items}}] }""";
	}

	private const string VatsimRadarJson =
		"""[{ "icao": "DAL", "name": "Fly Delta Virtual", "callsign": "Delta", "virtual": true }, { "icao": "RSF", "name": "RAAF", "callsign": "AUSSIE", "virtual": false }]""";

	private string GngPath => Path.Combine(_testRoot, "gng_fhairlines.json");

	private string VatsimRadarPath => Path.Combine(_testRoot, "vatsim_radar_custom_airlines.json");

	private string OlderPath => Path.Combine(_testRoot, "vatsim_radar_airlines.json");

	private string Keep(string path, string content, DateTime writtenUtc)
	{
		Directory.CreateDirectory(_testRoot);
		File.WriteAllText(path, content);
		File.SetLastWriteTimeUtc(path, writtenUtc);
		return path;
	}

	/// <summary>Starts a server for each part, refreshes the list against them, then stops them.</summary>
	private async Task<(VirtualAirlineListRefreshResult Result, int GngRequests, int VatsimRadarRequests)> RefreshListAsync(
		(string Payload, HttpStatusCode Status) gng,
		(string Payload, HttpStatusCode Status) vatsimRadar,
		bool downloadNow = false)
	{
		(HttpListener gngListener, string gngUrl, Func<int> gngRequests) = StartServer(gng.Payload, gng.Status);
		(HttpListener vatsimRadarListener, string vatsimRadarUrl, Func<int> vatsimRadarRequests) = StartServer(vatsimRadar.Payload, vatsimRadar.Status);

		try
		{
			VirtualAirlineListRefreshResult result = await TelephonyDownloader.RefreshVirtualAirlineListFromUrlsAsync(
				gngUrl, GngPath, vatsimRadarUrl, VatsimRadarPath, OlderPath, downloadNow, CancellationToken.None);

			return (result, gngRequests(), vatsimRadarRequests());
		}
		finally
		{
			gngListener.Stop();
			gngListener.Close();
			vatsimRadarListener.Stop();
			vatsimRadarListener.Close();
		}
	}

	/// <summary>Both parts download into copies of their own; with both in place, the older single copy goes.</summary>
	[Fact]
	public async Task both_parts_download_into_their_own_copies_and_the_older_copy_goes()
	{
		Keep(OlderPath, "{}", DateTime.UtcNow.AddDays(-9));

		(VirtualAirlineListRefreshResult result, _, _) = await RefreshListAsync((GngJson(), HttpStatusCode.OK), (VatsimRadarJson, HttpStatusCode.OK));

		Assert.True(result.Gng.IsFresh);
		Assert.True(result.VatsimRadar.IsFresh);
		Assert.Equal(GngJson(), File.ReadAllText(GngPath));
		Assert.Equal(VatsimRadarJson, File.ReadAllText(VatsimRadarPath));
		Assert.Null(result.OlderCopyPath);
		Assert.False(File.Exists(OlderPath));
		Assert.True(result.HasCopy);
	}

	/// <summary>An older copy that can't be deleted (open elsewhere) is left for next time, with a warning; the refresh still succeeds.</summary>
	[Fact]
	public async Task an_older_copy_that_cannot_be_deleted_is_left_for_next_time()
	{
		Keep(OlderPath, "{}", DateTime.UtcNow.AddDays(-9));
		VirtualAirlineListRefreshResult result;

		using (new FileStream(OlderPath, FileMode.Open, FileAccess.Read, FileShare.None))
		{
			(result, _, _) = await RefreshListAsync((GngJson(), HttpStatusCode.OK), (VatsimRadarJson, HttpStatusCode.OK));
		}

		Assert.True(result.Gng.IsFresh);
		Assert.True(result.VatsimRadar.IsFresh);
		Assert.Null(result.OlderCopyPath);
		Assert.True(File.Exists(OlderPath));
		Assert.Contains(AppLog.Entries, e => e.Message.StartsWith("Could not delete FE-Buddy's older copy of the virtual airline list", StringComparison.Ordinal));
	}

	/// <summary>A GNG reply that isn't the whole list - its rows don't number its records, or too few - fails and keeps the last good copy.</summary>
	[Theory]
	[InlineData(100, 488)]
	[InlineData(99, null)]
	public async Task a_gng_reply_that_is_not_the_whole_list_keeps_the_last_good_copy(int rows, int? records)
	{
		Keep(GngPath, GngJson(), DateTime.UtcNow.AddDays(-3));

		(VirtualAirlineListRefreshResult result, _, _) = await RefreshListAsync((GngJson(rows, records), HttpStatusCode.OK), (VatsimRadarJson, HttpStatusCode.OK));

		Assert.Equal(GngPath, result.Gng.FilePath);
		Assert.NotNull(result.Gng.FailureReason);
		Assert.Equal(GngJson(), File.ReadAllText(GngPath));
		Assert.True(result.VatsimRadar.IsFresh);
	}

	/// <summary>A GitHub reply that isn't the array, or has no virtual airline, fails and keeps the last good copy.</summary>
	[Theory]
	[InlineData("""{ "virtual": [{ "icao": "DAL", "name": "Fly Delta Virtual", "callsign": "Delta", "virtual": true }] }""")]
	[InlineData("""[{ "icao": "RSF", "name": "RAAF", "callsign": "AUSSIE", "virtual": false }]""")]
	[InlineData("<html></html>")]
	public async Task a_vatsim_radar_reply_that_is_not_the_list_keeps_the_last_good_copy(string payload)
	{
		Keep(VatsimRadarPath, VatsimRadarJson, DateTime.UtcNow.AddDays(-3));

		(VirtualAirlineListRefreshResult result, _, _) = await RefreshListAsync((GngJson(), HttpStatusCode.OK), (payload, HttpStatusCode.OK));

		Assert.Equal(VatsimRadarPath, result.VatsimRadar.FilePath);
		Assert.NotNull(result.VatsimRadar.FailureReason);
		Assert.Equal(VatsimRadarJson, File.ReadAllText(VatsimRadarPath));
		Assert.True(result.Gng.IsFresh);
	}

	/// <summary>Each part falls back on its own: one that fails with no copy leaves the other's fresh one, and no older copy is offered.</summary>
	[Fact]
	public async Task each_part_succeeds_or_fails_on_its_own()
	{
		Keep(OlderPath, "{}", DateTime.UtcNow.AddDays(-9));

		(VirtualAirlineListRefreshResult result, _, _) = await RefreshListAsync((string.Empty, HttpStatusCode.InternalServerError), (VatsimRadarJson, HttpStatusCode.OK));

		Assert.False(result.Gng.HasCopy);
		Assert.NotNull(result.Gng.FailureReason);
		Assert.True(result.VatsimRadar.IsFresh);
		Assert.Null(result.OlderCopyPath);
		Assert.True(File.Exists(OlderPath));
	}

	/// <summary>The older single copy is offered only while neither part has a copy.</summary>
	[Fact]
	public async Task the_older_copy_is_offered_only_while_neither_part_has_a_copy()
	{
		DateTime olderUtc = DateTime.UtcNow.AddDays(-9);
		Keep(OlderPath, "{}", olderUtc);

		(VirtualAirlineListRefreshResult result, _, _) = await RefreshListAsync((string.Empty, HttpStatusCode.InternalServerError), (string.Empty, HttpStatusCode.InternalServerError));

		Assert.Equal(OlderPath, result.OlderCopyPath);
		Assert.Equal(File.GetLastWriteTimeUtc(OlderPath), result.OlderCopyDownloadedUtc);
		Assert.True(result.HasCopy);
	}

	/// <summary>During a run a copy under a day old is used as it is; one a day old or more is downloaded again.</summary>
	[Fact]
	public async Task a_run_uses_a_copy_under_a_day_old_without_downloading_it()
	{
		DateTime recentUtc = DateTime.UtcNow.AddHours(-23);
		Keep(GngPath, GngJson(), recentUtc);
		Keep(VatsimRadarPath, VatsimRadarJson, DateTime.UtcNow.AddHours(-25));

		(VirtualAirlineListRefreshResult result, int gngRequests, int vatsimRadarRequests) =
			await RefreshListAsync((GngJson(120), HttpStatusCode.OK), (VatsimRadarJson, HttpStatusCode.OK));

		Assert.Equal(0, gngRequests);
		Assert.True(result.Gng.Reused);
		Assert.False(result.Gng.IsFresh);
		Assert.Equal(recentUtc, result.Gng.DownloadedUtc);
		Assert.Equal(GngJson(), File.ReadAllText(GngPath));

		Assert.Equal(1, vatsimRadarRequests);
		Assert.True(result.VatsimRadar.IsFresh);
	}

	/// <summary>The Telephony tab's button downloads both, however new FE-Buddy's copies are.</summary>
	[Fact]
	public async Task download_now_downloads_both_however_new_the_copies_are()
	{
		Keep(GngPath, GngJson(), DateTime.UtcNow.AddMinutes(-5));
		Keep(VatsimRadarPath, VatsimRadarJson, DateTime.UtcNow.AddMinutes(-5));

		(VirtualAirlineListRefreshResult result, int gngRequests, int vatsimRadarRequests) =
			await RefreshListAsync((GngJson(120), HttpStatusCode.OK), (VatsimRadarJson, HttpStatusCode.OK), downloadNow: true);

		Assert.Equal(1, gngRequests);
		Assert.Equal(1, vatsimRadarRequests);
		Assert.True(result.Gng.IsFresh);
		Assert.Equal(GngJson(120), File.ReadAllText(GngPath));
	}
}
