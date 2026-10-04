using System.Net;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.VnasAlias.Models;
using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Application.Launch.Models;
using FeBuddy.Core.Application.News;
using FeBuddy.Core.Domain.Airac;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;
using FeBuddy.Core.Infrastructure.Platform;
using FeBuddy.Core.Infrastructure.Platform.Models;
using FeBuddy.Core.Infrastructure.SharedData.Models;
using FeBuddy.Core.Infrastructure.Telephony.Models;

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
	private readonly List<string> _squirrelRuns = [];

	public LaunchSequenceTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		TempWorkspace.ConfigureForTesting(Path.Combine(_root, "temp"));
		UserConfigFile.ConfigureForTesting(Path.Combine(_root, "config"));
		AppEnvironment.ResetForTesting();
		LegacyGitHubTokenNotice.ConfigureForTesting(() => []);
		LegacySquirrelShortcuts.ConfigureForTesting([Path.Combine(_root, "desktop")], Path.Combine(_root, "squirrel", "FE-BUDDY.exe"));
		LegacySquirrelInstall.ConfigureForTesting(
			Path.Combine(_root, "squirrel"),
			@"Software\FeBuddyTests_LaunchNoSuchKey_" + Guid.NewGuid().ToString("N"),
			(file, args, _) =>
			{
				_squirrelRuns.Add($"{file} {args}");
				return 0;
			});
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
		LegacyGitHubTokenNotice.ConfigureForTesting(null);
		LegacySquirrelShortcuts.ConfigureForTesting(null, null);
		LegacySquirrelInstall.ConfigureForTesting(null, null, null);
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

	/// <summary>Launch looks for FE-Buddy 2.x's GitHub token variable and leaves the one-time notice for the shell.</summary>
	[Fact]
	public async Task launch_finds_the_old_token_variable_for_the_one_time_notice()
	{
		EnvironmentVariableTarget[] forUser = [EnvironmentVariableTarget.User];
		AppEnvironment.HttpClientForTesting = new HttpClient(new StubHttpHandler(Online));
		LegacyGitHubTokenNotice.ConfigureForTesting(() => forUser);

		await LaunchSequence.RunAsync("3.0.0");

		Assert.Equal(forUser, LegacyGitHubTokenNotice.Take());
		Assert.True(LegacyGitHubTokenNotice.HasBeenShown);
	}

	/// <summary>Launch runs the uninstaller of a copy of FE-Buddy 2.x that Squirrel left, and does nothing without one.</summary>
	[Fact]
	public async Task launch_uninstalls_fe_buddy_2x_left_by_squirrel()
	{
		AppEnvironment.HttpClientForTesting = new HttpClient(new StubHttpHandler(Online));

		await LaunchSequence.RunAsync("3.0.0");
		Assert.Empty(_squirrelRuns);

		string updateExe = Path.Combine(Directory.CreateDirectory(Path.Combine(_root, "squirrel")).FullName, "Update.exe");
		File.WriteAllText(updateExe, "exe");

		await LaunchSequence.RunAsync("3.0.0");
		Assert.Equal($"{updateExe} --uninstall", Assert.Single(_squirrelRuns));
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
				LaunchStep.ClearTempWorkspace, LaunchStep.ReadUserConfig, LaunchStep.CheckLegacyGitHubToken,
				LaunchStep.RemoveLegacySquirrelInstall, LaunchStep.RemoveLegacyShortcuts, LaunchStep.CheckUtcTimeAndInternet, LaunchStep.CheckVersion, LaunchStep.PrepareAiracData, LaunchStep.CheckNews,
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

	[Fact]
	public async Task airac_service_loads_dtpp_and_previous_dtpp_from_the_cache_when_procedures_is_selected()
	{
		AiracCycleInfo previous = new("2608", "06_Aug_2026", new DateOnly(2026, 8, 6));
		AiracCycleInfo current = new("2609", "03_Sep_2026", new DateOnly(2026, 9, 3));
		AiracCycleInfo next = new("2610", "01_Oct_2026", new DateOnly(2026, 10, 1));

		// The cycle immediately before "current" - what AiracService itself resolves to load the
		// previous cycle's d-TPP Metafile, so the test does not have to hard-code which cycle ID
		// that is.
		AiracCycleInfo previousOfCurrent = AiracCycleResolver.GetCycle(AiracCyclePosition.Previous, asOfUtc: current.EffectiveDateUtc);

		DtppMetafileDataCollection currentDtpp = new() { Cycle = current.AiracCycleId };
		DtppMetafileDataCollection previousDtpp = new() { Cycle = previousOfCurrent.AiracCycleId };
		List<string> loadedForCycles = [];

		// The single-settings RunAsync overload resolves both the selected cycle's d-TPP Metafile
		// and the previous cycle's from AiracCycleDataCache.Instance, only when settings.Procedures
		// is not null - this re-wires that instance with a loadDtpp step to exercise it.
		AiracCycleDataCache.ConfigureForTesting(new AiracCycleDataCache(
			probe: (_, _) => Task.FromResult(AiracCyclePublicationState.Published),
			download: (cycle, _) => Task.FromResult(cycle.AiracCycleId),
			// Apt and ClsArsp must be non-null (even empty) for ProcedureBuilder.Build, which the
			// Procedures block below now actually reaches.
			parse: (_, _) => Task.FromResult(new NasrCsvDataCollection
			{
				Apt = new AptCsvDataCollection(),
				ClsArsp = new ClsArspCsvDataCollection(),
			}),
			loadDtpp: (cycleDirectory, _) =>
			{
				lock (loadedForCycles) { loadedForCycles.Add(cycleDirectory); }

				DtppMetafileDataCollection? found =
					cycleDirectory == current.AiracCycleId ? currentDtpp
					: cycleDirectory == previousOfCurrent.AiracCycleId ? previousDtpp
					: null;

				return Task.FromResult(found);
			}));

		await AiracCycleDataCache.Instance.PrepareCyclesAsync(previous, current, next);

		List<AiracServiceProgress> reports = [];
		AiracServiceResult result = await AiracService.RunAsync(
			new AiracServiceSettings
			{
				SelectedCycle = current,
				OutputDirectory = Path.Combine(_root, "output"),
				Procedures = new Dictionary<string, string> { ["Facilities"] = "ZOB" },
			},
			new SynchronousProgress<AiracServiceProgress>(reports.Add));

		Assert.Equal(new[] { current.AiracCycleId, previousOfCurrent.AiracCycleId }.Order(), loadedForCycles.Order());
		Assert.Contains(reports, r => r.Message == $"Loading d-TPP Metafile data for cycle {current.AiracCycleId}");
		Assert.Contains(reports, r => r.Message == $"Loading d-TPP Metafile data for cycle {previousOfCurrent.AiracCycleId}");
	}

	[Fact]
	public async Task airac_service_downloads_the_latest_wx_station_and_telephony_data_when_those_are_selected()
	{
		AiracCycleInfo previous = new("2608", "06_Aug_2026", new DateOnly(2026, 8, 6));
		AiracCycleInfo current = new("2609", "03_Sep_2026", new DateOnly(2026, 9, 3));
		AiracCycleInfo next = new("2610", "01_Oct_2026", new DateOnly(2026, 10, 1));

		Directory.CreateDirectory(_root);
		string stationsFile = Path.Combine(_root, "stations.cache.xml");
		File.WriteAllText(stationsFile, "<response><data num_results=\"1\"><Station><icao_id>KDTW</icao_id></Station></data></response>");
		string registerFile = Path.Combine(_root, "telephony_register.html");
		File.WriteAllText(registerFile,
			"<table><thead><tr><th>Company</th><th>Country</th><th>Telephony</th><th>3-Ltr</th></tr></thead>" +
			"<tbody><tr><td>AEROVIAS DEL CONTINENTE AMERICANO S.A.</td><td>COLOMBIA</td><td>AVIANCA</td><td>AVA</td></tr></tbody></table>");

		// The single-settings RunAsync overload downloads the latest copies itself, only for the
		// sub-services selected - these stand in for the downloads, so nothing touches the network.
		AiracSharedDataLoader.ConfigureForTesting(
			refreshWxStations: _ => Task.FromResult(new SharedDataRefreshResult(stationsFile, DateTime.UtcNow, FailureReason: null)),
			refreshTelephony: _ => Task.FromResult(new TelephonyRefreshResult(
				new SharedDataRefreshResult(registerFile, DateTime.UtcNow, FailureReason: null),
				new SharedDataRefreshResult(FilePath: null, DownloadedUtc: null, "offline"))));

		try
		{
			await AiracCycleDataCache.Instance.PrepareCyclesAsync(previous, current, next);

			List<AiracServiceProgress> reports = [];
			AiracServiceResult result = await AiracService.RunAsync(
				new AiracServiceSettings
				{
					SelectedCycle = current,
					OutputDirectory = Path.Combine(_root, "output"),
					WxStations = new Dictionary<string, string>(),
					Telephony = new Dictionary<string, string>(),
				},
				new SynchronousProgress<AiracServiceProgress>(reports.Add));

			Assert.Contains(reports, r => r.Message == "Downloading the latest Wx station data");
			Assert.Contains(reports, r => r.Message == "Downloading the latest FAA telephony pages");
			Assert.Equal(1, result.WxStations!.TotalStationCount);
			Assert.Equal(2, result.Telephony!.AliasCommandCount);

			// What the downloads said leads the run's messages: two fresh copies, then the optional
			// page that could not be had.
			Assert.Equal("Downloaded the latest Wx station data.", result.Messages[0].Text);
			Assert.Equal("Downloaded the latest FAA telephony register.", result.Messages[1].Text);
			Assert.True(result.Messages[2].IsAdvisory);
			Assert.Contains("FAA U.S. special call signs (offline)", result.Messages[2].Text, StringComparison.Ordinal);
		}
		finally
		{
			AiracSharedDataLoader.ConfigureForTesting(null, null);
		}
	}

	/// <summary>
	/// With vNAS Alias Upload selected, the single-settings overload reads the custom alias files
	/// before any sub-service runs: a readable one is merged into vNAS_Alias.txt, one that cannot be
	/// read is left out with a warning, and the block's own parsing messages reach the run.
	/// </summary>
	[Fact]
	public async Task airac_service_reads_the_custom_alias_files_first_when_vnas_alias_upload_is_selected()
	{
		AiracCycleInfo previous = new("2608", "06_Aug_2026", new DateOnly(2026, 8, 6));
		AiracCycleInfo current = new("2609", "03_Sep_2026", new DateOnly(2026, 9, 3));
		AiracCycleInfo next = new("2610", "01_Oct_2026", new DateOnly(2026, 10, 1));
		await AiracCycleDataCache.Instance.PrepareCyclesAsync(previous, current, next);

		Directory.CreateDirectory(_root);
		string customFile = Path.Combine(_root, "ZOB-Alias.txt");
		File.WriteAllText(customFile, ".zobtest .msg Hello from ZOB\r\n.zobtest2 .msg Again\r\n");
		string missingFile = Path.Combine(_root, "Missing-Alias.txt");

		List<AiracServiceProgress> reports = [];
		AiracServiceResult result = await AiracService.RunAsync(
			new AiracServiceSettings
			{
				SelectedCycle = current,
				OutputDirectory = Path.Combine(_root, "output"),
				VnasAlias = new Dictionary<string, string>
				{
					["Sources.1.FilePath"] = customFile,
					["Sources.2.FilePath"] = missingFile,
					["Colour"] = "blue",
				},
			},
			new SynchronousProgress<AiracServiceProgress>(reports.Add));

		Assert.Contains(reports, r => r.SubService == "vNAS Alias Upload" && r.Message == "Reading your custom alias files");

		VnasAliasResult merged = result.VnasAlias!;
		Assert.Equal(2, merged.CustomFileCount);
		Assert.Equal(1, merged.CustomFilesMerged);
		Assert.Equal(2, merged.CustomCommandCount);
		Assert.Contains(".zobtest .msg Hello from ZOB", File.ReadAllText(merged.FilePath!), StringComparison.Ordinal);

		Assert.Contains(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains("'Colour'", StringComparison.Ordinal));
		Assert.Contains(result.Messages, m => m.IsAdvisory && m.Text.Contains($"{missingFile} was not found", StringComparison.Ordinal));
	}

	private sealed class SynchronousProgress<T>(Action<T> report) : IProgress<T>
	{
		public void Report(T value) => report(value);
	}
}
