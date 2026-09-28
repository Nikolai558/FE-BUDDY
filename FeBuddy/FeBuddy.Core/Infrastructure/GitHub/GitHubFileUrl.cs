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
/// https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{path}
/// https://raw.githubusercontent.com/{owner}/{repo}/refs/heads/{branch}/{path}
///   → https://api.github.com/repos/{owner}/{repo}/contents/{path}?ref={branch}
/// </code>
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

		// {owner}/{repo}/blob|raw/{branch}/{path...}
		if ((host is WebHost or WwwHost)
			&& parts.Length >= 5
			&& (parts[2].Equals("blob", StringComparison.OrdinalIgnoreCase) || parts[2].Equals("raw", StringComparison.OrdinalIgnoreCase)))
		{
			return Build(parts[0], parts[1], parts[3], parts[4..]);
		}

		if (host == RawHost)
		{
			bool isRefsForm = parts.Length >= 4
				&& parts[2].Equals("refs", StringComparison.OrdinalIgnoreCase)
				&& parts[3].Equals("heads", StringComparison.OrdinalIgnoreCase);

			// {owner}/{repo}/refs/heads/{branch}/{path...}
			if (isRefsForm)
			{
				return parts.Length >= 6 ? Build(parts[0], parts[1], parts[4], parts[5..]) : null;
			}

			// {owner}/{repo}/{branch}/{path...}
			if (parts.Length >= 4)
			{
				return Build(parts[0], parts[1], parts[2], parts[3..]);
			}
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

	/// <summary>The contents endpoint for one file. The parts are still escaped, as they came in the address.</summary>
	private static Uri Build(string owner, string repo, string branch, string[] path) =>
		new($"https://{CredentialHosts.GitHubApiHost}/repos/{owner}/{repo}/contents/{string.Join('/', path)}?ref={branch}");
}

