using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Telephony;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.SharedData.Models;
using FeBuddy.Core.Infrastructure.Telephony;
using FeBuddy.Core.Infrastructure.Telephony.Models;
using FeBuddy.Core.Infrastructure.Telephony.Parsers;
using FeBuddy.Core.Infrastructure.WxStations;
using FeBuddy.Core.Infrastructure.WxStations.Models;
using FeBuddy.Core.Infrastructure.WxStations.Parsers;

namespace FeBuddy.Core.Application.Airac;

/// <summary>
/// Gets an AIRAC Service run the data that is not published per AIRAC cycle - the Wx Stations list,
/// the FAA telephony pages and, when included, the virtual airline list - by downloading the latest
/// copy and parsing it, and says what happened for the run's Review tab.
/// </summary>
/// <remarks>
/// <para>
/// Every run downloads the latest copy, whichever cycle is being run - except the virtual airline
/// list's two parts, each downloaded at most once a day (<see cref="TelephonyDownloader.VirtualAirlineListMaxAge"/>).
/// When the download fails, the last good copy is used and the run carries an advisory warning saying
/// how old it is. When there is no copy at all, a required file (the station list, the telephony
/// register) leaves its sub-service with nothing to build from - an error on the Review tab, while
/// the rest of the run goes on - and an optional one (the U.S. special call signs, a part of the
/// virtual airline list) is left out with a warning.
/// </para>
/// </remarks>
public static class AiracSharedDataLoader
{
	private const string WxStationsSource = "WxStationDownload";
	private const string TelephonySource = "TelephonyDownload";

	private static Func<CancellationToken, Task<SharedDataRefreshResult>> _refreshWxStations = WxStationDownloader.RefreshAsync;
	private static Func<CancellationToken, Task<TelephonyRefreshResult>> _refreshTelephony = TelephonyDownloader.RefreshAsync;
	private static Func<CancellationToken, Task<VirtualAirlineListRefreshResult>> _refreshVirtualAirlineList = RefreshVirtualAirlineListForRunAsync;

	/// <summary>
	/// Replaces the real downloads the public <c>Load*Async</c> methods use - so a test of a whole
	/// AIRAC Service run never touches the network - or restores them when an argument is
	/// <see langword="null"/>. Unit tests only.
	/// </summary>
	/// <param name="refreshWxStations">Stands in for <see cref="WxStationDownloader.RefreshAsync"/>.</param>
	/// <param name="refreshTelephony">Stands in for <see cref="TelephonyDownloader.RefreshAsync"/>.</param>
	/// <param name="refreshVirtualAirlineList">Stands in for <see cref="TelephonyDownloader.RefreshVirtualAirlineListAsync"/>.</param>
	internal static void ConfigureForTesting(
		Func<CancellationToken, Task<SharedDataRefreshResult>>? refreshWxStations,
		Func<CancellationToken, Task<TelephonyRefreshResult>>? refreshTelephony,
		Func<CancellationToken, Task<VirtualAirlineListRefreshResult>>? refreshVirtualAirlineList = null)
	{
		_refreshWxStations = refreshWxStations ?? WxStationDownloader.RefreshAsync;
		_refreshTelephony = refreshTelephony ?? TelephonyDownloader.RefreshAsync;
		_refreshVirtualAirlineList = refreshVirtualAirlineList ?? RefreshVirtualAirlineListForRunAsync;
	}

	/// <summary>A run's refresh of the virtual airline list: each part at most once a day.</summary>
	private static Task<VirtualAirlineListRefreshResult> RefreshVirtualAirlineListForRunAsync(CancellationToken cancellationToken) =>
		TelephonyDownloader.RefreshVirtualAirlineListAsync(downloadNow: false, cancellationToken);

	/// <summary>Downloads (or falls back on) and parses the Wx Stations list.</summary>
	/// <param name="cancellationToken">Cancels the download.</param>
	/// <returns>The parsed list, or <see langword="null"/> when there is no usable copy, and the messages for the run.</returns>
	public static Task<AiracSharedDataLoadResult<WxStationDataCollection>> LoadWxStationsAsync(CancellationToken cancellationToken = default) =>
		LoadWxStationsAsync(_refreshWxStations, DateTime.UtcNow, cancellationToken);

