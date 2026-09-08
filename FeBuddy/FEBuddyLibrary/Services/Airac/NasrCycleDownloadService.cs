using System.IO.Compression;
using System.Net.Http;

using FEBuddyLibrary.Helpers;
using FEBuddyLibrary.Models.Services.General;
using FEBuddyLibrary.Models.Services.Airac;
using FEBuddyLibrary.Services.General;

namespace FEBuddyLibrary.Services.Airac;

/// <summary>
/// Downloads and caches a NASR 28-day subscription CSV cycle from the FAA, so a service like
/// Airways can be pointed at a folder without the user manually downloading and unzipping
/// anything.
/// </summary>
/// <remarks>
/// Matches the dev notes' AIRAC Data Download Management rules: cycles are cached under
/// <c>%APPDATA%\FE-Buddy\AiracCycles\&lt;AiracCycleId&gt;\</c>, keyed by cycle ID so a cycle
/// already downloaded is never re-fetched, and <see cref="PruneStaleCycles"/> removes cached
/// cycles that are no longer the previous/current/next cycle.
/// </remarks>
public static class NasrCycleDownloadService
{
	/// <summary>
	/// The FAA NASR 28-day subscription CSV download URL. <c>{0}</c> is the cycle's
	/// <see cref="AiracCycleInfo.NasrCsvEffectiveDate"/> (e.g. <c>01_Oct_2026</c>).
	/// </summary>
	private const string DownloadUrlTemplate = "https://nfdc.faa.gov/webContent/28DaySub/extra/{0}_CSV.zip";

	private const string LogSource = "NasrCycleDownload";

	/// <summary>
	/// A couple of files every Airways run needs, used as a cheap sanity check that a cached
	/// cycle folder actually contains a real, complete extraction rather than a partial or
	/// corrupt one.
	/// </summary>
	private static readonly string[] RequiredFiles = { "AWY_BASE.csv", "AWY_SEG_ALT.csv", "FIX_BASE.csv", "NAV_BASE.csv", "APT_BASE.csv" };

	/// <summary>
	/// The default local cache root: <c>%APPDATA%\FE-Buddy\AiracCycles</c>.
	/// </summary>
	public static string GetDefaultCacheRoot() =>
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FE-Buddy", "AiracCycles");

	/// <summary>
	/// Ensures a cycle's NASR CSV data is downloaded and extracted locally, downloading it
	/// only if it is not already cached.
	/// </summary>
	/// <param name="cycle">The cycle to ensure is available (see <see cref="AiracCycleResolver.GetCycle"/>).</param>
	/// <param name="cacheRootDirectory">
	/// Where cycle folders are cached. Defaults to <see cref="GetDefaultCacheRoot"/>.
	/// </param>
	/// <param name="progress">Optional progress reporting (download percentage, then extracting, then complete).</param>
	/// <param name="cancellationToken">Allows cancelling an in-progress download.</param>
	/// <returns>The local folder containing the cycle's CSV files - usable directly as a NASR source directory.</returns>
	/// <exception cref="HttpRequestException">Thrown when the download fails (network error, 404, etc.).</exception>
	public static Task<string> EnsureCycleAvailableAsync(
		AiracCycleInfo cycle,
		string? cacheRootDirectory = null,
		IProgress<AiracDownloadProgress>? progress = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(cycle);

		string url = string.Format(DownloadUrlTemplate, cycle.NasrCsvEffectiveDate);

		return EnsureCycleAvailableFromUrlAsync(cycle, url, cacheRootDirectory, progress, cancellationToken);
	}

