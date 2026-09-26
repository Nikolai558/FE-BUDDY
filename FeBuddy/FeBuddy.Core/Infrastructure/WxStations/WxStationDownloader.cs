using System.IO.Compression;

using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Http;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.WxStations.Parsers;

namespace FeBuddy.Core.Infrastructure.WxStations;

/// <summary>
/// Downloads and caches the aviationweather.gov Wx Stations file
/// (<see cref="WxStationFiles.DownloadUrl"/>) into an AIRAC cycle's cache folder, alongside its
/// NASR CSVs.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="Nasr.NasrCycleDownloader"/>, this file is not keyed by AIRAC cycle - the
/// source is aviationweather.gov's live station list, not an FAA 28-day subscription - and it is
/// small enough (~5 MB unzipped) that fetching it once per launch and copying it into every cycle
/// folder that needs it is simplest. An instance of this class remembers a successful
/// download for its own lifetime, single-flighting concurrent callers and never memoizing a
/// failure, so <see cref="Application.Airac.AiracCycleDataCache"/> shares one instance across the
/// previous/current/next cycles to keep a whole launch to at most one HTTP request.
/// </para>
/// <para>
/// A cycle folder that already has <see cref="WxStationFiles.FileName"/> is left alone -
/// <see cref="EnsureCycleHasWxStationsAsync"/> returns immediately without touching the network -
/// so a folder from before this feature existed, or one whose earlier attempt failed, is filled
/// in (or retried) at the next launch, while a folder that already succeeded is never
/// re-downloaded.
/// </para>
/// </remarks>
public sealed class WxStationDownloader
{
	private const string LogSource = "WxStationDownload";

	private readonly Lock _gate = new();
	private Task<string>? _decompressedFile;

	/// <summary>
	/// Ensures <paramref name="cycleDirectory"/> has <see cref="WxStationFiles.FileName"/>,
	/// downloading and decompressing the source (at most once per <see cref="WxStationDownloader"/>
	/// instance) only if this folder does not already have it.
	/// </summary>
	/// <param name="cycleDirectory">The AIRAC cycle folder to place the file in.</param>
	/// <param name="cancellationToken">Cancels an in-progress download.</param>
	/// <returns>A task that completes once the file is in place, or is confirmed already there.</returns>
	/// <exception cref="HttpRequestException">Thrown when the download fails (network error, 404, etc.).</exception>
	/// <exception cref="InvalidDataException">
	/// Thrown when the downloaded data is neither gzip-compressed nor a parseable stations file.
	/// </exception>
	public Task EnsureCycleHasWxStationsAsync(string cycleDirectory, CancellationToken cancellationToken = default) =>
		EnsureCycleHasWxStationsFromUrlAsync(cycleDirectory, WxStationFiles.DownloadUrl, cancellationToken);

	/// <summary>
	/// Same as <see cref="EnsureCycleHasWxStationsAsync"/>, but with the download URL supplied
	/// explicitly instead of the real aviationweather.gov endpoint. This seam exists so tests can
	/// point the download/decompress/place mechanics at a local test server instead of the real
	/// endpoint.
	/// </summary>
	/// <param name="cycleDirectory">The AIRAC cycle folder to place the file in.</param>
	/// <param name="downloadUrl">Where to download the gzip (or plain XML) source from.</param>
	/// <param name="cancellationToken">Cancels an in-progress download.</param>
	/// <returns>A task that completes once the file is in place, or is confirmed already there.</returns>
	internal async Task EnsureCycleHasWxStationsFromUrlAsync(string cycleDirectory, string downloadUrl, CancellationToken cancellationToken)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(cycleDirectory);
		ArgumentException.ThrowIfNullOrWhiteSpace(downloadUrl);

		string destinationPath = Path.Combine(cycleDirectory, WxStationFiles.FileName);

		// Already have it - never touched, and no download is even started for it.
		if (File.Exists(destinationPath))
		{
			return;
		}

		string sourcePath = await GetOrDownloadAsync(downloadUrl, cancellationToken).ConfigureAwait(false);