	/// <summary>Downloads (or falls back on) and parses the two FAA telephony pages.</summary>
	/// <param name="cancellationToken">Cancels the downloads.</param>
	/// <returns>The parsed pages, or <see langword="null"/> when there is no usable copy of the register, and the messages for the run.</returns>
	public static Task<AiracSharedDataLoadResult<TelephonyDataCollection>> LoadTelephonyAsync(CancellationToken cancellationToken = default) =>
		LoadTelephonyAsync(includeVatsimRadar: false, cancellationToken);

	/// <summary>
	/// Downloads (or falls back on) and parses the two FAA telephony pages and, when the run includes
	/// it, the virtual airline list - optional, like the U.S. special call signs.
	/// </summary>
	/// <param name="includeVatsimRadar">Whether the run includes the virtual airline list (see <c>TelephonySettingsParser.IncludesVatsimRadarList</c>).</param>
	/// <param name="cancellationToken">Cancels the downloads.</param>
	/// <returns>The parsed data, or <see langword="null"/> when there is no usable copy of the register, and the messages for the run.</returns>
	public static Task<AiracSharedDataLoadResult<TelephonyDataCollection>> LoadTelephonyAsync(bool includeVatsimRadar, CancellationToken cancellationToken = default) =>
		LoadTelephonyAsync(_refreshTelephony, includeVatsimRadar ? _refreshVirtualAirlineList : null, DateTime.UtcNow, cancellationToken);

	/// <summary>
	/// Same as <see cref="LoadWxStationsAsync(CancellationToken)"/>, with the download and the clock
	/// supplied, so tests need neither the network nor a real date.
	/// </summary>
	internal static async Task<AiracSharedDataLoadResult<WxStationDataCollection>> LoadWxStationsAsync(
		Func<CancellationToken, Task<SharedDataRefreshResult>> refresh,
		DateTime nowUtc,
		CancellationToken cancellationToken)
	{
		SharedDataRefreshResult refreshed = await refresh(cancellationToken).ConfigureAwait(false);
		List<ServiceMessage> messages = [RefreshMessage(refreshed, WxStationsSource, "Wx station data", "Wx Stations wrote nothing", nowUtc, required: true)];

		if (refreshed.FilePath is not { } path)
		{
			return new AiracSharedDataLoadResult<WxStationDataCollection>(null, messages);
		}

		try
		{
			WxStationDataCollection data = await WxStationXmlParser.ParseAsync(path, cancellationToken).ConfigureAwait(false);
			return new AiracSharedDataLoadResult<WxStationDataCollection>(data, messages);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			messages.Add(UnreadableCopyMessage(WxStationsSource, "Wx station data", ex, "Wx Stations wrote nothing"));
			return new AiracSharedDataLoadResult<WxStationDataCollection>(null, messages);
		}
	}

	/// <summary>
	/// Same as <see cref="LoadTelephonyAsync(CancellationToken)"/>, with the downloads and the clock
	/// supplied, so tests need neither the network nor a real date.
	/// </summary>
	internal static Task<AiracSharedDataLoadResult<TelephonyDataCollection>> LoadTelephonyAsync(
		Func<CancellationToken, Task<TelephonyRefreshResult>> refresh,
		DateTime nowUtc,
		CancellationToken cancellationToken) =>
		LoadTelephonyAsync(refresh, refreshVirtualAirlineList: null, nowUtc, cancellationToken);

