using System;

namespace FeBuddyLibrary.Update
{
    /// <summary>
    /// Optional GitHub token support. NOT required for normal use - FE-BUDDY's releases
    /// live in a public repo, and the vast majority of users will never set this. It
    /// exists for two reasons: (1) raising GitHub's 60-requests/hour unauthenticated rate
    /// limit for anyone who happens to hit it, and (2) letting a technically-inclined user
    /// point their own token at a private FE-BUDDY-related repo they have access to (e.g.
    /// a test/dev repo, or a future private release channel).
    ///
    /// Only ever consulted as a fallback, after an unauthenticated request has already
    /// failed (see UpdateChecker/UpdateInstaller) - never sent up front. That's deliberate:
    /// a stale or unrelated token sitting in someone's environment for a different tool
    /// must never be able to break a request that would otherwise have worked fine.
    /// </summary>
    public static class GitHubAuth
    {
        public const string EnvironmentVariableName = "FEBUDDY_GITHUB_TOKEN";

        /// <summary>Returns the token if set and non-empty, otherwise null.</summary>
        public static string GetOptionalToken()
        {
            var value = Environment.GetEnvironmentVariable(EnvironmentVariableName);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
