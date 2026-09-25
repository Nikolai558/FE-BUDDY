using System.Net;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Application.Launch.Models;
using FeBuddy.Core.Application.News;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Platform.Models;

using FeBuddy.Versioning.Models;

namespace FeBuddy.UnitTests.Application.Launch;

/// <summary>
/// Runs <see cref="LaunchSequence.RunAsync"/> end to end with every outside dependency stubbed:
/// HTTP through <see cref="AppEnvironment.HttpClientForTesting"/>, the AIRAC cache through
/// <see cref="AiracCycleDataCache.ConfigureForTesting"/>, and config, temp and log folders
/// redirected to a throwaway directory.
/// </summary>
[Collection("AppLog")]
public sealed class LaunchSequenceTests : IDisposable
{
	private const string ReleasesJson = """
		[
		  { "tag_name": "v3.1.0", "prerelease": false, "draft": false },
		  { "tag_name": "v3.0.0", "prerelease": false, "draft": false }
		]
		""";

	private const string NewsMarkdown = """
		# FE-Buddy News

		---

		## 2026-09-02
		<!--
		PostId: 2026-09-02.1
		-->

		A post.
		""";

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_Launch_" + Guid.NewGuid().ToString("N"));
	private readonly List<string> _preparedCycles = [];

	public LaunchSequenceTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		TempWorkspace.ConfigureForTesting(Path.Combine(_root, "temp"));
		UserConfigFile.ConfigureForTesting(Path.Combine(_root, "config"));
		AppEnvironment.ResetForTesting();
		AiracCycleDataCache.ConfigureForTesting(new AiracCycleDataCache(
			probe: (_, _) => Task.FromResult(AiracCyclePublicationState.Published),
			download: (cycle, _) =>
			{
				lock (_preparedCycles) { _preparedCycles.Add(cycle.AiracCycleId); }
				return Task.FromResult(cycle.AiracCycleId);
			},
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection())));
	}

	public void Dispose()
	{
		AppEnvironment.HttpClientForTesting?.Dispose();
		AppEnvironment.ResetForTesting();
		AiracCycleDataCache.ConfigureForTesting(null);
		UserConfigFile.ConfigureForTesting(null);
		TempWorkspace.ConfigureForTesting(null);
		AppLog.ConfigureForTesting(null);

		try
		{
			Directory.Delete(_root, recursive: true);
		}
		catch
		{
			// Best-effort.
		}
	}

	/// <summary>Answers the three services the launch talks to; anything else is a 404.</summary>
	private static HttpResponseMessage Online(HttpRequestMessage request)
	{
		string url = request.RequestUri!.ToString();

		string? body =
			url.StartsWith("https://timeapi.io/", StringComparison.Ordinal) ? """{"dateTime":"2026-09-07T12:34:56.0000000"}"""
			: url.StartsWith("https://api.github.com/repos/Nikolai558/FE-BUDDY/releases", StringComparison.Ordinal) ? ReleasesJson
			: url == NewsService.RawUrl ? NewsMarkdown
			: null;

		return body is null
			? new HttpResponseMessage(HttpStatusCode.NotFound)
			: new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
	}

	/// <summary>Records every report; optionally throws when a step (not the final Complete) succeeds.</summary>
	private sealed class RecordingProgress(bool failEveryStep = false) : IProgress<LaunchProgress>
	{
		public List<LaunchProgress> Reports { get; } = [];

		public void Report(LaunchProgress value)
		{
			lock (Reports)
			{
				Reports.Add(value);
			}

			if (failEveryStep && value.Status == LaunchStepStatus.Succeeded && value.Step != LaunchStep.Complete)
			{
				throw new InvalidOperationException("step blew up");
			}
		}
	}

	[Fact]
	public async Task online_launch_publishes_time_version_news_and_airac_state()
	{
		AppEnvironment.HttpClientForTesting = new HttpClient(new StubHttpHandler(Online));
		int changes = 0;
		AppEnvironment.Changed += (_, _) => Interlocked.Increment(ref changes);
		RecordingProgress progress = new();

		LaunchResult result = await LaunchSequence.RunAsync("3.0.0", progress);

		Assert.Equal(UtcTimeSource.TimeApi, result.Time.Source);
		Assert.True(result.Time.HasInternetConnection);
		Assert.Equal(new DateTime(2026, 9, 7, 12, 34, 56, DateTimeKind.Utc), result.Time.UtcNow);
		Assert.True(result.Version.UpdateAvailable);
		Assert.Equal("3.1.0", result.Version.LatestVersion);
		Assert.Equal(ReleaseChannel.Stable, result.Version.Channel);
		Assert.Equal(0, result.TempClearFailures);

		Assert.True(AppEnvironment.HasInternetConnection);
		Assert.Equal(result.Time.UtcNow, AppEnvironment.LaunchUtcNow);
		Assert.Equal(UtcTimeSource.TimeApi, AppEnvironment.LaunchUtcSource);
		Assert.Same(result.Version, AppEnvironment.Version);
		Assert.True(AppEnvironment.News!.FromNetwork);
		Assert.True(AppEnvironment.LaunchCompleted);
		Assert.True(changes >= 4);

		// 2026-09-07 is in cycle 2609: previous 2608, current 2609, next 2610.
		Assert.Equal(["2608", "2609", "2610"], _preparedCycles.Order());
		Assert.Equal(AiracCycleReadiness.Ready, AiracCycleDataCache.Instance.ComputeReadiness());

		Assert.DoesNotContain(progress.Reports, r => r.Status == LaunchStepStatus.Failed);
		Assert.Equal(new LaunchProgress(LaunchStep.Complete, LaunchStepStatus.Succeeded, "Launch complete"), progress.Reports[^1]);
	}

	[Fact]
	public async Task offline_launch_falls_back_to_the_local_clock_and_the_bundled_news()
	{
		AppEnvironment.HttpClientForTesting = new HttpClient(new StubHttpHandler(_ => throw new HttpRequestException("no network")));

		LaunchResult result = await LaunchSequence.RunAsync("3.0.0");

		Assert.Equal(UtcTimeSource.LocalClock, result.Time.Source);
		Assert.False(AppEnvironment.HasInternetConnection);
		Assert.False(result.Version.CheckSucceeded);
		Assert.False(AppEnvironment.News!.FromNetwork);
		Assert.True(AppEnvironment.LaunchCompleted);
	}

	[Fact]
	public async Task a_failing_step_is_logged_and_replaced_by_its_default_so_launch_still_completes()
	{
		AppEnvironment.HttpClientForTesting = new HttpClient(new StubHttpHandler(Online));
		RecordingProgress progress = new(failEveryStep: true);

		LaunchResult result = await LaunchSequence.RunAsync("3.0.0", progress);

		// Every step's result is the fallback: offline local clock, no version check, no news.
		Assert.Equal(UtcTimeSource.LocalClock, result.Time.Source);
		Assert.False(result.Time.HasInternetConnection);
		Assert.Equal(0, result.TempClearFailures);
		Assert.Equal("Version check did not run.", result.Version.Message);
		Assert.False(AppEnvironment.News!.ParseSucceeded);
		Assert.True(AppEnvironment.LaunchCompleted);

		LaunchStep[] failed = [.. progress.Reports.Where(r => r.Status == LaunchStepStatus.Failed).Select(r => r.Step).Order()];
		Assert.Equal(
			new[]
			{
				LaunchStep.ClearTempWorkspace, LaunchStep.ReadUserConfig, LaunchStep.CheckUtcTimeAndInternet,
				LaunchStep.CheckVersion, LaunchStep.PrepareAiracData, LaunchStep.CheckNews,
			}.Order(),
			failed);
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("step blew up. Continuing launch.", StringComparison.Ordinal));
	}

	[Fact]
	public async Task recheck_refreshes_the_online_state_and_version()
	{
		AppEnvironment.HttpClientForTesting = new HttpClient(new StubHttpHandler(Online));
		AppEnvironment.Changed += (_, _) => throw new InvalidOperationException("a misbehaving subscriber");

		await AppEnvironment.RecheckAsync();

		Assert.True(AppEnvironment.HasInternetConnection);
		Assert.Equal(UtcTimeSource.TimeApi, AppEnvironment.LaunchUtcSource);
		Assert.True(AppEnvironment.Version!.CheckSucceeded);
		Assert.Equal("3.1.0", AppEnvironment.Version.LatestVersion);
	}

	[Fact]
	public async Task airac_service_loads_the_selected_cycle_from_the_cache()
	{
		AiracCycleInfo previous = new("2608", "06_Aug_2026", new DateOnly(2026, 8, 6));
		AiracCycleInfo current = new("2609", "03_Sep_2026", new DateOnly(2026, 9, 3));
		AiracCycleInfo next = new("2610", "01_Oct_2026", new DateOnly(2026, 10, 1));
		await AiracCycleDataCache.Instance.PrepareCyclesAsync(previous, current, next);

		List<AiracServiceProgress> reports = [];
		AiracServiceResult result = await AiracService.RunAsync(
			new AiracServiceSettings { SelectedCycle = current, OutputDirectory = Path.Combine(_root, "output") },
			new SynchronousProgress<AiracServiceProgress>(reports.Add));

		Assert.Contains(result.Warnings, w => w.Contains("no sub-service", StringComparison.OrdinalIgnoreCase));
		Assert.Equal("Loading parsed data for cycle 2609", Assert.Single(reports).Message);
		await Assert.ThrowsAsync<ArgumentNullException>(() => AiracService.RunAsync((AiracServiceSettings)null!));
	}

	private sealed class SynchronousProgress<T>(Action<T> report) : IProgress<T>
	{
		public void Report(T value) => report(value);
	}
}
