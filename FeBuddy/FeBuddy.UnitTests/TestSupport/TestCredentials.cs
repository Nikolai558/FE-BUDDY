using System.ComponentModel;
using System.Runtime.CompilerServices;

using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;
using FeBuddy.Core.Infrastructure.GitHub;

namespace FeBuddy.UnitTests.TestSupport;

/// <summary>
/// Keeps every test away from the real Windows Credential Manager: from the moment the test
/// assembly loads, <see cref="GitHubAuth"/> reads an empty in-memory store, so a GitHub token the
/// developer saved can never change what a test sees.
/// </summary>
internal static class TestCredentials
{
#pragma warning disable CA2255 // The initializer is the point: it must run before any test touches GitHubAuth.
	[ModuleInitializer]
	internal static void UseEmptyStore() => Reset();
#pragma warning restore CA2255

	/// <summary>Points <see cref="GitHubAuth"/> at a new, empty in-memory store.</summary>
	public static void Reset() => GitHubAuth.ConfigureForTesting(new CredentialStore(new InMemoryCredentialVault()));

	/// <summary>
	/// Points <see cref="GitHubAuth"/> at a store holding one GitHub token, chosen for FE-Buddy's own
	/// requests, until the returned scope is disposed. Use from tests in the <c>AppLog</c> collection,
	/// which run one at a time.
	/// </summary>
	/// <param name="token">The token.</param>
	/// <returns>Restores an empty store when disposed.</returns>
	public static IDisposable UseGitHubToken(string token)
	{
		CredentialStore store = new(new InMemoryCredentialVault());
		CredentialInfo saved = store.Save(new CredentialDraft(null, "Test GitHub", CredentialKind.GitHubToken, null, token, CredentialHosts.GitHubDefaults));
		GitHubAuth.ConfigureForTesting(store, saved.Id);
		return new Scope();
	}

	/// <summary>
	/// Points <see cref="GitHubAuth"/> at a chosen GitHub token that Windows Credential Manager
	/// cannot read - as when it fails with "no logon session" - until the returned scope is
	/// disposed. Use from tests in the <c>AppLog</c> collection, which run one at a time.
	/// </summary>
	/// <returns>Restores an empty store when disposed.</returns>
	public static IDisposable UseUnreadableGitHubToken()
	{
		InMemoryCredentialVault vault = new();
		CredentialStore store = new(vault);
		CredentialInfo saved = store.Save(new CredentialDraft(null, "Test GitHub", CredentialKind.GitHubToken, null, "unreadable-token", CredentialHosts.GitHubDefaults));
		vault.Failure = new Win32Exception(1312);
		GitHubAuth.ConfigureForTesting(store, saved.Id);
		return new Scope();
	}

	private sealed class Scope : IDisposable
	{
		public void Dispose() => Reset();
	}
}
