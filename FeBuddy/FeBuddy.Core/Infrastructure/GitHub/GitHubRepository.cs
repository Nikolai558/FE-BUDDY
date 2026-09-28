namespace FeBuddy.Core.Infrastructure.GitHub;

/// <summary>
/// Where FE-Buddy lives on GitHub: the public <c>Nikolai558/FE-BUDDY</c> repository its releases
/// and News come from.
/// </summary>
public static class GitHubRepository
{
	/// <summary>
	/// The branch FE-Buddy 3.x files (News) are read from. Named rather than left to the
	/// repository's default branch, so changing the default never moves them.
	/// </summary>
	public const string Branch = "v3-development";

	/// <summary>The repository's web page.</summary>
	public const string WebUrl = "https://github.com/Nikolai558/FE-BUDDY";

	/// <summary>The repository's REST API root.</summary>
	public const string ApiUrl = "https://api.github.com/repos/Nikolai558/FE-BUDDY";

	/// <summary>The root for raw file downloads on <see cref="Branch"/>.</summary>
	public const string RawUrl = "https://raw.githubusercontent.com/Nikolai558/FE-BUDDY/" + Branch;
}
