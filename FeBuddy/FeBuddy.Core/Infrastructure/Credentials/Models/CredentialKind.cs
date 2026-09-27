namespace FeBuddy.Core.Infrastructure.Credentials.Models;

/// <summary>What a saved credential is, which decides how it is sent with a request.</summary>
public enum CredentialKind
{
	/// <summary>A user name and password, sent as HTTP Basic authentication.</summary>
	UsernamePassword = 0,

	/// <summary>A GitHub personal access token, sent as a bearer token. The only kind FE-Buddy's own GitHub requests can use.</summary>
	GitHubToken = 1,

	/// <summary>Any other token or API key, sent as a bearer token.</summary>
	Token = 2,
}
