using System.IO.Compression;
using System.Net;

using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Nasr;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.UnitTests.Infrastructure.Nasr;

/// <summary>
/// Exercises the real download -&gt; extract -&gt; cache -&gt; prune mechanics against a local
/// <see cref="HttpListener"/> serving a small synthetic zip, rather than the real FAA
/// endpoint (not reachable from every environment, and far too large to fix in a unit test).
/// <see cref="NasrCycleDownloader.EnsureCycleAvailableFromUrlAsync"/> is the same code
/// path the real <c>EnsureCycleAvailableAsync</c> uses, with only the URL swapped out.
/// </summary>
[Collection("AppLog")]
public class NasrCycleDownloaderTests : IDisposable
{
	private readonly string _testRoot =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_AiracDl_" + Guid.NewGuid().ToString("N"));

	private readonly string _cacheRoot;

	public NasrCycleDownloaderTests()
	{
		_cacheRoot = Path.Combine(_testRoot, "cache");
		AppLog.ConfigureForTesting(Path.Combine(_testRoot, "logs"));
		TempWorkspace.ConfigureForTesting(Path.Combine(_testRoot, "temp"));
	}

	/// <summary>
	/// A synchronous <see cref="IProgress{T}"/> - unlike <see cref="Progress{T}"/>, the
	/// callback runs inline on <see cref="IProgress{T}.Report"/> rather than being posted to a
	/// synchronization context / the thread pool, so a test can assert on the collected
	/// reports immediately after the awaited call returns.
	/// </summary>
	private sealed class SyncProgress<T> : IProgress<T>
	{
		public List<T> Reports { get; } = new();

		public void Report(T value)
		{
			lock (Reports)
			{
				Reports.Add(value);
			}
		}
	}

	public void Dispose()
	{
		TempWorkspace.ConfigureForTesting(null);
		AppLog.ConfigureForTesting(null);

		if (Directory.Exists(_testRoot))
		{
			try
			{
				Directory.Delete(_testRoot, recursive: true);
			}
			catch
			{
				// Best-effort.
			}
		}
	}

