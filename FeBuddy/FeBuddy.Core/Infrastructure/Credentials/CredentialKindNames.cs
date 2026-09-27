using FeBuddy.Core.Infrastructure.Credentials.Models;

namespace FeBuddy.Core.Infrastructure.Credentials;

/// <summary>How each <see cref="CredentialKind"/> is named to people, wherever credentials are listed or picked.</summary>
public static class CredentialKindNames
{
	/// <summary>The kind's name, e.g. <c>GitHub personal access token</c>.</summary>
	/// <param name="kind">The kind.</param>
	/// <returns>The name to show.</returns>
	public static string DisplayName(this CredentialKind kind) => kind switch
	{
		CredentialKind.UsernamePassword => "User name and password",
		CredentialKind.GitHubToken => "GitHub personal access token",
		_ => "Token or API key",
	};

	/// <summary>What the secret is called for this kind, e.g. <c>password</c>.</summary>
	/// <param name="kind">The kind.</param>
	/// <returns>The lower-case word for the secret.</returns>
	public static string SecretName(this CredentialKind kind) => kind switch
	{
		CredentialKind.UsernamePassword => "password",
		CredentialKind.GitHubToken => "token",
		_ => "token or key",
	};
}
