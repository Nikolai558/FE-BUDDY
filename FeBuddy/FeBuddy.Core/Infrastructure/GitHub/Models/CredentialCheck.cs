namespace FeBuddy.Core.Infrastructure.GitHub.Models;

/// <summary>The outcome of <see cref="GitHubAuth.CheckTokenAsync(Credentials.CredentialStore, Guid, HttpClient?, CancellationToken)"/>.</summary>
/// <param name="Succeeded">Whether GitHub accepted the token.</param>
/// <param name="Message">What happened, for the user.</param>
public sealed record CredentialCheck(bool Succeeded, string Message);