	/// <summary>
	/// Same as <see cref="LoadTelephonyAsync(bool, CancellationToken)"/>, with the downloads and the
	/// clock supplied - <paramref name="refreshVirtualAirlineList"/> <see langword="null"/> when the run
	/// does not include the virtual airline list - so tests need neither the network nor a real date.
	/// </summary>
	internal static async Task<AiracSharedDataLoadResult<TelephonyDataCollection>> LoadTelephonyAsync(
		Func<CancellationToken, Task<TelephonyRefreshResult>> refresh,
		Func<CancellationToken, Task<VirtualAirlineListRefreshResult>>? refreshVirtualAirlineList,
		DateTime nowUtc,
		CancellationToken cancellationToken)
	{
		TelephonyRefreshResult refreshed = await refresh(cancellationToken).ConfigureAwait(false);

		List<ServiceMessage> messages =
		[
			RefreshMessage(refreshed.Register, TelephonySource, "FAA telephony register", "Telephony wrote nothing", nowUtc, required: true),
			RefreshMessage(refreshed.SpecialCallSigns, TelephonySource, "FAA U.S. special call signs", "Telephony.txt leaves them out", nowUtc, required: false),
		];

		if (refreshed.Register.FilePath is not { } registerPath)
		{
			return new AiracSharedDataLoadResult<TelephonyDataCollection>(null, messages);
		}

		TelephonyDataCollection data;

		try
		{
			data = new TelephonyDataCollection { Assignments = TelephonyHtmlParser.ParseRegister(registerPath) };
		}
		catch (Exception ex)
		{
			messages.Add(UnreadableCopyMessage(TelephonySource, "FAA telephony register", ex, "Telephony wrote nothing"));
			return new AiracSharedDataLoadResult<TelephonyDataCollection>(null, messages);
		}

		if (refreshed.SpecialCallSigns.FilePath is { } specialCallSignsPath)
		{
			try
			{
				data.SpecialCallSigns = TelephonyHtmlParser.ParseSpecialCallSigns(specialCallSignsPath);
			}
			catch (Exception ex)
			{
				messages.Add(UnreadableCopyMessage(TelephonySource, "FAA U.S. special call signs", ex, "Telephony.txt leaves them out"));
			}
		}

		if (refreshVirtualAirlineList is not null)
		{
			await AddVirtualAirlineListAsync(refreshVirtualAirlineList, data, messages, nowUtc, cancellationToken).ConfigureAwait(false);
		}

		return new AiracSharedDataLoadResult<TelephonyDataCollection>(data, messages);
	}

	/// <summary>
	/// Refreshes (or falls back on) the virtual airline list's two parts, merges them and adds the list
	/// to the run's telephony data, with a message for each part. A part without a usable copy is left
	/// out with a warning, and the list is the other part alone. Only while neither part has a copy is
	/// the older single copy used - and without that either, the run goes on without the list.
	/// </summary>
	private static async Task AddVirtualAirlineListAsync(
		Func<CancellationToken, Task<VirtualAirlineListRefreshResult>> refresh,
		TelephonyDataCollection data,
		List<ServiceMessage> messages,
		DateTime nowUtc,
		CancellationToken cancellationToken)
	{
		const string WithoutTheList = "Telephony.txt leaves the virtual airline list out";
		const string Gng = TelephonyFiles.GngAirlinesDescription;
		const string VatsimRadar = TelephonyFiles.VatsimRadarAirlinesDescription;

		VirtualAirlineListRefreshResult refreshed = await refresh(cancellationToken).ConfigureAwait(false);

		string WithoutPart(SharedDataRefreshResult other, string otherName) =>
			other.HasCopy ? $"the virtual airline list has only the {otherName}'s airlines"
			: refreshed.OlderCopyPath is not null ? "this run used FE-Buddy's older copy of the whole list"
			: WithoutTheList;

		messages.Add(RefreshMessage(refreshed.Gng, TelephonySource, Gng, WithoutPart(refreshed.VatsimRadar, VatsimRadar), nowUtc, required: false));
		messages.Add(RefreshMessage(refreshed.VatsimRadar, TelephonySource, VatsimRadar, WithoutPart(refreshed.Gng, Gng), nowUtc, required: false));

		const string LeavesThemOut = "the virtual airline list leaves its airlines out";
		IReadOnlyList<VatsimRadarAirline>? gng = ReadPart(refreshed.Gng.FilePath, GngAirlineParser.Parse, Gng, LeavesThemOut, messages);
		IReadOnlyList<VatsimRadarAirline>? vatsimRadar = ReadPart(refreshed.VatsimRadar.FilePath, VatsimRadarAirlineParser.Parse, VatsimRadar, LeavesThemOut, messages);

		if (gng is not null || vatsimRadar is not null)
		{
			data.VatsimRadarAirlines = VatsimRadarVirtualAirlines.Merge(gng ?? [], vatsimRadar ?? []);
			return;
		}

		if (refreshed.OlderCopyPath is { } olderPath
			&& ReadPart(olderPath, VatsimRadarAirlineParser.Parse, "virtual airline list (its older copy)", WithoutTheList, messages) is { } older)
		{
			data.VatsimRadarAirlines = [.. older];

			string from = refreshed.OlderCopyDownloadedUtc is { } downloadedUtc
				? $" from {downloadedUtc.ToLocalTime():d MMM yyyy} ({DescribeAge(nowUtc - downloadedUtc)})"
				: string.Empty;

			messages.Add(new ServiceMessage(LogLevel.Warning, TelephonySource,
				$"FE-Buddy has no copy of either part of the virtual airline list yet, so this run used its older copy of the whole list{from}.")
			{
				IsAdvisory = true
			});
		}
	}

