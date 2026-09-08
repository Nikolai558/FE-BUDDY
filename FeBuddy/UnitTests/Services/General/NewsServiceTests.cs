using FEBuddyLibrary.Models.Services.General;
using FEBuddyLibrary.Services.General;

namespace UnitTests.Services.General;

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

    public NewsServiceTests() => AppLog.ConfigureForTesting(Path.Combine(Path.GetTempPath(), "FEBuddyTests_News_" + Guid.NewGuid().ToString("N")));

    public void Dispose() => AppLog.ConfigureForTesting(null);

    [Theory]
    [InlineData("2026-08-30.3", true, 3)]
    [InlineData("2026-08-30.1", true, 1)]
    [InlineData("2026-13-01.1", false, 0)]
    [InlineData("2026-08-30.0", false, 0)]
    [InlineData("garbage", false, 0)]
    [InlineData(null, false, 0)]
    public void NewsPostId_TryParse(string? value, bool expectOk, int expectSeq)
    {
        bool ok = NewsPostId.TryParse(value, out NewsPostId id);

        Assert.Equal(expectOk, ok);
        if (expectOk)
        {
            Assert.Equal(expectSeq, id.Sequence);
        }
    }

    [Fact]
    public void NewsPostId_Ordering_IsByDateThenSequence()
    {
        NewsPostId.TryParse("2026-08-30.2", out NewsPostId a);
        NewsPostId.TryParse("2026-08-30.3", out NewsPostId b);
        NewsPostId.TryParse("2026-09-01.1", out NewsPostId c);

        Assert.True(a.CompareTo(b) < 0);
        Assert.True(b.CompareTo(c) < 0);
        Assert.True(c.CompareTo(a) > 0);
    }

    [Fact]
    public void Parse_ReturnsPostsNewestFirst_WithTitlesAndIds()
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
    public void Parse_OnJunk_ReturnsNoPosts()
    {
        Assert.Empty(NewsService.Parse("not markdown, no headings, no post ids"));
    }

    [Fact]
    public async Task CheckAsync_Offline_UsesBundledCopyAndCountsUnreadPosts()
    {
        // Bundled News.md (embedded in FEBuddyLibrary) has known posts; never checked -> all unread.
        NewsCheckResult result = await NewsService.CheckAsync(lastOpenPostId: null, hasInternetConnection: false);

        Assert.True(result.ParseSucceeded);
        Assert.False(result.FromNetwork);
        Assert.NotEmpty(result.Posts);
        Assert.Equal(result.Posts.Count, result.NewPostCount);
        Assert.Equal(result.Posts[0].Id, result.LatestPostId);
    }

    [Fact]
    public async Task CheckAsync_WithLastOpenAtNewest_ReportsZeroUnread()
    {
        NewsCheckResult first = await NewsService.CheckAsync(null, hasInternetConnection: false);
        string newest = first.LatestPostId!.Value.ToString();

        NewsCheckResult second = await NewsService.CheckAsync(newest, hasInternetConnection: false);

        Assert.Equal(0, second.NewPostCount);
    }

    [Fact]
    public void GetBundledMarkdown_IsEmbeddedAndNonEmpty()
    {
        string bundled = NewsService.GetBundledMarkdown();

        Assert.False(string.IsNullOrWhiteSpace(bundled));
        Assert.Contains("FE-Buddy News", bundled);
    }
}
