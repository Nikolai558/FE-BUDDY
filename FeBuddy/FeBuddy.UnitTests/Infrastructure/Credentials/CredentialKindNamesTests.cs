using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;

namespace FeBuddy.UnitTests.Infrastructure.Credentials;

/// <summary>Exercises <see cref="CredentialKindNames"/>: every kind has a name and a word for its secret.</summary>
public sealed class CredentialKindNamesTests
{
	/// <summary>Each kind reads as people would say it.</summary>
	[Theory]
	[InlineData(CredentialKind.UsernamePassword, "User name and password", "password")]
	[InlineData(CredentialKind.GitHubToken, "GitHub personal access token", "token")]
	[InlineData(CredentialKind.Token, "Token or API key", "token or key")]
	public void names_are_readable(CredentialKind kind, string displayName, string secretName)
	{
		Assert.Equal(displayName, kind.DisplayName());
		Assert.Equal(secretName, kind.SecretName());
	}
}
