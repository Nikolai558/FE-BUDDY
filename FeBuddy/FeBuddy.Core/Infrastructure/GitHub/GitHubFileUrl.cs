using FeBuddy.Core.Infrastructure.Credentials;

namespace FeBuddy.Core.Infrastructure.GitHub;

/// <summary>
/// Turns the address of a file on GitHub - as copied from the browser, or its "Raw" link - into the
/// GitHub API address that downloads it, so a private repository's file can be read with a token.
/// </summary>
/// <remarks>
/// <para>
/// <c>raw.githubusercontent.com</c> does not reliably honour a token, and a file's page on
/// <c>github.com</c> is a web page, not the file. The API's contents endpoint, asked for
/// <c>application/vnd.github.raw</c>, returns the file itself for public and private repositories
/// alike:
/// </para>
/// <code>
/// https://github.com/{owner}/{repo}/blob/{branch}/{path}
/// https://github.com/{owner}/{repo}/raw/{branch}/{path}
/// https://github.com/{owner}/{repo}/raw/refs/heads/{branch}/{path}      (GitHub's own Raw button)
/// https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{path}
/// https://raw.githubusercontent.com/{owner}/{repo}/refs/heads/{branch}/{path}
///   → https://api.github.com/repos/{owner}/{repo}/contents/{path}?ref={branch}
/// </code>
/// <para>
/// <c>blob</c> addresses can carry <c>refs/heads/</c> too, and a tag reads the same way as a branch,
/// through <c>refs/tags/{tag}</c> or as the plain name. The branch goes in the query escaped, so a name
/// with <c>&amp;</c>, <c>+</c> or <c>#</c> in it still names that branch.
/// </para>
/// <para>
/// A branch name with a <c>/</c> in it cannot be told apart from the path in these addresses; the
/// first part is taken as the branch, as GitHub's own links do for most repositories.
/// </para>
/// </remarks>
public static class GitHubFileUrl
{
	/// <summary>The media type that makes the contents endpoint return the file itself.</summary>
	public const string RawMediaType = "application/vnd.github.raw";

	private const string WebHost = "github.com";
	private const string WwwHost = "www.github.com";
	private const string RawHost = "raw.githubusercontent.com";

	/// <summary>
	/// The API address that downloads the file <paramref name="url"/> points at.
	/// </summary>
	/// <param name="url">A file's address on GitHub.</param>
	/// <returns>
	/// The API address, or <see langword="null"/> when <paramref name="url"/> is not a file on
	/// GitHub (another website, or a GitHub page that is not a file, such as a repository's front page).
	/// </returns>
	public static Uri? ToContentsApi(Uri url)
	{
		ArgumentNullException.ThrowIfNull(url);

		if (!url.IsAbsoluteUri)
		{
			return null;
		}

		string[] parts = url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		string host = url.Host.ToLowerInvariant();

		// {owner}/{repo}/blob|raw/{ref}/{path...}
		if ((host is WebHost or WwwHost)
			&& parts.Length >= 3
			&& (parts[2].Equals("blob", StringComparison.OrdinalIgnoreCase) || parts[2].Equals("raw", StringComparison.OrdinalIgnoreCase)))
		{
			return FromRef(parts[0], parts[1], parts[3..]);
		}

		// {owner}/{repo}/{ref}/{path...}
		if (host == RawHost && parts.Length >= 2)
		{
			return FromRef(parts[0], parts[1], parts[2..]);
		}

		return null;
	}

	/// <summary>
	/// Whether <paramref name="url"/> is a page on <c>github.com</c> that
	/// <see cref="ToContentsApi"/> cannot turn into a file - a repository's front page, a folder, and
	/// so on. Downloading one gives a web page, not an alias file.
	/// </summary>
	/// <param name="url">A web address.</param>
	/// <returns><see langword="true"/> for a GitHub page that is not a file.</returns>
	public static bool IsPageButNotFile(Uri url)
	{
		ArgumentNullException.ThrowIfNull(url);

		return url.IsAbsoluteUri
			&& url.Host.ToLowerInvariant() is WebHost or WwwHost
			&& ToContentsApi(url) is null;
	}

	/// <summary>
	/// The contents endpoint for the part of an address from the branch on: <c>{branch}/{path...}</c>,
	/// or <c>refs/heads/{branch}/{path...}</c> (<c>refs/tags/{tag}/…</c> for a tag). <see langword="null"/>
	/// when there is no path after the branch - a folder, not a file.
	/// </summary>
	private static Uri? FromRef(string owner, string repo, string[] rest)
	{
		bool isRefsForm = rest.Length >= 2
			&& rest[0].Equals("refs", StringComparison.OrdinalIgnoreCase)
			&& (rest[1].Equals("heads", StringComparison.OrdinalIgnoreCase) || rest[1].Equals("tags", StringComparison.OrdinalIgnoreCase));

		string[] fromBranch = isRefsForm ? rest[2..] : rest;

		return fromBranch.Length >= 2 ? Build(owner, repo, fromBranch[0], fromBranch[1..]) : null;
	}

	/// <summary>
	/// The contents endpoint for one file. The owner, repository and path stay escaped, as they came
	/// in the address; the branch is escaped again for the query, where <c>&amp;</c>, <c>+</c> and
	/// <c>#</c> mean something else.
	/// </summary>
	private static Uri Build(string owner, string repo, string branch, string[] path) =>
		new($"https://{CredentialHosts.GitHubApiHost}/repos/{owner}/{repo}/contents/{string.Join('/', path)}" +
			$"?ref={Uri.EscapeDataString(Uri.UnescapeDataString(branch))}");
}

