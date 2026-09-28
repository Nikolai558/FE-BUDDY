using System.IO.Compression;
using System.Net;
using System.Text;

using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.SharedData.Models;
using FeBuddy.Core.Infrastructure.WxStations;
using FeBuddy.Core.Infrastructure.WxStations.Parsers;

namespace FeBuddy.UnitTests.Infrastructure.WxStations;

/// <summary>
/// Exercises the real download -&gt; decompress -&gt; replace mechanics of
/// <see cref="WxStationDownloader"/> against a local <see cref="HttpListener"/> serving synthetic
/// payloads, through <see cref="WxStationDownloader.RefreshFromUrlAsync"/> - the same code path
/// <see cref="WxStationDownloader.RefreshAsync"/> uses, with only the URL and destination swapped
/// out for a local server and a scratch folder.
/// </summary>
[Collection("AppLog")]
public sealed class WxStationDownloaderTests : IDisposable
{
	private readonly string _testRoot =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_WxStationDl_" + Guid.NewGuid().ToString("N"));

	public WxStationDownloaderTests()
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

	private const string StationsXml =
		"<response><data num_results=\"1\"><Station><icao_id>KDTW</icao_id></Station></data></response>";

	/// <summary>A destination under this test's own scratch root - never the real <c>%APPDATA%</c>.</summary>
	private string Destination => Path.Combine(_testRoot, "shared", WxStationFiles.FileName);

	private static byte[] Gzip(string text)
	{
		using MemoryStream memoryStream = new();

		using (GZipStream gzip = new(memoryStream, CompressionMode.Compress, leaveOpen: true))
		{
			byte[] bytes = Encoding.UTF8.GetBytes(text);
			gzip.Write(bytes);
		}

		return memoryStream.ToArray();
	}