	/// <summary>Reads a kept copy of a part of the virtual airline list, or says it can't be read; <see langword="null"/> without one.</summary>
	private static IReadOnlyList<VatsimRadarAirline>? ReadPart(
		string? path,
		Func<string, IReadOnlyList<VatsimRadarAirline>> parse,
		string what,
		string withoutIt,
		List<ServiceMessage> messages)
	{
		if (path is null)
		{
			return null;
		}

		try
		{
			return parse(path);
		}
		catch (Exception ex)
		{
			messages.Add(UnreadableCopyMessage(TelephonySource, what, ex, withoutIt));
			return null;
		}
	}

	/// <summary>
	/// What the run says about one download: an Info note when it is fresh, or when a copy under a day
	/// old was used without downloading; an advisory warning with the kept copy's age when it fell back
	/// on that; and, with no copy at all, an error for a required file or an advisory warning for an
	/// optional one.
	/// </summary>
	/// <param name="refreshed">What the download did.</param>
	/// <param name="source">The message's log source.</param>
	/// <param name="what">What the file is, e.g. <c>Wx station data</c>.</param>
	/// <param name="withoutIt">What happens without any copy, e.g. <c>Wx Stations wrote nothing</c>.</param>
	/// <param name="nowUtc">Now, to say how old a kept copy is.</param>
	/// <param name="required">Whether the sub-service cannot run without the file.</param>
	/// <returns>The message.</returns>
	internal static ServiceMessage RefreshMessage(
		SharedDataRefreshResult refreshed,
		string source,
		string what,
		string withoutIt,
		DateTime nowUtc,
		bool required)
	{
		if (refreshed.IsFresh)
		{
			return new ServiceMessage(LogLevel.Info, source, $"Downloaded the latest {what}.");
		}

		if (refreshed.Reused && refreshed.DownloadedUtc is { } keptUtc)
		{
			return new ServiceMessage(LogLevel.Info, source,
				$"Used FE-Buddy's copy of the {what} from {keptUtc.ToLocalTime():d MMM yyyy HH:mm}: it's less than a day old, so it wasn't downloaded again.");
		}

		if (refreshed.DownloadedUtc is { } downloadedUtc)
		{
			return new ServiceMessage(LogLevel.Warning, source,
				$"Couldn't download the latest {what} ({refreshed.FailureReason}), so this run used FE-Buddy's copy from " +
				$"{downloadedUtc.ToLocalTime():d MMM yyyy} ({DescribeAge(nowUtc - downloadedUtc)}).")
			{
				IsAdvisory = true
			};
		}

		string text = $"Couldn't download the {what} ({refreshed.FailureReason}), and FE-Buddy has no earlier copy, so {withoutIt}. " +
			"Check the internet connection and run again.";

		return required
			? new ServiceMessage(LogLevel.Error, source, text)
			: new ServiceMessage(LogLevel.Warning, source, text) { IsAdvisory = true };
	}

	/// <summary>How old a kept copy is, in words: <c>less than a day old</c>, <c>1 day old</c>, <c>15 days old</c>.</summary>
	internal static string DescribeAge(TimeSpan age)
	{
		int days = (int)Math.Floor(age.TotalDays);

		return days switch
		{
			< 1 => "less than a day old",
			1 => "1 day old",
			_ => $"{days} days old",
		};
	}

	/// <summary>A kept copy that no longer parses - checked when it was downloaded, so damaged since.</summary>
	private static ServiceMessage UnreadableCopyMessage(string source, string what, Exception ex, string withoutIt) =>
		new(LogLevel.Error, source, $"FE-Buddy's copy of the {what} can't be read ({ex.Message}), so {withoutIt}.");
}
