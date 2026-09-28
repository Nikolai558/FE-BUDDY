using System.Net.Http.Headers;

using FeBuddy.Core.Application.Updates.Models;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.GitHub;
using FeBuddy.Core.Infrastructure.Http;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Core.Application.Updates;

/// <summary>
/// Downloads a release's MSI for the update window's "Update now", and builds the msiexec
/// command line that installs it. Launching msiexec (elevated) and closing the app are the GUI's
/// job - the MSI cannot replace FE-Buddy's files while FE-Buddy is running.
/// </summary>
/// <remarks>
/// The same design as FE-Buddy 2.x's updater: the download goes to the temporary workspace
/// (<c>%TEMP%\FE-Buddy\Updates</c>, cleared on every launch). When the user chose a GitHub token
/// (<see cref="GitHubAuth"/>), it comes through the releases-assets API with that token, and a
/// download with it that fails in any way is tried once more from the public asset URL; otherwise
/// it comes from the public asset URL.
/// </remarks>
public static class UpdateInstaller
{
	private const string LogSource = "UpdateInstaller";
	private const string AssetApiUrl = GitHubRepository.ApiUrl + "/releases/assets/";

	/// <summary>Where installers are downloaded: <c>%TEMP%\FE-Buddy\Updates</c>.</summary>
	public static string UpdatesDirectory => Path.Combine(TempWorkspace.RootDirectory, "Updates");

	/// <summary>
	/// Downloads <paramref name="installer"/> into <see cref="UpdatesDirectory"/>. A failed or
	/// cancelled download leaves no partial file behind.
	/// </summary>
	/// <param name="installer">The release's MSI.</param>
	/// <param name="progress">Optional progress, reported as bytes arrive.</param>
	/// <param name="httpClient">An <see cref="HttpClient"/> to use, or <see langword="null"/> to create one. A test seam.</param>
	/// <param name="cancellationToken">Cancels the download.</param>
	/// <returns>The downloaded MSI's full path.</returns>
	/// <exception cref="ArgumentException">The asset's name is not a plain <c>.msi</c> file name.</exception>
	/// <exception cref="HttpRequestException">The download failed.</exception>
	/// <exception cref="IOException">The download ended short of the size GitHub reported.</exception>
	public static async Task<string> DownloadAsync(
		ReleaseInstaller installer,
		IProgress<DownloadProgress>? progress = null,
		HttpClient? httpClient = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(installer);

		string fileName = installer.FileName;
		if (string.IsNullOrWhiteSpace(fileName)
			|| fileName != Path.GetFileName(fileName)
			|| !fileName.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
		{
			throw new ArgumentException($"'{fileName}' is not an .msi file name.", nameof(installer));
		}

		Directory.CreateDirectory(UpdatesDirectory);
		string destination = Path.Combine(UpdatesDirectory, fileName);

		using HttpClient? owned = httpClient is null ? FeBuddyHttp.CreateClient(TimeSpan.FromMinutes(10)) : null;
		HttpClient client = httpClient ?? owned!;

		try
		{
			AppLog.Info(LogSource, $"Downloading {fileName} from {installer.DownloadUrl}.");

			// With a token, the installer comes through the release-assets API, which honours it; a
			// download with it that fails in any way - refused, cut short, timed out - is tried once
			// more from the public link, without it.
			bool downloaded = false;

			if (installer.AssetId > 0)
			{
				using HttpRequestMessage withToken = new(HttpMethod.Get, AssetApiUrl + installer.AssetId);

				// The by-id assets endpoint returns JSON metadata unless asked for the file itself.
				withToken.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

				if (GitHubAuth.TryAuthorize(withToken))
				{
					try
					{
						await DownloadToFileAsync(client, withToken, installer.SizeBytes, destination, progress, cancellationToken)
							.ConfigureAwait(false);
						downloaded = true;
					}
					catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
					{
						AppLog.Warning(LogSource, $"Downloading with {GitHubAuth.TokenDescription} failed ({ex.Message}); trying the public link without it.");
					}
				}
			}

			if (!downloaded)
			{
				using HttpRequestMessage anonymous = new(HttpMethod.Get, installer.DownloadUrl);
				await DownloadToFileAsync(client, anonymous, installer.SizeBytes, destination, progress, cancellationToken)
					.ConfigureAwait(false);
			}

			AppLog.Success(LogSource, $"Downloaded {fileName} to {destination}.");
			return destination;
		}
		catch (Exception ex)
		{
			AppLog.Warning(LogSource, $"Downloading {fileName} failed: {ex.Message}");
			TryDelete(destination);
			throw;
		}
	}

	/// <summary>
	/// The msiexec arguments that install <paramref name="msiPath"/> with its normal UI.
	/// <c>REINSTALLMODE=amus</c> makes any MSI - including an older 2.x one - copy every file, so
	/// installing an older release than the one installed (the rollback the version policy allows)
	/// does not leave files missing. See docs/Developers/VERSIONING.md.
	/// </summary>
	/// <param name="msiPath">The downloaded MSI.</param>
	/// <returns>The argument string for <c>msiexec</c>.</returns>
	public static string InstallerArguments(string msiPath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(msiPath);
		return $"/i \"{msiPath}\" REINSTALLMODE=amus";
	}

	private static async Task DownloadToFileAsync(
		HttpClient client,
		HttpRequestMessage request,
		long knownSizeBytes,
		string destination,
		IProgress<DownloadProgress>? progress,
		CancellationToken cancellationToken)
	{
		using HttpResponseMessage response = await client
			.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
			.ConfigureAwait(false);
		response.EnsureSuccessStatusCode();

		await FeBuddyHttp.DownloadToFileAsync(
			response,
			destination,
			expectedBytes: knownSizeBytes > 0 ? knownSizeBytes : null,
			(received, total) => progress?.Report(new DownloadProgress(received, total)),
			cancellationToken).ConfigureAwait(false);
	}

	private static void TryDelete(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Best-effort: the temporary workspace is cleared on the next launch anyway.
		}
	}
}
