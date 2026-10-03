using FeBuddy.Core.Infrastructure.SharedData;
using FeBuddy.Core.Infrastructure.SharedData.Models;
using FeBuddy.Core.Infrastructure.Telephony.Models;
using FeBuddy.Core.Infrastructure.Telephony.Parsers;

namespace FeBuddy.Core.Infrastructure.Telephony;

/// <summary>
/// Downloads the latest copies of the two FAA telephony pages (<see cref="TelephonyFiles"/>) - and,
/// when the user includes it, the VATSIM-Radar Virtual Airline List - into FE-Buddy's kept copies
/// under <see cref="TelephonyFiles.SharedDirectory"/>.
/// </summary>
/// <remarks>
/// Telephony is not published per AIRAC cycle, so every AIRAC Service run that includes Telephony
/// downloads the latest pages, whichever cycle is being run. Each file is checked by parsing it
/// before it replaces its kept copy, and a file whose download fails keeps its last good copy for
/// the run to fall back on (see <see cref="SharedDataDownload"/>). Each succeeds or fails on its own.
/// </remarks>
public static class TelephonyDownloader
{
	/// <summary>Downloads the latest copies of both pages.</summary>
	/// <param name="cancellationToken">Cancels the downloads.</param>
	/// <returns>Each page's copy to use - fresh, or the last good one - and why a fresh one could not be had.</returns>
	/// <exception cref="OperationCanceledException">Thrown only when <paramref name="cancellationToken"/> is cancelled.</exception>
	public static Task<TelephonyRefreshResult> RefreshAsync(CancellationToken cancellationToken = default) =>
		RefreshFromUrlsAsync(
			TelephonyFiles.RegisterUrl,
			TelephonyFiles.RegisterFilePath,
			TelephonyFiles.SpecialCallSignsUrl,
			TelephonyFiles.SpecialCallSignsFilePath,
			cancellationToken);

	/// <summary>
	/// Same as <see cref="RefreshAsync"/>, but with the URLs and kept-copy paths supplied explicitly.
	/// This seam exists so tests can point the download/check/replace mechanics at a local test
	/// server and a scratch folder.
	/// </summary>
	internal static async Task<TelephonyRefreshResult> RefreshFromUrlsAsync(
		string registerUrl,
		string registerPath,
		string specialCallSignsUrl,
		string specialCallSignsPath,
		CancellationToken cancellationToken)
	{
		SharedDataRefreshResult register = await SharedDataDownload.RefreshAsync(
			registerUrl,
			registerPath,
			prepare: CopyAsIs,
			validate: path => TelephonyHtmlParser.ParseRegister(path),
			"FAA telephony register",
			cancellationToken).ConfigureAwait(false);

		SharedDataRefreshResult specialCallSigns = await SharedDataDownload.RefreshAsync(
			specialCallSignsUrl,
			specialCallSignsPath,
			prepare: CopyAsIs,
			validate: path => TelephonyHtmlParser.ParseSpecialCallSigns(path),
			"FAA U.S. special call signs",
			cancellationToken).ConfigureAwait(false);

		return new TelephonyRefreshResult(register, specialCallSigns);
	}

	/// <summary>
	/// Downloads the latest VATSIM-Radar Virtual Airline List - for a run that includes it, or when
	/// the Telephony tab is asked for it, so it can check a new virtual airline against the list.
	/// </summary>
	/// <param name="cancellationToken">Cancels the download.</param>
	/// <returns>The copy to use - fresh, or the last good one - and why a fresh one could not be had.</returns>
	/// <exception cref="OperationCanceledException">Thrown only when <paramref name="cancellationToken"/> is cancelled.</exception>
	public static Task<SharedDataRefreshResult> RefreshVatsimRadarAirlinesAsync(CancellationToken cancellationToken = default) =>
		RefreshVatsimRadarAirlinesFromUrlAsync(TelephonyFiles.VatsimRadarAirlinesUrl, TelephonyFiles.VatsimRadarAirlinesFilePath, cancellationToken);

	/// <summary>
	/// Same as <see cref="RefreshVatsimRadarAirlinesAsync"/>, with the URL and kept-copy path supplied,
	/// so tests can point it at a local test server and a scratch folder.
	/// </summary>
	internal static Task<SharedDataRefreshResult> RefreshVatsimRadarAirlinesFromUrlAsync(string url, string path, CancellationToken cancellationToken) =>
		SharedDataDownload.RefreshAsync(
			url,
			path,
			prepare: CopyAsIs,
			validate: copy => VatsimRadarAirlineParser.Parse(copy),
			"VATSIM-Radar Virtual Airline List",
			cancellationToken);

	/// <summary>The pages are kept exactly as downloaded.</summary>
	private static void CopyAsIs(string downloadedPath, string preparedPath) => File.Copy(downloadedPath, preparedPath, overwrite: true);
}
