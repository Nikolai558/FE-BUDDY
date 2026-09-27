using System.Net;

using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;
using FeBuddy.Core.Infrastructure.GitHub;
using FeBuddy.Core.Infrastructure.GitHub.Models;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Infrastructure.GitHub;

/// <summary>
/// Exercises <see cref="GitHubAuth"/>: FE-Buddy's own GitHub token comes from the chosen credential,
/// a token in the old environment variable moves into one, and a saved token can be checked.
/// </summary>
[Collection("AppLog")]
public sealed class GitHubAuthTests : IDisposable
{
	private const string Variable = GitHubAuth.LegacyEnvironmentVariableName;

	private readonly string _logs = Path.Combine(Path.GetTempPath(), "FeBuddyTests_GitHubAuth_" + Guid.NewGuid().ToString("N"));
	private readonly CredentialStore _store = new(new InMemoryCredentialVault());

	/// <summary>Points <see cref="AppLog"/> and <see cref="GitHubAuth"/> at throwaway state.</summary>
	public GitHubAuthTests()
	{
		AppLog.ConfigureForTesting(_logs);
		GitHubAuth.ConfigureForTesting(_store);
	}

	/// <summary>Restores the empty test store and the log.</summary>
	public void Dispose()
	{
		TestCredentials.Reset();
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

	/// <summary>The token is the credential marked for FE-Buddy's GitHub requests, and none until one is.</summary>
	[Fact]
	public void get_optional_token_is_the_marked_credential()
	{
		Assert.Null(GitHubAuth.GetOptionalToken());

		_store.Save(GitHubToken("ghp_abc", forGitHub: true));

		Assert.Equal("ghp_abc", GitHubAuth.GetOptionalToken());
	}

	// ============================ the old environment variable ============================

	/// <summary>A token in the user's, FE-Buddy's or the machine's environment is found, and where.</summary>
	[Theory]
	[InlineData(" user-token ", null, null, "user-token", false)]
	[InlineData(null, "process-token", null, "process-token", false)]
	[InlineData(null, null, "machine-token", "machine-token", true)]
	[InlineData("user-token", null, "machine-token", "user-token", true)]
	public void find_legacy_token_reads_every_environment(string? user, string? process, string? machine, string expected, bool machineWide)
	{
		LegacyGitHubToken? found = GitHubAuth.FindLegacyToken(Reader(user, process, machine));

		Assert.Equal(new LegacyGitHubToken(expected, machineWide), found);
	}

	/// <summary>No variable, or a blank one, is no token.</summary>
	[Fact]
	public void find_legacy_token_blank_is_none() =>
		Assert.Null(GitHubAuth.FindLegacyToken(Reader(" ", null, "")));

	/// <summary>The found token never shows up when the record is printed.</summary>
	[Fact]
	public void legacy_token_never_prints_the_token() =>
		Assert.DoesNotContain("secret", new LegacyGitHubToken("secret", false).ToString(), StringComparison.Ordinal);

	/// <summary>Reading this PC's real environment works, whatever it holds.</summary>
	[Fact]
	public void find_legacy_token_reads_the_real_environment()
	{
		LegacyGitHubToken? found = GitHubAuth.FindLegacyToken();

		Assert.True(found is null || found.Token.Length > 0);
	}

	/// <summary>
	/// Moving the token makes it a GitHub credential used for FE-Buddy's requests, then removes the
	/// variable from the user's and FE-Buddy's environments.
	/// </summary>
	[Fact]
	public void move_legacy_token_makes_a_credential_and_clears_the_variable()
	{
		List<EnvironmentVariableTarget> cleared = [];

		LegacyTokenMove? move = GitHubAuth.MoveLegacyToken(_store, Reader("ghp_old", null, null), cleared.Add);

		Assert.NotNull(move);
		Assert.False(move.MachineVariableRemains);
		Assert.Equal(GitHubAuth.LegacyCredentialName, move.Credential.Name);
		Assert.Equal(CredentialKind.GitHubToken, move.Credential.Kind);
		Assert.True(move.Credential.UseForFeBuddyGitHub);
		Assert.Equal(CredentialHosts.GitHubDefaults, move.Credential.Hosts);
		Assert.Equal("ghp_old", GitHubAuth.GetOptionalToken());
		Assert.Equal<EnvironmentVariableTarget>([EnvironmentVariableTarget.User, EnvironmentVariableTarget.Process], cleared);
	}

	/// <summary>A machine-wide copy is reported as left behind; a name already taken gets a number.</summary>
	[Fact]
	public void move_legacy_token_reports_a_machine_variable_and_avoids_a_taken_name()
	{
		_store.Save(new CredentialDraft(null, GitHubAuth.LegacyCredentialName, CredentialKind.Token, null, "x", ["example.com"]));

		LegacyTokenMove? move = GitHubAuth.MoveLegacyToken(_store, Reader(null, null, "ghp_machine"), _ => { });

		Assert.True(move!.MachineVariableRemains);
		Assert.Equal(GitHubAuth.LegacyCredentialName + " 2", move.Credential.Name);
	}

	/// <summary>With no variable set there is nothing to move, and nothing is saved or cleared.</summary>
	[Fact]
	public void move_legacy_token_without_a_variable_does_nothing()
	{
		bool cleared = false;

		Assert.Null(GitHubAuth.MoveLegacyToken(_store, Reader(null, null, null), _ => cleared = true));
		Assert.False(cleared);
		Assert.Empty(_store.List());
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

	private static CredentialDraft GitHubToken(string token, bool forGitHub = false) =>
		new(null, "GitHub " + Guid.NewGuid().ToString("N")[..6], CredentialKind.GitHubToken, null, token, CredentialHosts.GitHubDefaults, forGitHub);

	private static Func<string, EnvironmentVariableTarget, string?> Reader(string? user, string? process, string? machine) =>
		(name, target) =>
		{
			Assert.Equal(Variable, name);
			return target switch
			{
				EnvironmentVariableTarget.User => user,
				EnvironmentVariableTarget.Machine => machine,
				_ => process,
			};
		};
}
