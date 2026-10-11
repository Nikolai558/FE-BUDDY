namespace FeBuddy.Core.Infrastructure.GitHub.Models;

/// <summary>The outcome of <see cref="GitHubAuth.CheckTokenAsync(Credentials.CredentialStore, Guid, HttpClient?, CancellationToken)"/>.</summary>
/// <param name="Succeeded">Whether GitHub accepted the token.</param>
/// <param name="Message">What happened, for the user.</param>
/// <param name="ExpiresAt">When the token expires, or <see langword="null"/> when GitHub didn't say (or didn't accept it).</param>
/// <param name="ExpiresSoon">Whether it expires within <see cref="GitHubTokenExpiry.WarningDays"/> days.</param>
/// <param name="IsRejected">Whether GitHub rejected it: it was mistyped, has expired or was revoked.</param>
public sealed record CredentialCheck(bool Succeeded, string Message, DateTimeOffset? ExpiresAt = null, bool ExpiresSoon = false, bool IsRejected = false);
