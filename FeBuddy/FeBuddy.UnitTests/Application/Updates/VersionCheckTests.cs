using System.Net;

using FeBuddy.Core.Application.Updates;
using FeBuddy.Core.Application.Updates.Models;
using FeBuddy.Core.Infrastructure.GitHub;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Versioning.Models;

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
	public async Task version_check_offline_reports_unknown()
	{
		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", ReleaseChannel.Stable, hasInternetConnection: false);

		Assert.False(result.CheckSucceeded);
		Assert.False(result.UpdateAvailable);
		Assert.Null(result.LatestVersion);
	}

	/// <summary>A newer stable release on GitHub is reported as an available update; pre-releases are ignored on Stable.</summary>
	[Fact]
	public async Task version_check_finds_newer_stable_release()
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
	public async Task version_check_alpha_channel_considers_pre_releases()
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
	public async Task version_check_current_ahead_of_latest_release_reports_ahead()
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
	/// With a GitHub token chosen (<see cref="GitHubAuth"/>), the check is sent with it - so it works
	/// even where the repository needs one to be visible.
	/// </summary>
	[Fact]
	public async Task version_check_with_a_token_sends_it()
	{
		const string releasesJson = """
		[
		  { "tag_name": "v3.1.0", "prerelease": false, "draft": false }
		]
		""";

		using IDisposable token = TestCredentials.UseGitHubToken("test-token");
		using HttpClient client = new(new StubHttpHandler(request =>
			request.Headers.Authorization is not null
				? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releasesJson) }
				: new HttpResponseMessage(HttpStatusCode.NotFound)));

		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", ReleaseChannel.Stable, hasInternetConnection: true, client);

		Assert.True(result.CheckSucceeded);
		Assert.True(result.UpdateAvailable);
		Assert.Equal("3.1.0", result.LatestVersion);
	}

	/// <summary>With no token chosen, an unauthenticated failure is reported as-is - no retry is attempted.</summary>
	[Fact]
	public async Task version_check_unauthenticated_fails_no_token_set_reports_failure_without_retrying()
	{
		TestCredentials.Reset();

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

	/// <summary>A check that fails with the token (say, a revoked one) is tried once more without it.</summary>
	[Fact]
	public async Task version_check_token_fails_retries_without_it()
	{
		const string releasesJson = """
		[
		  { "tag_name": "v3.1.0", "prerelease": false, "draft": false }
		]
		""";

		using IDisposable token = TestCredentials.UseGitHubToken("revoked-token");
		int callCount = 0;
		using HttpClient client = new(new StubHttpHandler(request =>
		{
			callCount++;
			return request.Headers.Authorization is null
				? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releasesJson) }
				: new HttpResponseMessage(HttpStatusCode.Unauthorized);
		}));

		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", ReleaseChannel.Stable, hasInternetConnection: true, client);

		Assert.True(result.CheckSucceeded);
		Assert.Equal(2, callCount);
	}

	/// <summary>A check with the token that gets no answer at all is tried once more without it too.</summary>
	[Fact]
	public async Task version_check_token_request_throws_retries_without_it()
	{
		const string releasesJson = """[ { "tag_name": "v3.1.0", "prerelease": false, "draft": false } ]""";

		using IDisposable token = TestCredentials.UseGitHubToken("test-token");
		int callCount = 0;
		using HttpClient client = new(new StubHttpHandler(request =>
		{
			callCount++;
			return request.Headers.Authorization is null
				? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releasesJson) }
				: throw new HttpRequestException("connection reset");
		}));

		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", ReleaseChannel.Stable, hasInternetConnection: true, client);

		Assert.True(result.CheckSucceeded);
		Assert.Equal("3.1.0", result.LatestVersion);
		Assert.Equal(2, callCount);
	}

	/// <summary>A token Windows Credential Manager cannot read never stops the check: it goes without it.</summary>
	[Fact]
	public async Task version_check_unreadable_token_checks_without_it()
	{
		const string releasesJson = """[ { "tag_name": "v3.1.0", "prerelease": false, "draft": false } ]""";

		using IDisposable token = TestCredentials.UseUnreadableGitHubToken();
		List<HttpRequestMessage> sent = [];
		using HttpClient client = new(new StubHttpHandler(request =>
		{
			sent.Add(request);
			return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releasesJson) };
		}));

		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", ReleaseChannel.Stable, hasInternetConnection: true, client);

		Assert.True(result.CheckSucceeded);
		Assert.Null(Assert.Single(sent).Headers.Authorization);
	}

	/// <summary>Cancelling during the attempt with the token stops the check - it is not retried.</summary>
	[Fact]
	public async Task version_check_cancelled_during_the_token_attempt_is_not_retried()
	{
		using IDisposable token = TestCredentials.UseGitHubToken("test-token");
		using CancellationTokenSource cts = new();
		int callCount = 0;
		using HttpClient client = new(new StubHttpHandler(_ =>
		{
			callCount++;
			cts.Cancel();
			throw new TaskCanceledException();
		}));

		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", ReleaseChannel.Stable, hasInternetConnection: true, client, cts.Token);

		Assert.False(result.CheckSucceeded);
		Assert.Equal(1, callCount);
	}

	/// <summary>
	/// Drafts, releases without a tag and tags that are not strict SemVer (a four-part number
	/// included) are skipped; a tag with or without a leading v parses; the winner's notes and URL
	/// are reported.
	/// </summary>
	[Fact]
	public async Task version_check_skips_drafts_and_unparseable_tags_and_reports_the_winners_notes()
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
	public async Task version_check_lists_every_newer_release_newest_first()
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
	public async Task version_check_channel_filters_releases_by_tag(ReleaseChannel channel, string latest, string[] expected)
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
	public async Task version_check_highest_version_wins_whatever_the_order()
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
	public async Task version_check_no_update_lists_no_releases()
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
	public async Task version_check_compares_by_sem_ver_precedence(
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
	/// Ahead of the chosen channel's latest release because the user moved to a more stable channel
	/// while running one of its published pre-releases is said as that, not as a development build.
	/// A development label (-dev), or an unreleased build on its own channel, is still a development build.
	/// </summary>
	[Theory]
	[InlineData("3.1.0-rc.1", ReleaseChannel.Stable, "3.0.2", ReleaseChannel.ReleaseCandidate, "Running a Release Candidate pre-release ahead of the latest Stable release (v3.0.2).")]
	[InlineData("3.1.0-beta.2", ReleaseChannel.ReleaseCandidate, "3.0.2", ReleaseChannel.Beta, "Running a Beta pre-release ahead of the latest Release Candidate release (v3.0.2).")]
	[InlineData("3.1.0-dev", ReleaseChannel.Stable, "3.0.2", null, "Running a development build ahead of the latest Stable release (v3.0.2).")]
	[InlineData("3.1.0-alpha.3", ReleaseChannel.Alpha, "3.1.0-alpha.2", null, "Running a development build ahead of the latest Alpha release (v3.1.0-alpha.2).")]
	[InlineData("3.1.0", ReleaseChannel.Stable, "3.0.2", null, "Running a development build ahead of the latest Stable release (v3.0.2).")]
	public async Task version_check_names_a_pre_release_ahead_of_a_more_stable_channel(
		string current, ReleaseChannel channel, string releaseTag, ReleaseChannel? expectedPreRelease, string expectedMessage)
	{
		string releasesJson = $$"""[ { "tag_name": "{{releaseTag}}", "draft": false } ]""";
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releasesJson) }));

		VersionCheckResult result = await VersionCheck.RunAsync(current, channel, hasInternetConnection: true, client);

		Assert.True(result.IsAheadOfLatestRelease);
		Assert.Equal(expectedPreRelease, result.RunningPreReleaseChannel);
		Assert.Equal(expectedMessage, result.Message);
		Assert.Equal(expectedPreRelease is not null, result.CanGoBack);
	}

	/// <summary>
	/// Running a pre-release after choosing Stable, the latest stable release - older, a 2.x one
	/// today - is offered to go back to, with its notes and installer.
	/// </summary>
	[Fact]
	public async Task version_check_offers_the_latest_stable_release_to_go_back_to_from_a_pre_release()
	{
		const string releasesJson = """
		[
		  { "tag_name": "3.0.0-alpha.2", "prerelease": true, "draft": false, "html_url": "https://example.test/a2",
		    "assets": [ { "id": 7, "name": "FE-BUDDY-Setup.msi", "browser_download_url": "https://example.test/a2.msi", "size": 5 } ] },
		  { "tag_name": "2.9.3", "prerelease": false, "draft": false, "html_url": "https://example.test/293", "body": "## Change log:\n- Fixes",
		    "assets": [ { "id": 8, "name": "FE-BUDDY-2.9.3.msi", "browser_download_url": "https://example.test/293.msi", "size": 9 } ] },
		  { "tag_name": "2.9.0", "prerelease": false, "draft": false }
		]
		""";
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releasesJson) }));

		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0-alpha.2", ReleaseChannel.Stable, hasInternetConnection: true, client);

		Assert.False(result.UpdateAvailable);
		Assert.True(result.CanGoBack);
		Assert.Equal(ReleaseChannel.Alpha, result.RunningPreReleaseChannel);
		Assert.Equal("2.9.3", result.LatestVersion);
		Assert.Empty(result.NewerReleases);
		Assert.Equal(new ReleaseSummary("2.9.3", null, false, "## Change log:\n- Fixes", "https://example.test/293"), result.LatestRelease);
		Assert.Equal(new ReleaseInstaller("FE-BUDDY-2.9.3.msi", "https://example.test/293.msi", 8, 9), result.LatestInstaller);
	}

	/// <summary>Up to date, or with an update waiting, there is nothing to go back to.</summary>
	[Theory]
	[InlineData("2.9.3")]
	[InlineData("2.9.0")]
	public async Task version_check_offers_nothing_to_go_back_to_on_the_running_builds_channel(string current)
	{
		const string releasesJson = """[ { "tag_name": "2.9.3", "draft": false } ]""";
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releasesJson) }));

		VersionCheckResult result = await VersionCheck.RunAsync(current, ReleaseChannel.Stable, hasInternetConnection: true, client);

		Assert.False(result.CanGoBack);
		Assert.Equal("2.9.3", result.LatestRelease!.Version);
	}

	/// <summary>
	/// The check asks for GitHub's largest page of releases, so a long run of pre-releases cannot
	/// hide every stable one; and the log names a channel as people read it.
	/// </summary>
	[Fact]
	public async Task version_check_asks_for_100_releases_and_logs_the_channels_display_name()
	{
		string? url = null;
		using HttpClient client = new(new StubHttpHandler(request =>
		{
			url = request.RequestUri!.ToString();
			return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") };
		}));

		await VersionCheck.RunAsync("3.0.0", ReleaseChannel.ReleaseCandidate, hasInternetConnection: true, client);

		Assert.EndsWith("/releases?per_page=100", url, StringComparison.Ordinal);
		Assert.Contains(AppLog.Entries, e => e.Message == "No comparable release found on the Release Candidate channel.");
	}

	/// <summary>
	/// The running version must be strict SemVer too: a four-part assembly number such as
	/// 2.8.1.0 cannot be compared, so no update is offered rather than a wrong one.
	/// </summary>
	[Fact]
	public async Task version_check_four_part_current_version_is_not_compared()
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
	public void strip_install_instructions_removes_only_that_section(string? notes, string? expected)
	{
		Assert.Equal(expected, VersionCheck.StripInstallInstructions(notes));
	}

	/// <summary>A dev build (unparseable current version) never claims an update is available.</summary>
	[Theory]
	[InlineData("dev")]
	[InlineData("")]
	public async Task version_check_unparseable_or_missing_current_version_is_not_offered(string currentVersion)
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
	public async Task version_check_no_comparable_release_succeeds_with_no_update()
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
	public async Task version_check_unreadable_response_reports_failure()
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
	public void parse_channel_handles_stored_values(string? stored, ReleaseChannel expected)
	{
		Assert.Equal(expected, VersionCheckResult.ParseChannel(stored));
	}

	/// <summary>With a fallback, a missing or unrecognized value gives the fallback; a stored one still wins.</summary>
	[Theory]
	[InlineData(null, ReleaseChannel.Alpha)]
	[InlineData(" ", ReleaseChannel.Alpha)]
	[InlineData("nonsense", ReleaseChannel.Alpha)]
	[InlineData("Stable", ReleaseChannel.Stable)]
	public void parse_channel_falls_back_to_the_given_channel(string? stored, ReleaseChannel expected)
	{
		Assert.Equal(expected, VersionCheckResult.ParseChannel(stored, ReleaseChannel.Alpha));
	}
}
