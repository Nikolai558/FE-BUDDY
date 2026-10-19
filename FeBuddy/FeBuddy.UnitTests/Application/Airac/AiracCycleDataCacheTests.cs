using System.Collections.Concurrent;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.UnitTests.Application.Airac;

/// <summary>
/// Exercises <see cref="AiracCycleDataCache"/> with injected probe/download/parse steps:
/// prepare order, single-flight parsing, one-retry-then-Failed, and the readiness table.
/// </summary>
[Collection("AppLog")]
public sealed class AiracCycleDataCacheTests : IDisposable
{
	private static readonly AiracCycleInfo Previous = new("2609", "03_Sep_2026", new DateOnly(2026, 9, 3));
	private static readonly AiracCycleInfo Current = new("2610", "01_Oct_2026", new DateOnly(2026, 10, 1));
	private static readonly AiracCycleInfo Next = new("2611", "29_Oct_2026", new DateOnly(2026, 10, 29));

	public AiracCycleDataCacheTests() => AppLog.ConfigureForTesting(Path.Combine(Path.GetTempPath(), "FeBuddyTests_Cache_" + Guid.NewGuid().ToString("N")));

	public void Dispose() => AppLog.ConfigureForTesting(null);

	private static Func<AiracCycleInfo, CancellationToken, Task<AiracCyclePublicationState>> AlwaysPublished =>
		(_, _) => Task.FromResult(AiracCyclePublicationState.Published);

