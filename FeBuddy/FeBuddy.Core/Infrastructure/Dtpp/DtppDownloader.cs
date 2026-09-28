using System.Net;

using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Dtpp.Parsers;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Http;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Core.Infrastructure.Dtpp;

/// <summary>
/// Downloads and places one AIRAC cycle's FAA d-TPP Metafile (<see cref="DtppFiles.FileName"/>)
/// into that cycle's cache folder, alongside its NASR CSVs and (if downloaded) its Wx Stations
/// file.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="WxStations.WxStationDownloader"/>, this file is not shared across cycles -
/// the download URL is keyed by cycle ID (<see cref="DtppFiles.DownloadUrl"/>), so every cycle
/// folder needs its own request - and a <see cref="DtppDownloader"/> instance keeps no state
/// between calls, so there is nothing to memoize.
/// </para>
/// <para>
/// A cycle folder that already has <see cref="DtppFiles.FileName"/> is left alone -
/// <see cref="EnsureCycleHasMetafileAsync"/> returns <see cref="DtppDownloadOutcome.AlreadyPresent"/>
/// without touching the network - so a folder from before this feature existed, or one whose
/// earlier attempt failed, is filled in (or retried) at the next launch, while a folder that
/// already succeeded is never re-downloaded.
/// </para>
/// <para>
/// The FAA publishes a cycle's metafile only about 15-18 days before its effective date (see
/// <see cref="DtppFiles.DownloadUrl"/>), so a 404 for the next cycle is normal, not a failure: it
/// is reported as <see cref="DtppDownloadOutcome.NotYetPublished"/> rather than thrown. Any other
/// failure - a real HTTP error, a payload that doesn't parse as a metafile, or a metafile for the
/// wrong cycle - throws.
/// </para>
/// </remarks>
public sealed class DtppDownloader
{
	private const string LogSource = "DtppDownload";

	/// <summary>
	/// Ensures <paramref name="cycleDirectory"/> has <see cref="DtppFiles.FileName"/>, downloading
	/// it from the FAA only if this folder does not already have it.
	/// </summary>
	/// <param name="cycleDirectory">The AIRAC cycle folder to place the file in.</param>
	/// <param name="cycleId">
	/// The cycle's AIRAC ID (e.g. <c>2609</c>), used to build the download URL and to check the
	/// downloaded file is for the cycle it was asked for.
	/// </param>
	/// <param name="cancellationToken">Cancels an in-progress download.</param>
	/// <returns>Whether the file was already there, was downloaded, or is not published yet.</returns>
	/// <exception cref="HttpRequestException">
	/// Thrown when the download fails for a reason other than "not found" (a 404).
	/// </exception>
	/// <exception cref="InvalidDataException">
	/// Thrown when the downloaded data does not parse as a d-TPP Metafile, or parses to a
	/// different cycle than <paramref name="cycleId"/>.
	/// </exception>
	public Task<DtppDownloadOutcome> EnsureCycleHasMetafileAsync(string cycleDirectory, string cycleId, CancellationToken cancellationToken = default) =>
		EnsureCycleHasMetafileFromUrlAsync(cycleDirectory, cycleId, DtppFiles.DownloadUrl(cycleId), cancellationToken);

	/// <summary>
	/// Same as <see cref="EnsureCycleHasMetafileAsync"/>, but with the download URL supplied
	/// explicitly instead of the real aeronav.faa.gov endpoint. This seam exists so tests can
	/// point the download/validate/place mechanics at a local test server instead of the real
	/// endpoint.
	/// </summary>
	/// <param name="cycleDirectory">The AIRAC cycle folder to place the file in.</param>
	/// <param name="cycleId">The cycle's AIRAC ID, checked against the downloaded file's own <c>cycle</c> attribute.</param>
	/// <param name="downloadUrl">Where to download the metafile XML from.</param>
	/// <param name="cancellationToken">Cancels an in-progress download.</param>
	/// <returns>Whether the file was already there, was downloaded, or is not published yet.</returns>
	internal async Task<DtppDownloadOutcome> EnsureCycleHasMetafileFromUrlAsync(
		string cycleDirectory, string cycleId, string downloadUrl, CancellationToken cancellationToken)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(cycleDirectory);
		ArgumentException.ThrowIfNullOrWhiteSpace(cycleId);
		ArgumentException.ThrowIfNullOrWhiteSpace(downloadUrl);

		string destinationPath = Path.Combine(cycleDirectory, DtppFiles.FileName);

		// Already have it - never touched, and no download is even started for it.
		if (File.Exists(destinationPath))
		{
			return DtppDownloadOutcome.AlreadyPresent;
		}

		string downloadsDirectory = TempWorkspace.EnsureDownloadsDirectory();
		string downloadedPath = Path.Combine(downloadsDirectory, $"d-tpp_Metafile.{cycleId}.{Guid.NewGuid():N}.download");

		try
		{
			using HttpClient client = FeBuddyHttp.CreateClient(TimeSpan.FromMinutes(2));
			using HttpResponseMessage response = await client.GetAsync(
				downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

			if (response.StatusCode == HttpStatusCode.NotFound)
			{
				AppLog.Info(LogSource, $"Cycle {cycleId}'s d-TPP Metafile is not published by the FAA yet.");
				return DtppDownloadOutcome.NotYetPublished;
			}

			response.EnsureSuccessStatusCode();

			await FeBuddyHttp.DownloadToFileAsync(response, downloadedPath, expectedBytes: null, progress: null, cancellationToken)
				.ConfigureAwait(false);

			// Validated eagerly so a corrupt payload, or one for the wrong cycle, fails the
			// download here rather than getting placed into the cycle folder.
			DtppMetafileDataCollection parsed = DtppMetafileXmlParser.Parse(downloadedPath);

			if (!string.Equals(parsed.Cycle, cycleId, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidDataException(
					$"The d-TPP Metafile downloaded from '{downloadUrl}' is for cycle '{parsed.Cycle}', not the requested cycle '{cycleId}'.");
			}

			Directory.CreateDirectory(cycleDirectory);
			string tempDestinationPath = Path.Combine(cycleDirectory, $"{DtppFiles.FileName}.{Guid.NewGuid():N}.tmp");

			try
			{
				// A temp name inside the cycle folder, then an atomic move/replace onto the real
				// name, so a crash mid-copy never leaves a half-written metafile behind.
				File.Copy(downloadedPath, tempDestinationPath, overwrite: true);
				File.Move(tempDestinationPath, destinationPath, overwrite: true);
			}
			finally
			{
				try
				{
					File.Delete(tempDestinationPath);
				}
				catch
				{
					// Already moved away on success; a leftover here only happens on failure and is
					// harmless clutter next to the cycle's other files.
				}
			}

			AppLog.Info(LogSource, $"Cycle {cycleId}'s d-TPP Metafile downloaded and placed in '{cycleDirectory}'.");
			return DtppDownloadOutcome.Downloaded;
		}
		finally
		{
			try
			{
				File.Delete(downloadedPath);
			}
			catch
			{
				// Best-effort scratch cleanup; TempWorkspace is wiped again at the next launch regardless.
			}
		}
	}
}
