using System.Net;
using System.Text;

using FeBuddy.Core.Infrastructure.Dtpp;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Infrastructure.Dtpp;

/// <summary>
/// Exercises the real download -&gt; validate -&gt; place mechanics of <see cref="DtppDownloader"/>
/// against a local <see cref="HttpListener"/> serving synthetic payloads, using
/// <see cref="DtppDownloader.EnsureCycleHasMetafileFromUrlAsync"/> - the same code path the real
/// <see cref="DtppDownloader.EnsureCycleHasMetafileAsync"/> uses, with only the URL swapped out.
/// </summary>
[Collection("AppLog")]
public sealed class DtppDownloaderTests : IDisposable
{
	private readonly string _testRoot =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_DtppDl_" + Guid.NewGuid().ToString("N"));

	public DtppDownloaderTests()
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

	/// <summary>A minimal, otherwise-empty d-TPP Metafile for <paramref name="cycle"/>.</summary>
	private static string ValidMetafile(string cycle) =>
		$"""<?xml version="1.0" encoding="utf-8"?><digital_tpp cycle="{cycle}" from_edate="0901Z  09/03/26" to_edate="0901Z  10/01/26"></digital_tpp>""";

	/// <summary>
	/// Starts a minimal local HTTP server that serves <paramref name="payload"/> with
	/// <paramref name="statusCode"/> (default 200).
	/// </summary>
	private static (HttpListener Listener, string Url, Func<int> RequestCount) StartServer(
		byte[] payload, HttpStatusCode statusCode = HttpStatusCode.OK)
	{
		HttpListener listener = new();
		string url;

		while (true)
		{
			int port = GetFreeTcpPort();
			url = $"http://127.0.0.1:{port}/d-tpp_Metafile.xml";
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

	// ---- successful download ----

	[Fact]
	public async Task a_valid_metafile_is_downloaded_and_placed_byte_identical()
	{
		byte[] payload = Encoding.UTF8.GetBytes(ValidMetafile("2609"));
		var (listener, url, _) = StartServer(payload);

		try
		{
			DtppDownloader downloader = new();
			string dir = Path.Combine(_testRoot, "cycle");

			DtppDownloadOutcome outcome = await downloader.EnsureCycleHasMetafileFromUrlAsync(dir, "2609", url, CancellationToken.None);

			Assert.Equal(DtppDownloadOutcome.Downloaded, outcome);
			Assert.Equal(payload, File.ReadAllBytes(Path.Combine(dir, DtppFiles.FileName)));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task no_tmp_or_download_files_are_left_behind_after_a_successful_download()
	{
		byte[] payload = Encoding.UTF8.GetBytes(ValidMetafile("2609"));
		var (listener, url, _) = StartServer(payload);

		try
		{
			DtppDownloader downloader = new();
			string dir = Path.Combine(_testRoot, "cycle");

			await downloader.EnsureCycleHasMetafileFromUrlAsync(dir, "2609", url, CancellationToken.None);

			Assert.Empty(Directory.GetFiles(dir, "*.tmp"));
			Assert.Empty(Directory.GetFiles(TempWorkspace.DownloadsDirectory, "*.download"));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	// ---- already present ----

	[Fact]
	public async Task a_folder_that_already_has_the_file_is_untouched_and_triggers_no_request()
	{
		var (listener, url, requestCount) = StartServer(Encoding.UTF8.GetBytes(ValidMetafile("2609")));

		try
		{
			string dir = Path.Combine(_testRoot, "cycle");
			Directory.CreateDirectory(dir);
			string destination = Path.Combine(dir, DtppFiles.FileName);
			File.WriteAllText(destination, "already here");

			DtppDownloader downloader = new();
			DtppDownloadOutcome outcome = await downloader.EnsureCycleHasMetafileFromUrlAsync(dir, "2609", url, CancellationToken.None);

			Assert.Equal(DtppDownloadOutcome.AlreadyPresent, outcome);
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
	public async Task the_public_entry_point_also_skips_an_already_cached_folder()
	{
		string dir = Path.Combine(_testRoot, "cycle");
		Directory.CreateDirectory(dir);
		File.WriteAllText(Path.Combine(dir, DtppFiles.FileName), "cached");

		DtppDownloader downloader = new();
		DtppDownloadOutcome outcome = await downloader.EnsureCycleHasMetafileAsync(dir, "2609", CancellationToken.None);

		Assert.Equal(DtppDownloadOutcome.AlreadyPresent, outcome);
		Assert.Equal("cached", File.ReadAllText(Path.Combine(dir, DtppFiles.FileName)));
	}

	// ---- not yet published (404) ----

	[Fact]
	public async Task a_404_is_reported_as_not_yet_published_and_places_nothing()
	{
		var (listener, url, requestCount) = StartServer([], HttpStatusCode.NotFound);

		try
		{
			DtppDownloader downloader = new();
			string dir = Path.Combine(_testRoot, "cycle");

			DtppDownloadOutcome outcome = await downloader.EnsureCycleHasMetafileFromUrlAsync(dir, "2611", url, CancellationToken.None);

			Assert.Equal(DtppDownloadOutcome.NotYetPublished, outcome);
			Assert.Equal(1, requestCount());
			Assert.False(Directory.Exists(dir));
			Assert.Empty(Directory.GetFiles(TempWorkspace.DownloadsDirectory, "*.download"));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	// ---- real failures ----

	[Fact]
	public async Task a_500_throws()
	{
		var (listener, url, _) = StartServer([], HttpStatusCode.InternalServerError);

		try
		{
			DtppDownloader downloader = new();
			string dir = Path.Combine(_testRoot, "cycle");

			await Assert.ThrowsAsync<HttpRequestException>(
				() => downloader.EnsureCycleHasMetafileFromUrlAsync(dir, "2609", url, CancellationToken.None));

			Assert.False(File.Exists(Path.Combine(dir, DtppFiles.FileName)));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task a_garbage_payload_throws_and_places_nothing()
	{
		var (listener, url, _) = StartServer([1, 2, 3, 4, 5]);

		try
		{
			DtppDownloader downloader = new();
			string dir = Path.Combine(_testRoot, "cycle");

			await Assert.ThrowsAnyAsync<Exception>(
				() => downloader.EnsureCycleHasMetafileFromUrlAsync(dir, "2609", url, CancellationToken.None));

			Assert.False(File.Exists(Path.Combine(dir, DtppFiles.FileName)));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task a_metafile_for_a_different_cycle_throws_and_places_nothing()
	{
		var (listener, url, _) = StartServer(Encoding.UTF8.GetBytes(ValidMetafile("2609")));

		try
		{
			DtppDownloader downloader = new();
			string dir = Path.Combine(_testRoot, "cycle");

			InvalidDataException ex = await Assert.ThrowsAsync<InvalidDataException>(
				() => downloader.EnsureCycleHasMetafileFromUrlAsync(dir, "2610", url, CancellationToken.None));

			Assert.Contains("2609", ex.Message, StringComparison.Ordinal);
			Assert.Contains("2610", ex.Message, StringComparison.Ordinal);
			Assert.False(File.Exists(Path.Combine(dir, DtppFiles.FileName)));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task no_leftover_tmp_or_download_files_after_a_failure()
	{
		var (listener, url, _) = StartServer([1, 2, 3, 4, 5]);

		try
		{
			DtppDownloader downloader = new();
			string dir = Path.Combine(_testRoot, "cycle");

			await Assert.ThrowsAnyAsync<Exception>(
				() => downloader.EnsureCycleHasMetafileFromUrlAsync(dir, "2609", url, CancellationToken.None));

			Assert.Empty(Directory.GetFiles(TempWorkspace.DownloadsDirectory, "*.download"));
			Assert.False(Directory.Exists(dir) && Directory.GetFiles(dir, "*.tmp").Length > 0);
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	// ---- argument checks ----

	[Fact]
	public async Task ensure_from_url_rejects_blank_arguments()
	{
		DtppDownloader downloader = new();
		string dir = Path.Combine(_testRoot, "cycle");

		await Assert.ThrowsAsync<ArgumentException>(
			() => downloader.EnsureCycleHasMetafileFromUrlAsync(" ", "2609", "http://example.test/x", CancellationToken.None));
		await Assert.ThrowsAsync<ArgumentException>(
			() => downloader.EnsureCycleHasMetafileFromUrlAsync(dir, " ", "http://example.test/x", CancellationToken.None));
		await Assert.ThrowsAsync<ArgumentException>(
			() => downloader.EnsureCycleHasMetafileFromUrlAsync(dir, "2609", " ", CancellationToken.None));
	}
}