	/// <summary>
	/// Same as <see cref="EnsureCycleAvailableAsync"/>, but with the download URL supplied
	/// explicitly instead of built from the FAA's URL pattern. This seam exists so tests can
	/// point the download/extract/cache/prune mechanics at a local test server instead of the
	/// real FAA endpoint.
	/// </summary>
	internal static async Task<string> EnsureCycleAvailableFromUrlAsync(
		AiracCycleInfo cycle,
		string downloadUrl,
		string? cacheRootDirectory,
		IProgress<AiracDownloadProgress>? progress,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(cycle);

		string cacheRoot = cacheRootDirectory ?? GetDefaultCacheRoot();
		string targetDirectory = Path.Combine(cacheRoot, cycle.AiracCycleId);

		if (IsCycleDataComplete(targetDirectory))
		{
			progress?.Report(new AiracDownloadProgress(AiracDownloadPhase.AlreadyAvailable, 100));
			return targetDirectory;
		}

		Directory.CreateDirectory(cacheRoot);

		string downloadsDirectory = TempWorkspace.EnsureDownloadsDirectory();
		string zipPath = Path.Combine(downloadsDirectory, $"{cycle.AiracCycleId}_CSV.zip");

		using (HttpClient client = new() { Timeout = TimeSpan.FromMinutes(15) })
		{
			using HttpResponseMessage response = await client.GetAsync(
				downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

			response.EnsureSuccessStatusCode();

			long? totalBytes = response.Content.Headers.ContentLength;

			await using (FileStream fileStream = File.Create(zipPath))
			await using (Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
			{
				byte[] buffer = new byte[81920];
				long totalRead = 0;
				int bytesRead;

				while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
				{
					await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
					totalRead += bytesRead;

					double? percent = totalBytes.HasValue
						? Math.Round((double)totalRead / totalBytes.Value * 100, 1)
						: null;

					progress?.Report(new AiracDownloadProgress(AiracDownloadPhase.Downloading, percent));
				}
			}
		}

		progress?.Report(new AiracDownloadProgress(AiracDownloadPhase.Extracting, null));

		bool extractSucceeded = false;

		try
		{
			if (Directory.Exists(targetDirectory))
			{
				// A previous attempt may have left a partial extraction behind.
				Directory.Delete(targetDirectory, recursive: true);
			}

			Directory.CreateDirectory(targetDirectory);
			ExtractCsvEntriesFlattened(zipPath, targetDirectory, cycle.AiracCycleId);
			extractSucceeded = true;
		}
		finally
		{
			// Keep the zip on failure so a developer can inspect it; delete it on success.
			if (extractSucceeded)
			{
				try
				{
					File.Delete(zipPath);
				}
				catch
				{
					// A leftover zip in %TEMP% is cleared on the next launch anyway.
				}
			}
		}

		if (!IsCycleDataComplete(targetDirectory))
		{
			throw new InvalidOperationException(
				$"Downloaded and extracted cycle '{cycle.AiracCycleId}', but one or more " +
				"expected NASR CSV files are missing from the result. The download may be corrupt.");
		}

		progress?.Report(new AiracDownloadProgress(AiracDownloadPhase.Complete, 100));

		return targetDirectory;
	}

	/// <summary>
	/// Deletes any cached cycle folders that are not in <paramref name="cycleIdsToKeep"/>.
	/// </summary>
	/// <param name="cycleIdsToKeep">
	/// The cycle IDs to retain - normally the previous, current, and next cycle's IDs.
	/// </param>
	/// <param name="cacheRootDirectory">Where cycle folders are cached. Defaults to <see cref="GetDefaultCacheRoot"/>.</param>
	/// <returns>The cycle IDs that were actually deleted.</returns>
	public static IReadOnlyList<string> PruneStaleCycles(
		IReadOnlyCollection<string> cycleIdsToKeep,
		string? cacheRootDirectory = null)
	{
		ArgumentNullException.ThrowIfNull(cycleIdsToKeep);

		string cacheRoot = cacheRootDirectory ?? GetDefaultCacheRoot();

		if (!Directory.Exists(cacheRoot))
		{
			return Array.Empty<string>();
		}

		List<string> deleted = new();

		foreach (string directory in Directory.GetDirectories(cacheRoot))
		{
			string cycleId = Path.GetFileName(directory);

			if (cycleIdsToKeep.Contains(cycleId, StringComparer.OrdinalIgnoreCase))
			{
				continue;
			}

			Directory.Delete(directory, recursive: true);
			deleted.Add(cycleId);
		}

		return deleted;
	}

	/// <summary>
	/// Extracts only the <c>*.csv</c> entries from <paramref name="zipPath"/> into
	/// <paramref name="targetDirectory"/>, flattened (named by <see cref="ZipArchiveEntry.Name"/>,
	/// ignoring any folder structure inside the archive). Everything else in the archive - the
	/// FAA's ~25 PDFs, the README, and the nested change-report zip - is skipped, saving ~5 MB
	/// per cycle and removing the old extract-then-clean step.
	/// </summary>
	/// <param name="zipPath">The downloaded cycle archive.</param>
	/// <param name="targetDirectory">The (already-created, empty) cycle cache folder to extract into.</param>
	/// <param name="cycleId">The cycle ID, for log messages.</param>
	private static void ExtractCsvEntriesFlattened(string zipPath, string targetDirectory, string cycleId)
	{
		using ZipArchive archive = ZipFile.OpenRead(zipPath);

		HashSet<string> written = new(StringComparer.OrdinalIgnoreCase);
		int csvCount = 0;

		foreach (ZipArchiveEntry entry in archive.Entries)
		{
			// Directory entries have an empty Name; skip anything that is not a .csv file.
			if (string.IsNullOrEmpty(entry.Name)
				|| !entry.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			// Defence in depth against a maliciously crafted archive (Zip Slip).
			if (entry.FullName.Contains("..", StringComparison.Ordinal))
			{
				AppLog.Warning(LogSource, $"Cycle {cycleId}: skipped archive entry '{entry.FullName}' - path contains '..'.");
				continue;
			}

			if (!written.Add(entry.Name))
			{
				AppLog.Warning(LogSource, $"Cycle {cycleId}: archive has more than one '{entry.Name}'; keeping the first and skipping the rest.");
				continue;
			}

			string destinationPath = Path.Combine(targetDirectory, entry.Name);
			entry.ExtractToFile(destinationPath, overwrite: true);
			csvCount++;
		}

		AppLog.Info(LogSource, $"Cycle {cycleId}: extracted {csvCount} CSV file(s) (non-CSV archive content skipped).");
	}

	/// <summary>
	/// A cheap sanity check that a cached cycle folder contains a real, complete extraction.
	/// </summary>
	private static bool IsCycleDataComplete(string directory)
	{
		if (!Directory.Exists(directory))
		{
			return false;
		}

		foreach (string requiredFile in RequiredFiles)
		{
			if (!File.Exists(Path.Combine(directory, requiredFile)))
			{
				return false;
			}
		}

		return true;
	}
}
