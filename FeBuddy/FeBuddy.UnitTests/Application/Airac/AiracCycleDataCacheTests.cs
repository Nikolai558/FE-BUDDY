using System.Collections.Concurrent;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Airac.Models;
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
	public async Task prepare_cycles_downloads_in_order_current_previous_next()
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

		await cache.PrepareCyclesAsync(Previous, Current, Next);

		Assert.Equal(new[] { "2610", "2609", "2611" }, downloadOrder);
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

		await cache.PrepareCyclesAsync(Previous, Current, Next);

		Assert.NotNull(kept);
		Assert.Equal(["2609", "2610", "2611"], kept.Order());
	}

	[Fact]
	public async Task next_download_starts_while_earlier_cycle_is_still_parsing()
	{
		TaskCompletionSource releaseCurrentParse = new(TaskCreationOptions.RunContinuationsAsynchronously);
		TaskCompletionSource previousDownloadStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

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
					await releaseCurrentParse.Task;
				}

				return new NasrCsvDataCollection();
			});

		Task prepare = cache.PrepareCyclesAsync(Previous, Current, Next);

		// Current's parse is held open, yet the previous cycle's download must still get going.
		await previousDownloadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
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

		await cache.PrepareCyclesAsync(Previous, Current, Next);

		Assert.Equal(1, maxRunning); // memory safeguard: one dataset being built at a time
		Assert.Equal(new[] { "2610", "2609", "2611" }, parseOrder);
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

		Task prepare = cache.PrepareCyclesAsync(Previous, Current, Next);
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

		await cache.PrepareCyclesAsync(Previous, Current, Next).WaitAsync(TimeSpan.FromSeconds(5));

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

		await cache.PrepareCyclesAsync(Previous, Current, Next);

		Task<NasrCsvDataCollection> a = cache.GetAsync("2610");
		Task<NasrCsvDataCollection> b = cache.GetAsync("2610");
		await Task.WhenAll(a, b);

		Assert.Same(await a, await b);
		Assert.Equal(3, parseCount); // one parse per cycle during prepare, none added by GetAsync
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

		await cache.PrepareCyclesAsync(Previous, Current, Next);

		Assert.Equal(2, attempts["2610"]); // one try + one retry
		Assert.Equal(CycleDataState.Failed, cache.GetEntry("2610")!.State);
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

		await cache.PrepareCyclesAsync(Previous, Current, Next);

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

		await cache.PrepareCyclesAsync(Previous, Current, Next);

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

		await cache.PrepareCyclesAsync(Previous, Current, Next);

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

		await cache.PrepareCyclesAsync(Previous, Current, Next);

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

		await cache.PrepareCyclesAsync(Previous, Current, Next);
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

		await cache.PrepareCyclesAsync(Previous, Current, Next);

		InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetAsync("2610"));
		Assert.Contains("could not be prepared (state: Failed)", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public async Task readiness_is_waiting_while_the_best_effort_cycles_are_still_parsing()
	{
		TaskCompletionSource previousParsing = new(TaskCreationOptions.RunContinuationsAsynchronously);
		TaskCompletionSource releasePrevious = new(TaskCreationOptions.RunContinuationsAsynchronously);

		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => Task.FromResult(cycle.AiracCycleId),
			parse: async (directory, _) =>
			{
				if (directory == Previous.AiracCycleId)
				{
					previousParsing.TrySetResult();
					await releasePrevious.Task;
				}

				return new NasrCsvDataCollection();
			});

		Task prepare = cache.PrepareCyclesAsync(Previous, Current, Next);
		await previousParsing.Task;

		Assert.Equal(CycleDataState.Ready, cache.GetEntry("2610")!.State);
		Assert.Equal(AiracCycleReadiness.Waiting, cache.ComputeReadiness());

		releasePrevious.SetResult();
		await prepare;
		Assert.Equal(AiracCycleReadiness.Ready, cache.ComputeReadiness());
	}

	[Fact]
	public async Task a_throwing_state_changed_subscriber_does_not_break_the_pipeline()
	{
		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => Task.FromResult(cycle.AiracCycleId),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()));
		cache.StateChanged += (_, _) => throw new InvalidOperationException("bad subscriber");

		await cache.PrepareCyclesAsync(Previous, Current, Next);

		Assert.Equal(AiracCycleReadiness.Ready, cache.ComputeReadiness());
	}

	[Fact]
	public async Task cancellation_during_download_is_not_swallowed_as_a_failure()
	{
		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (_, _) => Task.FromException<string>(new OperationCanceledException()),
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection()));

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.PrepareCyclesAsync(Previous, Current, Next));
		Assert.NotEqual(CycleDataState.Failed, cache.GetEntry("2610")!.State);
	}

	[Fact]
	public async Task cancellation_during_parse_is_not_swallowed_as_a_failure()
	{
		AiracCycleDataCache cache = new(
			probe: AlwaysPublished,
			download: (cycle, _) => Task.FromResult(cycle.AiracCycleId),
			parse: (_, _) => Task.FromException<NasrCsvDataCollection>(new OperationCanceledException()));

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.PrepareCyclesAsync(Previous, Current, Next));
		Assert.NotEqual(CycleDataState.Failed, cache.GetEntry("2610")!.State);
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
}