	[Fact]
	public async Task prepare_cycles_downloads_in_order_current_next_previous()
	{
		List<string> downloadOrder = [];

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) =>
			{
				lock (downloadOrder) { downloadOrder.Add(cycle.AiracCycleId); }
				return Task.FromResult($@"C:\cache\{cycle.AiracCycleId}");
			},
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()));

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		Assert.Equal(new[] { "2610", "2611", "2609" }, downloadOrder);
		Assert.Equal(AiracCycleReadiness.Ready, cache.ComputeReadiness());
	}

	[Fact]
	public async Task prepare_cycles_prunes_every_cached_cycle_but_the_three_offered()
	{
		IReadOnlyCollection<string>? kept = null;

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => Task.FromResult($@"C:\cache\{cycle.AiracCycleId}"),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()),
			pruneAllBut: cycleIds => kept = cycleIds);

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		Assert.NotNull(kept);
		Assert.Equal(["2609", "2610", "2611"], kept.Order());
	}

	[Fact]
	public async Task next_download_starts_while_earlier_cycle_is_still_parsing()
	{
		TaskCompletionSource releaseCurrentParse = new(TaskCreationOptions.RunContinuationsAsynchronously);
		TaskCompletionSource previousDownloadStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
		TaskCompletionSource currentParseStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) =>
			{
				if (cycle.AiracCycleId == Previous.AiracCycleId)
				{
					previousDownloadStarted.TrySetResult();
				}

				return Task.FromResult($@"C:\cache\{cycle.AiracCycleId}");
			},
			parse: async (dir, _) =>
			{
				if (dir.EndsWith("2610", StringComparison.Ordinal))
				{
					currentParseStarted.TrySetResult();
					await releaseCurrentParse.Task;
				}

				return new NasrCsvDataCollection();
			});

		Task prepare = cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		// Current's parse is held open, yet the previous cycle's download must still get going.
		// Wait for both: the pipeline may start that download before current's queued parse begins.
		await Task.WhenAll(currentParseStarted.Task, previousDownloadStarted.Task).WaitAsync(TimeSpan.FromSeconds(5));
		Assert.Equal(CycleDataState.Parsing, cache.GetEntry("2610")!.State);
		Assert.False(prepare.IsCompleted);

		releaseCurrentParse.SetResult();
		await prepare.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.Equal(AiracCycleReadiness.Ready, cache.ComputeReadiness());
	}

	[Fact]
	public async Task parses_never_overlap_and_run_in_priority_order()
	{
		int running = 0;
		int maxRunning = 0;
		List<string> parseOrder = [];

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => Task.FromResult($@"C:\cache\{cycle.AiracCycleId}"),
			parse: async (dir, _) =>
			{
				int now = Interlocked.Increment(ref running);
				InterlockedMax(ref maxRunning, now);
				lock (parseOrder) { parseOrder.Add(Path.GetFileName(dir)); }

				await Task.Delay(30);

				Interlocked.Decrement(ref running);
				return new NasrCsvDataCollection();
			});

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		Assert.Equal(1, maxRunning); // memory safeguard: one dataset being built at a time
		Assert.Equal(new[] { "2610", "2611", "2609" }, parseOrder);
	}

	[Fact]
	public async Task get_async_for_cycle_waiting_for_its_parse_turn_does_not_parse_it_twice()
	{
		TaskCompletionSource releaseCurrentParse = new(TaskCreationOptions.RunContinuationsAsynchronously);
		TaskCompletionSource previousDownloaded = new(TaskCreationOptions.RunContinuationsAsynchronously);
		int previousParseCount = 0;

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) =>
			{
				if (cycle.AiracCycleId == Previous.AiracCycleId)
				{
					previousDownloaded.TrySetResult();
				}

				return Task.FromResult($@"C:\cache\{cycle.AiracCycleId}");
			},
			parse: async (dir, _) =>
			{
				if (dir.EndsWith("2610", StringComparison.Ordinal))
				{
					await releaseCurrentParse.Task;
				}
				else if (dir.EndsWith("2609", StringComparison.Ordinal))
				{
					Interlocked.Increment(ref previousParseCount);
				}

				return new NasrCsvDataCollection();
			});

		Task prepare = cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);
		await previousDownloaded.Task.WaitAsync(TimeSpan.FromSeconds(5));

		// Previous is on disk but its parse is queued behind current's. Asking for it now must
		// wait on that queued parse rather than start a second one.
		Task<NasrCsvDataCollection> requested = cache.GetAsync("2609");

		releaseCurrentParse.SetResult();
		await Task.WhenAll(prepare, requested).WaitAsync(TimeSpan.FromSeconds(5));

		Assert.Equal(1, previousParseCount);
	}

	[Fact]
	public async Task failed_parse_does_not_stall_the_parses_queued_behind_it()
	{
		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => Task.FromResult($@"C:\cache\{cycle.AiracCycleId}"),
			parse: (dir, _) => dir.EndsWith("2609", StringComparison.Ordinal)
				? Task.FromException<NasrCsvDataCollection>(new InvalidDataException("bad csv"))
				: Task.FromResult(new NasrCsvDataCollection()));

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything).WaitAsync(TimeSpan.FromSeconds(5));

		Assert.Equal(CycleDataState.Ready, cache.GetEntry("2610")!.State);
		Assert.Equal(CycleDataState.Failed, cache.GetEntry("2609")!.State);
		Assert.Equal(CycleDataState.Ready, cache.GetEntry("2611")!.State);
		Assert.Equal(AiracCycleReadiness.Degraded, cache.ComputeReadiness());
	}

	private static void InterlockedMax(ref int location, int value)
	{
		int seen;
		do
		{
			seen = Volatile.Read(ref location);
		}
		while (value > seen && Interlocked.CompareExchange(ref location, value, seen) != seen);
	}

	[Fact]
	public async Task concurrent_get_async_does_not_parse_twice()
	{
		int parseCount = 0;

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => Task.FromResult($@"C:\cache\{cycle.AiracCycleId}"),
			parse: async (_, _) =>
			{
				Interlocked.Increment(ref parseCount);
				await Task.Delay(20);
				return new NasrCsvDataCollection();
			});

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		Task<NasrCsvDataCollection> a = cache.GetAsync("2610");
		Task<NasrCsvDataCollection> b = cache.GetAsync("2610");
		await Task.WhenAll(a, b);

		Assert.Same(await a, await b);
		Assert.Equal(3, parseCount); // one parse per cycle during prepare, none added by GetAsync
	}

	[Fact]
	public async Task concurrent_get_async_calls_for_a_failed_cycles_retry_share_the_same_flight()
	{
		TaskCompletionSource<string> releaseThirdAttempt = new(TaskCreationOptions.RunContinuationsAsynchronously);
		int currentAttempts = 0;

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) =>
			{
				if (cycle.AiracCycleId != Current.AiracCycleId)
				{
					return Task.FromResult($@"C:\cache\{cycle.AiracCycleId}");
				}

				// The first two attempts (both during PrepareCyclesAsync's own one-retry) fail; the
				// third (GetAsync's retry) hangs until released, so a second concurrent GetAsync call
				// has a real in-flight task to find.
				return Interlocked.Increment(ref currentAttempts) <= 2
					? Task.FromException<string>(new IOException("network down"))
					: releaseThirdAttempt.Task;
			},
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()));

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);
		Assert.Equal(CycleDataState.Failed, cache.GetEntry("2610")!.State);
		Assert.Equal(2, currentAttempts);

		Task<NasrCsvDataCollection> first = cache.GetAsync("2610");
		Task<NasrCsvDataCollection> second = cache.GetAsync("2610");

		// The second call found the first's retry already in _flights rather than starting its own.
		Assert.Same(first, second);

		releaseThirdAttempt.SetResult(@"C:\cache\2610");
		await Task.WhenAll(first, second);

		Assert.Equal(3, currentAttempts);
		Assert.Equal(CycleDataState.Ready, cache.GetEntry("2610")!.State);
	}

	[Fact]
	public async Task download_that_keeps_failing_is_retried_once_then_marked_failed()
	{
		ConcurrentDictionary<string, int> attempts = new();

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) =>
			{
				attempts.AddOrUpdate(cycle.AiracCycleId, 1, (_, n) => n + 1);
				return Task.FromException<string>(new IOException("network down"));
			},
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()));

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		Assert.Equal(2, attempts["2610"]); // one try + one retry
		Assert.Equal(CycleDataState.Failed, cache.GetEntry("2610")!.State);
		Assert.Equal(AiracCycleReadiness.Unavailable, cache.ComputeReadiness());
	}

	/// <summary>
	/// The FAA always has the current cycle, so one the probe calls not yet published (a proxy
	/// answering 403, a PC clock set ahead) is unavailable, not something to wait for forever.
	/// </summary>
	[Fact]
	public async Task a_current_cycle_that_is_not_published_is_unavailable_not_waiting()
	{
		AiracCycleDataCache cache = new(
			probe: (cycle, _) => Task.FromResult(
				cycle.AiracCycleId == Current.AiracCycleId
					? AiracCyclePublicationState.NotYetPublished
					: AiracCyclePublicationState.Published),
			download: (cycle, _) => Task.FromResult($@"C:\cache\{cycle.AiracCycleId}"),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()));

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		Assert.Equal(CycleDataState.NotYetPublished, cache.GetEntry("2610")!.State);
		Assert.Equal(AiracCycleReadiness.Unavailable, cache.ComputeReadiness());
	}

	[Fact]
	public async Task readiness_is_ready_when_next_is_not_yet_published()
	{
		AiracCycleDataCache cache = new(
			probe: (cycle, _) => Task.FromResult(
				cycle.AiracCycleId == Next.AiracCycleId
					? AiracCyclePublicationState.NotYetPublished
					: AiracCyclePublicationState.Published),
			download: (cycle, _) => Task.FromResult($@"C:\cache\{cycle.AiracCycleId}"),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()));

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		Assert.Equal(CycleDataState.NotYetPublished, cache.GetEntry("2611")!.State);
		Assert.Equal(AiracCycleReadiness.Ready, cache.ComputeReadiness());
	}

	[Fact]
	public async Task readiness_is_degraded_when_only_previous_fails()
	{
		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => cycle.AiracCycleId == Previous.AiracCycleId
				? Task.FromException<string>(new IOException("boom"))
				: Task.FromResult($@"C:\cache\{cycle.AiracCycleId}"),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()));

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		Assert.Equal(CycleDataState.Failed, cache.GetEntry("2609")!.State);
		Assert.Equal(CycleDataState.Ready, cache.GetEntry("2610")!.State);
		Assert.Equal(AiracCycleReadiness.Degraded, cache.ComputeReadiness());
	}

	[Fact]
	public async Task before_prepare_nothing_is_tracked()
	{
		AiracCycleDataCache cache = new(AlwaysPublished, (_, _) => Task.FromResult("x"), (_, _) => Task.FromResult(new NasrCsvDataCollection()));

		Assert.Empty(cache.Entries);
		Assert.Equal(AiracCycleReadiness.Waiting, cache.ComputeReadiness());
		await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetAsync("2610"));
	}

	[Fact]
	public async Task inconclusive_probe_is_asked_twice_then_the_download_decides()
	{
		ConcurrentDictionary<string, int> probes = new();

		AiracCycleDataCache cache = new(
			probe: (cycle, _) =>
			{
				probes.AddOrUpdate(cycle.AiracCycleId, 1, (_, n) => n + 1);
				return Task.FromResult(AiracCyclePublicationState.Unknown);
			},
			download: (cycle, _) => Task.FromResult($@"C:\cache\{cycle.AiracCycleId}"),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()));

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		Assert.All(probes.Values, count => Assert.Equal(2, count));
		Assert.Equal(["2610", "2609", "2611"], cache.Entries.Select(e => e.Cycle.AiracCycleId));
		Assert.Equal(AiracCycleReadiness.Ready, cache.ComputeReadiness());
	}

	[Fact]
	public async Task get_async_for_an_unpublished_cycle_throws()
	{
		AiracCycleDataCache cache = new(
			probe: (cycle, _) => Task.FromResult(
				cycle.AiracCycleId == Next.AiracCycleId ? AiracCyclePublicationState.NotYetPublished : AiracCyclePublicationState.Published),
			download: (cycle, _) => Task.FromResult($@"C:\cache\{cycle.AiracCycleId}"),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()));

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetAsync("2611"));
		Assert.Contains("has not been published", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public async Task get_async_for_a_failed_cycle_tries_the_download_again()
	{
		int currentAttempts = 0;
		NasrCsvDataCollection parsed = new();

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) =>
				cycle.AiracCycleId == Current.AiracCycleId && Interlocked.Increment(ref currentAttempts) <= 2
					? Task.FromException<string>(new IOException("network down"))
					: Task.FromResult($@"C:\cache\{cycle.AiracCycleId}"),
			parse: (_, _) => Task.FromResult(parsed));

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);
		Assert.Equal(CycleDataState.Failed, cache.GetEntry("2610")!.State);

		Assert.Same(parsed, await cache.GetAsync("2610"));
		Assert.Equal(CycleDataState.Ready, cache.GetEntry("2610")!.State);
		Assert.Equal(3, currentAttempts);
	}

	[Fact]
	public async Task get_async_for_a_cycle_that_still_fails_throws()
	{
		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (_, _) => Task.FromException<string>(new IOException("network down")),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()));

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetAsync("2610"));
		Assert.Contains("could not be prepared (state: Failed)", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public async Task readiness_is_waiting_until_the_current_cycle_is_ready()
	{
		TaskCompletionSource currentParsing = new(TaskCreationOptions.RunContinuationsAsynchronously);
		TaskCompletionSource releaseCurrent = new(TaskCreationOptions.RunContinuationsAsynchronously);

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => Task.FromResult(cycle.AiracCycleId),
			parse: async (directory, _) =>
			{
				if (directory == Current.AiracCycleId)
				{
					currentParsing.TrySetResult();
					await releaseCurrent.Task;
				}

				return new NasrCsvDataCollection();
			});

		Task prepare = cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);
		await currentParsing.Task.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.Equal(AiracCycleReadiness.Waiting, cache.ComputeReadiness());

		releaseCurrent.SetResult();
		await prepare.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.Equal(AiracCycleReadiness.Ready, cache.ComputeReadiness());
	}

	[Fact]
	public async Task readiness_is_ready_while_the_next_and_previous_cycles_are_still_loading()
	{
		TaskCompletionSource nextParsing = new(TaskCreationOptions.RunContinuationsAsynchronously);
		TaskCompletionSource releaseNext = new(TaskCreationOptions.RunContinuationsAsynchronously);
		List<string> parsed = [];

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => Task.FromResult(cycle.AiracCycleId),
			parse: async (directory, _) =>
			{
				if (directory == Next.AiracCycleId)
				{
					nextParsing.TrySetResult();
					await releaseNext.Task;
				}

				lock (parsed) { parsed.Add(directory); }
				return new NasrCsvDataCollection();
			});

		Task prepare = cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);
		await nextParsing.Task.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.Equal(CycleDataState.Ready, cache.GetEntry("2610")!.State);
		Assert.Equal(CycleDataState.Parsing, cache.GetEntry("2611")!.State);
		Assert.False(AiracCycleDataCache.IsSettled(cache.GetEntry("2609")!));
		Assert.Equal(AiracCycleReadiness.Ready, cache.ComputeReadiness());
		Assert.False(prepare.IsCompleted);

		releaseNext.SetResult();
		await prepare.WaitAsync(TimeSpan.FromSeconds(5));

		// Launch still parses all three.
		Assert.Equal(["2610", "2611", "2609"], parsed);
		Assert.All(cache.Entries, entry => Assert.True(AiracCycleDataCache.IsSettled(entry)));
		Assert.Equal(AiracCycleReadiness.Ready, cache.ComputeReadiness());
	}

	[Fact]
	public async Task readiness_turns_degraded_when_a_cycle_loading_in_the_background_fails()
	{
		TaskCompletionSource<string> previousDownload = new(TaskCreationOptions.RunContinuationsAsynchronously);

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => cycle.AiracCycleId == Previous.AiracCycleId
				? previousDownload.Task
				: Task.FromResult(cycle.AiracCycleId),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()));

		Task prepare = cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);
		await cache.GetAsync("2610").WaitAsync(TimeSpan.FromSeconds(5));

		Assert.Equal(AiracCycleReadiness.Ready, cache.ComputeReadiness());

		previousDownload.SetException(new IOException("network down"));
		await prepare.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.Equal(AiracCycleReadiness.Degraded, cache.ComputeReadiness());
	}

	[Fact]
	public async Task get_async_for_a_cycle_still_loading_waits_for_it_then_returns_its_data()
	{
		TaskCompletionSource releaseNext = new(TaskCreationOptions.RunContinuationsAsynchronously);
		NasrCsvDataCollection nextData = new();

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => Task.FromResult(cycle.AiracCycleId),
			parse: async (directory, _) =>
			{
				if (directory == Next.AiracCycleId)
				{
					await releaseNext.Task;
					return nextData;
				}

				return new NasrCsvDataCollection();
			});

		Task prepare = cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);
		await cache.GetAsync("2610").WaitAsync(TimeSpan.FromSeconds(5));

		Task<NasrCsvDataCollection> run = cache.GetAsync("2611");
		await Task.Delay(50);
		Assert.False(run.IsCompleted);

		releaseNext.SetResult();
		Assert.Same(nextData, await run.WaitAsync(TimeSpan.FromSeconds(5)));
		await prepare.WaitAsync(TimeSpan.FromSeconds(5));
	}

	/// <summary>
	/// A run that starts a cycle's parse and is cancelled while that parse waits its turn only stops
	/// waiting: the parse goes on, and the next ask gets the data.
	/// </summary>
	[Fact]
	public async Task a_cancelled_run_never_leaves_the_parse_it_started_cancelled()
	{
		TaskCompletionSource currentParsing = new(TaskCreationOptions.RunContinuationsAsynchronously);
		TaskCompletionSource releaseCurrent = new(TaskCreationOptions.RunContinuationsAsynchronously);
		NasrCsvDataCollection previousData = new();

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => Task.FromResult(cycle.AiracCycleId),
			parse: async (directory, _) =>
			{
				if (directory == Current.AiracCycleId)
				{
					currentParsing.SetResult();
					await releaseCurrent.Task;
				}

				return directory == Previous.AiracCycleId ? previousData : new NasrCsvDataCollection();
			});

		await cache.PrepareCyclesAsync(Previous, Current, Next, new AiracLaunchPlan(Download: true, Parse: false, Prune: false));

		Task<NasrCsvDataCollection> current = cache.GetAsync("2610");
		await currentParsing.Task.WaitAsync(TimeSpan.FromSeconds(5));

		// The run's ask starts the previous cycle's parse, queued behind the current one's.
		using CancellationTokenSource run = new();
		Task<NasrCsvDataCollection> cancelled = cache.GetAsync("2609", run.Token);
		Assert.True(SpinWait.SpinUntil(() => cache.GetEntry("2609")!.ParseTask is not null, TimeSpan.FromSeconds(5)));

		run.Cancel();
		releaseCurrent.SetResult();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled.WaitAsync(TimeSpan.FromSeconds(5)));
		await current.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.Same(previousData, await cache.GetAsync("2609").WaitAsync(TimeSpan.FromSeconds(5)));
		Assert.Equal(CycleDataState.Ready, cache.GetEntry("2609")!.State);
	}

	[Fact]
	public async Task each_parse_logs_how_long_it_took_and_how_many_groups_it_parsed()
	{
		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => Task.FromResult(cycle.AiracCycleId),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection { Apt = new(), Fix = new() }));

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		string[] messages = [.. AppLog.Entries.Select(e => e.Message)];
		Assert.Contains(messages, m => m.StartsWith("Cycle 2610 parsed in ", StringComparison.Ordinal) && m.EndsWith(" ms (2 groups).", StringComparison.Ordinal));
		Assert.Contains(messages, m => m.StartsWith("Cycle 2609 parsed in ", StringComparison.Ordinal));
		Assert.Contains(messages, m => m.StartsWith("The AIRAC Service is ready: the current cycle was ready ", StringComparison.Ordinal));
		Assert.Contains(messages, m => m.StartsWith("AIRAC cycle preparation finished in ", StringComparison.Ordinal));
	}

	[Fact]
	public async Task a_throwing_state_changed_subscriber_does_not_break_the_pipeline()
	{
		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => Task.FromResult(cycle.AiracCycleId),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()));
		cache.StateChanged += (_, _) => throw new InvalidOperationException("bad subscriber");

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		Assert.Equal(AiracCycleReadiness.Ready, cache.ComputeReadiness());
	}

	[Fact]
	public async Task cancellation_during_download_is_not_swallowed_as_a_failure()
	{
		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (_, _) => Task.FromException<string>(new OperationCanceledException()),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()));

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything));
		Assert.NotEqual(CycleDataState.Failed, cache.GetEntry("2610")!.State);
	}

	[Fact]
	public async Task cancellation_during_parse_is_not_swallowed_as_a_failure()
	{
		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => Task.FromResult(cycle.AiracCycleId),
			parse: (_, _) => Task.FromException<NasrCsvDataCollection>(new OperationCanceledException()));

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything));
		Assert.NotEqual(CycleDataState.Failed, cache.GetEntry("2610")!.State);
	}

	// ---- Wx Stations: parameterless constructor ----

	[Fact]
	public void the_parameterless_constructor_produces_an_untouched_cache_without_touching_the_network()
	{
		AiracCycleDataCache cache = new();

		Assert.Empty(cache.Entries);
		Assert.Equal(AiracCycleReadiness.Waiting, cache.ComputeReadiness());
	}

	[Fact]
	public void configure_for_testing_swaps_and_restores_the_shared_instance()
	{
		AiracCycleDataCache original = AiracCycleDataCache.Instance;
		AiracCycleDataCache replacement = new(AlwaysPublished, (_, _) => Task.FromResult("x"), (_, _) => Task.FromResult(new NasrCsvDataCollection()));

		try
		{
			AiracCycleDataCache.ConfigureForTesting(replacement);
			Assert.Same(replacement, AiracCycleDataCache.Instance);
		}
		finally
		{
			AiracCycleDataCache.ConfigureForTesting(null);
		}

		Assert.NotSame(replacement, AiracCycleDataCache.Instance);
		Assert.NotSame(original, AiracCycleDataCache.Instance);
	}

	// ---- Dtpp: ensureDtpp ----

	[Fact]
	public async Task ensure_dtpp_is_called_with_the_cycle_folder_and_cycle_id_after_a_successful_download()
	{
		List<(string Folder, string CycleId)> calledWith = [];

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => Task.FromResult($@"C:\cache\{cycle.AiracCycleId}"),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()),
			ensureDtpp: (cycleDirectory, cycleId, _) =>
			{
				lock (calledWith) { calledWith.Add((cycleDirectory, cycleId)); }
				return Task.CompletedTask;
			});

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		Assert.Equal(3, calledWith.Count);
		Assert.Contains((@"C:\cache\2609", "2609"), calledWith);
		Assert.Contains((@"C:\cache\2610", "2610"), calledWith);
		Assert.Contains((@"C:\cache\2611", "2611"), calledWith);
	}

	[Fact]
	public async Task ensure_dtpp_runs_even_for_an_already_cached_cycle()
	{
		bool called = false;

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => Task.FromResult($@"C:\cache\{cycle.AiracCycleId}"),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()),
			isLocallyAvailable: _ => true,
			ensureDtpp: (_, _, _) =>
			{
				called = true;
				return Task.CompletedTask;
			});

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		Assert.True(called);
	}

	[Fact]
	public async Task an_exception_from_ensure_dtpp_is_swallowed_and_the_cycle_still_reaches_ready()
	{
		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => Task.FromResult($@"C:\cache\{cycle.AiracCycleId}"),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()),
			ensureDtpp: (_, _, _) => Task.FromException(new IOException("network down")));

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		Assert.Equal(CycleDataState.Ready, cache.GetEntry("2610")!.State);
		Assert.Equal(AiracCycleReadiness.Ready, cache.ComputeReadiness());
	}

	[Fact]
	public async Task cancellation_from_ensure_dtpp_propagates()
	{
		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => Task.FromResult($@"C:\cache\{cycle.AiracCycleId}"),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()),
			ensureDtpp: (_, _, _) => Task.FromException(new OperationCanceledException()));

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything));
	}

	// ---- Dtpp: GetDtppAsync ----

	[Fact]
	public async Task get_dtpp_async_for_an_untracked_cycle_returns_null_without_calling_the_loader()
	{
		bool loaderCalled = false;

		AiracCycleDataCache cache = new(
			AlwaysPublished,
			(_, _) => Task.FromResult("x"),
			(_, _) => Task.FromResult(new NasrCsvDataCollection()),
			loadDtpp: (_, _) =>
			{
				loaderCalled = true;
				return Task.FromResult<DtppMetafileDataCollection?>(null);
			});

		// Unlike GetAsync, an untracked cycle - e.g. the one before the selected one, which
		// Procedures also asks for and which may not be one of the three tracked cycles - answers
		// null rather than throwing.
		DtppMetafileDataCollection? result = await cache.GetDtppAsync("2610");

		Assert.Null(result);
		Assert.False(loaderCalled);
	}

	[Fact]
	public async Task get_dtpp_async_before_download_returns_null_without_calling_the_loader()
	{
		bool loaderCalled = false;

		AiracCycleDataCache cache = new(
			probe: (cycle, _) => Task.FromResult(
				cycle.AiracCycleId == Next.AiracCycleId ? AiracCyclePublicationState.NotYetPublished : AiracCyclePublicationState.Published),
			download: (cycle, _) => Task.FromResult($@"C:\cache\{cycle.AiracCycleId}"),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()),
			loadDtpp: (_, _) =>
			{
				loaderCalled = true;
				return Task.FromResult<DtppMetafileDataCollection?>(null);
			});

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		// Next has not been published, so its CycleDirectory is still null.
		DtppMetafileDataCollection? result = await cache.GetDtppAsync("2611");

		Assert.Null(result);
		Assert.False(loaderCalled);
	}

	[Fact]
	public async Task get_dtpp_async_after_download_returns_the_injected_loaders_result()
	{
		DtppMetafileDataCollection expected = new() { Cycle = "2610" };
		string? loadedFromFolder = null;

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => Task.FromResult($@"C:\cache\{cycle.AiracCycleId}"),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()),
			loadDtpp: (cycleDirectory, _) =>
			{
				loadedFromFolder = cycleDirectory;
				return Task.FromResult<DtppMetafileDataCollection?>(expected);
			});

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		DtppMetafileDataCollection? result = await cache.GetDtppAsync("2610");

		Assert.Same(expected, result);
		Assert.Equal(@"C:\cache\2610", loadedFromFolder);
	}

	[Fact]
	public async Task get_dtpp_async_waits_for_a_cycle_still_downloading()
	{
		TaskCompletionSource<string> previousDownload = new(TaskCreationOptions.RunContinuationsAsynchronously);
		DtppMetafileDataCollection expected = new() { Cycle = "2609" };
		int previousDownloads = 0;

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) =>
			{
				if (cycle.AiracCycleId != Previous.AiracCycleId)
				{
					return Task.FromResult($@"C:\cache\{cycle.AiracCycleId}");
				}

				Interlocked.Increment(ref previousDownloads);
				return previousDownload.Task;
			},
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()),
			loadDtpp: (cycleDirectory, _) => Task.FromResult<DtppMetafileDataCollection?>(cycleDirectory == @"C:\cache\2609" ? expected : null));

		Task prepare = cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);
		await cache.GetAsync("2610").WaitAsync(TimeSpan.FromSeconds(5));

		// A run on the current cycle asks for the previous cycle's metafile before it has landed.
		Task<DtppMetafileDataCollection?> dtpp = cache.GetDtppAsync("2609");
		await Task.Delay(50);
		Assert.False(dtpp.IsCompleted);

		previousDownload.SetResult(@"C:\cache\2609");

		Assert.Same(expected, await dtpp.WaitAsync(TimeSpan.FromSeconds(5)));
		await prepare.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.Equal(1, previousDownloads);
		Assert.Equal(CycleDataState.Ready, cache.GetEntry("2609")!.State);
	}

	[Fact]
	public async Task get_dtpp_async_downloads_a_cycle_launch_has_not_reached_and_launch_does_not_download_it_again()
	{
		TaskCompletionSource<string> currentDownload = new(TaskCreationOptions.RunContinuationsAsynchronously);
		ConcurrentDictionary<string, int> downloads = new();
		DtppMetafileDataCollection expected = new() { Cycle = "2609" };

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) =>
			{
				downloads.AddOrUpdate(cycle.AiracCycleId, 1, (_, n) => n + 1);
				return cycle.AiracCycleId == Current.AiracCycleId ? currentDownload.Task : Task.FromResult($@"C:\cache\{cycle.AiracCycleId}");
			},
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()),
			loadDtpp: (cycleDirectory, _) => Task.FromResult<DtppMetafileDataCollection?>(cycleDirectory == @"C:\cache\2609" ? expected : null));

		Task prepare = cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		// Launch is still on the current cycle's download.
		Assert.Same(expected, await cache.GetDtppAsync("2609").WaitAsync(TimeSpan.FromSeconds(5)));
		Assert.Equal(CycleDataState.Downloaded, cache.GetEntry("2609")!.State);

		currentDownload.SetResult(@"C:\cache\2610");
		await prepare.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.Equal(1, downloads["2609"]);
		Assert.All(cache.Entries, entry => Assert.Equal(CycleDataState.Ready, entry.State));
	}

	[Fact]
	public async Task get_dtpp_async_does_not_try_a_failed_cycle_again()
	{
		int previousAttempts = 0;
		bool loaderCalled = false;

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) =>
			{
				if (cycle.AiracCycleId != Previous.AiracCycleId)
				{
					return Task.FromResult($@"C:\cache\{cycle.AiracCycleId}");
				}

				Interlocked.Increment(ref previousAttempts);
				return Task.FromException<string>(new IOException("network down"));
			},
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()),
			loadDtpp: (_, _) =>
			{
				loaderCalled = true;
				return Task.FromResult<DtppMetafileDataCollection?>(null);
			});

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		Assert.Null(await cache.GetDtppAsync("2609"));
		Assert.Equal(2, previousAttempts);
		Assert.False(loaderCalled);
	}

	[Fact]
	public async Task a_caller_still_waiting_starts_its_own_download_when_the_one_it_shared_is_cancelled()
	{
		using CancellationTokenSource firstCaller = new();
		TaskCompletionSource<string> currentDownload = new(TaskCreationOptions.RunContinuationsAsynchronously);
		TaskCompletionSource firstAttemptStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
		DtppMetafileDataCollection expected = new() { Cycle = "2609" };
		int previousAttempts = 0;

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: async (cycle, ct) =>
			{
				if (cycle.AiracCycleId == Current.AiracCycleId)
				{
					return await currentDownload.Task;
				}

				if (cycle.AiracCycleId == Previous.AiracCycleId && Interlocked.Increment(ref previousAttempts) == 1)
				{
					firstAttemptStarted.TrySetResult();
					await Task.Delay(Timeout.Infinite, ct);
				}

				return $@"C:\cache\{cycle.AiracCycleId}";
			},
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()),
			loadDtpp: (cycleDirectory, _) => Task.FromResult<DtppMetafileDataCollection?>(cycleDirectory == @"C:\cache\2609" ? expected : null));

		// Launch is held on the current cycle, so the previous cycle's download is the callers' own.
		Task prepare = cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		Task<DtppMetafileDataCollection?> first = cache.GetDtppAsync("2609", firstCaller.Token);
		await firstAttemptStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Task<DtppMetafileDataCollection?> second = cache.GetDtppAsync("2609");

		firstCaller.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
		Assert.Same(expected, await second.WaitAsync(TimeSpan.FromSeconds(5)));
		Assert.Equal(2, previousAttempts);

		currentDownload.SetResult(@"C:\cache\2610");
		await prepare.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.Equal(2, previousAttempts);
	}

	// ---- Dev launch: what the launch pipeline leaves out ----

	/// <summary>How many times each step ran.</summary>
	private sealed class StepCounts
	{
		public int Probes;
		public int Downloads;
		public int Parses;
		public int Prunes;
	}

	/// <summary>A cache whose every step is counted; <paramref name="onDisk"/> are the cycles already downloaded.</summary>
	private static AiracCycleDataCache CountingCache(StepCounts counts, string[]? onDisk = null, string? notPublished = null) => new(
		probe: (cycle, _) =>
		{
			Interlocked.Increment(ref counts.Probes);
			return Task.FromResult(cycle.AiracCycleId == notPublished ? AiracCyclePublicationState.NotYetPublished : AiracCyclePublicationState.Published);
		},
		download: (cycle, _) =>
		{
			Interlocked.Increment(ref counts.Downloads);
			return Task.FromResult($@"C:\cache\{cycle.AiracCycleId}");
		},
		parse: (_, _) =>
		{
			Interlocked.Increment(ref counts.Parses);
			return Task.FromResult(new NasrCsvDataCollection());
		},
		isLocallyAvailable: cycle => onDisk?.Contains(cycle.AiracCycleId) == true,
		pruneAllBut: _ => Interlocked.Increment(ref counts.Prunes),
		cycleDirectory: cycle => $@"C:\cache\{cycle.AiracCycleId}");

	[Fact]
	public async Task a_normal_launch_loads_nothing_on_demand()
	{
		AiracCycleDataCache cache = CountingCache(new StepCounts());

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		Assert.False(cache.LoadsOnDemand);
		Assert.All(cache.Entries, entry => Assert.False(cache.IsLoading(entry)));
	}

	[Fact]
	public async Task download_on_parse_off_downloads_every_cycle_and_parses_none_until_one_is_asked_for()
	{
		StepCounts counts = new();
		AiracCycleDataCache cache = CountingCache(counts);

		await cache.PrepareCyclesAsync(Previous, Current, Next, new AiracLaunchPlan(Download: true, Parse: false, Prune: true));

		Assert.Equal(3, counts.Probes);
		Assert.Equal(3, counts.Downloads);
		Assert.Equal(0, counts.Parses);
		Assert.Equal(1, counts.Prunes);
		Assert.All(cache.Entries, entry => Assert.Equal(CycleDataState.Downloaded, entry.State));
		Assert.All(cache.Entries, entry => Assert.False(cache.IsLoading(entry)));
		Assert.True(cache.LoadsOnDemand);
		Assert.Equal(AiracCycleReadiness.NotLoaded, cache.ComputeReadiness());

		await cache.GetAsync("2610");

		Assert.Equal(1, counts.Parses);
		Assert.Equal(3, counts.Downloads);
		Assert.Equal(CycleDataState.Ready, cache.GetEntry("2610")!.State);
		Assert.Equal(AiracCycleReadiness.Ready, cache.ComputeReadiness());
	}

	[Fact]
	public async Task download_off_parse_on_parses_only_the_cycles_on_disk_without_touching_the_network()
	{
		StepCounts counts = new();
		AiracCycleDataCache cache = CountingCache(counts, onDisk: ["2610", "2609"]);

		await cache.PrepareCyclesAsync(Previous, Current, Next, new AiracLaunchPlan(Download: false, Parse: true, Prune: false));

		Assert.Equal(0, counts.Probes);
		Assert.Equal(0, counts.Downloads);
		Assert.Equal(2, counts.Parses);
		Assert.Equal(0, counts.Prunes);
		Assert.Equal(CycleDataState.Ready, cache.GetEntry("2610")!.State);
		Assert.Equal(CycleDataState.Ready, cache.GetEntry("2609")!.State);
		Assert.Equal(CycleDataState.NotDownloaded, cache.GetEntry("2611")!.State);
		Assert.False(cache.IsLoading(cache.GetEntry("2611")!));
		Assert.Equal(AiracCycleReadiness.Ready, cache.ComputeReadiness());
		Assert.Contains(AppLog.Entries, e => e.Message.StartsWith("Cycle 2611 isn't on disk and wasn't downloaded", StringComparison.Ordinal));

		// Asked for later, the missing cycle is checked, downloaded and parsed then.
		await cache.GetAsync("2611");

		Assert.Equal(1, counts.Probes);
		Assert.Equal(1, counts.Downloads);
		Assert.Equal(3, counts.Parses);
		Assert.Equal(CycleDataState.Ready, cache.GetEntry("2611")!.State);
	}

	[Fact]
	public async Task download_off_parse_off_only_tracks_the_three_cycles()
	{
		StepCounts counts = new();
		AiracCycleDataCache cache = CountingCache(counts, onDisk: ["2609"]);

		await cache.PrepareCyclesAsync(Previous, Current, Next, new AiracLaunchPlan(Download: false, Parse: false, Prune: false));

		Assert.Equal(0, counts.Probes + counts.Downloads + counts.Parses + counts.Prunes);
		Assert.Equal(["2610", "2609", "2611"], cache.Entries.Select(e => e.Cycle.AiracCycleId));
		Assert.Equal(CycleDataState.NotDownloaded, cache.GetEntry("2610")!.State);
		Assert.Equal(CycleDataState.Downloaded, cache.GetEntry("2609")!.State);
		Assert.Equal(@"C:\cache\2609", cache.GetEntry("2609")!.CycleDirectory);
		Assert.Equal(AiracCycleReadiness.NotLoaded, cache.ComputeReadiness());

		// The current cycle loads on demand; the one on disk only needs parsing.
		await cache.GetAsync("2610");
		await cache.GetAsync("2609");

		Assert.Equal(1, counts.Downloads);
		Assert.Equal(2, counts.Parses);
		Assert.Equal(AiracCycleReadiness.Ready, cache.ComputeReadiness());
	}

	/// <summary>
	/// A cycle loaded on demand from the UI thread downloads and parses on the thread pool, never on the
	/// caller's context: the download unzips a cycle, which would freeze the window for seconds.
	/// </summary>
	[Fact]
	public async Task a_cycle_loaded_on_demand_never_downloads_or_parses_on_the_callers_context()
	{
		SynchronizationContext? downloadedOn = null;
		SynchronizationContext? parsedOn = null;
		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) =>
			{
				downloadedOn = SynchronizationContext.Current;
				return Task.FromResult($@"C:\cache\{cycle.AiracCycleId}");
			},
			parse: (_, _) =>
			{
				parsedOn = SynchronizationContext.Current;
				return Task.FromResult(new NasrCsvDataCollection());
			});
		await cache.PrepareCyclesAsync(Previous, Current, Next, new AiracLaunchPlan(Download: false, Parse: false, Prune: false));

		CallersContext callers = new();
		SynchronizationContext? before = SynchronizationContext.Current;
		SynchronizationContext.SetSynchronizationContext(callers);
		Task load;

		try
		{
			load = cache.GetAsync("2610");
		}
		finally
		{
			SynchronizationContext.SetSynchronizationContext(before);
		}

		await load;

		Assert.Null(downloadedOn);
		Assert.Null(parsedOn);
		Assert.Equal(0, callers.Posts);
	}

	/// <summary>A context that stands for the UI thread's: it counts what is posted to it, and runs it on the pool as itself.</summary>
	private sealed class CallersContext : SynchronizationContext
	{
		private int _posts;

		public int Posts => _posts;

		public override void Post(SendOrPostCallback d, object? state)
		{
			Interlocked.Increment(ref _posts);
			ThreadPool.QueueUserWorkItem(_ =>
			{
				SetSynchronizationContext(this);
				d(state);
			});
		}
	}

	[Fact]
	public async Task a_cycle_left_unloaded_is_waiting_while_it_loads_on_demand()
	{
		TaskCompletionSource<string> currentDownload = new(TaskCreationOptions.RunContinuationsAsynchronously);

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => cycle.AiracCycleId == Current.AiracCycleId ? currentDownload.Task : Task.FromResult(cycle.AiracCycleId),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()));

		await cache.PrepareCyclesAsync(Previous, Current, Next, new AiracLaunchPlan(Download: false, Parse: false, Prune: false));
		Assert.Equal(AiracCycleReadiness.NotLoaded, cache.ComputeReadiness());

		Task<NasrCsvDataCollection> load = cache.GetAsync("2610");

		Assert.True(cache.IsLoading(cache.GetEntry("2610")!));
		Assert.Equal(AiracCycleReadiness.Waiting, cache.ComputeReadiness());

		currentDownload.SetResult("2610");
		await load.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.Equal(AiracCycleReadiness.Ready, cache.ComputeReadiness());
	}

	[Fact]
	public async Task a_cycle_asked_for_later_that_the_faa_has_not_published_is_said_to_be_that()
	{
		StepCounts counts = new();
		AiracCycleDataCache cache = CountingCache(counts, notPublished: "2611");

		await cache.PrepareCyclesAsync(Previous, Current, Next, new AiracLaunchPlan(Download: false, Parse: false, Prune: false));

		InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetAsync("2611"));

		Assert.Contains("has not been published", ex.Message, StringComparison.Ordinal);
		Assert.Equal(CycleDataState.NotYetPublished, cache.GetEntry("2611")!.State);
		Assert.Equal(0, counts.Downloads);
	}

	// ---- Dtpp: FindDtppFile ----

	[Fact]
	public void find_dtpp_file_for_an_untracked_cycle_is_null()
	{
		AiracCycleDataCache cache = new(AlwaysPublished, (_, _) => Task.FromResult("x"), (_, _) => Task.FromResult(new NasrCsvDataCollection()));

		Assert.Null(cache.FindDtppFile("2610"));
	}

	[Fact]
	public async Task find_dtpp_file_before_download_is_null()
	{
		AiracCycleDataCache cache = new(
			probe: (cycle, _) => Task.FromResult(
				cycle.AiracCycleId == Next.AiracCycleId ? AiracCyclePublicationState.NotYetPublished : AiracCyclePublicationState.Published),
			download: (cycle, _) => Task.FromResult($@"C:\cache\{cycle.AiracCycleId}"),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()));

		await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

		Assert.Null(cache.FindDtppFile("2611"));
	}

	[Fact]
	public async Task find_dtpp_file_when_missing_from_the_cycle_folder_is_null()
	{
		string cycleDirectory = Path.Combine(Path.GetTempPath(), "FeBuddyTests_FindDtpp_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(cycleDirectory);

		try
		{
			AiracCycleDataCache cache = new(
				probe: AlwaysPublished,
				download: (_, _) => Task.FromResult(cycleDirectory),
				parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()));

			await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

			Assert.Null(cache.FindDtppFile("2610"));
		}
		finally
		{
			Directory.Delete(cycleDirectory, recursive: true);
		}
	}

	/// <summary>
	/// The GUI asks on the UI thread at every state change, so the answer is what the cache found, not
	/// a fresh look at the disk: a file gone since still counts, and one that turned up since doesn't.
	/// </summary>
	[Fact]
	public async Task find_dtpp_file_answers_from_what_the_cache_found()
	{
		string root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_FindDtpp_" + Guid.NewGuid().ToString("N"));
		string withFile = Path.Combine(root, "2610");
		string withoutFile = Path.Combine(root, "2611");
		Directory.CreateDirectory(withFile);
		Directory.CreateDirectory(withoutFile);
		string dtppFile = Path.Combine(withFile, "d-tpp_Metafile.xml");
		File.WriteAllText(dtppFile, "<digital_tpp></digital_tpp>");

		try
		{
			AiracCycleDataCache cache = new(
				probe: AlwaysPublished,
				download: (cycle, _) => Task.FromResult(cycle.AiracCycleId == "2610" ? withFile : withoutFile),
				parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()));

			await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

			File.Delete(dtppFile);
			File.WriteAllText(Path.Combine(withoutFile, "d-tpp_Metafile.xml"), "<digital_tpp></digital_tpp>");

			Assert.Equal(dtppFile, cache.FindDtppFile("2610"));
			Assert.Null(cache.FindDtppFile("2611"));
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	/// <summary>A dev launch that finds a cycle on disk, without downloading it, finds its metafile there too.</summary>
	[Fact]
	public async Task find_dtpp_file_for_a_cycle_found_on_disk_without_downloading()
	{
		string cycleDirectory = Path.Combine(Path.GetTempPath(), "FeBuddyTests_FindDtpp_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(cycleDirectory);
		string dtppFile = Path.Combine(cycleDirectory, "d-tpp_Metafile.xml");
		File.WriteAllText(dtppFile, "<digital_tpp></digital_tpp>");

		try
		{
			AiracCycleDataCache cache = new(
				probe: AlwaysPublished,
				download: (_, _) => Task.FromResult(cycleDirectory),
				parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()),
				isLocallyAvailable: _ => true,
				cycleDirectory: _ => cycleDirectory);

			await cache.PrepareCyclesAsync(Previous, Current, Next, new AiracLaunchPlan(Download: false, Parse: false, Prune: false));

			Assert.Equal(dtppFile, cache.FindDtppFile("2610"));
		}
		finally
		{
			Directory.Delete(cycleDirectory, recursive: true);
		}
	}

	[Fact]
	public async Task find_dtpp_file_when_present_returns_its_path()
	{
		string cycleDirectory = Path.Combine(Path.GetTempPath(), "FeBuddyTests_FindDtpp_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(cycleDirectory);
		string dtppFile = Path.Combine(cycleDirectory, "d-tpp_Metafile.xml");
		File.WriteAllText(dtppFile, "<digital_tpp></digital_tpp>");

		try
		{
			AiracCycleDataCache cache = new(
				probe: AlwaysPublished,
				download: (_, _) => Task.FromResult(cycleDirectory),
				parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()));

			await cache.PrepareCyclesAsync(Previous, Current, Next, AiracLaunchPlan.Everything);

			Assert.Equal(dtppFile, cache.FindDtppFile("2610"));
		}
		finally
		{
			Directory.Delete(cycleDirectory, recursive: true);
		}
	}
}
