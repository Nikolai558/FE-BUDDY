using System.Collections.Concurrent;

using FEBuddyLibrary.Models.NASR.CSV;
using FEBuddyLibrary.Models.Services.Airac;
using FEBuddyLibrary.Services.Airac;
using FEBuddyLibrary.Services.General;

namespace UnitTests.Services.Airac;

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

	public AiracCycleDataCacheTests() => AppLog.ConfigureForTesting(Path.Combine(Path.GetTempPath(), "FEBuddyTests_Cache_" + Guid.NewGuid().ToString("N")));

	public void Dispose() => AppLog.ConfigureForTesting(null);

	private static Func<AiracCycleInfo, CancellationToken, Task<AiracCyclePublicationState>> AlwaysPublished =>
		(_, _) => Task.FromResult(AiracCyclePublicationState.Published);

	[Fact]
	public async Task PrepareCycles_DownloadsInOrder_CurrentPreviousNext()
	{
		List<string> downloadOrder = new();

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
	public async Task ConcurrentGetAsync_DoesNotParseTwice()
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
	public async Task DownloadThatKeepsFailing_IsRetriedOnceThenMarkedFailed()
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
	public async Task Readiness_IsReady_WhenNextIsNotYetPublished()
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
	public async Task Readiness_IsDegraded_WhenOnlyPreviousFails()
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
}
