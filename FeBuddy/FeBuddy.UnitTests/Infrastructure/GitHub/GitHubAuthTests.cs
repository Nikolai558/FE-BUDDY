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

	/// <summary>With no credential chosen, FE-Buddy's GitHub requests go without a token.</summary>
	[Fact]
	public void no_chosen_credential_adds_no_token()
	{
		_store.Save(GitHubToken("ghp_abc"));
		using HttpRequestMessage request = ApiRequest();

		Assert.Null(GitHubAuth.ChosenCredentialId);
		Assert.False(GitHubAuth.TryAuthorize(request));
		Assert.Null(request.Headers.Authorization);
	}

	/// <summary>The chosen GitHub token goes on the request as a Bearer header.</summary>
	[Fact]
	public void the_chosen_token_goes_on_the_request()
	{
		CredentialInfo chosen = _store.Save(GitHubToken("ghp_abc"));
		GitHubAuth.ConfigureForTesting(_store, chosen.Id);
		using HttpRequestMessage request = ApiRequest();

		Assert.Equal(chosen.Id, GitHubAuth.ChosenCredentialId);
		Assert.True(GitHubAuth.TryAuthorize(request));
		Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
		Assert.Equal("ghp_abc", request.Headers.Authorization.Parameter);
		Assert.Throws<ArgumentNullException>(() => GitHubAuth.TryAuthorize(null!));
	}

	/// <summary>
	/// A chosen credential that is gone, not a GitHub token, not for GitHub's API, or asked for over
	/// plain HTTP adds nothing.
	/// </summary>
	[Theory]
	[InlineData("gone")]
	[InlineData("token")]
	[InlineData("elsewhere")]
	[InlineData("http")]
	public void a_chosen_credential_that_does_not_qualify_adds_nothing(string which)
	{
		Guid id = which switch
		{
			"token" => _store.Save(new CredentialDraft(null, "Api key", CredentialKind.Token, null, "abc", CredentialHosts.GitHubDefaults)).Id,
			"elsewhere" => _store.Save(new CredentialDraft(null, "Elsewhere", CredentialKind.GitHubToken, null, "abc", ["example.com"])).Id,
			"http" => _store.Save(GitHubToken("ghp_abc")).Id,
			_ => Guid.NewGuid(),
		};
		GitHubAuth.ConfigureForTesting(_store, id);
		using HttpRequestMessage request = which == "http" ? new(HttpMethod.Get, "http://api.github.com/rate_limit") : ApiRequest();

		Assert.False(GitHubAuth.TryAuthorize(request));
		Assert.Null(request.Headers.Authorization);
	}

	/// <summary>A token Windows Credential Manager cannot read is left off the request - never thrown.</summary>
	[Fact]
	public void an_unreadable_token_is_left_off()
	{
		using IDisposable token = TestCredentials.UseUnreadableGitHubToken();
		using HttpRequestMessage request = ApiRequest();

		Assert.False(GitHubAuth.TryAuthorize(request));
		Assert.Null(request.Headers.Authorization);
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

		CredentialCheck check = await GitHubAuth.CheckTokenAsync(_store, token.Id, client);

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

		CredentialCheck check = await GitHubAuth.CheckTokenAsync(_store, token.Id, client);

		Assert.Equal(new CredentialCheck(false, "Could not reach GitHub: no route"), check);
	}

	/// <summary>The check uses the store it is given, not the one FE-Buddy's own requests read.</summary>
	[Fact]
	public async Task check_token_uses_the_store_it_is_given()
	{
		CredentialStore other = new(new InMemoryCredentialVault());
		CredentialInfo token = other.Save(GitHubToken("ghp_other"));
		HttpRequestMessage? sent = null;
		using HttpClient client = new(new StubHttpHandler(request =>
		{
			sent = request;
			return new HttpResponseMessage(HttpStatusCode.OK);
		}));

		Assert.True((await GitHubAuth.CheckTokenAsync(other, token.Id, client)).Succeeded);
		Assert.Equal("ghp_other", sent!.Headers.Authorization!.Parameter);
		await Assert.ThrowsAsync<ArgumentNullException>(() => GitHubAuth.CheckTokenAsync(null!, token.Id, client));
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

		Assert.Equal("This credential no longer exists.", (await GitHubAuth.CheckTokenAsync(_store, Guid.NewGuid(), client)).Message);
		Assert.Equal(
			"Its websites do not cover api.github.com, GitHub's API, so it cannot be checked. Add github.com to its websites.",
			(await GitHubAuth.CheckTokenAsync(_store, elsewhere.Id, client)).Message);
		Assert.Equal(0, calls);
	}

	private static CredentialDraft GitHubToken(string token) =>
		new(null, "GitHub " + Guid.NewGuid().ToString("N")[..6], CredentialKind.GitHubToken, null, token, CredentialHosts.GitHubDefaults);

	private static HttpRequestMessage ApiRequest() => new(HttpMethod.Get, "https://api.github.com/rate_limit");
}
