using System.Net;
using System.Net.Http;

using FeBuddy.Core.Helpers;
using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Services.General;

namespace FeBuddy.UnitTests.Services.General;

/// <summary>
/// A canned <see cref="HttpMessageHandler"/> so the launch-time network checks can be tested
/// without touching the real internet.
/// </summary>
internal sealed class StubHttpHandler : HttpMessageHandler
{
	private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

	public StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
		Task.FromResult(_responder(request));
}

/// <summary>
/// Covers the Phase 0.4 launch-sequence building blocks: <see cref="TempWorkspace"/>,
/// <see cref="UtcTimeCheck"/>, and <see cref="VersionCheck"/>.
/// </summary>
[Collection("AppLog")]
public sealed class LaunchServicesTests : IDisposable
{
	private readonly string _tempRoot =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_TempWs_" + Guid.NewGuid().ToString("N"));

	/// <summary>Redirects static file/log state to throwaway locations.</summary>
	public LaunchServicesTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_tempRoot, "logs"));
		TempWorkspace.ConfigureForTesting(_tempRoot);
		AppEnvironment.ResetForTesting();
	}

	/// <summary>Restores defaults and cleans up.</summary>
	public void Dispose()
	{
		TempWorkspace.ConfigureForTesting(null);
		AppLog.ConfigureForTesting(null);
		AppEnvironment.ResetForTesting();

		try
		{
			if (Directory.Exists(_tempRoot))
			{
				Directory.Delete(_tempRoot, recursive: true);
			}
		}
		catch
		{
			// Best-effort.
		}
	}

	/// <summary><see cref="TempWorkspace.ClearOnLaunch"/> empties the tree and does not throw on a missing root.</summary>
	[Fact]
	public void TempWorkspace_ClearOnLaunch_EmptiesTree()
	{
		Assert.Equal(0, TempWorkspace.ClearOnLaunch()); // root does not exist yet

		Directory.CreateDirectory(Path.Combine(_tempRoot, "Downloads", "nested"));
		File.WriteAllText(Path.Combine(_tempRoot, "a.zip"), "x");
		File.WriteAllText(Path.Combine(_tempRoot, "Downloads", "b.csv"), "y");

		int failures = TempWorkspace.ClearOnLaunch();

		Assert.Equal(0, failures);
		Assert.True(Directory.Exists(_tempRoot));
		Assert.Empty(Directory.EnumerateFileSystemEntries(_tempRoot));
	}

	/// <summary>A good timeapi.io response yields <see cref="UtcTimeSource.TimeApi"/> and an online result.</summary>
	[Fact]
	public async Task UtcTimeCheck_ParsesTimeApiResponse()
	{
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("""{"dateTime":"2026-09-07T12:34:56.0000000"}"""),
		}));

		UtcTimeCheckResult result = await UtcTimeCheck.RunAsync(client);

		Assert.True(result.HasInternetConnection);
		Assert.Equal(UtcTimeSource.TimeApi, result.Source);
		Assert.Equal(new DateTime(2026, 9, 7, 12, 34, 56, DateTimeKind.Utc), result.UtcNow);
	}

	/// <summary>When every network call fails, the check falls back to the local clock and reports offline.</summary>
	[Fact]
	public async Task UtcTimeCheck_FallsBackToLocalClockWhenOffline()
	{
		using HttpClient client = new(new StubHttpHandler(_ => throw new HttpRequestException("no network")));

		UtcTimeCheckResult result = await UtcTimeCheck.RunAsync(client);

		Assert.False(result.HasInternetConnection);
		Assert.Equal(UtcTimeSource.LocalClock, result.Source);
	}

	/// <summary>Offline: the version check is skipped and reports an unknown state, not "up to date".</summary>
	[Fact]
	public async Task VersionCheck_Offline_ReportsUnknown()
	{
		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", UpdateChannel.Stable, hasInternetConnection: false);

		Assert.False(result.CheckSucceeded);
		Assert.False(result.UpdateAvailable);
		Assert.Null(result.LatestVersion);
	}

	/// <summary>A newer stable release on GitHub is reported as an available update; pre-releases are ignored on Stable.</summary>
	[Fact]
	public async Task VersionCheck_FindsNewerStableRelease()
	{
		const string releasesJson = """
		[
		  { "tag_name": "v3.2.0", "prerelease": true,  "draft": false },
		  { "tag_name": "v3.1.0", "prerelease": false, "draft": false },
		  { "tag_name": "v3.0.0", "prerelease": false, "draft": false }
		]
		""";

		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(releasesJson),
		}));

		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", UpdateChannel.Stable, hasInternetConnection: true, client);

		Assert.True(result.CheckSucceeded);
		Assert.True(result.UpdateAvailable);
		Assert.Equal("3.1.0", result.LatestVersion); // 3.2.0 skipped: pre-release on the Stable channel
	}

	/// <summary>On the Alpha channel the pre-release is considered.</summary>
	[Fact]
	public async Task VersionCheck_AlphaChannel_ConsidersPreReleases()
	{
		const string releasesJson = """
		[
		  { "tag_name": "v3.2.0", "prerelease": true,  "draft": false },
		  { "tag_name": "v3.1.0", "prerelease": false, "draft": false }
		]
		""";

		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(releasesJson),
		}));

		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", UpdateChannel.Alpha, hasInternetConnection: true, client);

		Assert.True(result.UpdateAvailable);
		Assert.Equal("3.2.0", result.LatestVersion);
	}

	/// <summary>
	/// A development build newer than every public release (e.g. this branch's 3.0.0 against the
	/// real repo's current 2.9.x releases) reports <see cref="VersionCheckResult.IsAheadOfLatestRelease"/>,
	/// not <see cref="VersionCheckResult.UpdateAvailable"/> - it must never suggest "downgrading" to
	/// the latest public release.
	/// </summary>
	[Fact]
	public async Task VersionCheck_CurrentAheadOfLatestRelease_ReportsAhead()
	{
		const string releasesJson = """
		[
		  { "tag_name": "v2.9.1-alpha.1", "prerelease": true,  "draft": false },
		  { "tag_name": "v2.9.0", "prerelease": false, "draft": false }
		]
		""";

		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(releasesJson),
		}));

		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", UpdateChannel.Stable, hasInternetConnection: true, client);

		Assert.True(result.CheckSucceeded);
		Assert.False(result.UpdateAvailable);
		Assert.True(result.IsAheadOfLatestRelease);
		Assert.Equal("2.9.0", result.LatestVersion);
	}

	/// <summary>
	/// An unauthenticated failure (e.g. the repo requires auth to be visible) retries once with
	/// <see cref="GitHubAuth.EnvironmentVariableName"/> when it's set, and succeeds off that retry.
	/// </summary>
	[Fact]
	public async Task VersionCheck_UnauthenticatedFails_RetriesWithToken()
	{
		const string releasesJson = """
		[
		  { "tag_name": "v3.1.0", "prerelease": false, "draft": false }
		]
		""";

		Environment.SetEnvironmentVariable(GitHubAuth.EnvironmentVariableName, "test-token");
		try
		{
			using HttpClient client = new(new StubHttpHandler(request =>
				request.Headers.Authorization is not null
					? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releasesJson) }
					: new HttpResponseMessage(HttpStatusCode.NotFound)));

			VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", UpdateChannel.Stable, hasInternetConnection: true, client);

			Assert.True(result.CheckSucceeded);
			Assert.True(result.UpdateAvailable);
			Assert.Equal("3.1.0", result.LatestVersion);
		}
		finally
		{
			Environment.SetEnvironmentVariable(GitHubAuth.EnvironmentVariableName, null);
		}
	}

	/// <summary>With no token set, an unauthenticated failure is reported as-is - no retry is attempted.</summary>
	[Fact]
	public async Task VersionCheck_UnauthenticatedFails_NoTokenSet_ReportsFailureWithoutRetrying()
	{
		Environment.SetEnvironmentVariable(GitHubAuth.EnvironmentVariableName, null);

		int callCount = 0;
		using HttpClient client = new(new StubHttpHandler(_ =>
		{
			callCount++;
			return new HttpResponseMessage(HttpStatusCode.NotFound);
		}));

		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", UpdateChannel.Stable, hasInternetConnection: true, client);

		Assert.False(result.CheckSucceeded);
		Assert.Equal(1, callCount);
	}

	/// <summary><see cref="VersionCheckResult.ParseChannel"/> is case-insensitive and defaults to Stable.</summary>
	[Theory]
	[InlineData("Stable", UpdateChannel.Stable)]
	[InlineData("beta", UpdateChannel.Beta)]
	[InlineData("ALPHA", UpdateChannel.Alpha)]
	[InlineData("", UpdateChannel.Stable)]
	[InlineData(null, UpdateChannel.Stable)]
	[InlineData("nonsense", UpdateChannel.Stable)]
	public void ParseChannel_HandlesStoredValues(string? stored, UpdateChannel expected)
	{
		Assert.Equal(expected, VersionCheckResult.ParseChannel(stored));
	}
}
