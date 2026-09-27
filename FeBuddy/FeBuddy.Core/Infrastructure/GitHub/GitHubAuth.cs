using System.Net;

using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;
using FeBuddy.Core.Infrastructure.GitHub.Models;
using FeBuddy.Core.Infrastructure.Http;

namespace FeBuddy.Core.Infrastructure.GitHub;

/// <summary>
/// The optional GitHub token for FE-Buddy's own GitHub requests: the version check, the News fetch
/// and the update download. Not needed for normal use - releases and News live in the public
/// repository (<see cref="GitHubRepository"/>) and every request works unauthenticated. A token
/// helps in two edge cases: getting past GitHub's 60-requests-an-hour anonymous limit, and letting
/// a developer point FE-Buddy at a private repository while testing.
/// </summary>
/// <remarks>
/// <para>
/// The token is a saved <see cref="CredentialKind.GitHubToken"/> credential the user marked "use
/// for FE-Buddy's own GitHub requests" (see <see cref="CredentialStore"/>), so it lives in Windows
/// Credential Manager. It used to be read from the <see cref="LegacyEnvironmentVariableName"/>
/// environment variable, which Windows keeps as plain text and hands to every program; FE-Buddy
/// no longer reads it for requests, and <see cref="MoveLegacyToken()"/> moves one into a credential.
/// </para>
/// <para>
/// Only ever used as a fallback, after an unauthenticated request has already failed, so a stale
/// token can never break a request that would otherwise have worked.
/// </para>
/// </remarks>
public static class GitHubAuth
{
	/// <summary>
	/// The environment variable FE-Buddy used to read a GitHub token from, and 2.x still does.
	/// FE-Buddy 3 only looks at it to offer moving the token into a credential.
	/// </summary>
	public const string LegacyEnvironmentVariableName = "FEBUDDY_GITHUB_TOKEN";

	/// <summary>The name a moved token's credential gets.</summary>
	public const string LegacyCredentialName = "GitHub (moved from " + LegacyEnvironmentVariableName + ")";

	/// <summary>What the log calls the token, instead of ever printing it.</summary>
	public const string TokenDescription = "your GitHub credential";

	private const string RateLimitUrl = "https://api.github.com/rate_limit";

	private static CredentialStore? _storeForTesting;

	private static CredentialStore Store => _storeForTesting ?? CredentialStore.Default;

	/// <summary>The token of the credential the user chose for FE-Buddy's own GitHub requests.</summary>
	/// <returns>The token, or <see langword="null"/> when none is chosen.</returns>
	public static string? GetOptionalToken() => Store.GetFeBuddyGitHubToken();

	/// <summary>
	/// The token in <see cref="LegacyEnvironmentVariableName"/>, when one is set - for offering to
	/// move it into a credential.
	/// </summary>
	/// <returns>Whether a token is set, and where.</returns>
	public static LegacyGitHubToken? FindLegacyToken() => FindLegacyToken(Environment.GetEnvironmentVariable);

	/// <summary>
	/// Moves the token in <see cref="LegacyEnvironmentVariableName"/> into a GitHub credential used
	/// for FE-Buddy's own GitHub requests, then removes the variable from this user's environment
	/// and FE-Buddy's own. A machine-wide variable needs an administrator to remove; the result
	/// says when one is left.
	/// </summary>
	/// <remarks>Removing a user variable tells every open window it changed, which can take a few seconds: call it off the UI thread.</remarks>
	/// <returns>The new credential and whether a machine-wide variable is left; <see langword="null"/> when no token is set.</returns>
	public static LegacyTokenMove? MoveLegacyToken() =>
		MoveLegacyToken(Store, Environment.GetEnvironmentVariable, target => Environment.SetEnvironmentVariable(LegacyEnvironmentVariableName, null, target));

	/// <summary>Asks GitHub whether a saved GitHub token works, with a request that needs no permissions.</summary>
	/// <param name="id">The credential's id.</param>
	/// <param name="httpClient">The client to use; <see langword="null"/> creates one.</param>
	/// <param name="cancellationToken">Cancels the check.</param>
	/// <returns>Whether GitHub accepted it, and a message for the user.</returns>
	public static async Task<CredentialCheck> CheckTokenAsync(Guid id, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
	{
		using HttpClient? owned = httpClient is null ? FeBuddyHttp.CreateClient(TimeSpan.FromSeconds(10)) : null;
		HttpClient client = httpClient ?? owned!;

		using HttpRequestMessage request = new(HttpMethod.Get, RateLimitUrl);
		request.Headers.Accept.ParseAdd("application/vnd.github+json");

		switch (Store.Authorize(request, id))
		{
			case CredentialUseResult.NotFound:
				return new CredentialCheck(false, "This credential no longer exists.");
			case CredentialUseResult.HostNotAllowed:
				return new CredentialCheck(false, $"Its websites do not include github.com, so it cannot be sent to {CredentialHosts.GitHubApiHost}.");
		}

		try
		{
			using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

			return response.StatusCode switch
			{
				HttpStatusCode.OK => new CredentialCheck(true, "GitHub accepted this token."),
				HttpStatusCode.Unauthorized => new CredentialCheck(false, "GitHub rejected this token. It may be mistyped, expired or revoked."),
				_ => new CredentialCheck(false, $"GitHub answered {(int)response.StatusCode}; try again later."),
			};
		}
		catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
		{
			return new CredentialCheck(false, $"Could not reach GitHub: {ex.Message}");
		}
	}

	/// <summary><see cref="FindLegacyToken()"/> over a given environment. Unit tests only.</summary>
	internal static LegacyGitHubToken? FindLegacyToken(Func<string, EnvironmentVariableTarget, string?> read)
	{
		string? user = Blank(read(LegacyEnvironmentVariableName, EnvironmentVariableTarget.User));
		string? machine = Blank(read(LegacyEnvironmentVariableName, EnvironmentVariableTarget.Machine));
		string? process = Blank(read(LegacyEnvironmentVariableName, EnvironmentVariableTarget.Process));

		return (user ?? process ?? machine) is { } token
			? new LegacyGitHubToken(token, IsMachineWide: machine is not null)
			: null;
	}

	/// <summary><see cref="MoveLegacyToken()"/> over a given store and environment. Unit tests only.</summary>
	internal static LegacyTokenMove? MoveLegacyToken(
		CredentialStore store,
		Func<string, EnvironmentVariableTarget, string?> read,
		Action<EnvironmentVariableTarget> clear)
	{
		if (FindLegacyToken(read) is not { } legacy)
		{
			return null;
		}

		IReadOnlyList<CredentialInfo> existing = store.List();
		string name = LegacyCredentialName;
		for (int n = 2; existing.Any(c => string.Equals(c.Name, name, StringComparison.CurrentCultureIgnoreCase)); n++)
		{
			name = $"{LegacyCredentialName} {n}";
		}

		CredentialInfo credential = store.Save(new CredentialDraft(
			null, name, CredentialKind.GitHubToken, null, legacy.Token, CredentialHosts.GitHubDefaults, UseForFeBuddyGitHub: true));

		clear(EnvironmentVariableTarget.User);
		clear(EnvironmentVariableTarget.Process);

		return new LegacyTokenMove(credential, MachineVariableRemains: legacy.IsMachineWide);
	}

	/// <summary>Points <see cref="GetOptionalToken"/> at another store, or back at <see cref="CredentialStore.Default"/>. Unit tests only.</summary>
	internal static void ConfigureForTesting(CredentialStore? store) => _storeForTesting = store;

	private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
