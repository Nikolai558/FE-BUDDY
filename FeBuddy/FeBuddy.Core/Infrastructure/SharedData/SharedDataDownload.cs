using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Http;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.SharedData.Models;

namespace FeBuddy.Core.Infrastructure.SharedData;

/// <summary>
/// Downloads a fresh copy of a data file FE-Buddy keeps one copy of, outside any AIRAC cycle - the
/// Wx Stations list and the FAA telephony pages - replacing the kept copy only once the new one has
/// been checked.
/// </summary>
/// <remarks>
/// <para>
/// These files are not published per AIRAC cycle, so every AIRAC Service run that needs one
/// downloads the latest copy, whichever cycle is being run, into its own folder under
/// <see cref="AppPaths.AppDataDirectory"/> (<see cref="SharedDataDirectory"/>).
/// </para>
/// <para>
/// A new copy is downloaded into the temporary downloads folder, turned into the file to keep
/// (<c>prepare</c>: decompressed, or copied as-is), and checked (<c>validate</c>: parsed) before it
/// replaces the kept copy, so a failed, cut-off or unexpected download never overwrites a good
/// copy. A failure is not an exception: the result then points at the last good copy, with its
/// download time and why the fresh download failed, for the caller to warn about - or at no copy
/// at all, when FE-Buddy never downloaded one. Only cancellation is thrown.
/// </para>
/// </remarks>
public static class SharedDataDownload
{
	/// <summary>How long one download may take before it counts as failed.</summary>
	private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

	/// <summary>
	/// The folder a kept data file lives in: <c>%APPDATA%\FE-Buddy\&lt;folderName&gt;</c>, next to
	/// the <c>AiracCycles</c> folder.
	/// </summary>
	/// <param name="folderName">The data's own folder name, e.g. <c>WxStations</c>.</param>
	/// <returns>The folder's full path.</returns>
	public static string SharedDataDirectory(string folderName) => Path.Combine(AppPaths.AppDataDirectory, folderName);

	/// <summary>
	/// Downloads <paramref name="url"/>, prepares and checks the result, and replaces
	/// <paramref name="destinationPath"/> with it.
	/// </summary>
	/// <param name="url">Where to download from.</param>
	/// <param name="destinationPath">The kept copy's full path.</param>
	/// <param name="prepare">
	/// Turns the downloaded file (first argument) into the file to keep (second argument), e.g. by
	/// decompressing it. Throws when the download is not in a form it can use.
	/// </param>
	/// <param name="validate">Throws when the prepared file is not usable, e.g. because it does not parse.</param>
	/// <param name="description">What the file is, for the log, e.g. <c>Wx station data</c>.</param>
	/// <param name="cancellationToken">Cancels the download.</param>
	/// <returns>The copy to use, fresh or not, and why a fresh one could not be had.</returns>
	/// <exception cref="OperationCanceledException">Thrown only when <paramref name="cancellationToken"/> is cancelled.</exception>
	public static async Task<SharedDataRefreshResult> RefreshAsync(
		string url,
		string destinationPath,
		Action<string, string> prepare,
		Action<string> validate,
		string description,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(url);
		ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
		ArgumentNullException.ThrowIfNull(prepare);
		ArgumentNullException.ThrowIfNull(validate);
		ArgumentException.ThrowIfNullOrWhiteSpace(description);

		string fileName = Path.GetFileName(destinationPath);
		string unique = Guid.NewGuid().ToString("N");
		string downloadsDirectory = TempWorkspace.EnsureDownloadsDirectory();
		string downloadedPath = Path.Combine(downloadsDirectory, $"{fileName}.{unique}.download");
		string preparedPath = Path.Combine(downloadsDirectory, $"{fileName}.{unique}.prepared");
		string destinationDirectory = Path.GetDirectoryName(destinationPath)!;
		string replacementPath = Path.Combine(destinationDirectory, $"{fileName}.{unique}.tmp");

		try
		{
			using (HttpClient client = FeBuddyHttp.CreateClient(Timeout))
			{
				using HttpResponseMessage response = await client.GetAsync(
					url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

				response.EnsureSuccessStatusCode();

				await FeBuddyHttp.DownloadToFileAsync(response, downloadedPath, expectedBytes: null, progress: null, cancellationToken)
					.ConfigureAwait(false);
			}

			prepare(downloadedPath, preparedPath);
			validate(preparedPath);

			// A temp name beside the kept copy, then an atomic move onto it, so a crash part way
			// through never leaves a half-written file where the good copy was.
			Directory.CreateDirectory(destinationDirectory);
			File.Copy(preparedPath, replacementPath, overwrite: true);
			File.Move(replacementPath, destinationPath, overwrite: true);

			// The kept copy's age is read from its last write time, so stamp it with the download.
			DateTime downloadedUtc = DateTime.UtcNow;
			File.SetLastWriteTimeUtc(destinationPath, downloadedUtc);

			AppLog.Info("SharedDataDownload", $"Downloaded the latest {description} to '{destinationPath}'.");
			return new SharedDataRefreshResult(destinationPath, downloadedUtc, FailureReason: null);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			// Anything else - a network error, an HTTP error, a timeout, a payload that does not
			// parse - leaves the kept copy as it was, for the caller to fall back on.
			AppLog.Warning("SharedDataDownload", $"Could not download the latest {description} from {url}: {ex.Message}");

			return File.Exists(destinationPath)
				? new SharedDataRefreshResult(destinationPath, File.GetLastWriteTimeUtc(destinationPath), ex.Message)
				: new SharedDataRefreshResult(FilePath: null, DownloadedUtc: null, ex.Message);
		}
		finally
		{
			DeleteQuietly(downloadedPath);
			DeleteQuietly(preparedPath);
			DeleteQuietly(replacementPath);
		}
	}

	/// <summary>Deletes a working file if it is there; a file that cannot be deleted is harmless clutter.</summary>
	private static void DeleteQuietly(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
		}
	}
}
