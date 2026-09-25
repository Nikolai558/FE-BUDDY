namespace FeBuddy.Core.Infrastructure.GitHub;

/// <summary>
/// Optional GitHub token for the version check, the News fetch and the update download. Not
/// needed for normal use: releases and News live in the public repository
/// (<see cref="GitHubRepository"/>) and every request works unauthenticated. The token exists
/// for two edge cases: getting past GitHub's 60-requests-an-hour anonymous limit, and letting a
/// developer point FE-Buddy at a private repository while testing.
/// </summary>
/// <remarks>
/// Only ever used as a fallback, after an unauthenticated request has already failed. That is
/// deliberate: a stale or unrelated token sitting in someone's
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

	/// <summary>Reads the token from <see cref="EnvironmentVariableName"/>.</summary>
	/// <returns>The token, or <see langword="null"/> when the variable is unset or blank.</returns>
	public static string? GetOptionalToken()
	{
		string? value = Environment.GetEnvironmentVariable(EnvironmentVariableName);
		return string.IsNullOrWhiteSpace(value) ? null : value;
	}
}
