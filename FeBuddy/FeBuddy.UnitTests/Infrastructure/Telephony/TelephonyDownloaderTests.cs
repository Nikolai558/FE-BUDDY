using System.Net;
using System.Text;

using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.SharedData.Models;
using FeBuddy.Core.Infrastructure.Telephony;
using FeBuddy.Core.Infrastructure.Telephony.Models;

namespace FeBuddy.UnitTests.Infrastructure.Telephony;

/// <summary>
/// Exercises the real download -&gt; validate -&gt; place mechanics of <see cref="TelephonyDownloader"/>
/// against local <see cref="HttpListener"/>s serving synthetic payloads, using
/// <see cref="TelephonyDownloader.RefreshFromUrlsAsync"/> - the same code path the real
/// <see cref="TelephonyDownloader.RefreshAsync"/> uses, with only the URLs and kept-copy paths
/// swapped out. The register and U.S. special call signs pages are independent: one page's success
/// or failure never affects the other's kept copy. The VATSIM-Radar Virtual Airline List goes the
/// same way through <see cref="TelephonyDownloader.RefreshVatsimRadarAirlinesFromUrlAsync"/>.
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

	// ---- the VATSIM-Radar Virtual Airline List ----

	private const string ValidVatsimRadarJson =
		"""{ "airlines": [], "virtual": [{ "icao": "DAL", "name": "Fly Delta Virtual", "callsign": "Delta", "virtual": true }] }""";

	[Fact]
	public async Task the_vatsim_radar_list_downloads_fresh()
	{
		(HttpListener listener, string url, _) = StartServer(ValidVatsimRadarJson);

		try
		{
			string path = Path.Combine(_testRoot, "vatsim_radar_airlines.json");

			SharedDataRefreshResult result = await TelephonyDownloader.RefreshVatsimRadarAirlinesFromUrlAsync(url, path, CancellationToken.None);

			Assert.Equal(path, result.FilePath);
			Assert.Null(result.FailureReason);
			Assert.Equal(ValidVatsimRadarJson, File.ReadAllText(path));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task a_vatsim_radar_payload_that_is_not_the_list_fails_validation_and_keeps_the_existing_copy()
	{
		(HttpListener listener, string url, _) = StartServer(NotThePage);

		try
		{
			string path = Path.Combine(_testRoot, "vatsim_radar_airlines.json");
			Directory.CreateDirectory(_testRoot);
			File.WriteAllText(path, ValidVatsimRadarJson);

			SharedDataRefreshResult result = await TelephonyDownloader.RefreshVatsimRadarAirlinesFromUrlAsync(url, path, CancellationToken.None);

			Assert.Equal(path, result.FilePath);
			Assert.NotNull(result.FailureReason);
			Assert.Equal(ValidVatsimRadarJson, File.ReadAllText(path));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}
}
