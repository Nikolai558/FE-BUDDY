using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Nasr;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.Core.Application.Airac;

/// <summary>
/// One cycle's slot in <see cref="AiracCycleDataCache"/>: its identity, current pipeline
/// state, the local folder its CSVs live in once downloaded, and the parse task the parsed
/// data can be awaited from.
/// </summary>
public sealed class AiracCycleDataCacheEntry
{
	internal AiracCycleDataCacheEntry(AiracCycleInfo cycle, AiracCyclePosition position)
	{
		Cycle = cycle;
		Position = position;
	}

	/// <summary>The cycle's identity (ID, effective date, CSV date string).</summary>
	public AiracCycleInfo Cycle { get; }

	/// <summary>Where this cycle sits relative to today.</summary>
	public AiracCyclePosition Position { get; }

	/// <summary>The cycle's current pipeline state.</summary>
	public CycleDataState State { get; internal set; } = CycleDataState.NotDownloaded;

	/// <summary>The local folder holding this cycle's CSV files, once downloaded.</summary>
	public string? CycleDirectory { get; internal set; }

	/// <summary>
	/// The in-flight (or completed) parse task. Awaiting this is how a caller gets the parsed
	/// data without triggering a second parse.
	/// </summary>
	public Task<NasrCsvDataCollection>? ParseTask { get; internal set; }
}

