using System.IO.Compression;

using FeBuddy.Core.Infrastructure.SharedData;
using FeBuddy.Core.Infrastructure.SharedData.Models;
using FeBuddy.Core.Infrastructure.WxStations.Parsers;

namespace FeBuddy.Core.Infrastructure.WxStations;

/// <summary>
/// Downloads the latest aviationweather.gov Wx Stations file (<see cref="WxStationFiles.DownloadUrl"/>)
/// into FE-Buddy's one kept copy, <see cref="WxStationFiles.SharedFilePath"/>.
/// </summary>
/// <remarks>
/// <para>
/// The source is aviationweather.gov's live station list, not an FAA 28-day subscription, so it is
/// not kept per AIRAC cycle: every AIRAC Service run that includes Wx Stations downloads the latest
/// copy, whichever cycle is being run. A download that fails, or does not parse, leaves the last
/// good copy in place for the run to fall back on (see <see cref="SharedDataDownload"/>).
/// </para>
/// <para>
/// The source is gzip-compressed, but a server or proxy may already have decoded it, so the
/// download is gunzipped when it starts with the gzip magic bytes and used as-is when it already
/// looks like XML.
/// </para>
/// </remarks>
public static class WxStationDownloader
{
	/// <summary>What the file is, for the log.</summary>
	private const string Description = "Wx station data";

	/// <summary>
	/// Downloads the latest station list into <see cref="WxStationFiles.SharedFilePath"/>.
	/// </summary>
	/// <param name="cancellationToken">Cancels the download.</param>
	/// <returns>The copy to use - fresh, or the last good one - and why a fresh one could not be had.</returns>
	/// <exception cref="OperationCanceledException">Thrown only when <paramref name="cancellationToken"/> is cancelled.</exception>
	public static Task<SharedDataRefreshResult> RefreshAsync(CancellationToken cancellationToken = default) =>
		RefreshFromUrlAsync(WxStationFiles.DownloadUrl, WxStationFiles.SharedFilePath, cancellationToken);

	/// <summary>
	/// Same as <see cref="RefreshAsync"/>, but with the download URL and the kept copy's path
	/// supplied explicitly. This seam exists so tests can point the download/decompress/replace
	/// mechanics at a local test server and a scratch folder.
	/// </summary>
	/// <param name="downloadUrl">Where to download the gzip (or plain XML) source from.</param>
	/// <param name="destinationPath">The kept copy's full path.</param>
	/// <param name="cancellationToken">Cancels the download.</param>
	/// <returns>The copy to use - fresh, or the last good one - and why a fresh one could not be had.</returns>
	internal static Task<SharedDataRefreshResult> RefreshFromUrlAsync(string downloadUrl, string destinationPath, CancellationToken cancellationToken) =>
		SharedDataDownload.RefreshAsync(
			downloadUrl,
			destinationPath,
			prepare: (downloadedPath, xmlPath) => Decompress(downloadedPath, xmlPath, downloadUrl),
			validate: xmlPath => WxStationXmlParser.Parse(xmlPath),
			Description,
			cancellationToken);

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
