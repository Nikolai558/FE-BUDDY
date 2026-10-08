using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.SharedData;
using FeBuddy.Core.Infrastructure.SharedData.Models;
using FeBuddy.Core.Infrastructure.Telephony.Models;
using FeBuddy.Core.Infrastructure.Telephony.Parsers;

namespace FeBuddy.Core.Infrastructure.Telephony;

/// <summary>
/// Downloads the latest copies of the two FAA telephony pages (<see cref="TelephonyFiles"/>) - and,
/// when the user includes it, the two parts of the virtual airline list - into FE-Buddy's kept copies
/// under <see cref="TelephonyFiles.SharedDirectory"/>.
/// </summary>
/// <remarks>
/// <para>
/// Telephony is not published per AIRAC cycle, so every AIRAC Service run that includes Telephony
/// downloads the latest pages, whichever cycle is being run. Each file is checked by parsing it
/// before it replaces its kept copy, and a file whose download fails keeps its last good copy for
/// the run to fall back on (see <see cref="SharedDataDownload"/>). Each succeeds or fails on its own.
/// </para>
/// <para>
/// The virtual airline list's parts change rarely, so a run downloads each at most once a day
/// (<see cref="VirtualAirlineListMaxAge"/>); the Telephony tab's <b>Download the latest list</b> always
/// downloads.
/// </para>
/// </remarks>
public static class TelephonyDownloader
{
	/// <summary>How new a kept copy of a part of the virtual airline list has to be for a run to use it without downloading.</summary>
	public static readonly TimeSpan VirtualAirlineListMaxAge = TimeSpan.FromHours(24);

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
	/// Refreshes both parts of the virtual airline list - for a run that includes it, or when the
	/// Telephony tab is asked for it, so it can check a new virtual airline against the list. Each part
	/// is downloaded, checked and kept on its own; one that can't be downloaded keeps its last good copy.
	/// </summary>
	/// <param name="downloadNow">
	/// <see langword="true"/> to download both whatever their age (the tab's button); <see langword="false"/>
	/// to use a copy under <see cref="VirtualAirlineListMaxAge"/> as it is (a run).
	/// </param>
	/// <param name="cancellationToken">Cancels the downloads.</param>
	/// <returns>Each part's copy to use, and the older single copy while neither part has one.</returns>
	/// <exception cref="OperationCanceledException">Thrown only when <paramref name="cancellationToken"/> is cancelled.</exception>
	public static Task<VirtualAirlineListRefreshResult> RefreshVirtualAirlineListAsync(bool downloadNow, CancellationToken cancellationToken = default) =>
		RefreshVirtualAirlineListFromUrlsAsync(
			TelephonyFiles.GngAirlinesUrl,
			TelephonyFiles.GngAirlinesFilePath,
			TelephonyFiles.VatsimRadarAirlinesUrl,
			TelephonyFiles.VatsimRadarAirlinesFilePath,
			TelephonyFiles.OlderVatsimRadarAirlinesFilePath,
			downloadNow,
			cancellationToken);

	/// <summary>
	/// Same as <see cref="RefreshVirtualAirlineListAsync"/>, with the URLs and kept-copy paths supplied,
	/// so tests can point it at local test servers and a scratch folder.
	/// </summary>
	/// <remarks>
	/// The older single copy is read only while neither part has a copy, and deleted once both have
	/// one: from then on it can never be used again.
	/// </remarks>
	internal static async Task<VirtualAirlineListRefreshResult> RefreshVirtualAirlineListFromUrlsAsync(
		string gngUrl,
		string gngPath,
		string vatsimRadarUrl,
		string vatsimRadarPath,
		string olderCopyPath,
		bool downloadNow,
		CancellationToken cancellationToken)
	{
		TimeSpan? reuseYoungerThan = downloadNow ? null : VirtualAirlineListMaxAge;

		SharedDataRefreshResult gng = await SharedDataDownload.RefreshAsync(
			gngUrl,
			gngPath,
			prepare: CopyAsIs,
			validate: copy => GngAirlineParser.Parse(copy),
			TelephonyFiles.GngAirlinesDescription,
			reuseYoungerThan,
			cancellationToken).ConfigureAwait(false);

		SharedDataRefreshResult vatsimRadar = await SharedDataDownload.RefreshAsync(
			vatsimRadarUrl,
			vatsimRadarPath,
			prepare: CopyAsIs,
			validate: copy => VatsimRadarAirlineParser.ParseGitHubList(copy),
			TelephonyFiles.VatsimRadarAirlinesDescription,
			reuseYoungerThan,
			cancellationToken).ConfigureAwait(false);

		if (gng.HasCopy && vatsimRadar.HasCopy)
		{
			DeleteOlderCopy(olderCopyPath);
			return new VirtualAirlineListRefreshResult(gng, vatsimRadar);
		}

		return !gng.HasCopy && !vatsimRadar.HasCopy && File.Exists(olderCopyPath)
			? new VirtualAirlineListRefreshResult(gng, vatsimRadar, olderCopyPath, File.GetLastWriteTimeUtc(olderCopyPath))
			: new VirtualAirlineListRefreshResult(gng, vatsimRadar);
	}

	/// <summary>Deletes the older single copy, once both parts have a copy of their own. One that can't be deleted is tried again next time.</summary>
	private static void DeleteOlderCopy(string path)
	{
		if (!File.Exists(path))
		{
			return;
		}

		try
		{
			File.Delete(path);
			AppLog.Info("TelephonyDownloader", $"Deleted FE-Buddy's older copy of the virtual airline list, '{path}': both parts have a copy of their own now.");
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AppLog.Warning("TelephonyDownloader", $"Could not delete FE-Buddy's older copy of the virtual airline list, '{path}': {ex.Message}");
		}
	}

	/// <summary>The pages are kept exactly as downloaded.</summary>
	private static void CopyAsIs(string downloadedPath, string preparedPath) => File.Copy(downloadedPath, preparedPath, overwrite: true);
}