	/// <summary>
	/// Starts a minimal local HTTP server that serves <paramref name="payload"/> on every request,
	/// or a 500 for every request when <paramref name="fail"/> is set.
	/// </summary>
	private static (HttpListener Listener, string Url) StartServer(byte[]? payload, bool fail = false)
	{
		HttpListener listener = new();
		string url;

		while (true)
		{
			int port = GetFreeTcpPort();
			url = $"http://127.0.0.1:{port}/stations.cache.xml.gz";
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

		_ = Task.Run(async () =>
		{
			try
			{
				while (listener.IsListening)
				{
					HttpListenerContext context = await listener.GetContextAsync();

					if (fail)
					{
						context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
						context.Response.OutputStream.Close();
						continue;
					}

					context.Response.ContentLength64 = payload!.Length;
					await context.Response.OutputStream.WriteAsync(payload);
					context.Response.OutputStream.Close();
				}
			}
			catch (Exception) when (!listener.IsListening)
			{
				// Expected once the listener is stopped mid-GetContextAsync.
			}
		});

		return (listener, url);
	}

	private static int GetFreeTcpPort()
	{
		System.Net.Sockets.TcpListener tcpListener = new(IPAddress.Loopback, 0);
		tcpListener.Start();
		int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
		tcpListener.Stop();
		return port;
	}

	// ---- gzip and already-XML payloads ----

	[Fact]
	public async Task a_gzip_payload_is_decompressed_and_lands_byte_identical()
	{
		(HttpListener listener, string url) = StartServer(Gzip(StationsXml));

		try
		{
			SharedDataRefreshResult result = await WxStationDownloader.RefreshFromUrlAsync(url, Destination, CancellationToken.None);

			Assert.True(result.IsFresh);
			Assert.Equal(Destination, result.FilePath);
			Assert.Equal(Encoding.UTF8.GetBytes(StationsXml), File.ReadAllBytes(Destination));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task an_already_xml_payload_is_used_as_is_without_decompression()
	{
		byte[] payload = Encoding.UTF8.GetBytes(StationsXml);
		(HttpListener listener, string url) = StartServer(payload);

		try
		{
			SharedDataRefreshResult result = await WxStationDownloader.RefreshFromUrlAsync(url, Destination, CancellationToken.None);

			Assert.True(result.IsFresh);
			Assert.Equal(payload, File.ReadAllBytes(Destination));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task an_already_xml_payload_with_a_bom_is_used_as_is()
	{
		byte[] bom = [0xEF, 0xBB, 0xBF];
		byte[] payload = [.. bom, .. Encoding.UTF8.GetBytes(StationsXml)];
		(HttpListener listener, string url) = StartServer(payload);

		try
		{
			SharedDataRefreshResult result = await WxStationDownloader.RefreshFromUrlAsync(url, Destination, CancellationToken.None);

			Assert.True(result.IsFresh);
			Assert.Equal(payload, File.ReadAllBytes(Destination));
			Assert.Equal("KDTW", WxStationXmlParser.Parse(Destination).Stations[0].IcaoId);
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task an_already_xml_payload_with_leading_whitespace_is_used_as_is()
	{
		byte[] payload = Encoding.UTF8.GetBytes("\n\n   " + StationsXml);
		(HttpListener listener, string url) = StartServer(payload);

		try
		{
			SharedDataRefreshResult result = await WxStationDownloader.RefreshFromUrlAsync(url, Destination, CancellationToken.None);

			Assert.True(result.IsFresh);
			Assert.Equal(payload, File.ReadAllBytes(Destination));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	// ---- bad payloads ----

	[Fact]
	public async Task a_garbage_payload_that_is_neither_gzip_nor_xml_is_a_failed_refresh_with_no_copy()
	{
		(HttpListener listener, string url) = StartServer([1, 2, 3, 4, 5]);

		try
		{
			SharedDataRefreshResult result = await WxStationDownloader.RefreshFromUrlAsync(url, Destination, CancellationToken.None);

			Assert.False(result.IsFresh);
			Assert.False(result.HasCopy);
			Assert.Null(result.FilePath);
			Assert.Null(result.DownloadedUtc);
			Assert.Contains("neither gzip-compressed nor XML", result.FailureReason, StringComparison.Ordinal);
			Assert.False(File.Exists(Destination));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Theory]
	[InlineData("")]
	[InlineData("  \r\n  ")]
	public async Task an_empty_or_blank_payload_is_a_failed_refresh_with_no_copy(string payload)
	{
		(HttpListener listener, string url) = StartServer(Encoding.UTF8.GetBytes(payload));

		try
		{
			SharedDataRefreshResult result = await WxStationDownloader.RefreshFromUrlAsync(url, Destination, CancellationToken.None);

			Assert.False(result.HasCopy);
			Assert.Contains("neither gzip-compressed nor XML", result.FailureReason, StringComparison.Ordinal);
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task an_xml_payload_that_is_not_a_stations_file_is_a_failed_refresh_with_no_copy()
	{
		(HttpListener listener, string url) = StartServer(Encoding.UTF8.GetBytes("<foo></foo>"));

		try
		{
			SharedDataRefreshResult result = await WxStationDownloader.RefreshFromUrlAsync(url, Destination, CancellationToken.None);

			Assert.False(result.IsFresh);
			Assert.False(result.HasCopy);
			Assert.Null(result.FilePath);
			Assert.NotNull(result.FailureReason);
			Assert.False(File.Exists(Destination));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	// ---- HTTP failure, with and without a previous copy ----

	[Fact]
	public async Task an_http_error_with_no_previous_copy_returns_no_file_and_no_downloaded_time()
	{
		(HttpListener listener, string url) = StartServer(payload: null, fail: true);

		try
		{
			SharedDataRefreshResult result = await WxStationDownloader.RefreshFromUrlAsync(url, Destination, CancellationToken.None);

			Assert.False(result.IsFresh);
			Assert.False(result.HasCopy);
			Assert.Null(result.FilePath);
			Assert.Null(result.DownloadedUtc);
			Assert.NotNull(result.FailureReason);
			Assert.False(File.Exists(Destination));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task an_http_error_with_a_previous_copy_keeps_it_byte_identical()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(Destination)!);
		byte[] previous = Encoding.UTF8.GetBytes(StationsXml);
		File.WriteAllBytes(Destination, previous);
		DateTime previousWriteUtc = DateTime.UtcNow.AddDays(-3);
		File.SetLastWriteTimeUtc(Destination, previousWriteUtc);

		(HttpListener listener, string url) = StartServer(payload: null, fail: true);

		try
		{
			SharedDataRefreshResult result = await WxStationDownloader.RefreshFromUrlAsync(url, Destination, CancellationToken.None);

			Assert.False(result.IsFresh);
			Assert.True(result.HasCopy);
			Assert.Equal(Destination, result.FilePath);
			Assert.Equal(File.GetLastWriteTimeUtc(Destination), result.DownloadedUtc);
			Assert.Equal(previousWriteUtc, result.DownloadedUtc!.Value, TimeSpan.FromSeconds(2));
			Assert.NotNull(result.FailureReason);
			Assert.Equal(previous, File.ReadAllBytes(Destination));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	// ---- a successful refresh replaces an existing older copy ----

	[Fact]
	public async Task a_successful_refresh_replaces_an_existing_older_copy_and_is_fresh()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(Destination)!);
		File.WriteAllText(Destination, "stale copy");
		File.SetLastWriteTimeUtc(Destination, DateTime.UtcNow.AddDays(-10));

		(HttpListener listener, string url) = StartServer(Gzip(StationsXml));

		try
		{
			DateTime before = DateTime.UtcNow;
			SharedDataRefreshResult result = await WxStationDownloader.RefreshFromUrlAsync(url, Destination, CancellationToken.None);
			DateTime after = DateTime.UtcNow;

			Assert.True(result.IsFresh);
			Assert.Equal(Destination, result.FilePath);
			Assert.InRange(result.DownloadedUtc!.Value, before.AddSeconds(-1), after.AddSeconds(1));
			Assert.Equal(File.GetLastWriteTimeUtc(Destination), result.DownloadedUtc);
			Assert.Equal(Encoding.UTF8.GetBytes(StationsXml), File.ReadAllBytes(Destination));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	// ---- no leftover temp files ----

	[Fact]
	public async Task no_temp_files_are_left_behind_after_a_successful_download()
	{
		(HttpListener listener, string url) = StartServer(Gzip(StationsXml));

		try
		{
			await WxStationDownloader.RefreshFromUrlAsync(url, Destination, CancellationToken.None);

			AssertNoLeftoverTempFiles();
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task no_temp_files_are_left_behind_after_a_failed_download()
	{
		(HttpListener listener, string url) = StartServer([1, 2, 3]);

		try
		{
			await WxStationDownloader.RefreshFromUrlAsync(url, Destination, CancellationToken.None);

			AssertNoLeftoverTempFiles();
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	/// <summary>No <c>*.tmp</c> beside the destination, and no <c>*.download</c>/<c>*.prepared</c> in the temp downloads folder.</summary>
	private void AssertNoLeftoverTempFiles()
	{
		string destinationDirectory = Path.GetDirectoryName(Destination)!;

		if (Directory.Exists(destinationDirectory))
		{
			Assert.Empty(Directory.GetFiles(destinationDirectory, "*.tmp"));
		}

		Assert.Empty(Directory.GetFiles(TempWorkspace.DownloadsDirectory, "*.download"));
		Assert.Empty(Directory.GetFiles(TempWorkspace.DownloadsDirectory, "*.prepared"));
	}

	// ---- argument checks ----

	[Fact]
	public async Task refresh_from_url_rejects_a_blank_url() =>
		await Assert.ThrowsAsync<ArgumentException>(
			() => WxStationDownloader.RefreshFromUrlAsync(" ", Destination, CancellationToken.None));

	[Fact]
	public async Task refresh_from_url_rejects_a_blank_destination_path() =>
		await Assert.ThrowsAsync<ArgumentException>(
			() => WxStationDownloader.RefreshFromUrlAsync("http://example.test/x", " ", CancellationToken.None));

	// ---- cancellation ----

	[Fact]
	public async Task an_already_cancelled_token_throws_operation_canceled() =>
		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => WxStationDownloader.RefreshFromUrlAsync("http://127.0.0.1:1/unused", Destination, new CancellationToken(canceled: true)));
}
