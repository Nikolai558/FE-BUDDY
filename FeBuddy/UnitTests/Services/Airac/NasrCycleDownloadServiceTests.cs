using System.IO.Compression;
using System.Net;

using FEBuddyLibrary.Models.Services.General;
using FEBuddyLibrary.Services.General;
using FEBuddyLibrary.Services.Airac;
using FEBuddyLibrary.Models.Services.Airac;

namespace UnitTests.Services.Airac;

/// <summary>
/// Exercises the real download -&gt; extract -&gt; cache -&gt; prune mechanics against a local
/// <see cref="HttpListener"/> serving a small synthetic zip, rather than the real FAA
/// endpoint (not reachable from every environment, and far too large to fix in a unit test).
/// <see cref="NasrCycleDownloadService.EnsureCycleAvailableFromUrlAsync"/> is the same code
/// path the real <c>EnsureCycleAvailableAsync</c> uses, with only the URL swapped out.
/// </summary>
public class NasrCycleDownloadServiceTests : IDisposable
{
	private readonly string _cacheRoot =
		Path.Combine(Path.GetTempPath(), "FEBuddyTests_AiracCache_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		if (Directory.Exists(_cacheRoot))
		{
			Directory.Delete(_cacheRoot, recursive: true);
		}
	}

	/// <summary>
	/// Builds a small in-memory zip containing empty versions of every file
	/// <see cref="NasrCycleDownloadService"/> checks for, so extraction produces a "complete"
	/// cycle folder.
	/// </summary>
	private static byte[] BuildSyntheticCycleZip()
	{
		using MemoryStream memoryStream = new();

		using (ZipArchive archive = new(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
		{
			foreach (string fileName in new[] { "AWY_BASE.csv", "AWY_SEG_ALT.csv", "FIX_BASE.csv", "NAV_BASE.csv", "APT_BASE.csv" })
			{
				ZipArchiveEntry entry = archive.CreateEntry(fileName);
				using StreamWriter writer = new(entry.Open());
				writer.Write("EFF_DATE\n2026-10-01\n");
			}
		}

		return memoryStream.ToArray();
	}

	/// <summary>
	/// Starts a minimal local HTTP server that serves <paramref name="zipBytes"/> for every
	/// request, and reports how many requests it received.
	/// </summary>
	private static (HttpListener Listener, string Url, Task ServerTask, Func<int> RequestCount) StartZipServer(byte[] zipBytes)
	{
		// Finding a free port and then binding a *different* listener to it is inherently
		// racy under parallel test execution (another test can grab the same port in
		// between) - retry with a fresh port on collision rather than let the whole test fail.
		HttpListener listener = new();
		string url;

		while (true)
		{
			int port = GetFreeTcpPort();
			url = $"http://127.0.0.1:{port}/cycle.zip";
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

		Task serverTask = Task.Run(async () =>
		{
			try
			{
				while (listener.IsListening)
				{
					HttpListenerContext context = await listener.GetContextAsync();
					Interlocked.Increment(ref requestCount);

					context.Response.ContentLength64 = zipBytes.Length;
					context.Response.ContentType = "application/zip";
					await context.Response.OutputStream.WriteAsync(zipBytes);
					context.Response.OutputStream.Close();
				}
			}
			catch (Exception) when (!listener.IsListening)
			{
				// Expected once the listener is stopped mid-GetContextAsync.
			}
		});

		return (listener, url, serverTask, () => requestCount);
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
	public async Task download_extracts_a_usable_cycle_folder_and_reports_progress()
	{
		byte[] zip = BuildSyntheticCycleZip();
		var (listener, url, serverTask, _) = StartZipServer(zip);

		try
		{
			AiracCycleInfo cycle = new("9991", "01_Jan_2099", new DateOnly(2099, 1, 1));
			List<AiracDownloadProgress> updates = new();
			Progress<AiracDownloadProgress> progress = new(updates.Add);

			string resultDirectory = await NasrCycleDownloadService.EnsureCycleAvailableFromUrlAsync(
				cycle, url, _cacheRoot, progress, CancellationToken.None);

			Assert.Equal(Path.Combine(_cacheRoot, "9991"), resultDirectory);
			Assert.True(File.Exists(Path.Combine(resultDirectory, "AWY_BASE.csv")));
			Assert.True(File.Exists(Path.Combine(resultDirectory, "AWY_SEG_ALT.csv")));
			Assert.True(File.Exists(Path.Combine(resultDirectory, "FIX_BASE.csv")));
			Assert.True(File.Exists(Path.Combine(resultDirectory, "NAV_BASE.csv")));
			Assert.True(File.Exists(Path.Combine(resultDirectory, "APT_BASE.csv")));

			Assert.Contains(updates, u => u.Phase == AiracDownloadPhase.Downloading);
			Assert.Contains(updates, u => u.Phase == AiracDownloadPhase.Extracting);
			Assert.Contains(updates, u => u.Phase == AiracDownloadPhase.Complete);
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task a_cycle_already_cached_is_not_downloaded_again()
	{
		byte[] zip = BuildSyntheticCycleZip();
		var (listener, url, serverTask, requestCount) = StartZipServer(zip);

		try
		{
			AiracCycleInfo cycle = new("9992", "01_Jan_2099", new DateOnly(2099, 1, 1));

			await NasrCycleDownloadService.EnsureCycleAvailableFromUrlAsync(
				cycle, url, _cacheRoot, null, CancellationToken.None);

			Assert.Equal(1, requestCount());

			List<AiracDownloadProgress> secondCallUpdates = new();
			Progress<AiracDownloadProgress> progress = new(secondCallUpdates.Add);

			string resultDirectory = await NasrCycleDownloadService.EnsureCycleAvailableFromUrlAsync(
				cycle, url, _cacheRoot, progress, CancellationToken.None);

			// No second HTTP request - the cached folder already had every required file.
			Assert.Equal(1, requestCount());
			Assert.Equal(Path.Combine(_cacheRoot, "9992"), resultDirectory);
			Assert.Contains(secondCallUpdates, u => u.Phase == AiracDownloadPhase.AlreadyAvailable);
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public void prune_stale_cycles_deletes_only_folders_not_in_the_keep_list()
	{
		Directory.CreateDirectory(Path.Combine(_cacheRoot, "2608"));
		Directory.CreateDirectory(Path.Combine(_cacheRoot, "2609"));
		Directory.CreateDirectory(Path.Combine(_cacheRoot, "2610"));

		IReadOnlyList<string> deleted = NasrCycleDownloadService.PruneStaleCycles(
			new[] { "2609", "2610" }, _cacheRoot);

		Assert.Equal(new[] { "2608" }, deleted);
		Assert.False(Directory.Exists(Path.Combine(_cacheRoot, "2608")));
		Assert.True(Directory.Exists(Path.Combine(_cacheRoot, "2609")));
		Assert.True(Directory.Exists(Path.Combine(_cacheRoot, "2610")));
	}

	[Fact]
	public void prune_stale_cycles_on_a_missing_cache_root_does_nothing()
	{
		string missingRoot = Path.Combine(_cacheRoot, "does-not-exist");

		IReadOnlyList<string> deleted = NasrCycleDownloadService.PruneStaleCycles(
			new[] { "2609" }, missingRoot);

		Assert.Empty(deleted);
	}
}