/// <summary>
/// Holds up to three cycles of parsed NASR data (previous / current / next) in memory and
/// runs the launch pipeline that fills it: probe each cycle's publication state, then, for
/// each published cycle in the order current -&gt; previous -&gt; next, download the CSVs and
/// parse them - one parse at a time so peak memory is one in-flight parse plus the finished
/// datasets.
/// </summary>
/// <remarks>
/// <para>
/// A not-yet-published next cycle is normal and never blocks the service (see
/// <see cref="ComputeReadiness"/>). The current cycle is mandatory; a failed previous or next
/// cycle only degrades the service.
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
public sealed class AiracCycleDataCache(
	Func<AiracCycleInfo, CancellationToken, Task<AiracCyclePublicationState>> probe,
	Func<AiracCycleInfo, CancellationToken, Task<string>> download,
	Func<string, CancellationToken, Task<NasrCsvDataCollection>> parse,
	Func<AiracCycleInfo, bool>? isLocallyAvailable = null,
	Action<IReadOnlyCollection<string>>? pruneAllBut = null)
{
	private const string LogSource = "AiracCycleCache";

	private readonly Func<AiracCycleInfo, CancellationToken, Task<AiracCyclePublicationState>> _probe = probe ?? throw new ArgumentNullException(nameof(probe));
	private readonly Func<AiracCycleInfo, CancellationToken, Task<string>> _download = download ?? throw new ArgumentNullException(nameof(download));
	private readonly Func<string, CancellationToken, Task<NasrCsvDataCollection>> _parse = parse ?? throw new ArgumentNullException(nameof(parse));
	private readonly Func<AiracCycleInfo, bool> _isLocallyAvailable = isLocallyAvailable ?? (_ => false);
	private readonly Action<IReadOnlyCollection<string>> _pruneAllBut = pruneAllBut ?? (_ => { });

	private readonly Lock _gate = new();
	private readonly List<AiracCycleDataCacheEntry> _entries = [];
	private readonly Dictionary<string, Task<NasrCsvDataCollection>> _flights = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Creates a cache wired to the real FAA download and the real NASR CSV parser.
	/// </summary>
	public AiracCycleDataCache()
		: this(
			probe: (cycle, ct) => AiracCycleAvailability.ProbeAsync(cycle, httpClient: null, ct),
			download: (cycle, ct) => NasrCycleDownloader.EnsureCycleAvailableAsync(cycle, cacheRootDirectory: null, progress: null, ct),
			// TODO (perf): this parses every NASR group, but the sub-services only read APT, AWY,
			// CLS_ARSP, DP, FIX, FRQ, NAV and STAR. Parsing just those would save memory and
			// launch time.
			parse: (dir, ct) => NasrCsvParser.ParseAllAsync(dir),
			isLocallyAvailable: cycle => NasrCycleDownloader.IsCycleAvailableLocally(cycle, cacheRootDirectory: null),
			pruneAllBut: cycleIds => NasrCycleDownloader.PruneStaleCycles(cycleIds, cacheRootDirectory: null))
	{
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

	/// <summary>The three cycle entries, once <see cref="PrepareCyclesAsync"/> has been called.</summary>
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
	/// Runs the launch pipeline for the three cycles: probe all three, then download and parse
	/// each published cycle in the order current -&gt; previous -&gt; next, one parse at a time.
	/// Never throws - a failed cycle is marked <see cref="CycleDataState.Failed"/>.
	/// </summary>
	/// <param name="previous">The previous cycle identity.</param>
	/// <param name="current">The current cycle identity.</param>
	/// <param name="next">The next cycle identity.</param>
	/// <param name="cancellationToken">Cancels remaining work.</param>
	public async Task PrepareCyclesAsync(
		AiracCycleInfo previous,
		AiracCycleInfo current,
		AiracCycleInfo next,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(previous);
		ArgumentNullException.ThrowIfNull(current);
		ArgumentNullException.ThrowIfNull(next);

		AiracCycleDataCacheEntry currentEntry;
		AiracCycleDataCacheEntry previousEntry;
		AiracCycleDataCacheEntry nextEntry;

		lock (_gate)
		{
			_entries.Clear();
			currentEntry = new AiracCycleDataCacheEntry(current, AiracCyclePosition.Current);
			previousEntry = new AiracCycleDataCacheEntry(previous, AiracCyclePosition.Previous);
			nextEntry = new AiracCycleDataCacheEntry(next, AiracCyclePosition.Next);
			_entries.Add(currentEntry);
			_entries.Add(previousEntry);
			_entries.Add(nextEntry);
		}

		AppLog.Info(LogSource, $"Preparing AIRAC cycles: previous {previous.AiracCycleId}, current {current.AiracCycleId}, next {next.AiracCycleId}.");

		// Only these three cycles are ever offered, so any other cached cycle is dead weight -
		// without this the cache grows by a cycle every 28 days.
		_pruneAllBut([previous.AiracCycleId, current.AiracCycleId, next.AiracCycleId]);

		// Probe all three concurrently - the probe is a cheap HEAD.
		await Task.WhenAll(
			ProbeEntryAsync(currentEntry, cancellationToken),
			ProbeEntryAsync(previousEntry, cancellationToken),
			ProbeEntryAsync(nextEntry, cancellationToken)).ConfigureAwait(false);

		// Current first (what the user almost always wants), then previous (always published),
		// then next (may not be). Downloads are network/disk-bound and parses are CPU/memory-bound,
		// so they overlap: while one cycle parses, the next one downloads. Downloads stay one at a
		// time (a saturated link gains nothing from more, and current must land first) and parses
		// stay one at a time (each holds the whole dataset, so parsing two at once doubles the
		// peak memory on a small machine).
		Task parseChain = Task.CompletedTask;

		foreach (AiracCycleDataCacheEntry entry in new[] { currentEntry, previousEntry, nextEntry })
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (await DownloadStageAsync(entry, cancellationToken).ConfigureAwait(false))
			{
				parseChain = ObserveParseAsync(entry, StartParse(entry, parseChain, cancellationToken));
			}
		}

		await parseChain.ConfigureAwait(false);

		AppLog.Info(LogSource, $"AIRAC cycle preparation finished. Readiness: {ComputeReadiness()}.");
	}

	/// <summary>
	/// Returns the parsed data for a cycle, awaiting an in-flight parse rather than starting a
	/// second one. If the cycle has not been downloaded/parsed yet, this kicks that off.
	/// </summary>
	/// <param name="cycleId">The cycle ID to get.</param>
	/// <param name="cancellationToken">Cancels a parse this call starts.</param>
	/// <returns>The parsed NASR data for the cycle.</returns>
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

	private async Task<NasrCsvDataCollection> RunFlightAsync(AiracCycleDataCacheEntry entry, CancellationToken cancellationToken)
	{
		try
		{
			if (entry.ParseTask is { } inFlightParse)
			{
				return await inFlightParse.ConfigureAwait(false);
			}

			await DownloadAndParseAsync(entry, cancellationToken).ConfigureAwait(false);

			if (entry.State == CycleDataState.Ready && entry.ParseTask is { } parsed)
			{
				return await parsed.ConfigureAwait(false);
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
	/// Decides the AIRAC Service's overall readiness from the three cycles' states. The current
	/// cycle is mandatory; a failed previous or next cycle only degrades the service; a
	/// not-yet-published next cycle is normal.
	/// </summary>
	/// <returns>The readiness verdict.</returns>
	public AiracCycleReadiness ComputeReadiness()
	{
		lock (_gate)
		{
			AiracCycleDataCacheEntry? current = _entries.FirstOrDefault(e => e.Position == AiracCyclePosition.Current);
			AiracCycleDataCacheEntry? previous = _entries.FirstOrDefault(e => e.Position == AiracCyclePosition.Previous);
			AiracCycleDataCacheEntry? next = _entries.FirstOrDefault(e => e.Position == AiracCyclePosition.Next);

			if (current is null || previous is null || next is null)
			{
				return AiracCycleReadiness.Waiting;
			}

			if (current.State == CycleDataState.Failed)
			{
				return AiracCycleReadiness.Unavailable;
			}

			bool currentReady = current.State == CycleDataState.Ready;
			bool previousSettled = previous.State is CycleDataState.Ready or CycleDataState.Failed;
			bool nextSettled = next.State is CycleDataState.Ready or CycleDataState.NotYetPublished or CycleDataState.Failed;

			if (!(currentReady && previousSettled && nextSettled))
			{
				return AiracCycleReadiness.Waiting;
			}

			bool anyBestEffortFailed = previous.State == CycleDataState.Failed || next.State == CycleDataState.Failed;
			return anyBestEffortFailed ? AiracCycleReadiness.Degraded : AiracCycleReadiness.Ready;
		}
	}

	private async Task ProbeEntryAsync(AiracCycleDataCacheEntry entry, CancellationToken cancellationToken)
	{
		AiracCyclePublicationState state = await _probe(entry.Cycle, cancellationToken).ConfigureAwait(false);

		// An inconclusive probe gets one more try before we just attempt the download anyway.
		if (state == AiracCyclePublicationState.Unknown)
		{
			state = await _probe(entry.Cycle, cancellationToken).ConfigureAwait(false);
		}

		SetState(entry, state switch
		{
			AiracCyclePublicationState.NotYetPublished => CycleDataState.NotYetPublished,
			_ => CycleDataState.NotDownloaded, // Published, or Unknown -> try the download and let it decide.
		});
	}

	private async Task DownloadAndParseAsync(AiracCycleDataCacheEntry entry, CancellationToken cancellationToken)
	{
		if (await DownloadStageAsync(entry, cancellationToken).ConfigureAwait(false))
		{
			await ObserveParseAsync(entry, StartParse(entry, Task.CompletedTask, cancellationToken)).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Downloads a cycle's CSVs (with one retry). Returns <see langword="true"/> when the cycle
	/// is now on disk and ready to parse; <see langword="false"/> when there is nothing to parse
	/// (not yet published, already ready, or the download failed - already marked
	/// <see cref="CycleDataState.Failed"/>).
	/// </summary>
	private async Task<bool> DownloadStageAsync(AiracCycleDataCacheEntry entry, CancellationToken cancellationToken)
	{
		if (entry.State is CycleDataState.NotYetPublished or CycleDataState.Ready)
		{
			return false;
		}

		string? cycleDirectory = await DownloadWithOneRetryAsync(entry, cancellationToken).ConfigureAwait(false);
		if (cycleDirectory is null)
		{
			SetState(entry, CycleDataState.Failed);
			return false;
		}

		entry.CycleDirectory = cycleDirectory;
		SetState(entry, CycleDataState.Downloaded);
		return true;
	}

	/// <summary>
	/// Starts a cycle's parse, which begins once <paramref name="startAfter"/> (the previous
	/// cycle's parse) has finished. The returned task is recorded as the entry's
	/// <see cref="AiracCycleDataCacheEntry.ParseTask"/> straight away - even while it is still
	/// queued behind another parse - so <see cref="GetAsync"/> awaits it instead of starting a
	/// second parse of the same folder.
	/// </summary>
	private Task<NasrCsvDataCollection> StartParse(AiracCycleDataCacheEntry entry, Task startAfter, CancellationToken cancellationToken)
	{
		string cycleDirectory = entry.CycleDirectory!;

		Task<NasrCsvDataCollection> parseTask;
		lock (_gate)
		{
			parseTask = ParseWhenTurnAsync();
			entry.ParseTask = parseTask;
		}

		return parseTask;

		async Task<NasrCsvDataCollection> ParseWhenTurnAsync()
		{
			// Yield first so the ParseTask assignment above completes (and the lock is released)
			// before any parse work or state notification runs.
			await Task.Yield();
			await startAfter.ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();

			SetState(entry, CycleDataState.Parsing);
			NasrCsvDataCollection data = await _parse(cycleDirectory, cancellationToken).ConfigureAwait(false);
			SetState(entry, CycleDataState.Ready);
			AppLog.Success(LogSource, $"Cycle {entry.Cycle.AiracCycleId} ready.");
			return data;
		}
	}

	/// <summary>
	/// Waits for a parse and turns a failure into <see cref="CycleDataState.Failed"/>. The task
	/// it returns never faults on a parse error, so it is safe to use as the next parse's
	/// <c>startAfter</c> gate; only cancellation propagates.
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
