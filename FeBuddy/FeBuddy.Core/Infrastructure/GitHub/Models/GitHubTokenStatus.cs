namespace FeBuddy.Core.Infrastructure.GitHub.Models;

/// <summary>
/// A saved GitHub token that needs attention: GitHub rejected it, or it expires within
/// <see cref="GitHubTokenExpiry.WarningDays"/> days (<see cref="GitHubTokenExpiry.FindTokensNeedingAttentionAsync"/>).
/// </summary>
/// <param name="Name">The credential's name.</param>
/// <param name="ExpiresAt">When it expires, or <see langword="null"/> when GitHub didn't say.</param>
/// <param name="IsRejected">Whether GitHub rejected it: it was mistyped, has expired or was revoked.</param>
public sealed record GitHubTokenStatus(string Name, DateTimeOffset? ExpiresAt, bool IsRejected);