		Directory.CreateDirectory(cycleDirectory);
		string tempDestinationPath = Path.Combine(cycleDirectory, $"{WxStationFiles.FileName}.{Guid.NewGuid():N}.tmp");

		try
		{
			// A temp name inside the cycle folder, then an atomic move/replace onto the real name,
			// so a crash mid-copy never leaves a half-written stations.cache.xml behind.
			File.Copy(sourcePath, tempDestinationPath, overwrite: true);
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

		AppLog.Info(LogSource, $"Wx station data placed in '{cycleDirectory}'.");
	}

	/// <summary>
	/// Returns the shared decompressed <c>stations.cache.xml</c> path for this instance's
	/// lifetime, downloading and validating it only once. A failed attempt is not memoized, so the
	/// next caller (the next cycle folder in the same launch) gets to try again.
	/// </summary>
	private Task<string> GetOrDownloadAsync(string downloadUrl, CancellationToken cancellationToken)
	{
		lock (_gate)
		{
			if (_decompressedFile is { IsFaulted: false, IsCanceled: false } inFlightOrDone)
			{
				return inFlightOrDone;
			}

			Task<string> download = DownloadAndDecompressAsync(downloadUrl, cancellationToken);
			_decompressedFile = download;
			return download;
		}
	}

	private static async Task<string> DownloadAndDecompressAsync(string downloadUrl, CancellationToken cancellationToken)
	{
		string downloadsDirectory = TempWorkspace.EnsureDownloadsDirectory();
		string downloadedPath = Path.Combine(downloadsDirectory, "stations.cache.download");

		using (HttpClient client = FeBuddyHttp.CreateClient(TimeSpan.FromMinutes(2)))
		{
			using HttpResponseMessage response = await client.GetAsync(
				downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

			response.EnsureSuccessStatusCode();

			await FeBuddyHttp.DownloadToFileAsync(response, downloadedPath, expectedBytes: null, progress: null, cancellationToken)
				.ConfigureAwait(false);
		}

		string xmlPath = Path.Combine(downloadsDirectory, WxStationFiles.FileName);
		Decompress(downloadedPath, xmlPath, downloadUrl);

		// Validated eagerly so a corrupt or unexpected payload fails the download here rather than
		// getting placed into every cycle folder that asks for it.
		WxStationXmlParser.Parse(xmlPath);

		AppLog.Info(LogSource, "Wx station data downloaded.");
		return xmlPath;
	}

	/// <summary>
	/// Decompresses <paramref name="downloadedPath"/> into <paramref name="xmlDestinationPath"/>:
	/// gunzips it when it starts with the gzip magic bytes, copies it as-is when it already looks
	/// like XML (a server or proxy may have already decoded it), or throws otherwise.
	/// </summary>
	private static void Decompress(string downloadedPath, string xmlDestinationPath, string downloadUrl)
	{
		if (StartsWithGzipMagic(downloadedPath))
		{
			using FileStream source = File.OpenRead(downloadedPath);
			using GZipStream gzip = new(source, CompressionMode.Decompress);
			using FileStream destination = File.Create(xmlDestinationPath);
			gzip.CopyTo(destination);
			return;
		}

		if (LooksLikeXml(downloadedPath))
		{
			File.Copy(downloadedPath, xmlDestinationPath, overwrite: true);
			return;
		}

		throw new InvalidDataException($"Wx station data downloaded from '{downloadUrl}' is neither gzip-compressed nor XML.");
	}

	private static bool StartsWithGzipMagic(string path)
	{
		using FileStream stream = File.OpenRead(path);
		return stream.ReadByte() == 0x1F && stream.ReadByte() == 0x8B;
	}

	/// <summary>Whether the file's first non-whitespace character (after an optional BOM) is <c>&lt;</c>.</summary>
	private static bool LooksLikeXml(string path)
	{
		using FileStream stream = File.OpenRead(path);
		using StreamReader reader = new(stream, detectEncodingFromByteOrderMarks: true);

		int next;
		while ((next = reader.Peek()) != -1 && char.IsWhiteSpace((char)next))
		{
			reader.Read();
		}

		return next == '<';
	}
}
