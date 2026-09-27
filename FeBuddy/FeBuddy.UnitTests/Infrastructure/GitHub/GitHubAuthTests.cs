using System.Net;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;
using FeBuddy.Core.Infrastructure.GitHub;
using FeBuddy.Core.Infrastructure.GitHub.Models;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Infrastructure.GitHub;

/// <summary>
/// Exercises <see cref="GitHubAuth"/>: FE-Buddy's own GitHub token is the credential chosen in
/// Settings, only when it is a GitHub token for GitHub's API, and a saved token can be checked.
/// </summary>
[Collection("AppLog")]
public sealed class GitHubAuthTests : IDisposable
{
	private readonly string _logs = Path.Combine(Path.GetTempPath(), "FeBuddyTests_GitHubAuth_" + Guid.NewGuid().ToString("N"));
	private readonly CredentialStore _store = new(new InMemoryCredentialVault());

	/// <summary>Points <see cref="AppLog"/> and <see cref="GitHubAuth"/> at throwaway state.</summary>
	public GitHubAuthTests()
	{
		AppLog.ConfigureForTesting(_logs);
		GitHubAuth.ConfigureForTesting(_store);
	}

	/// <summary>Restores the empty test store, the config and the log.</summary>
	public void Dispose()
	{
		TestCredentials.Reset();
		UserConfigFile.ConfigureForTesting(null);
		AppLog.ConfigureForTesting(null);

		try
		{
			Directory.Delete(_logs, recursive: true);
		}
		catch (IOException)
		{
			// Best-effort cleanup.
		}
	}

	// ============================ the token ============================

	/// <summary>With no credential chosen, FE-Buddy's GitHub requests have no token.</summary>
	[Fact]
	public void no_chosen_credential_is_no_token()
	{
		_store.Save(GitHubToken("ghp_abc"));

		Assert.Null(GitHubAuth.ChosenCredentialId);
		Assert.Null(GitHubAuth.GetOptionalToken());
	}

	/// <summary>The token is the chosen GitHub token's.</summary>
	[Fact]
	public void the_token_is_the_chosen_credentials()
	{
		CredentialInfo chosen = _store.Save(GitHubToken("ghp_abc"));
		GitHubAuth.ConfigureForTesting(_store, chosen.Id);

		Assert.Equal(chosen.Id, GitHubAuth.ChosenCredentialId);
		Assert.Equal("ghp_abc", GitHubAuth.GetOptionalToken());
	}

	/// <summary>A chosen credential that is gone, not a GitHub token, or not for GitHub's API gives no token.</summary>
	[Theory]
	[InlineData("gone")]
	[InlineData("token")]
	[InlineData("elsewhere")]
	public void a_chosen_credential_that_does_not_qualify_gives_no_token(string which)
	{
		Guid id = which switch
		{
			"token" => _store.Save(new CredentialDraft(null, "Api key", CredentialKind.Token, null, "abc", CredentialHosts.GitHubDefaults)).Id,
			"elsewhere" => _store.Save(new CredentialDraft(null, "Elsewhere", CredentialKind.GitHubToken, null, "abc", ["example.com"])).Id,
			_ => Guid.NewGuid(),
		};
		GitHubAuth.ConfigureForTesting(_store, id);

		Assert.Null(GitHubAuth.GetOptionalToken());
	}

	/// <summary>Outside tests, the chosen credential is the id saved in Settings; anything else is none.</summary>
	[Theory]
	[InlineData("0f8fad5bd9cb469fa16570867728950e", true)]
	[InlineData("", false)]
	[InlineData("not an id", false)]
	public void the_chosen_credential_is_read_from_settings(string saved, bool expected)
	{
		UserConfigFile.ConfigureForTesting(Path.Combine(_logs, "config"));
		UserConfigFile.TrySetValue(UserConfigKeys.FeBuddyGitHubCredentialId, saved);
		GitHubAuth.ConfigureForTesting(null);

		Assert.Equal(expected ? Guid.Parse(saved) : null, GitHubAuth.ChosenCredentialId);
	}

	// ============================ checking a token ============================

	/// <summary>The check sends the token to GitHub's rate-limit endpoint and reads GitHub's answer.</summary>
	[Theory]
	[InlineData(HttpStatusCode.OK, true, "GitHub accepted this token.")]
	[InlineData(HttpStatusCode.Unauthorized, false, "GitHub rejected this token. It may be mistyped, expired or revoked.")]
	[InlineData(HttpStatusCode.ServiceUnavailable, false, "GitHub answered 503; try again later.")]
	public async Task check_token_reads_githubs_answer(HttpStatusCode status, bool succeeded, string message)
	{
		CredentialInfo token = _store.Save(GitHubToken("ghp_abc"));
		HttpRequestMessage? sent = null;
		using HttpClient client = new(new StubHttpHandler(request =>
		{
			sent = request;
			return new HttpResponseMessage(status);
		}));

		CredentialCheck check = await GitHubAuth.CheckTokenAsync(token.Id, client);

		Assert.Equal(new CredentialCheck(succeeded, message), check);
		Assert.Equal("https://api.github.com/rate_limit", sent!.RequestUri!.ToString());
		Assert.Equal("ghp_abc", sent.Headers.Authorization!.Parameter);
	}

	/// <summary>No network is reported as that, not as a rejected token.</summary>
	[Fact]
	public async Task check_token_offline_says_so()
	{
		CredentialInfo token = _store.Save(GitHubToken("ghp_abc"));
		using HttpClient client = new(new StubHttpHandler(_ => throw new HttpRequestException("no route")));

		CredentialCheck check = await GitHubAuth.CheckTokenAsync(token.Id, client);

		Assert.Equal(new CredentialCheck(false, "Could not reach GitHub: no route"), check);
	}

	/// <summary>A removed credential, or one not allowed on GitHub's API, is never sent at all.</summary>
	[Fact]
	public async Task check_token_that_cannot_be_sent_is_not_sent()
	{
		CredentialInfo elsewhere = _store.Save(new CredentialDraft(null, "Elsewhere", CredentialKind.GitHubToken, null, "abc", ["example.com"]));
		int calls = 0;
		using HttpClient client = new(new StubHttpHandler(_ =>
		{
			calls++;
			return new HttpResponseMessage(HttpStatusCode.OK);
		}));

		Assert.Equal("This credential no longer exists.", (await GitHubAuth.CheckTokenAsync(Guid.NewGuid(), client)).Message);
		Assert.StartsWith("Its websites do not include github.com", (await GitHubAuth.CheckTokenAsync(elsewhere.Id, client)).Message, StringComparison.Ordinal);
		Assert.Equal(0, calls);
	}

	private static CredentialDraft GitHubToken(string token) =>
		new(null, "GitHub " + Guid.NewGuid().ToString("N")[..6], CredentialKind.GitHubToken, null, token, CredentialHosts.GitHubDefaults);
}
