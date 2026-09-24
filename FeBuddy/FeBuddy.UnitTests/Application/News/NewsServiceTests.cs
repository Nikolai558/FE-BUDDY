using System.Net;
using System.Net.Http;

using FeBuddy.Core.Application.News;
using FeBuddy.Core.Application.News.Models;
using FeBuddy.Core.Infrastructure.GitHub;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Application.News;

/// <summary>
/// Covers <see cref="NewsService"/>: PostId parsing/ordering, markdown parsing, the
/// unread-count comparison against <c>General.NewsLastOpen</c>, and the bundled-copy
/// fallback (remediation plan 6.1).
/// </summary>
[Collection("AppLog")]
public sealed class NewsServiceTests : IDisposable
{
	private const string SampleMarkdown = """
        # FE-Buddy News
        <!-- PostId format = yyyy-mm-dd.# -->

        News here.

        ---

        ## 2026-09-02
        <!--
        PostId: 2026-09-02.1
        -->

        **Cycle 2610 support**

        Details about the newest post.

        ---

        ## 2026-08-30
        <!--
        PostId: 2026-08-30.2
        -->

        **Version 1.4.1 Released**

        Second post that day.

        ---

        ## 2026-08-30
        <!--
        PostId: 2026-08-30.1
        -->

        **Version 1.4 Released**

        First post that day.
        """;

	public NewsServiceTests() => AppLog.ConfigureForTesting(Path.Combine(Path.GetTempPath(), "FeBuddyTests_News_" + Guid.NewGuid().ToString("N")));

	public void Dispose() => AppLog.ConfigureForTesting(null);

	[Theory]
	[InlineData("2026-08-30.3", true, 3)]
	[InlineData("2026-08-30.1", true, 1)]
	[InlineData("2026-13-01.1", false, 0)]
	[InlineData("2026-08-30.0", false, 0)]
	[InlineData("garbage", false, 0)]
	[InlineData(null, false, 0)]
	public void news_post_id_try_parse(string? value, bool expectOk, int expectSeq)
	{
		bool ok = NewsPostId.TryParse(value, out NewsPostId id);

		Assert.Equal(expectOk, ok);
		if (expectOk)
		{
			Assert.Equal(expectSeq, id.Sequence);
		}
	}

	[Fact]
	public void news_post_id_ordering_is_by_date_then_sequence()
	{
		NewsPostId.TryParse("2026-08-30.2", out NewsPostId a);
		NewsPostId.TryParse("2026-08-30.3", out NewsPostId b);
		NewsPostId.TryParse("2026-09-01.1", out NewsPostId c);

		Assert.True(a.CompareTo(b) < 0);
		Assert.True(b.CompareTo(c) < 0);
		Assert.True(c.CompareTo(a) > 0);
	}

	[Fact]
	public void parse_returns_posts_newest_first_with_titles_and_ids()
	{
		IReadOnlyList<NewsPost> posts = NewsService.Parse(SampleMarkdown);

		Assert.Equal(3, posts.Count);
		Assert.Equal("2026-09-02.1", posts[0].Id.ToString());
		Assert.Equal("2026-08-30.2", posts[1].Id.ToString());
		Assert.Equal("2026-08-30.1", posts[2].Id.ToString());
		Assert.Equal("Cycle 2610 support", posts[0].Title);
		Assert.Contains("newest post", posts[0].Body);
	}

	[Fact]
	public void parse_on_junk_returns_no_posts()
	{
		Assert.Empty(NewsService.Parse("not markdown, no headings, no post ids"));
	}

	[Fact]
	public async Task check_async_offline_uses_bundled_copy_and_counts_unread_posts()
	{
		// Bundled News.md (embedded in FeBuddy.Core) has known posts; never checked -> all unread.
		NewsCheckResult result = await NewsService.CheckAsync(lastOpenPostId: null, hasInternetConnection: false);

		Assert.True(result.ParseSucceeded);
		Assert.False(result.FromNetwork);
		Assert.NotEmpty(result.Posts);
		Assert.Equal(result.Posts.Count, result.NewPostCount);
		Assert.Equal(result.Posts[0].Id, result.LatestPostId);
	}

