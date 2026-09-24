using FeBuddy.Core.Application.News;
using FeBuddy.Core.Application.Updates;

namespace FeBuddy.Core.Infrastructure.GitHub;

/// <summary>
/// Optional GitHub token support for <see cref="VersionCheck"/> and <see cref="NewsService"/>. NOT
/// required for normal use - FE-Buddy's releases and News both live in the public
/// <c>Nikolai558/FE-BUDDY</c> repo, and both fetches work completely unauthenticated by default. This exists for two edge cases: (1) raising
/// GitHub's 60-requests/hour unauthenticated rate limit for anyone who happens to hit it, and
/// (2) letting a developer point the check at a private repo (e.g. while testing) that requires
/// authentication to even see.
/// </summary>
/// <remarks>
/// Only ever consulted as a fallback, after an unauthenticated request has already failed - see
/// <see cref="VersionCheck"/>. That's deliberate: a stale or unrelated token sitting in someone's
/// environment for a different tool must never be able to break a request that would otherwise
/// have worked fine. Matches the same variable name and fallback-only behavior as the v2.x
/// code's <c>FeBuddyLibrary.Update.GitHubAuth</c> (on the <c>development</c> branch), so a token a
/// developer has already set up for v2 works here too.
/// </remarks>
public static class GitHubAuth
{
	/// <summary>
	/// The environment variable name. Deliberately FE-Buddy-specific rather than a generic name
	/// like <c>GITHUB_TOKEN</c> (which other tools commonly use), so this never picks up a token
	/// that has nothing to do with FE-Buddy.
	/// </summary>
	public const string EnvironmentVariableName = "FEBUDDY_GITHUB_TOKEN";

	/// <summary>Returns the token if set and non-empty, otherwise <see langword="null"/>.</summary>
	public static string? GetOptionalToken()
	{
		string? value = Environment.GetEnvironmentVariable(EnvironmentVariableName);
		return string.IsNullOrWhiteSpace(value) ? null : value;
	}
}