	/// <summary>
	/// Builds a small in-memory zip containing empty versions of every file
	/// <see cref="NasrCycleDownloader"/> checks for, so extraction produces a "complete"
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
	/// Builds a zip that looks like the real FAA archive: every required CSV plus a PDF, a
	/// README .txt, and a CSV nested inside a folder - so a test can assert only the CSVs
	/// land, flattened.
	/// </summary>
	private static byte[] BuildMixedContentCycleZip()
	{
		using MemoryStream memoryStream = new();

		using (ZipArchive archive = new(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
		{
			void Add(string path, string content)
			{
				ZipArchiveEntry entry = archive.CreateEntry(path);
				using StreamWriter writer = new(entry.Open());
				writer.Write(content);
			}

			foreach (string fileName in new[] { "AWY_BASE.csv", "AWY_SEG_ALT.csv", "FIX_BASE.csv", "NAV_BASE.csv", "APT_BASE.csv" })
			{
				Add(fileName, "EFF_DATE\n2026-10-01\n");
			}

			Add("CSV_Data/AWY_SEG.csv", "X\n1\n");     // nested CSV -> must be flattened in
			Add("Read_Me.txt", "not a csv");            // must be skipped
			Add("changes/28DaySub_Changes.pdf", "%PDF"); // must be skipped
			Add("28DaySub_Changes.zip", "PK");          // nested zip -> must be skipped
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
			SyncProgress<AiracDownloadProgress> progress = new();

			string resultDirectory = await NasrCycleDownloader.EnsureCycleAvailableFromUrlAsync(
				cycle, url, _cacheRoot, progress, CancellationToken.None);

			Assert.Equal(Path.Combine(_cacheRoot, "9991"), resultDirectory);
			Assert.True(File.Exists(Path.Combine(resultDirectory, "AWY_BASE.csv")));
			Assert.True(File.Exists(Path.Combine(resultDirectory, "AWY_SEG_ALT.csv")));
			Assert.True(File.Exists(Path.Combine(resultDirectory, "FIX_BASE.csv")));
			Assert.True(File.Exists(Path.Combine(resultDirectory, "NAV_BASE.csv")));
			Assert.True(File.Exists(Path.Combine(resultDirectory, "APT_BASE.csv")));

			Assert.Contains(progress.Reports, u => u.Phase == AiracDownloadPhase.Downloading);
			Assert.Contains(progress.Reports, u => u.Phase == AiracDownloadPhase.Extracting);
			Assert.Contains(progress.Reports, u => u.Phase == AiracDownloadPhase.Complete);
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

			await NasrCycleDownloader.EnsureCycleAvailableFromUrlAsync(
				cycle, url, _cacheRoot, null, CancellationToken.None);

			Assert.Equal(1, requestCount());

			SyncProgress<AiracDownloadProgress> progress = new();

			string resultDirectory = await NasrCycleDownloader.EnsureCycleAvailableFromUrlAsync(
				cycle, url, _cacheRoot, progress, CancellationToken.None);

			// No second HTTP request - the cached folder already had every required file.
			Assert.Equal(1, requestCount());
			Assert.Equal(Path.Combine(_cacheRoot, "9992"), resultDirectory);
			Assert.Contains(progress.Reports, u => u.Phase == AiracDownloadPhase.AlreadyAvailable);
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task extract_keeps_only_csv_entries_flattened_and_removes_the_zip()
	{
		byte[] zip = BuildMixedContentCycleZip();
		var (listener, url, _, _) = StartZipServer(zip);

		try
		{
			AiracCycleInfo cycle = new("9995", "01_Jan_2099", new DateOnly(2099, 1, 1));

			string resultDirectory = await NasrCycleDownloader.EnsureCycleAvailableFromUrlAsync(
				cycle, url, _cacheRoot, null, CancellationToken.None);

			string[] extracted = Directory.GetFiles(resultDirectory, "*", SearchOption.AllDirectories)
				.Select(Path.GetFileName)!
				.OrderBy(name => name, StringComparer.Ordinal)
				.ToArray()!;

			// Only CSVs, and the nested CSV_Data/AWY_SEG.csv landed flattened at the root.
			Assert.Equal(
				new[] { "APT_BASE.csv", "AWY_BASE.csv", "AWY_SEG.csv", "AWY_SEG_ALT.csv", "FIX_BASE.csv", "NAV_BASE.csv" },
				extracted);
			Assert.All(extracted, name => Assert.EndsWith(".csv", name, StringComparison.OrdinalIgnoreCase));

			// The zip was downloaded into %TEMP%\FE-Buddy\Downloads and removed after a clean extract.
			string zipPath = Path.Combine(TempWorkspace.DownloadsDirectory, "9995_CSV.zip");
			Assert.True(Directory.Exists(TempWorkspace.DownloadsDirectory));
			Assert.False(File.Exists(zipPath));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task a_failed_extract_leaves_the_zip_in_downloads_for_diagnosis()
	{
		// Serve bytes that are not a valid zip so ZipFile.OpenRead throws during extract.
		var (listener, url, _, _) = StartZipServer(new byte[] { 1, 2, 3, 4, 5 });

		try
		{
			AiracCycleInfo cycle = new("9996", "01_Jan_2099", new DateOnly(2099, 1, 1));

			await Assert.ThrowsAnyAsync<Exception>(() => NasrCycleDownloader.EnsureCycleAvailableFromUrlAsync(
				cycle, url, _cacheRoot, null, CancellationToken.None));

			string zipPath = Path.Combine(TempWorkspace.DownloadsDirectory, "9996_CSV.zip");
			Assert.True(File.Exists(zipPath), "the zip should be kept on failure for diagnosis");
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public void clear_on_launch_empties_the_temp_tree_including_downloads()
	{
		string downloads = TempWorkspace.EnsureDownloadsDirectory();
		File.WriteAllText(Path.Combine(downloads, "2610_CSV.zip"), "stale");
		Directory.CreateDirectory(Path.Combine(TempWorkspace.RootDirectory, "v2-legacy"));

		int failures = TempWorkspace.ClearOnLaunch();

		Assert.Equal(0, failures);
		Assert.True(Directory.Exists(TempWorkspace.RootDirectory));
		Assert.Empty(Directory.EnumerateFileSystemEntries(TempWorkspace.RootDirectory));
	}

	[Fact]
	public void prune_stale_cycles_deletes_only_folders_not_in_the_keep_list()
	{
		Directory.CreateDirectory(Path.Combine(_cacheRoot, "2608"));
		Directory.CreateDirectory(Path.Combine(_cacheRoot, "2609"));
		Directory.CreateDirectory(Path.Combine(_cacheRoot, "2610"));

		IReadOnlyList<string> deleted = NasrCycleDownloader.PruneStaleCycles(
			new[] { "2609", "2610" }, _cacheRoot);

		Assert.Equal(new[] { "2608" }, deleted);
		Assert.False(Directory.Exists(Path.Combine(_cacheRoot, "2608")));
		Assert.True(Directory.Exists(Path.Combine(_cacheRoot, "2609")));
		Assert.True(Directory.Exists(Path.Combine(_cacheRoot, "2610")));
	}

	[Fact]
	public void prune_stale_cycles_skips_a_folder_it_cannot_delete_and_carries_on()
	{
		Directory.CreateDirectory(Path.Combine(_cacheRoot, "2607"));
		Directory.CreateDirectory(Path.Combine(_cacheRoot, "2608"));
		Directory.CreateDirectory(Path.Combine(_cacheRoot, "2609"));

		// On Windows an open handle without FileShare.Delete blocks deleting the file, and so its folder.
		using FileStream busy = new(Path.Combine(_cacheRoot, "2607", "APT_BASE.csv"), FileMode.Create, FileAccess.Write, FileShare.None);

		IReadOnlyList<string> deleted = NasrCycleDownloader.PruneStaleCycles(new[] { "2609" }, _cacheRoot);

		Assert.Equal(new[] { "2608" }, deleted);
		Assert.True(Directory.Exists(Path.Combine(_cacheRoot, "2607")));
		Assert.True(Directory.Exists(Path.Combine(_cacheRoot, "2609")));
	}

	[Fact]
	public void prune_stale_cycles_on_a_missing_cache_root_does_nothing()
	{
		string missingRoot = Path.Combine(_cacheRoot, "does-not-exist");

		IReadOnlyList<string> deleted = NasrCycleDownloader.PruneStaleCycles(
			new[] { "2609" }, missingRoot);

		Assert.Empty(deleted);
	}

	private static readonly string[] RequiredFiles = { "AWY_BASE.csv", "AWY_SEG_ALT.csv", "FIX_BASE.csv", "NAV_BASE.csv", "APT_BASE.csv" };

	private static byte[] BuildZip(params (string Path, string Content)[] entries)
	{
		using MemoryStream memoryStream = new();

		using (ZipArchive archive = new(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
		{
			foreach ((string path, string content) in entries)
			{
				using StreamWriter writer = new(archive.CreateEntry(path).Open());
				writer.Write(content);
			}
		}

		return memoryStream.ToArray();
	}

	[Fact]
	public async Task a_complete_cached_cycle_is_available_locally_and_returned_without_a_download()
	{
		AiracCycleInfo cached = new("9981", "01_Jan_2099", new DateOnly(2099, 1, 1));
		AiracCycleInfo missing = new("9982", "29_Jan_2099", new DateOnly(2099, 1, 29));
		string folder = Path.Combine(_cacheRoot, cached.AiracCycleId);
		Directory.CreateDirectory(folder);

		foreach (string fileName in RequiredFiles)
		{
			File.WriteAllText(Path.Combine(folder, fileName), "EFF_DATE\n");
		}

		Assert.True(NasrCycleDownloader.IsCycleAvailableLocally(cached, _cacheRoot));
		Assert.False(NasrCycleDownloader.IsCycleAvailableLocally(missing, _cacheRoot));

		// The public entry point builds the FAA URL, but a complete cache means it never uses it.
		SyncProgress<AiracDownloadProgress> progress = new();
		string result = await NasrCycleDownloader.EnsureCycleAvailableAsync(cached, _cacheRoot, progress);

		Assert.Equal(folder, result);
		Assert.Equal(AiracDownloadPhase.AlreadyAvailable, Assert.Single(progress.Reports).Phase);
	}

	[Fact]
	public void the_default_cache_root_is_under_app_data()
	{
		Assert.Equal(
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FE-Buddy", "AiracCycles"),
			NasrCycleDownloader.GetDefaultCacheRoot());
	}

	[Fact]
	public async Task a_partial_folder_left_by_an_earlier_attempt_is_replaced()
	{
		AiracCycleInfo cycle = new("9983", "01_Jan_2099", new DateOnly(2099, 1, 1));
		string folder = Path.Combine(_cacheRoot, cycle.AiracCycleId);
		Directory.CreateDirectory(folder);
		File.WriteAllText(Path.Combine(folder, "APT_BASE.csv"), "half written");
		File.WriteAllText(Path.Combine(folder, "leftover.csv"), "stale");

		var (listener, url, _, _) = StartZipServer(BuildSyntheticCycleZip());

		try
		{
			await NasrCycleDownloader.EnsureCycleAvailableFromUrlAsync(cycle, url, _cacheRoot, null, CancellationToken.None);

			Assert.False(File.Exists(Path.Combine(folder, "leftover.csv")));
			Assert.Equal("EFF_DATE\n2026-10-01\n", File.ReadAllText(Path.Combine(folder, "APT_BASE.csv")));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task an_archive_missing_required_files_is_reported_as_corrupt()
	{
		AiracCycleInfo cycle = new("9984", "01_Jan_2099", new DateOnly(2099, 1, 1));
		var (listener, url, _, _) = StartZipServer(BuildZip(("APT_BASE.csv", "EFF_DATE\n")));

		try
		{
			InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
				NasrCycleDownloader.EnsureCycleAvailableFromUrlAsync(cycle, url, _cacheRoot, null, CancellationToken.None));

			Assert.Contains("expected NASR CSV files are missing", ex.Message, StringComparison.Ordinal);
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task unsafe_and_duplicate_archive_entries_are_skipped()
	{
		AiracCycleInfo cycle = new("9985", "01_Jan_2099", new DateOnly(2099, 1, 1));
		byte[] zip = BuildZip(RequiredFiles.Select(f => (f, "first"))
			.Append(("../escape.csv", "outside"))
			.Append(("CSV_Data/APT_BASE.csv", "second"))
			.ToArray());
		var (listener, url, _, _) = StartZipServer(zip);

		try
		{
			string folder = await NasrCycleDownloader.EnsureCycleAvailableFromUrlAsync(cycle, url, _cacheRoot, null, CancellationToken.None);

			Assert.Equal("first", File.ReadAllText(Path.Combine(folder, "APT_BASE.csv")));
			Assert.False(File.Exists(Path.Combine(folder, "escape.csv")));
			Assert.False(File.Exists(Path.Combine(_cacheRoot, "escape.csv")));
			Assert.Contains(AppLog.Entries, e => e.Message.Contains("path contains '..'", StringComparison.Ordinal));
			Assert.Contains(AppLog.Entries, e => e.Message.Contains("more than one 'APT_BASE.csv'", StringComparison.Ordinal));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}
}
