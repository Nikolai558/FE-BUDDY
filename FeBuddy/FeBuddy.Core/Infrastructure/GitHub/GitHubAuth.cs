using System.ComponentModel;
using System.Net;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;
using FeBuddy.Core.Infrastructure.GitHub.Models;
using FeBuddy.Core.Infrastructure.Http;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Core.Infrastructure.GitHub;

/// <summary>
/// The optional GitHub token for FE-Buddy's own GitHub requests: the version check, the News fetch
/// and the update download. Not needed for normal use - releases and News live in the public
/// repository (<see cref="GitHubRepository"/>) and every request works without one. A token only
/// gets past GitHub's limit of 60 requests an hour for requests without one.
/// </summary>
/// <remarks>
/// <para>
/// Whether to use one is an advanced setting (Settings ▸ FE-Buddy's GitHub Requests): the id of a
/// saved <see cref="CredentialKind.GitHubToken"/> credential, kept in
/// <see cref="UserConfigKeys.FeBuddyGitHubCredentialId"/>. The token itself stays in Windows
/// Credential Manager (see <see cref="CredentialStore"/>), and only
/// <see cref="TryAuthorize(HttpRequestMessage)"/> ever puts it on a request.
/// </para>
/// <para>
/// When one is chosen, the requests are sent with it. If a request with it fails in any way, it is
/// tried once more without it, and a token Windows Credential Manager cannot read is treated as no
/// token - so a stale token, or a broken Credential Manager, can never stop FE-Buddy from finding
/// updates.
/// </para>
/// </remarks>
public static class GitHubAuth
{
	/// <summary>What the log calls the token, instead of ever printing it.</summary>
	public const string TokenDescription = "your GitHub credential";

	private const string LogSource = "GitHub";

	private const string RateLimitUrl = "https://api.github.com/rate_limit";

	private static CredentialStore? _storeForTesting;
	private static Func<Guid?>? _chosenIdForTesting;

	private static CredentialStore Store => _storeForTesting ?? CredentialStore.Default;

	/// <summary>
	/// The id of the GitHub token credential chosen for FE-Buddy's own GitHub requests, or
	/// <see langword="null"/> when they are sent without one.
	/// </summary>
	public static Guid? ChosenCredentialId =>
		_chosenIdForTesting is { } chosen ? chosen()
		: Guid.TryParse(UserConfigFile.GetValue(UserConfigKeys.FeBuddyGitHubCredentialId), out Guid id) ? id
		: null;

	/// <summary>
	/// Adds the chosen GitHub token to one of FE-Buddy's own GitHub requests - when one is chosen,
	/// it is a <see cref="CredentialKind.GitHubToken"/>, and it may be sent to the request's website
	/// over HTTPS (<see cref="CredentialStore.Authorize(HttpRequestMessage, Guid)"/>'s rules).
	/// </summary>
	/// <remarks>
	/// Never throws for Credential Manager: a token it cannot read is logged and left off, and the
	/// request goes without one.
	/// </remarks>
	/// <param name="request">The request to send.</param>
	/// <returns>Whether the request now carries the token.</returns>
	internal static bool TryAuthorize(HttpRequestMessage request)
	{
		ArgumentNullException.ThrowIfNull(request);

		if (ChosenCredentialId is not { } id)
		{
			return false;
		}

		try
		{
			return Store.AuthorizeGitHubToken(request, id);
		}
		catch (Win32Exception ex)
		{
			AppLog.Warning(LogSource, $"Windows Credential Manager could not read {TokenDescription} ({ex.Message}); sending the request without it.");
			return false;
		}
	}

	/// <summary>Asks GitHub whether a saved GitHub token works, with a request that needs no permissions.</summary>
	/// <param name="store">The credentials the token is saved in.</param>
	/// <param name="id">The credential's id.</param>
	/// <param name="httpClient">The client to use; <see langword="null"/> creates one.</param>
	/// <param name="cancellationToken">Cancels the check.</param>
	/// <returns>Whether GitHub accepted it, and a message for the user.</returns>
	public static async Task<CredentialCheck> CheckTokenAsync(
		CredentialStore store,
		Guid id,
		HttpClient? httpClient = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(store);

		using HttpClient? owned = httpClient is null ? FeBuddyHttp.CreateClient(TimeSpan.FromSeconds(10)) : null;
		HttpClient client = httpClient ?? owned!;

		using HttpRequestMessage request = new(HttpMethod.Get, RateLimitUrl);
		request.Headers.Accept.ParseAdd("application/vnd.github+json");

		CredentialUseResult use = store.Authorize(request, id);

		if (use == CredentialUseResult.NotFound)
		{
			return new CredentialCheck(false, "This credential no longer exists.");
		}

		if (use != CredentialUseResult.Applied)
		{
			return new CredentialCheck(false,
				$"Its websites do not cover {CredentialHosts.GitHubApiHost}, GitHub's API, so it cannot be checked. Add github.com to its websites.");
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

	/// <summary>
	/// Points <see cref="TryAuthorize(HttpRequestMessage)"/> at another store and chosen credential,
	/// or back at <see cref="CredentialStore.Default"/> and the saved setting. Unit tests only.
	/// </summary>
	/// <param name="store">The store, or <see langword="null"/> for the real one.</param>
	/// <param name="chosenId">The chosen credential; ignored when <paramref name="store"/> is <see langword="null"/>.</param>
	internal static void ConfigureForTesting(CredentialStore? store, Guid? chosenId = null)
	{
		_storeForTesting = store;
		_chosenIdForTesting = store is null ? null : () => chosenId;
	}
}
