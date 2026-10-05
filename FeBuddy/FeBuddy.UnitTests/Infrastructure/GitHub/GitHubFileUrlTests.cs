using FeBuddy.Core.Infrastructure.GitHub;

namespace FeBuddy.UnitTests.Infrastructure.GitHub;

/// <summary>
/// Covers <see cref="GitHubFileUrl"/>: every way a user can copy a file's address from GitHub becomes
/// the API address that downloads it and the Raw link FE-Buddy shows, and a GitHub page that is not a
/// file is recognised as such.
/// </summary>
public sealed class GitHubFileUrlTests
{
	private const string Api = "https://api.github.com/repos/vZOB/facility/contents/aliases/ZOB-Alias.txt?ref=main";
	private const string Raw = "https://github.com/vZOB/facility/raw/refs/heads/main/aliases/ZOB-Alias.txt";

	[Theory]
	[InlineData("https://github.com/vZOB/facility/blob/main/aliases/ZOB-Alias.txt")]
	[InlineData("https://www.github.com/vZOB/facility/blob/main/aliases/ZOB-Alias.txt")]
	[InlineData("https://GitHub.com/vZOB/facility/BLOB/main/aliases/ZOB-Alias.txt")]
	[InlineData("https://github.com/vZOB/facility/raw/main/aliases/ZOB-Alias.txt")]
	[InlineData("https://github.com/vZOB/facility/blob/main/aliases/ZOB-Alias.txt?plain=1")]
	[InlineData("https://raw.githubusercontent.com/vZOB/facility/main/aliases/ZOB-Alias.txt")]
	[InlineData("https://raw.githubusercontent.com/vZOB/facility/refs/heads/main/aliases/ZOB-Alias.txt")]
	[InlineData("https://github.com/vZOB/facility/raw/refs/heads/main/aliases/ZOB-Alias.txt")]
	[InlineData("https://github.com/vZOB/facility/blob/refs/heads/main/aliases/ZOB-Alias.txt")]
	public void a_file_address_becomes_the_contents_api(string url)
	{
		Assert.Equal(Api, GitHubFileUrl.ToContentsApi(new Uri(url))!.AbsoluteUri);
		Assert.False(GitHubFileUrl.IsPageButNotFile(new Uri(url)));
	}

	/// <summary>A tag reads like a branch, and a branch name goes into the query escaped, whatever is in it.</summary>
	[Theory]
	[InlineData("https://github.com/o/r/raw/refs/tags/v1.2/a.txt", "v1.2")]
	[InlineData("https://raw.githubusercontent.com/o/r/refs/tags/v1.2/a.txt", "v1.2")]
	[InlineData("https://github.com/o/r/blob/v1.2/a.txt", "v1.2")]
	[InlineData("https://github.com/o/r/blob/fix&test/a.txt", "fix%26test")]
	[InlineData("https://github.com/o/r/blob/a+b/a.txt", "a%2Bb")]
	[InlineData("https://github.com/o/r/blob/x%23y/a.txt", "x%23y")]
	[InlineData("https://github.com/o/r/blob/my%20branch/a.txt", "my%20branch")]
	public void the_branch_or_tag_goes_in_the_query_escaped(string url, string expectedRef) =>
		Assert.Equal($"https://api.github.com/repos/o/r/contents/a.txt?ref={expectedRef}", GitHubFileUrl.ToContentsApi(new Uri(url))!.AbsoluteUri);

	[Fact]
	public void an_escaped_path_stays_escaped()
	{
		Uri api = GitHubFileUrl.ToContentsApi(new Uri("https://github.com/o/r/blob/main/My%20Aliases/ZOB%20Alias.txt"))!;

		Assert.Equal("https://api.github.com/repos/o/r/contents/My%20Aliases/ZOB%20Alias.txt?ref=main", api.AbsoluteUri);
	}

	[Theory]
	[InlineData("https://github.com/Nikolai558/test-repo")]
	[InlineData("https://github.com/Nikolai558/test-repo/tree/main/aliases")]
	[InlineData("https://github.com/Nikolai558/test-repo/blob/main")]
	[InlineData("https://github.com/Nikolai558/test-repo/raw/refs/heads/main")]
	[InlineData("https://github.com/Nikolai558/test-repo/blob")]
	[InlineData("https://www.github.com/Nikolai558")]
	public void a_github_page_that_is_not_a_file_has_no_api_address(string url)
	{
		Assert.Null(GitHubFileUrl.ToContentsApi(new Uri(url)));
		Assert.True(GitHubFileUrl.IsPageButNotFile(new Uri(url)));
	}

