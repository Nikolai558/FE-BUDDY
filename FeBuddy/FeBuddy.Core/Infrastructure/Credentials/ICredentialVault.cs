using FeBuddy.Core.Infrastructure.Credentials.Models;

namespace FeBuddy.Core.Infrastructure.Credentials;

/// <summary>
/// Where <see cref="CredentialStore"/> keeps its entries: Windows Credential Manager
/// (<see cref="WindowsCredentialVault"/>) in the app, an in-memory stand-in in tests.
/// </summary>
public interface ICredentialVault
{
	/// <summary>Creates or replaces an entry.</summary>
	/// <param name="target">The entry's name.</param>
	/// <param name="userName">The user-name field shown beside it in Credential Manager.</param>
	/// <param name="secret">The bytes to protect.</param>
	void Write(string target, string userName, byte[] secret);

	/// <summary>Reads one entry.</summary>
	/// <param name="target">The entry's name.</param>
	/// <returns>Its bytes, or <see langword="null"/> when there is no such entry.</returns>
	byte[]? Read(string target);

	/// <summary>Removes one entry.</summary>
	/// <param name="target">The entry's name.</param>
	/// <returns><see langword="true"/> if it existed.</returns>
	bool Delete(string target);

	/// <summary>Every entry whose name starts with <paramref name="prefix"/>.</summary>
	/// <param name="prefix">The start of the names to list.</param>
	/// <returns>The entries, in no particular order.</returns>
	IReadOnlyList<VaultEntry> Enumerate(string prefix);
}
