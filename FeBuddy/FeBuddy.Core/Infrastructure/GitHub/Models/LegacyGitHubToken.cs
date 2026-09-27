namespace FeBuddy.Core.Infrastructure.GitHub.Models;

/// <summary>A token found in the old <see cref="GitHubAuth.LegacyEnvironmentVariableName"/> environment variable.</summary>
/// <param name="Token">The token. Never shown or logged.</param>
/// <param name="IsMachineWide">Whether it is (also) set for every user of the PC, which only an administrator can remove.</param>
public sealed record LegacyGitHubToken(string Token, bool IsMachineWide)
{
	/// <summary>Keeps the token out of anything that prints the record.</summary>
	/// <returns>A description without the token.</returns>
	public override string ToString() => $"LegacyGitHubToken {{ IsMachineWide = {IsMachineWide} }}";
}
