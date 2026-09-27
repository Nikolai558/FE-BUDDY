namespace FeBuddy.Core.Infrastructure.Credentials.Models;

/// <summary>
/// A saved credential as the rest of FE-Buddy sees it: everything except the secret, which never
/// leaves <see cref="CredentialStore"/> other than as a request header.
/// </summary>
/// <param name="Id">The credential's id; what a setting that uses it saves.</param>
/// <param name="Name">The name the user gave it, e.g. <c>ZOB GitHub</c>.</param>
/// <param name="Kind">What it is.</param>
/// <param name="UserName">The user name, for <see cref="CredentialKind.UsernamePassword"/>; otherwise <see langword="null"/>.</param>
/// <param name="Hosts">The websites it may be sent to (each host and its subdomains), e.g. <c>github.com</c>.</param>
public sealed record CredentialInfo(
	Guid Id,
	string Name,
	CredentialKind Kind,
	string? UserName,
	IReadOnlyList<string> Hosts);
