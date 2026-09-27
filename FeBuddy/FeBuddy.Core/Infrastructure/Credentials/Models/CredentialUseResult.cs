namespace FeBuddy.Core.Infrastructure.Credentials.Models;

/// <summary>What <see cref="CredentialStore.Authorize(HttpRequestMessage, Guid)"/> did with a request.</summary>
public enum CredentialUseResult
{
	/// <summary>The credential was added to the request.</summary>
	Applied = 0,

	/// <summary>There is no credential with that id on this PC - removed, or chosen on another PC.</summary>
	NotFound = 1,

	/// <summary>The request is not HTTPS, so the credential was not added: it would travel unencrypted.</summary>
	NotHttps = 2,

	/// <summary>The request's website is not one the credential may be sent to, so it was not added.</summary>
	HostNotAllowed = 3,
}
