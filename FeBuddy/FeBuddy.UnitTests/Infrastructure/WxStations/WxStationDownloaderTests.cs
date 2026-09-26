using System.IO.Compression;
using System.Net;
using System.Text;

using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.WxStations;
using FeBuddy.Core.Infrastructure.WxStations.Parsers;

namespace FeBuddy.UnitTests.Infrastructure.WxStations;

/// <summary>
/// Exercises the real download -&gt; decompress -&gt; place mechanics of <see cref="WxStationDownloader"/>
/// against a local <see cref="HttpListener"/> serving synthetic payloads, using
/// <see cref="WxStationDownloader.EnsureCycleHasWxStationsFromUrlAsync"/> - the same code path the
/// real <see cref="WxStationDownloader.EnsureCycleHasWxStationsAsync"/> uses, with only the URL
/// swapped out.
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
	/// Starts a minimal local HTTP server that serves <paramref name="payload"/>, failing the first
	/// <paramref name="failFirstNRequests"/> requests with a 500 instead, optionally delaying each
	/// response by <paramref name="delay"/> (to widen the window for a concurrency test).
	/// </summary>
	private static (HttpListener Listener, string Url, Func<int> RequestCount) StartServer(
		byte[] payload, int failFirstNRequests = 0, TimeSpan delay = default)
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

		int requestCount = 0;

		_ = Task.Run(async () =>
		{
			try
			{
				while (listener.IsListening)
				{
					HttpListenerContext context = await listener.GetContextAsync();
					int thisRequest = Interlocked.Increment(ref requestCount);

					if (delay > TimeSpan.Zero)
					{
						await Task.Delay(delay);
					}

					if (thisRequest <= failFirstNRequests)
					{
						context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
						context.Response.OutputStream.Close();
						continue;
					}

					context.Response.ContentLength64 = payload.Length;
					await context.Response.OutputStream.WriteAsync(payload);
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

	// ---- gzip and already-XML payloads ----

	[Fact]
	public async Task a_gzip_payload_is_decompressed_and_lands_byte_identical()
	{
		var (listener, url, _) = StartServer(Gzip(StationsXml));

		try
		{
			WxStationDownloader downloader = new();
			string dir = Path.Combine(_testRoot, "cycle");

			await downloader.EnsureCycleHasWxStationsFromUrlAsync(dir, url, CancellationToken.None);

			byte[] decompressed = File.ReadAllBytes(Path.Combine(dir, WxStationFiles.FileName));
			Assert.Equal(Encoding.UTF8.GetBytes(StationsXml), decompressed);
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
		var (listener, url, _) = StartServer(payload);

		try
		{
			WxStationDownloader downloader = new();
			string dir = Path.Combine(_testRoot, "cycle");

			await downloader.EnsureCycleHasWxStationsFromUrlAsync(dir, url, CancellationToken.None);

			Assert.Equal(payload, File.ReadAllBytes(Path.Combine(dir, WxStationFiles.FileName)));
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
		var (listener, url, _) = StartServer(payload);

		try
		{
			WxStationDownloader downloader = new();
			string dir = Path.Combine(_testRoot, "cycle");

			await downloader.EnsureCycleHasWxStationsFromUrlAsync(dir, url, CancellationToken.None);

			string destination = Path.Combine(dir, WxStationFiles.FileName);
			Assert.Equal(payload, File.ReadAllBytes(destination));
			Assert.Equal("KDTW", WxStationXmlParser.Parse(destination).Stations[0].IcaoId);
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
		var (listener, url, _) = StartServer(payload);

		try
		{
			WxStationDownloader downloader = new();
			string dir = Path.Combine(_testRoot, "cycle");

			await downloader.EnsureCycleHasWxStationsFromUrlAsync(dir, url, CancellationToken.None);

			Assert.Equal(payload, File.ReadAllBytes(Path.Combine(dir, WxStationFiles.FileName)));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	// ---- bad payloads ----

	[Fact]
	public async Task a_garbage_payload_that_is_neither_gzip_nor_xml_throws_and_places_nothing()
	{
		var (listener, url, _) = StartServer([1, 2, 3, 4, 5]);

		try
		{
			WxStationDownloader downloader = new();
			string dir = Path.Combine(_testRoot, "cycle");

			InvalidDataException ex = await Assert.ThrowsAsync<InvalidDataException>(
				() => downloader.EnsureCycleHasWxStationsFromUrlAsync(dir, url, CancellationToken.None));

			Assert.Contains("neither gzip-compressed nor XML", ex.Message, StringComparison.Ordinal);
			Assert.False(File.Exists(Path.Combine(dir, WxStationFiles.FileName)));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task an_xml_payload_that_is_not_a_stations_file_throws_and_places_nothing()
	{
		var (listener, url, _) = StartServer(Encoding.UTF8.GetBytes("<foo></foo>"));

		try
		{
			WxStationDownloader downloader = new();
			string dir = Path.Combine(_testRoot, "cycle");

			await Assert.ThrowsAsync<InvalidDataException>(
				() => downloader.EnsureCycleHasWxStationsFromUrlAsync(dir, url, CancellationToken.None));

			Assert.False(File.Exists(Path.Combine(dir, WxStationFiles.FileName)));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	// ---- caching / single-flight ----

	[Fact]
	public async Task a_folder_that_already_has_the_file_is_untouched_and_triggers_no_request()
	{
		var (listener, url, requestCount) = StartServer(Gzip(StationsXml));

		try
		{
			string dir = Path.Combine(_testRoot, "cycle");
			Directory.CreateDirectory(dir);
			string destination = Path.Combine(dir, WxStationFiles.FileName);
			File.WriteAllText(destination, "already here");

			WxStationDownloader downloader = new();
			await downloader.EnsureCycleHasWxStationsFromUrlAsync(dir, url, CancellationToken.None);

			Assert.Equal(0, requestCount());
			Assert.Equal("already here", File.ReadAllText(destination));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task a_second_folder_reuses_the_first_folders_download()
	{
		var (listener, url, requestCount) = StartServer(Gzip(StationsXml));

		try
		{
			WxStationDownloader downloader = new();
			string dir1 = Path.Combine(_testRoot, "cycleA");
			string dir2 = Path.Combine(_testRoot, "cycleB");

			await downloader.EnsureCycleHasWxStationsFromUrlAsync(dir1, url, CancellationToken.None);
			await downloader.EnsureCycleHasWxStationsFromUrlAsync(dir2, url, CancellationToken.None);

			Assert.Equal(1, requestCount());
			Assert.True(File.Exists(Path.Combine(dir1, WxStationFiles.FileName)));
			Assert.True(File.Exists(Path.Combine(dir2, WxStationFiles.FileName)));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task concurrent_calls_on_the_same_instance_result_in_one_request()
	{
		var (listener, url, requestCount) = StartServer(Gzip(StationsXml), delay: TimeSpan.FromMilliseconds(50));

		try
		{
			WxStationDownloader downloader = new();
			string dir1 = Path.Combine(_testRoot, "cycle1");
			string dir2 = Path.Combine(_testRoot, "cycle2");

			Task first = downloader.EnsureCycleHasWxStationsFromUrlAsync(dir1, url, CancellationToken.None);
			Task second = downloader.EnsureCycleHasWxStationsFromUrlAsync(dir2, url, CancellationToken.None);

			await Task.WhenAll(first, second);

			Assert.Equal(1, requestCount());
			Assert.True(File.Exists(Path.Combine(dir1, WxStationFiles.FileName)));
			Assert.True(File.Exists(Path.Combine(dir2, WxStationFiles.FileName)));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	// ---- failures ----

	[Fact]
	public async Task a_failed_download_is_not_memoized_and_a_later_call_retries_and_succeeds()
	{
		var (listener, url, requestCount) = StartServer(Gzip(StationsXml), failFirstNRequests: 1);

		try
		{
			WxStationDownloader downloader = new();
			string dir1 = Path.Combine(_testRoot, "cycle1");
			string dir2 = Path.Combine(_testRoot, "cycle2");

			await Assert.ThrowsAsync<HttpRequestException>(
				() => downloader.EnsureCycleHasWxStationsFromUrlAsync(dir1, url, CancellationToken.None));
			Assert.False(File.Exists(Path.Combine(dir1, WxStationFiles.FileName)));

			await downloader.EnsureCycleHasWxStationsFromUrlAsync(dir2, url, CancellationToken.None);

			Assert.True(File.Exists(Path.Combine(dir2, WxStationFiles.FileName)));
			Assert.Equal(2, requestCount());
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task an_http_error_from_the_server_propagates()
	{
		var (listener, url, _) = StartServer(Gzip(StationsXml), failFirstNRequests: int.MaxValue);

		try
		{
			WxStationDownloader downloader = new();
			string dir = Path.Combine(_testRoot, "cycle");

			await Assert.ThrowsAsync<HttpRequestException>(
				() => downloader.EnsureCycleHasWxStationsFromUrlAsync(dir, url, CancellationToken.None));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task no_tmp_files_are_left_behind_after_a_successful_download()
	{
		var (listener, url, _) = StartServer(Gzip(StationsXml));

		try
		{
			WxStationDownloader downloader = new();
			string dir = Path.Combine(_testRoot, "cycle");

			await downloader.EnsureCycleHasWxStationsFromUrlAsync(dir, url, CancellationToken.None);

			Assert.Empty(Directory.GetFiles(dir, "*.tmp"));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	// ---- argument checks and the public entry point ----

	[Fact]
	public async Task ensure_from_url_rejects_blank_arguments()
	{
		WxStationDownloader downloader = new();

		await Assert.ThrowsAsync<ArgumentException>(
			() => downloader.EnsureCycleHasWxStationsFromUrlAsync(" ", "http://example.test/x", CancellationToken.None));
		await Assert.ThrowsAsync<ArgumentException>(
			() => downloader.EnsureCycleHasWxStationsFromUrlAsync(Path.Combine(_testRoot, "cycle"), " ", CancellationToken.None));
	}

	[Fact]
	public async Task the_public_entry_point_also_skips_an_already_cached_folder()
	{
		string dir = Path.Combine(_testRoot, "cycle");
		Directory.CreateDirectory(dir);
		File.WriteAllText(Path.Combine(dir, WxStationFiles.FileName), "cached");

		WxStationDownloader downloader = new();
		await downloader.EnsureCycleHasWxStationsAsync(dir, CancellationToken.None);

		Assert.Equal("cached", File.ReadAllText(Path.Combine(dir, WxStationFiles.FileName)));
	}
}