	[Theory]
	[InlineData("https://example.com/vZOB/facility/blob/main/ZOB-Alias.txt")]
	[InlineData("https://raw.githubusercontent.com/vZOB/facility/main")]
	[InlineData("https://raw.githubusercontent.com/vZOB/facility/refs/heads/main")]
	[InlineData("https://api.github.com/repos/vZOB/facility/contents/ZOB-Alias.txt")]
	public void another_address_is_left_alone(string url)
	{
		Assert.Null(GitHubFileUrl.ToContentsApi(new Uri(url)));
		Assert.False(GitHubFileUrl.IsPageButNotFile(new Uri(url)));
	}

	[Fact]
	public void a_relative_address_is_neither()
	{
		Uri relative = new("vZOB/facility/blob/main/ZOB-Alias.txt", UriKind.Relative);

		Assert.Null(GitHubFileUrl.ToContentsApi(relative));
		Assert.False(GitHubFileUrl.IsPageButNotFile(relative));
	}

	/// <summary>Every form a file's address comes in is shown as its Raw link, the one GitHub's Raw button gives.</summary>
	[Theory]
	[InlineData("https://github.com/vZOB/facility/blob/main/aliases/ZOB-Alias.txt")]
	[InlineData("https://www.github.com/vZOB/facility/blob/main/aliases/ZOB-Alias.txt")]
	[InlineData("https://GitHub.com/vZOB/facility/BLOB/main/aliases/ZOB-Alias.txt")]
	[InlineData("https://github.com/vZOB/facility/raw/main/aliases/ZOB-Alias.txt")]
	[InlineData("https://github.com/vZOB/facility/blob/main/aliases/ZOB-Alias.txt?plain=1#L10")]
	[InlineData("https://raw.githubusercontent.com/vZOB/facility/main/aliases/ZOB-Alias.txt")]
	[InlineData("https://raw.githubusercontent.com/vZOB/facility/refs/heads/main/aliases/ZOB-Alias.txt")]
	[InlineData("https://github.com/vZOB/facility/raw/refs/heads/main/aliases/ZOB-Alias.txt")]
	[InlineData("https://github.com/vZOB/facility/blob/refs/heads/main/aliases/ZOB-Alias.txt")]
	public void a_file_address_becomes_its_raw_link(string url) =>
		Assert.Equal(Raw, GitHubFileUrl.ToRawLink(new Uri(url))!.AbsoluteUri);

	[Theory]
	[InlineData("https://github.com/o/r/blob/refs/tags/v1.2/a.txt", "https://github.com/o/r/raw/refs/tags/v1.2/a.txt")]
	[InlineData("https://raw.githubusercontent.com/o/r/REFS/TAGS/v1.2/a.txt", "https://github.com/o/r/raw/refs/tags/v1.2/a.txt")]
	[InlineData("https://github.com/o/r/blob/main/My%20Aliases/ZOB%20Alias.txt", "https://github.com/o/r/raw/refs/heads/main/My%20Aliases/ZOB%20Alias.txt")]
	public void a_tag_stays_a_tag_and_an_escaped_path_stays_escaped(string url, string expected) =>
		Assert.Equal(expected, GitHubFileUrl.ToRawLink(new Uri(url))!.AbsoluteUri);

	[Theory]
	[InlineData("https://github.com/Nikolai558/test-repo/tree/main/aliases")]
	[InlineData("https://github.com/Nikolai558/test-repo/blob/main")]
	[InlineData("https://example.com/vZOB/facility/blob/main/ZOB-Alias.txt")]
	public void an_address_that_is_not_a_file_on_github_has_no_raw_link(string url) =>
		Assert.Null(GitHubFileUrl.ToRawLink(new Uri(url)));

	[Fact]
	public void null_is_refused()
	{
		Assert.Throws<ArgumentNullException>(() => GitHubFileUrl.ToContentsApi(null!));
		Assert.Throws<ArgumentNullException>(() => GitHubFileUrl.IsPageButNotFile(null!));
		Assert.Throws<ArgumentNullException>(() => GitHubFileUrl.ToRawLink(null!));
	}
}
