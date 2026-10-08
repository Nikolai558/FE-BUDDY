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
/// at all, when FE-Buddy never downloaded one. Only cancellation is thrown. Why it failed names the
/// innermost cause too (<see cref="DescribeFailure"/>): .NET's own "see inner exception" says nothing.
/// </para>
/// <para>
/// A caller can ask for a kept copy younger than a given age to be used as it is, without
/// downloading (<see cref="SharedDataRefreshResult.Reused"/>) - the virtual airline list's parts,
/// downloaded at most once a day.
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
	public static Task<SharedDataRefreshResult> RefreshAsync(
		string url,
		string destinationPath,
		Action<string, string> prepare,
		Action<string> validate,
		string description,
		CancellationToken cancellationToken = default) =>
		RefreshAsync(url, destinationPath, prepare, validate, description, reuseCopyYoungerThan: null, cancellationToken);

	/// <summary>
	/// Same as <see cref="RefreshAsync(string, string, Action{string, string}, Action{string}, string, CancellationToken)"/>,
	/// but a kept copy younger than <paramref name="reuseCopyYoungerThan"/> is used as it is, without
	/// downloading (<see cref="SharedDataRefreshResult.Reused"/>).
	/// </summary>
	/// <param name="url">Where to download from.</param>
	/// <param name="destinationPath">The kept copy's full path.</param>
	/// <param name="prepare">Turns the downloaded file into the file to keep; throws when it can't.</param>
	/// <param name="validate">Throws when the prepared file is not usable.</param>
	/// <param name="description">What the file is, for the log.</param>
	/// <param name="reuseCopyYoungerThan">How new a kept copy has to be to be used without downloading; <see langword="null"/> always downloads.</param>
	/// <param name="cancellationToken">Cancels the download.</param>
	/// <returns>The copy to use, fresh, reused or the last good one, and why a fresh one could not be had.</returns>
	/// <exception cref="OperationCanceledException">Thrown only when <paramref name="cancellationToken"/> is cancelled.</exception>
	public static async Task<SharedDataRefreshResult> RefreshAsync(
		string url,
		string destinationPath,
		Action<string, string> prepare,
		Action<string> validate,
		string description,
		TimeSpan? reuseCopyYoungerThan,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(url);
		ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
		ArgumentNullException.ThrowIfNull(prepare);
		ArgumentNullException.ThrowIfNull(validate);
		ArgumentException.ThrowIfNullOrWhiteSpace(description);

		if (reuseCopyYoungerThan is { } maxAge && File.Exists(destinationPath))
		{
			DateTime keptUtc = File.GetLastWriteTimeUtc(destinationPath);
			TimeSpan age = DateTime.UtcNow - keptUtc;

			// A copy dated in the future (the clock was moved back) is downloaded again, not trusted for ever.
			if (age >= TimeSpan.Zero && age < maxAge)
			{
				AppLog.Info("SharedDataDownload",
					$"Used FE-Buddy's copy of the {description} from {keptUtc.ToLocalTime():d MMM yyyy HH:mm} without downloading it: " +
					$"it is under {maxAge.TotalHours:0} hours old.");
				return new SharedDataRefreshResult(destinationPath, keptUtc, FailureReason: null) { Reused = true };
			}
		}

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
			string reason = DescribeFailure(ex);
			AppLog.Warning("SharedDataDownload", $"Could not download the latest {description} from {url}: {reason}");

			return File.Exists(destinationPath)
				? new SharedDataRefreshResult(destinationPath, File.GetLastWriteTimeUtc(destinationPath), reason)
				: new SharedDataRefreshResult(FilePath: null, DownloadedUtc: null, reason);
		}
		finally
		{
			DeleteQuietly(downloadedPath);
			DeleteQuietly(preparedPath);
			DeleteQuietly(replacementPath);
		}
	}

	/// <summary>
	/// Why a download failed, for the log and the run: the exception's message, and the innermost
	/// cause's when it has one that says more - e.g. <c>The SSL connection could not be established:
	/// Authentication failed because the remote party sent a TLS alert: 'ProtocolVersion'.</c> rather
	/// than .NET's own "..., see inner exception."
	/// </summary>
	/// <param name="ex">What was thrown.</param>
	/// <returns>The reason.</returns>
	internal static string DescribeFailure(Exception ex)
	{
		ArgumentNullException.ThrowIfNull(ex);

		Exception innermost = ex;
		while (innermost.InnerException is { } inner)
		{
			innermost = inner;
		}

		string outer = ex.Message.Trim();
		string cause = innermost.Message.Trim();

		if (ReferenceEquals(innermost, ex) || cause.Length == 0 || outer.Contains(cause, StringComparison.Ordinal))
		{
			return outer;
		}

		const string SeeInner = ", see inner exception.";
		string lead = outer.EndsWith(SeeInner, StringComparison.OrdinalIgnoreCase) ? outer[..^SeeInner.Length] : outer.TrimEnd('.');
		return $"{lead}: {cause}";
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
