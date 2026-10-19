using System.Diagnostics;
using System.Globalization;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Dtpp;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Dtpp.Parsers;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Nasr;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.Core.Application.Airac;

/// <summary>
/// Holds up to three cycles of parsed NASR data (previous / current / next) in memory and
/// runs the launch pipeline that fills it: probe each cycle's publication state, then, for
/// each published cycle in the order current -&gt; next -&gt; previous, download the CSVs and
/// parse them - one parse at a time so peak memory is one in-flight parse plus the finished
/// datasets. Only the NASR groups FE-Buddy reads are parsed (<see cref="NasrCsvGroups.InUse"/>).
/// The same pipeline also fills in each cycle's FAA d-TPP Metafile - not part of the NASR cycle,
/// but published per cycle - see <see cref="GetDtppAsync"/> - best-effort and never blocking
/// readiness.
/// </summary>
/// <remarks>
/// <para>
/// The service is ready as soon as the current cycle is (see <see cref="ComputeReadiness"/>):
/// the next and previous cycles go on loading in the background, and asking for one that is
/// still loading waits for it. A not-yet-published next cycle is normal; a failed previous or
/// next cycle only degrades the service.
/// </para>
/// <para>
/// Data that is not published per cycle - the Wx Stations list and the FAA telephony pages - is not
/// kept here at all: every AIRAC Service run downloads the latest copy of it into one shared folder
/// (see <see cref="Infrastructure.SharedData.SharedDataDownload"/>).
/// </para>
/// <para>
/// Each pipeline step is a parameter, so tests can run the pipeline without the FAA or real CSV
/// files. The parameterless constructor wires in the real steps.
/// </para>
/// </remarks>
/// <param name="probe">Publication probe for a cycle.</param>
/// <param name="download">Downloads a cycle's CSVs and returns the local folder.</param>
/// <param name="parse">Parses a cycle folder into a <see cref="NasrCsvDataCollection"/>.</param>
/// <param name="isLocallyAvailable">
/// Whether a cycle's CSVs are already cached, so the pipeline can skip announcing
/// <see cref="CycleDataState.Downloading"/> when <paramref name="download"/> is about to
/// resolve instantly from disk rather than actually fetch anything. Defaults to "never
/// cached", so the state always passes through Downloading.
/// </param>
/// <param name="pruneAllBut">
/// Deletes every cached cycle except the IDs given. Defaults to doing nothing, so a test
/// never touches the real cycle cache.
/// </param>
/// <param name="ensureDtpp">
/// Ensures a cycle folder has its FAA d-TPP Metafile, given the folder and the cycle's AIRAC ID.
/// Defaults to doing nothing, so a test never touches the network.
/// </param>
/// <param name="loadDtpp">
/// Reads a cycle folder's d-TPP Metafile data, or <see langword="null"/> when it is not there
/// yet. Defaults to always returning <see langword="null"/>.
/// </param>
/// <param name="cycleDirectory">
/// Where a cycle on disk is (one <paramref name="isLocallyAvailable"/> says is there), for a dev
/// launch that leaves out downloading and so never calls <paramref name="download"/>. Defaults to
/// its folder in FE-Buddy's cycle cache.
/// </param>
public sealed class AiracCycleDataCache(
	Func<AiracCycleInfo, CancellationToken, Task<AiracCyclePublicationState>> probe,
	Func<AiracCycleInfo, CancellationToken, Task<string>> download,
	Func<string, CancellationToken, Task<NasrCsvDataCollection>> parse,
	Func<AiracCycleInfo, bool>? isLocallyAvailable = null,
	Action<IReadOnlyCollection<string>>? pruneAllBut = null,
	Func<string, string, CancellationToken, Task>? ensureDtpp = null,
	Func<string, CancellationToken, Task<DtppMetafileDataCollection?>>? loadDtpp = null,
	Func<AiracCycleInfo, string>? cycleDirectory = null)
{
	private const string LogSource = "AiracCycleCache";

	private readonly Func<AiracCycleInfo, CancellationToken, Task<AiracCyclePublicationState>> _probe = probe ?? throw new ArgumentNullException(nameof(probe));
	private readonly Func<AiracCycleInfo, CancellationToken, Task<string>> _download = download ?? throw new ArgumentNullException(nameof(download));
	private readonly Func<string, CancellationToken, Task<NasrCsvDataCollection>> _parse = parse ?? throw new ArgumentNullException(nameof(parse));
	private readonly Func<AiracCycleInfo, bool> _isLocallyAvailable = isLocallyAvailable ?? (_ => false);
	private readonly Action<IReadOnlyCollection<string>> _pruneAllBut = pruneAllBut ?? (_ => { });
	private readonly Func<string, string, CancellationToken, Task> _ensureDtpp = ensureDtpp ?? ((_, _, _) => Task.CompletedTask);
	private readonly Func<string, CancellationToken, Task<DtppMetafileDataCollection?>> _loadDtpp = loadDtpp ?? ((_, _) => Task.FromResult<DtppMetafileDataCollection?>(null));
	private readonly Func<AiracCycleInfo, string> _cycleDirectory = cycleDirectory
		?? (cycle => Path.Combine(NasrCycleDownloader.GetDefaultCacheRoot(), cycle.AiracCycleId));

	private readonly Lock _gate = new();
	private readonly List<AiracCycleDataCacheEntry> _entries = [];
	private readonly Dictionary<string, Task<NasrCsvDataCollection>> _flights = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>What the last launch pipeline was asked to do.</summary>
	private AiracLaunchPlan _plan = AiracLaunchPlan.Everything;

	/// <summary>Whether the launch pipeline is running.</summary>
	private bool _preparing;

	/// <summary>
	/// The last parse queued, which the next one starts after - launch's or one asked for later - so
	/// only one dataset is ever being built. It never faults.
	/// </summary>
	private Task _parseQueue = Task.CompletedTask;

	/// <summary>Times the launch pipeline, for the log.</summary>
	private Stopwatch? _preparation;

	/// <summary>
	/// Creates a cache wired to the real FAA download, the real NASR CSV parser (the groups
	/// FE-Buddy reads only), and the real d-TPP Metafile download/parse.
	/// </summary>
	public AiracCycleDataCache()
		: this(
			probe: (cycle, ct) => AiracCycleAvailability.ProbeAsync(cycle, httpClient: null, ct),
			download: (cycle, ct) => NasrCycleDownloader.EnsureCycleAvailableAsync(cycle, cacheRootDirectory: null, progress: null, ct),
			parse: ParseInUseAsync,
			isLocallyAvailable: cycle => NasrCycleDownloader.IsCycleAvailableLocally(cycle, cacheRootDirectory: null),
			pruneAllBut: cycleIds => NasrCycleDownloader.PruneStaleCycles(cycleIds, cacheRootDirectory: null),
			ensureDtpp: (cycleDirectory, cycleId, ct) => new DtppDownloader().EnsureCycleHasMetafileAsync(cycleDirectory, cycleId, ct),
			loadDtpp: LoadDtppFromDiskAsync)
	{
	}

	/// <summary>The real cache's parse: only the NASR groups FE-Buddy reads (<see cref="NasrCsvGroups.InUse"/>).</summary>
	/// <param name="cycleDirectory">The cycle's folder.</param>
	/// <param name="cancellationToken">Unused: a parse runs to the end once started.</param>
	/// <returns>The parsed groups.</returns>
	internal static Task<NasrCsvDataCollection> ParseInUseAsync(string cycleDirectory, CancellationToken cancellationToken) =>
		NasrCsvParser.ParseAsync(cycleDirectory, NasrCsvGroups.InUse);

	/// <summary>Reads a cycle folder's <see cref="DtppFiles.FileName"/>, or <see langword="null"/> when it is not there.</summary>
	private static async Task<DtppMetafileDataCollection?> LoadDtppFromDiskAsync(string cycleDirectory, CancellationToken cancellationToken)
	{
		string path = Path.Combine(cycleDirectory, DtppFiles.FileName);

		if (!File.Exists(path))
		{
			return null;
		}

		return await DtppMetafileXmlParser.ParseAsync(path, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>The process-wide cache the GUI binds to.</summary>
	public static AiracCycleDataCache Instance { get; private set; } = new();

	/// <summary>
	/// Replaces <see cref="Instance"/> with a cache built from injected steps, or restores a
	/// real one when <paramref name="cache"/> is <see langword="null"/>. Unit tests only.
	/// </summary>
	/// <param name="cache">The cache to use.</param>
	internal static void ConfigureForTesting(AiracCycleDataCache? cache) => Instance = cache ?? new();

	/// <summary>Raised (with the entry that changed) on every cycle state transition. The GUI marshals to its dispatcher.</summary>
	public event EventHandler<AiracCycleDataCacheEntry>? StateChanged;

	/// <summary>
	/// The three cycle entries, once
	/// <see cref="PrepareCyclesAsync"/>
	/// has been called.
	/// </summary>
	public IReadOnlyList<AiracCycleDataCacheEntry> Entries
	{
		get
		{
			lock (_gate)
			{
				return [.. _entries];
			}
		}
	}

	/// <summary>Gets the entry for a cycle ID, or <see langword="null"/> if it is not tracked.</summary>
	/// <param name="cycleId">The four-digit cycle ID.</param>
	/// <returns>The entry, or <see langword="null"/>.</returns>
	public AiracCycleDataCacheEntry? GetEntry(string cycleId)
	{
		lock (_gate)
		{
			return _entries.FirstOrDefault(e => string.Equals(e.Cycle.AiracCycleId, cycleId, StringComparison.OrdinalIgnoreCase));
		}
	}

	/// <summary>
	/// Runs the launch pipeline for the three cycles as <paramref name="plan"/> says - all of it in
	/// a normal launch: probe all three, then download and parse each published cycle in the order
	/// current -&gt; next -&gt; previous, one parse at a time. Never throws - a failed cycle is marked
	/// <see cref="CycleDataState.Failed"/>. The three cycles are always tracked, whatever the plan: a
	/// cycle it leaves unloaded is downloaded and parsed when something asks for it
	/// (<see cref="GetAsync"/>). Without <see cref="AiracLaunchPlan.Download"/>, nothing touches the
	/// network: a cycle already on disk is found by its folder alone, and only those can be parsed.
	/// </summary>
	/// <param name="previous">The previous cycle identity.</param>
	/// <param name="current">The current cycle identity.</param>
	/// <param name="next">The next cycle identity.</param>
	/// <param name="plan">What to do with them.</param>
	/// <param name="cancellationToken">Cancels remaining work.</param>
	public async Task PrepareCyclesAsync(
		AiracCycleInfo previous,
		AiracCycleInfo current,
		AiracCycleInfo next,
		AiracLaunchPlan plan,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(previous);
		ArgumentNullException.ThrowIfNull(current);
		ArgumentNullException.ThrowIfNull(next);
		ArgumentNullException.ThrowIfNull(plan);

		_preparation = Stopwatch.StartNew();

		(AiracCycleDataCacheEntry currentEntry, AiracCycleDataCacheEntry previousEntry, AiracCycleDataCacheEntry nextEntry) =
			Register(previous, current, next, plan);

		try
		{
			AppLog.Info(LogSource, $"Preparing AIRAC cycles: previous {previous.AiracCycleId}, current {current.AiracCycleId}, next {next.AiracCycleId}.");

			// Only these three cycles are ever offered, so any other cached cycle is dead weight -
			// without this the cache grows by a cycle every 28 days.
			if (plan.Prune)
			{
				_pruneAllBut([previous.AiracCycleId, current.AiracCycleId, next.AiracCycleId]);
			}

			// Current first (what the user almost always wants, and all the service waits for), then
			// next (what they want next, when it's published), then previous (rarely wanted).
			AiracCycleDataCacheEntry[] inOrder = [currentEntry, nextEntry, previousEntry];

			if (plan.Download)
			{
				// Probe all three concurrently - the probe is a cheap HEAD.
				await Task.WhenAll(inOrder.Select(entry => ProbeEntryAsync(entry, cancellationToken))).ConfigureAwait(false);
			}
			else
			{
				FindCyclesOnDisk(inOrder);
			}

			// Downloads are network/disk-bound and parses are CPU/memory-bound, so they overlap: while
			// one cycle parses, the next one downloads. Downloads stay one at a time (a saturated link
			// gains nothing from more, and current must land first) and parses stay one at a time (each
			// holds the whole dataset, so parsing two at once doubles the peak memory on a small machine).
			List<Task> parses = [];

			foreach (AiracCycleDataCacheEntry entry in inOrder)
			{
				cancellationToken.ThrowIfCancellationRequested();

				if (plan.Download)
				{
					await WaitForDownloadAsync(entry, retryFailed: true, cancellationToken).ConfigureAwait(false);
				}

				if (plan.Parse && StartParse(entry, cancellationToken) is { } parse)
				{
					parses.Add(ObserveParseAsync(entry, parse));
				}
			}

			await Task.WhenAll(parses).ConfigureAwait(false);
		}
		finally
		{
			_preparation.Stop();

			lock (_gate)
			{
				_preparing = false;
			}
		}

		AppLog.Info(LogSource,
			$"AIRAC cycle preparation finished in {Milliseconds(_preparation)}. Readiness: {ComputeReadiness()}.");
	}

	/// <summary>
	/// Without downloading, marks each cycle already on disk as downloaded, from its folder alone, and
	/// says which aren't there: those download when something needs them.
	/// </summary>
	private void FindCyclesOnDisk(IEnumerable<AiracCycleDataCacheEntry> entries)
	{
		foreach (AiracCycleDataCacheEntry entry in entries)
		{
			if (_isLocallyAvailable(entry.Cycle))
			{
				entry.CycleDirectory = _cycleDirectory(entry.Cycle);
				FindDtppOnDisk(entry, entry.CycleDirectory);
				SetState(entry, CycleDataState.Downloaded);
			}
			else
			{
				AppLog.Info(LogSource,
					$"Cycle {entry.Cycle.AiracCycleId} isn't on disk and wasn't downloaded: this dev launch leaves out downloading AIRAC data. " +
					"It downloads when something needs it.");
			}
		}
	}

	/// <summary>Starts tracking the three cycles, in place of any tracked before.</summary>
	private (AiracCycleDataCacheEntry Current, AiracCycleDataCacheEntry Previous, AiracCycleDataCacheEntry Next) Register(
		AiracCycleInfo previous,
		AiracCycleInfo current,
		AiracCycleInfo next,
		AiracLaunchPlan plan)
	{
		lock (_gate)
		{
			_plan = plan;
			_preparing = true;
			_entries.Clear();
			AiracCycleDataCacheEntry currentEntry = new(current, AiracCyclePosition.Current);
			AiracCycleDataCacheEntry previousEntry = new(previous, AiracCyclePosition.Previous);
			AiracCycleDataCacheEntry nextEntry = new(next, AiracCyclePosition.Next);
			_entries.Add(currentEntry);
			_entries.Add(previousEntry);
			_entries.Add(nextEntry);
			return (currentEntry, previousEntry, nextEntry);
		}
	}

	/// <summary>A stopwatch's time so far, for the log, e.g. <c>1,234 ms</c>.</summary>
	private static string Milliseconds(Stopwatch stopwatch) =>
		string.Create(CultureInfo.InvariantCulture, $"{stopwatch.ElapsedMilliseconds:N0} ms");

	/// <summary>
	/// Returns the parsed data for a cycle, awaiting an in-flight parse rather than starting a
	/// second one. If the cycle has not been downloaded/parsed yet, this kicks that off.
	/// </summary>
	/// <param name="cycleId">The cycle ID to get.</param>
	/// <param name="cancellationToken">
	/// Stops this call waiting. A parse it starts is shared, so it carries on and finishes for every caller.
	/// </param>
	/// <returns>
	/// The parsed NASR data for the cycle. The task faults with <see cref="InvalidOperationException"/>
	/// when the cycle couldn't be prepared, or with the parse's own exception when its parse failed.
	/// </returns>
	/// <exception cref="InvalidOperationException">The cycle is not tracked, or it is not published.</exception>
	public Task<NasrCsvDataCollection> GetAsync(string cycleId, CancellationToken cancellationToken = default)
	{
		AiracCycleDataCacheEntry? entry = GetEntry(cycleId)
			?? throw new InvalidOperationException($"Cycle '{cycleId}' is not tracked by the cache. Call {nameof(PrepareCyclesAsync)} first.");

		lock (_gate)
		{
			// Already parsed by the launch pipeline - hand back the same task, no re-parse.
			if (entry.State == CycleDataState.Ready && entry.ParseTask is { IsCompletedSuccessfully: true } completed)
			{
				return completed;
			}

			// A download+parse is already in flight for this cycle (from here or the pipeline).
			if (_flights.TryGetValue(entry.Cycle.AiracCycleId, out Task<NasrCsvDataCollection>? existingFlight))
			{
				return existingFlight;
			}

			if (entry.State == CycleDataState.NotYetPublished)
			{
				throw new InvalidOperationException($"Cycle '{cycleId}' has not been published by the FAA yet.");
			}

			Task<NasrCsvDataCollection> flight = RunFlightAsync(entry, cancellationToken);
			_flights[entry.Cycle.AiracCycleId] = flight;
			return flight;
		}
	}

	/// <summary>
	/// Returns a cycle's FAA d-TPP Metafile data, parsed fresh from its folder on every call -
	/// there is no need to memoize it the way <see cref="GetAsync"/> does for the much larger NASR
	/// data. A tracked cycle still downloading is waited for (at launch, the previous cycle often
	/// is when a run starts, and the Procedure Charts changes report needs its metafile), and one
	/// not downloaded yet is downloaded now.
	/// </summary>
	/// <param name="cycleId">The cycle ID to get.</param>
	/// <param name="cancellationToken">Cancels the wait and the read.</param>
	/// <returns>
	/// The parsed d-TPP Metafile data; <see langword="null"/> when <paramref name="cycleId"/> is
	/// not tracked by the cache (this never throws for an untracked cycle - the Procedures
	/// sub-service also asks for the cycle before the selected one, which may not be one of the
	/// three tracked cycles); or <see langword="null"/> when the
	/// cycle's folder does not have <see cref="DtppFiles.FileName"/> (not yet published by the
	/// FAA, or the download failed).
	/// </returns>
	public async Task<DtppMetafileDataCollection?> GetDtppAsync(string cycleId, CancellationToken cancellationToken = default)
	{
		if (GetEntry(cycleId) is not { } entry)
		{
			return null;
		}

		// A cycle that failed isn't tried again here: the metafile is best-effort, and a run
		// shouldn't sit through a download that just failed.
		await WaitForDownloadAsync(entry, retryFailed: false, cancellationToken).ConfigureAwait(false);

		return entry.CycleDirectory is { } cycleDirectory
			? await _loadDtpp(cycleDirectory, cancellationToken).ConfigureAwait(false)
			: null;
	}

	/// <summary>
	/// Where a cycle's FAA d-TPP Metafile is, if it has one - so the GUI can say up front that
	/// Procedures has nothing to run on (the General tab shows such a cycle as partial), rather than
	/// let a run fail on it.
	/// </summary>
	/// <remarks>
	/// Answers from memory, never the disk: the GUI asks on the UI thread every time a cycle's state
	/// changes, and the folder can be slow to answer (a roaming or redirected profile). The cache looks
	/// for the file when it learns the cycle's folder and again once the download has fetched it; the
	/// metafile is fetched before the cycle is parsed, so a ready cycle keeps its answer until the next launch.
	/// </remarks>
	/// <param name="cycleId">The cycle ID.</param>
	/// <returns>
	/// The full path of the cycle's <see cref="DtppFiles.FileName"/>, or <see langword="null"/>
	/// when the cycle is not tracked, not downloaded yet, or its folder does not have the file.
	/// </returns>
	public string? FindDtppFile(string cycleId)
	{
		AiracCycleDataCacheEntry? entry = GetEntry(cycleId);
		return entry?.CycleDirectory is null ? null : entry.DtppFile;
	}

	/// <summary>Looks for the cycle's metafile in its folder, for <see cref="FindDtppFile"/> to answer from.</summary>
	private static void FindDtppOnDisk(AiracCycleDataCacheEntry entry, string cycleDirectory)
	{
		string path = Path.Combine(cycleDirectory, DtppFiles.FileName);
		entry.DtppFile = File.Exists(path) ? path : null;
	}

	private async Task<NasrCsvDataCollection> RunFlightAsync(AiracCycleDataCacheEntry entry, CancellationToken cancellationToken)
	{
		try
		{
			// The parse is shared by every caller, so it never gets a caller's token: cancelling only
			// stops this call waiting, and the parse finishes for everyone.
			if (entry.ParseTask is { } inFlightParse)
			{
				return await inFlightParse.WaitAsync(cancellationToken).ConfigureAwait(false);
			}

			await WaitForDownloadAsync(entry, retryFailed: true, cancellationToken).ConfigureAwait(false);

			if (StartParse(entry, CancellationToken.None) is { } started)
			{
				await ObserveParseAsync(entry, started).WaitAsync(cancellationToken).ConfigureAwait(false);
			}
			else if (entry.ParseTask is { } queued)
			{
				// The launch pipeline queued this cycle's parse while the download above was waited for.
				return await queued.WaitAsync(cancellationToken).ConfigureAwait(false);
			}

			if (entry.State == CycleDataState.Ready && entry.ParseTask is { } parsed)
			{
				return await parsed.WaitAsync(cancellationToken).ConfigureAwait(false);
			}

			// Found only now, when the download asked first: a dev launch that left out downloading
			// never asked at launch.
			if (entry.State == CycleDataState.NotYetPublished)
			{
				throw new InvalidOperationException($"Cycle '{entry.Cycle.AiracCycleId}' has not been published by the FAA yet.");
			}

			throw new InvalidOperationException($"Cycle '{entry.Cycle.AiracCycleId}' could not be prepared (state: {entry.State}).");
		}
		finally
		{
			lock (_gate)
			{
				_flights.Remove(entry.Cycle.AiracCycleId);
			}
		}
	}

	/// <summary>
	/// Decides the AIRAC Service's overall readiness from the three cycles' states. Only the
	/// current cycle is waited for: the service is ready as soon as it is, while the next and
	/// previous cycles go on loading. A failed previous or next cycle degrades the service; a
	/// not-yet-published next cycle is normal.
	/// </summary>
	/// <returns>The readiness verdict.</returns>
	public AiracCycleReadiness ComputeReadiness()
	{
		lock (_gate)
		{
			AiracCycleDataCacheEntry? current = _entries.FirstOrDefault(e => e.Position == AiracCyclePosition.Current);

			if (current is null)
			{
				return AiracCycleReadiness.Waiting;
			}

			// The FAA always has the current cycle, so one it seems not to have (a proxy or firewall
			// answering for its site, or a PC clock set ahead) won't arrive by waiting: it is as
			// unavailable as one that failed.
			if (current.State is CycleDataState.Failed or CycleDataState.NotYetPublished)
			{
				return AiracCycleReadiness.Unavailable;
			}

			if (current.State != CycleDataState.Ready)
			{
				// Only a dev launch leaves the current cycle for later: until something asks for it,
				// it isn't on its way.
				return _plan.LeavesCyclesToLoad && !IsLoadingLocked(current) ? AiracCycleReadiness.NotLoaded : AiracCycleReadiness.Waiting;
			}

			bool anyBestEffortFailed = _entries.Any(e => e.Position != AiracCyclePosition.Current && e.State == CycleDataState.Failed);
			return anyBestEffortFailed ? AiracCycleReadiness.Degraded : AiracCycleReadiness.Ready;
		}
	}

	/// <summary>
	/// Whether a cycle has finished loading, one way or another: parsed, failed, or not published
	/// yet. Anything else is still to come.
	/// </summary>
	/// <param name="entry">The cycle.</param>
	/// <returns><see langword="true"/> when nothing more will happen to it.</returns>
	public static bool IsSettled(AiracCycleDataCacheEntry entry)
	{
		ArgumentNullException.ThrowIfNull(entry);
		return entry.State is CycleDataState.Ready or CycleDataState.Failed or CycleDataState.NotYetPublished;
	}

	/// <summary>
	/// Whether the launch pipeline left cycles for something to load later: only a dev launch that
	/// leaves out downloading or parsing does.
	/// </summary>
	public bool LoadsOnDemand
	{
		get
		{
			lock (_gate)
			{
				return _plan.LeavesCyclesToLoad;
			}
		}
	}

	/// <summary>
	/// Whether a cycle is on its way: downloading or parsing now, waiting its turn, or still to come in
	/// the launch pipeline. A cycle a dev launch left unloaded isn't, until something asks for it.
	/// </summary>
	/// <param name="entry">The cycle.</param>
	/// <returns><see langword="true"/> while it is loading.</returns>
	public bool IsLoading(AiracCycleDataCacheEntry entry)
	{
		ArgumentNullException.ThrowIfNull(entry);

		lock (_gate)
		{
			return IsLoadingLocked(entry);
		}
	}

	/// <summary><see cref="IsLoading"/>, for a caller holding <see cref="_gate"/>.</summary>
	private bool IsLoadingLocked(AiracCycleDataCacheEntry entry)
	{
		if (IsSettled(entry))
		{
			return false;
		}

		if (entry.DownloadTask is { IsCompleted: false }
			|| entry.ParseTask is { IsCompleted: false }
			|| _flights.ContainsKey(entry.Cycle.AiracCycleId)
			|| entry.State is CycleDataState.Downloading or CycleDataState.Parsing)
		{
			return true;
		}

		// Not started yet: the launch pipeline gets to it if it downloads, or parses what's on disk.
		return _preparing && entry.State switch
		{
			CycleDataState.NotDownloaded => _plan.Download,
			CycleDataState.Downloaded => _plan.Parse,
			_ => false,
		};
	}

	private async Task ProbeEntryAsync(AiracCycleDataCacheEntry entry, CancellationToken cancellationToken)
	{
		AiracCyclePublicationState state = await _probe(entry.Cycle, cancellationToken).ConfigureAwait(false);

		// An inconclusive probe gets one more try before we just attempt the download anyway.
		if (state == AiracCyclePublicationState.Unknown)
		{
			state = await _probe(entry.Cycle, cancellationToken).ConfigureAwait(false);
		}

		entry.Probed = true;
		SetState(entry, state switch
		{
			AiracCyclePublicationState.NotYetPublished => CycleDataState.NotYetPublished,
			_ => CycleDataState.NotDownloaded, // Published, or Unknown -> try the download and let it decide.
		});
	}

	/// <summary>
	/// Waits until a cycle is on disk, downloading it when nothing else is (see
	/// <see cref="StartDownload"/>). Returns at once for a cycle already downloaded or not yet
	/// published, and for one that failed unless <paramref name="retryFailed"/>.
	/// </summary>
	private async Task WaitForDownloadAsync(AiracCycleDataCacheEntry entry, bool retryFailed, CancellationToken cancellationToken)
	{
		while (true)
		{
			(Task download, bool startedHere) = StartDownload(entry, retryFailed, cancellationToken);

			try
			{
				await download.WaitAsync(cancellationToken).ConfigureAwait(false);
				return;
			}
			catch (OperationCanceledException) when (!startedHere && !cancellationToken.IsCancellationRequested)
			{
				// The download belonged to another caller, who cancelled it. This one still wants
				// the cycle, so it goes round and starts its own.
			}
		}
	}

	/// <summary>
	/// The cycle's download: the one under way, or a new one when the cycle still needs it.
	/// <c>StartedHere</c> says whether this call started it.
	/// </summary>
	private (Task Download, bool StartedHere) StartDownload(AiracCycleDataCacheEntry entry, bool retryFailed, CancellationToken cancellationToken)
	{
		lock (_gate)
		{
			if (entry.DownloadTask is { IsCompleted: false } underWay)
			{
				return (underWay, false);
			}

			bool needed = entry.CycleDirectory is null && entry.State switch
			{
				CycleDataState.NotYetPublished or CycleDataState.Ready => false,
				CycleDataState.Failed => retryFailed,
				_ => true,
			};

			if (!needed)
			{
				return (Task.CompletedTask, false);
			}

			Task download = DownloadWhenUnlockedAsync();
			entry.DownloadTask = download;
			return (download, true);
		}

		async Task DownloadWhenUnlockedAsync()
		{
			// Yield first so the DownloadTask assignment above completes (and the lock is released)
			// before any download work or state notification runs - to the thread pool, never back to
			// the caller's context: the caller can be the UI thread, and the download unzips a cycle.
			await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
			await DownloadStageAsync(entry, cancellationToken).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Downloads a cycle's CSVs (with one retry), then its d-TPP Metafile. Marks the cycle
	/// <see cref="CycleDataState.Downloaded"/>, or <see cref="CycleDataState.Failed"/> when the
	/// CSVs can't be downloaded. A cycle no one has probed yet (a dev launch that left out
	/// downloading) is probed first, so one the FAA hasn't published is said to be that, not failed.
	/// </summary>
	private async Task DownloadStageAsync(AiracCycleDataCacheEntry entry, CancellationToken cancellationToken)
	{
		if (!entry.Probed)
		{
			await ProbeEntryAsync(entry, cancellationToken).ConfigureAwait(false);

			if (entry.State == CycleDataState.NotYetPublished)
			{
				return;
			}
		}

		string? cycleDirectory = await DownloadWithOneRetryAsync(entry, cancellationToken).ConfigureAwait(false);
		if (cycleDirectory is null)
		{
			SetState(entry, CycleDataState.Failed);
			return;
		}

		entry.CycleDirectory = cycleDirectory;

		// Runs for an already-cached cycle too (the NASR download above just resolved instantly
		// from disk), which is what fills in a next cycle's metafile once the FAA publishes it, at
		// a later launch, or retries one whose earlier attempt failed.
		await EnsureDtppAsync(entry, cycleDirectory, cancellationToken).ConfigureAwait(false);
		FindDtppOnDisk(entry, cycleDirectory);

		SetState(entry, CycleDataState.Downloaded);
	}

	/// <summary>
	/// Ensures <paramref name="cycleDirectory"/> has its FAA d-TPP Metafile. Never fails or
	/// delay-fails the cycle: any failure but cancellation is logged and swallowed, so the NASR
	/// data this cycle otherwise has is not held back by a d-TPP Metafile problem (the FAA not
	/// having published it yet is already logged as Info by <see cref="DtppDownloader"/> and is
	/// not a failure at all). FE-Buddy retries at the next launch.
	/// </summary>
	private async Task EnsureDtppAsync(AiracCycleDataCacheEntry entry, string cycleDirectory, CancellationToken cancellationToken)
	{
		try
		{
			await _ensureDtpp(cycleDirectory, entry.Cycle.AiracCycleId, cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			AppLog.Warning(LogSource,
				$"d-TPP Metafile could not be downloaded for cycle {entry.Cycle.AiracCycleId}; the Procedure Charts sub-service " +
				$"can't run on it until it is (FE-Buddy retries at the next launch). {ex.Message}");
		}
	}

	/// <summary>
	/// Queues a downloaded cycle's parse behind any other (see <see cref="_parseQueue"/>), or
	/// returns <see langword="null"/> when it already has one or isn't downloaded. The returned
	/// task is recorded as the entry's <see cref="AiracCycleDataCacheEntry.ParseTask"/> straight
	/// away - even while it is still queued - so <see cref="GetAsync"/> awaits it instead of
	/// starting a second parse of the same folder. Whoever starts it observes it
	/// (<see cref="ObserveParseAsync"/>).
	/// </summary>
	private Task<NasrCsvDataCollection>? StartParse(AiracCycleDataCacheEntry entry, CancellationToken cancellationToken)
	{
		lock (_gate)
		{
			if (entry.ParseTask is not null || entry.State != CycleDataState.Downloaded || entry.CycleDirectory is not { } cycleDirectory)
			{
				return null;
			}

			Task startAfter = _parseQueue;
			Task<NasrCsvDataCollection> parseTask = ParseWhenTurnAsync(cycleDirectory, startAfter);
			entry.ParseTask = parseTask;

			// The queue moves on whatever happens to this parse.
			_parseQueue = parseTask.ContinueWith(
				static _ => { }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

			return parseTask;
		}

		async Task<NasrCsvDataCollection> ParseWhenTurnAsync(string cycleDirectory, Task startAfter)
		{
			// Yield first so the ParseTask assignment above completes (and the lock is released)
			// before any parse work or state notification runs - to the thread pool, as the download does.
			await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
			await startAfter.ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();

			SetState(entry, CycleDataState.Parsing);
			Stopwatch parsing = Stopwatch.StartNew();
			NasrCsvDataCollection data = await _parse(cycleDirectory, cancellationToken).ConfigureAwait(false);

			AppLog.Info(LogSource,
				$"Cycle {entry.Cycle.AiracCycleId} parsed in {Milliseconds(parsing)} ({data.ParsedGroups().Count} groups).");

			SetState(entry, CycleDataState.Ready);
			AppLog.Success(LogSource, $"Cycle {entry.Cycle.AiracCycleId} ready.");

			if (entry.Position == AiracCyclePosition.Current && _preparation is { IsRunning: true } preparation)
			{
				AppLog.Info(LogSource, $"The AIRAC Service is ready: the current cycle was ready {Milliseconds(preparation)} after AIRAC preparation began.");
			}

			return data;
		}
	}

	/// <summary>
	/// Waits for a parse and turns a failure into <see cref="CycleDataState.Failed"/>. Only
	/// cancellation propagates.
	/// </summary>
	private async Task ObserveParseAsync(AiracCycleDataCacheEntry entry, Task<NasrCsvDataCollection> parseTask)
	{
		try
		{
			await parseTask.ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			AppLog.Warning(LogSource, $"Cycle {entry.Cycle.AiracCycleId}: parse failed ({ex.Message}).");
			SetState(entry, CycleDataState.Failed);
		}
	}

	private async Task<string?> DownloadWithOneRetryAsync(AiracCycleDataCacheEntry entry, CancellationToken cancellationToken)
	{
		for (int attempt = 1; attempt <= 2; attempt++)
		{
			cancellationToken.ThrowIfCancellationRequested();

			// The CSVs are already on disk - EnsureCycleAvailableAsync (the real _download) is
			// about to resolve instantly without touching the network. Saying "Downloading" here
			// would be a flat-out lie the user has no way to tell apart from a real fetch.
			if (!_isLocallyAvailable(entry.Cycle))
			{
				SetState(entry, CycleDataState.Downloading);
			}

			try
			{
				return await _download(entry.Cycle, cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				if (attempt == 1)
				{
					AppLog.Info(LogSource, $"Cycle {entry.Cycle.AiracCycleId}: download attempt 1 failed ({ex.Message}). Retrying once.");
				}
				else
				{
					AppLog.Warning(LogSource, $"Cycle {entry.Cycle.AiracCycleId}: download failed after a retry ({ex.Message}).");
				}
			}
		}

		return null;
	}

	private void SetState(AiracCycleDataCacheEntry entry, CycleDataState state)
	{
		bool changed;
		lock (_gate)
		{
			changed = entry.State != state;
			entry.State = state;
		}

		if (changed)
		{
			AppLog.Info(LogSource, $"Cycle {entry.Cycle.AiracCycleId} ({entry.Position}): {state}.");
			try
			{
				StateChanged?.Invoke(this, entry);
			}
			catch
			{
				// A misbehaving subscriber must not break the pipeline.
			}
		}
	}
}