	[Fact]
	public async Task check_async_with_last_open_at_newest_reports_zero_unread()
	{
		NewsCheckResult first = await NewsService.CheckAsync(null, hasInternetConnection: false);
		string newest = first.LatestPostId!.Value.ToString();

		NewsCheckResult second = await NewsService.CheckAsync(newest, hasInternetConnection: false);

		Assert.Equal(0, second.NewPostCount);
	}

	/// <summary>A reachable GitHub raw fetch is used as-is - no bundled fallback and no retry needed.</summary>
	[Fact]
	public async Task check_async_fetches_from_git_hub_when_reachable()
	{
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(SampleMarkdown),
		}));

		NewsCheckResult result = await NewsService.CheckAsync(lastOpenPostId: null, hasInternetConnection: true, client);

		Assert.True(result.FromNetwork);
		Assert.True(result.ParseSucceeded);
		Assert.Equal("2026-09-02.1", result.LatestPostId!.Value.ToString());
	}

	/// <summary>
	/// When the plain raw URL fails and FEBUDDY_GITHUB_TOKEN is set, the retry via the Contents
	/// API succeeds and is used.
	/// </summary>
	[Fact]
	public async Task check_async_unauthenticated_fails_retries_with_token_via_contents_api()
	{
		Environment.SetEnvironmentVariable(GitHubAuth.EnvironmentVariableName, "test-token");
		try
		{
			using HttpClient client = new(new StubHttpHandler(request =>
				request.Headers.Authorization is not null
					? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(SampleMarkdown) }
					: new HttpResponseMessage(HttpStatusCode.NotFound)));

			NewsCheckResult result = await NewsService.CheckAsync(null, hasInternetConnection: true, client);

			Assert.True(result.FromNetwork);
			Assert.True(result.ParseSucceeded);
			Assert.Equal("2026-09-02.1", result.LatestPostId!.Value.ToString());
		}
		finally
		{
			Environment.SetEnvironmentVariable(GitHubAuth.EnvironmentVariableName, null);
		}
	}

	/// <summary>With no token set, a failed fetch falls back to the bundled copy rather than retrying.</summary>
	[Fact]
	public async Task check_async_unauthenticated_fails_no_token_falls_back_to_bundled_copy()
	{
		Environment.SetEnvironmentVariable(GitHubAuth.EnvironmentVariableName, null);

		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

		NewsCheckResult result = await NewsService.CheckAsync(null, hasInternetConnection: true, client);

		Assert.False(result.FromNetwork);
		Assert.True(result.ParseSucceeded);
	}

	/// <summary>An empty document has no posts.</summary>
	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void parse_empty_document_has_no_posts(string markdown)
	{
		Assert.Empty(NewsService.Parse(markdown));
	}

	/// <summary>A document with no valid posts is reported as a failed parse and marks nothing unread.</summary>
	[Fact]
	public async Task check_async_document_with_no_posts_is_a_failed_parse()
	{
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("# FE-Buddy News\n\n---\n\n## A heading with no PostId\n\nText."),
		}));

		NewsCheckResult result = await NewsService.CheckAsync(null, hasInternetConnection: true, client);

		Assert.True(result.FromNetwork);
		Assert.False(result.ParseSucceeded);
		Assert.Empty(result.Posts);
		Assert.Equal(0, result.NewPostCount);
	}

	/// <summary>A network error while fetching falls back to the bundled copy.</summary>
	[Fact]
	public async Task check_async_network_error_falls_back_to_bundled_copy()
	{
		using HttpClient client = new(new StubHttpHandler(_ => throw new HttpRequestException("no network")));

		NewsCheckResult result = await NewsService.CheckAsync(null, hasInternetConnection: true, client);

		Assert.False(result.FromNetwork);
		Assert.True(result.ParseSucceeded);
	}

	[Fact]
	public void get_bundled_markdown_is_embedded_and_non_empty()
	{
		string bundled = NewsService.GetBundledMarkdown();

		Assert.False(string.IsNullOrWhiteSpace(bundled));
		Assert.Contains("FE-Buddy News", bundled);
	}
}
