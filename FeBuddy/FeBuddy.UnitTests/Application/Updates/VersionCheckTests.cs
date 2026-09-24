using System.Net;
using System.Net.Http;

using FeBuddy.Core.Application.Updates;
using FeBuddy.Core.Application.Updates.Models;
using FeBuddy.Core.Infrastructure.GitHub;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Versioning;

namespace FeBuddy.UnitTests.Application.Updates;

/// <summary>
/// Covers <see cref="VersionCheck"/>: channel filtering, SemVer precedence, the token retry, and release notes.
/// </summary>
[Collection("AppLog")]
public sealed class VersionCheckTests : IDisposable
{
	private readonly string _logRoot =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_Log_" + Guid.NewGuid().ToString("N"));

	/// <summary>Redirects the log to a throwaway folder.</summary>
	public VersionCheckTests() => AppLog.ConfigureForTesting(_logRoot);

	/// <summary>Restores the default log folder and cleans up.</summary>
	public void Dispose()
	{
		AppLog.ConfigureForTesting(null);

		try
		{
			if (Directory.Exists(_logRoot))
			{
				Directory.Delete(_logRoot, recursive: true);
			}
		}
		catch
		{
			// Best-effort.
		}
	}

	/// <summary>Offline: the version check is skipped and reports an unknown state, not "up to date".</summary>
	[Fact]
	public async Task VersionCheck_Offline_ReportsUnknown()
	{
		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", ReleaseChannel.Stable, hasInternetConnection: false);

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
		  { "tag_name": "v3.2.0-beta.1", "prerelease": true, "draft": false },
		  { "tag_name": "v3.1.0", "prerelease": false, "draft": false },
		  { "tag_name": "v3.0.0", "prerelease": false, "draft": false }
		]
		""";

		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(releasesJson),
		}));

		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", ReleaseChannel.Stable, hasInternetConnection: true, client);

		Assert.True(result.CheckSucceeded);
		Assert.True(result.UpdateAvailable);
		Assert.Equal("3.1.0", result.LatestVersion); // 3.2.0-beta.1 skipped: pre-release on the Stable channel
	}

	/// <summary>On the Alpha channel the pre-release is considered.</summary>
	[Fact]
	public async Task VersionCheck_AlphaChannel_ConsidersPreReleases()
	{
		const string releasesJson = """
		[
		  { "tag_name": "v3.2.0-alpha.1", "prerelease": true, "draft": false },
		  { "tag_name": "v3.1.0", "prerelease": false, "draft": false }
		]
		""";

		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(releasesJson),
		}));

		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", ReleaseChannel.Alpha, hasInternetConnection: true, client);

		Assert.True(result.UpdateAvailable);
		Assert.Equal("3.2.0-alpha.1", result.LatestVersion);
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

		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", ReleaseChannel.Stable, hasInternetConnection: true, client);

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

			VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", ReleaseChannel.Stable, hasInternetConnection: true, client);

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

		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", ReleaseChannel.Stable, hasInternetConnection: true, client);

		Assert.False(result.CheckSucceeded);
		Assert.Equal(1, callCount);
	}

	/// <summary>
	/// Drafts, releases without a tag and tags that are not strict SemVer (a four-part number
	/// included) are skipped; a tag with or without a leading v parses; the winner's notes and URL
	/// are reported.
	/// </summary>
	[Fact]
	public async Task VersionCheck_SkipsDraftsAndUnparseableTags_AndReportsTheWinnersNotes()
	{
		const string releasesJson = """
		[
		  { "tag_name": "v9.0.0", "prerelease": false, "draft": true },
		  { "prerelease": false, "draft": false },
		  { "tag_name": "nightly", "prerelease": false, "draft": false },
		  { "tag_name": "v8.0.0.0", "prerelease": false, "draft": false },
		  { "tag_name": "3.2.0", "prerelease": false, "draft": false, "body": "Fixes.", "html_url": "https://example.test/3.2.0" },
		  { "tag_name": "V3.1.0", "prerelease": false, "draft": false }
		]
		""";

		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releasesJson) }));

		VersionCheckResult result = await VersionCheck.RunAsync("  3.0.0 ", ReleaseChannel.Stable, hasInternetConnection: true, client);

		Assert.True(result.UpdateAvailable);
		Assert.Equal("3.2.0", result.LatestVersion);
		Assert.Equal(["3.2.0", "3.1.0"], result.NewerReleases.Select(r => r.Version));
		Assert.Equal("Fixes.", result.NewerReleases[0].Notes);
		Assert.Equal("https://example.test/3.2.0", result.LatestReleaseUrl);
		Assert.Equal("3.0.0", result.CurrentVersion);
	}

	/// <summary>
	/// Every release between the running version and the latest is returned newest first, with
	/// dates, pre-release flags and install instructions stripped; older and equal ones are not.
	/// </summary>
	[Fact]
	public async Task VersionCheck_ListsEveryNewerRelease_NewestFirst()
	{
		const string releasesJson = """
		[
		  { "tag_name": "v2.9.1-beta.1", "prerelease": true, "draft": false, "published_at": "2026-09-01T00:00:00Z", "body": "Beta notes." },
		  { "tag_name": "v2.9.0", "prerelease": false, "draft": false, "published_at": "2026-08-30T02:14:57Z",
		    "body": "## Instructions to install:\n- Download it.\n\n## Change log:\n- Things.", "html_url": "https://example.test/2.9.0" },
		  { "tag_name": "v2.8.3", "prerelease": false, "draft": false, "published_at": "not a date" },
		  { "tag_name": "v2.8.2", "prerelease": false, "draft": false, "published_at": null },
		  { "tag_name": "v2.8.1", "prerelease": false, "draft": false },
		  { "tag_name": "v2.8.0", "prerelease": false, "draft": false }
		]
		""";

		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releasesJson) }));

		VersionCheckResult result = await VersionCheck.RunAsync("2.8.1", ReleaseChannel.Beta, hasInternetConnection: true, client);

		Assert.Equal("2.9.1-beta.1", result.LatestVersion);
		Assert.Equal(["2.9.1-beta.1", "2.9.0", "2.8.3", "2.8.2"], result.NewerReleases.Select(r => r.Version));
		Assert.True(result.NewerReleases[0].IsPrerelease);
		Assert.False(result.NewerReleases[1].IsPrerelease);
		Assert.Equal(new DateTimeOffset(2026, 8, 30, 2, 14, 57, TimeSpan.Zero), result.NewerReleases[1].PublishedAt);
		Assert.Equal("## Change log:\n- Things.", result.NewerReleases[1].Notes);
		Assert.Equal("https://example.test/2.9.0", result.NewerReleases[1].Url);
		Assert.Null(result.NewerReleases[2].PublishedAt);
		Assert.Null(result.NewerReleases[3].PublishedAt);
		Assert.Null(result.NewerReleases[3].Notes);
	}

	/// <summary>
	/// Each channel sees its own releases and every more-stable one - Stable, then
	/// ReleaseCandidate adds -rc, Beta adds -beta, Alpha sees everything (an unrecognised tag such
	/// as -preview counts as Alpha). The tag alone decides: GitHub's pre-release flag on 3.1.1 is
	/// ignored. Same-numbered pre-releases sort by SemVer precedence (rc &gt; preview &gt; beta).
	/// </summary>
	[Theory]
	[InlineData(ReleaseChannel.Stable, "3.1.1", new[] { "3.1.1", "3.1.0" })]
	[InlineData(ReleaseChannel.ReleaseCandidate, "3.2.0-rc.1", new[] { "3.2.0-rc.1", "3.1.1", "3.1.0" })]
	[InlineData(ReleaseChannel.Beta, "3.2.0-rc.1", new[] { "3.2.0-rc.1", "3.2.0-beta.1", "3.1.1", "3.1.0" })]
	[InlineData(ReleaseChannel.Alpha, "3.3.0-alpha.1", new[] { "3.3.0-alpha.1", "3.2.0-rc.1", "3.2.0-preview", "3.2.0-beta.1", "3.1.1", "3.1.0" })]
	public async Task VersionCheck_ChannelFiltersReleasesByTag(ReleaseChannel channel, string latest, string[] expected)
	{
		const string releasesJson = """
		[
		  { "tag_name": "v3.3.0-alpha.1", "prerelease": true, "draft": false },
		  { "tag_name": "v3.2.0-beta.1", "prerelease": true, "draft": false },
		  { "tag_name": "v3.2.0-rc.1", "prerelease": false, "draft": false },
		  { "tag_name": "v3.2.0-preview", "prerelease": true, "draft": false },
		  { "tag_name": "v3.1.1", "prerelease": true, "draft": false },
		  { "tag_name": "v3.1.0", "prerelease": false, "draft": false }
		]
		""";

		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releasesJson) }));

		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", channel, hasInternetConnection: true, client);

		Assert.Equal(latest, result.LatestVersion);
		Assert.Equal(expected, result.NewerReleases.Select(r => r.Version));
	}

	/// <summary>The highest version wins even when GitHub does not list it first.</summary>
	[Fact]
	public async Task VersionCheck_HighestVersionWins_WhateverTheOrder()
	{
		const string releasesJson = """
		[
		  { "tag_name": "v3.1.0", "prerelease": false, "draft": false, "html_url": "https://example.test/3.1.0" },
		  { "tag_name": "v3.2.0", "prerelease": false, "draft": false, "html_url": "https://example.test/3.2.0" }
		]
		""";

		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releasesJson) }));

		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", ReleaseChannel.Stable, hasInternetConnection: true, client);

		Assert.Equal("3.2.0", result.LatestVersion);
		Assert.Equal("https://example.test/3.2.0", result.LatestReleaseUrl);
		Assert.Equal(["3.2.0", "3.1.0"], result.NewerReleases.Select(r => r.Version));
	}

	/// <summary>No update means no release list, including for a build ahead of every release.</summary>
	[Fact]
	public async Task VersionCheck_NoUpdate_ListsNoReleases()
	{
		const string releasesJson = """[ { "tag_name": "v3.1.0", "prerelease": false, "draft": false } ]""";
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releasesJson) }));

		VersionCheckResult result = await VersionCheck.RunAsync("3.1.0", ReleaseChannel.Stable, hasInternetConnection: true, client);

		Assert.False(result.UpdateAvailable);
		Assert.Empty(result.NewerReleases);
	}

	/// <summary>
	/// Versions compare by SemVer precedence: a pre-release is below its final release, and a
	/// later pre-release above an earlier one. An -rc user on the Stable channel is offered the
	/// final release; a 3.0.0-dev build is ahead of every 2.x release.
	/// </summary>
	[Theory]
	[InlineData("3.0.0-rc.1", ReleaseChannel.Stable, "3.0.0", true, false)]
	[InlineData("3.0.0-alpha.1", ReleaseChannel.Alpha, "3.0.0-alpha.2", true, false)]
	[InlineData("3.0.0", ReleaseChannel.Alpha, "3.0.0-alpha.2", false, true)]
	[InlineData("3.0.0-rc.1", ReleaseChannel.ReleaseCandidate, "3.0.0-rc.1", false, false)]
	[InlineData("3.0.0-dev", ReleaseChannel.Stable, "2.9.0", false, true)]
	[InlineData("v2.8.3", ReleaseChannel.Stable, "2.9.0", true, false)]
	public async Task VersionCheck_ComparesBySemVerPrecedence(
		string current, ReleaseChannel channel, string releaseTag, bool updateAvailable, bool ahead)
	{
		string releasesJson = $$"""[ { "tag_name": "{{releaseTag}}", "draft": false } ]""";
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releasesJson) }));

		VersionCheckResult result = await VersionCheck.RunAsync(current, channel, hasInternetConnection: true, client);

		Assert.Equal(releaseTag, result.LatestVersion);
		Assert.Equal(updateAvailable, result.UpdateAvailable);
		Assert.Equal(ahead, result.IsAheadOfLatestRelease);
	}

	/// <summary>
	/// The running version must be strict SemVer too: a four-part assembly number such as
	/// 2.8.1.0 cannot be compared, so no update is offered rather than a wrong one.
	/// </summary>
	[Fact]
	public async Task VersionCheck_FourPartCurrentVersion_IsNotCompared()
	{
		const string releasesJson = """[ { "tag_name": "2.9.0", "draft": false } ]""";
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releasesJson) }));

		VersionCheckResult result = await VersionCheck.RunAsync("2.8.1.0", ReleaseChannel.Stable, hasInternetConnection: true, client);

		Assert.True(result.CheckSucceeded);
		Assert.False(result.UpdateAvailable);
		Assert.False(result.IsAheadOfLatestRelease);
		Assert.Empty(result.NewerReleases);
	}

	/// <summary>The install-instructions section is removed up to the next heading of its level or higher.</summary>
	[Theory]
	[InlineData(null, null)]
	[InlineData("  ", null)]
	[InlineData("## Instructions to install:\r\n- Run it.", null)]
	[InlineData("## Instructions to install:\r\n- Run it.\r\n### Sub\r\nmore\r\n\r\n## Change log:\r\n- A", "## Change log:\n- A")]
	[InlineData("# Intro\n## instructions TO INSTALL\n- x\n# Next\ny", "# Intro\n# Next\ny")]
	[InlineData("## Change log:\n- Instructions to install are now shorter.", "## Change log:\n- Instructions to install are now shorter.")]
	[InlineData("## Instructions to installer\n- kept", "## Instructions to installer\n- kept")]
	public void StripInstallInstructions_RemovesOnlyThatSection(string? notes, string? expected)
	{
		Assert.Equal(expected, VersionCheck.StripInstallInstructions(notes));
	}

	/// <summary>A dev build (unparseable current version) never claims an update is available.</summary>
	[Theory]
	[InlineData("dev")]
	[InlineData("")]
	public async Task VersionCheck_UnparseableOrMissingCurrentVersion_IsNotOffered(string currentVersion)
	{
		const string releasesJson = """[ { "tag_name": "v3.1.0", "prerelease": false, "draft": false } ]""";
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releasesJson) }));

		VersionCheckResult result = await VersionCheck.RunAsync(currentVersion, ReleaseChannel.Stable, hasInternetConnection: true, client);

		Assert.True(result.CheckSucceeded);
		Assert.Equal(currentVersion == "" ? "0.0.0" : "dev", result.CurrentVersion);
		Assert.Equal(currentVersion == "", result.UpdateAvailable);
	}

	/// <summary>No release on the channel is a successful check with nothing to offer.</summary>
	[Fact]
	public async Task VersionCheck_NoComparableRelease_SucceedsWithNoUpdate()
	{
		const string releasesJson = """[ { "tag_name": "v3.1.0-rc.1", "prerelease": true, "draft": false } ]""";
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releasesJson) }));

		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", ReleaseChannel.Stable, hasInternetConnection: true, client);

		Assert.True(result.CheckSucceeded);
		Assert.False(result.UpdateAvailable);
		Assert.Null(result.LatestVersion);
		Assert.Equal("No comparable release found.", result.Message);
	}

	/// <summary>A response that is not JSON is reported as a failed check, not an exception.</summary>
	[Fact]
	public async Task VersionCheck_UnreadableResponse_ReportsFailure()
	{
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>rate limited</html>") }));

		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", ReleaseChannel.Stable, hasInternetConnection: true, client);

		Assert.False(result.CheckSucceeded);
		Assert.False(result.UpdateAvailable);
	}

	/// <summary><see cref="VersionCheckResult.ParseChannel"/> is case-insensitive and defaults to Stable.</summary>
	[Theory]
	[InlineData("Stable", ReleaseChannel.Stable)]
	[InlineData("beta", ReleaseChannel.Beta)]
	[InlineData("ALPHA", ReleaseChannel.Alpha)]
	[InlineData(" ReleaseCandidate ", ReleaseChannel.ReleaseCandidate)]
	[InlineData("", ReleaseChannel.Stable)]
	[InlineData(null, ReleaseChannel.Stable)]
	[InlineData("nonsense", ReleaseChannel.Stable)]
	[InlineData("0", ReleaseChannel.Stable)]
	public void ParseChannel_HandlesStoredValues(string? stored, ReleaseChannel expected)
	{
		Assert.Equal(expected, VersionCheckResult.ParseChannel(stored));
	}
}
