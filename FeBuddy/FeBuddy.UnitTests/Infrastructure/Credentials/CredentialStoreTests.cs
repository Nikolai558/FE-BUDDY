using System.Net.Http.Headers;
using System.Text;

using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Infrastructure.Credentials;

/// <summary>
/// Exercises <see cref="CredentialStore"/> over an in-memory vault: saving and editing, the rules a
/// credential must meet, and that a secret only ever leaves as a header on an HTTPS request to one
/// of the credential's own websites.
/// </summary>
[Collection("AppLog")]
public sealed class CredentialStoreTests : IDisposable
{
	private static readonly string[] Example = ["example.com"];

	private readonly string _logs = Path.Combine(Path.GetTempPath(), "FeBuddyTests_Credentials_" + Guid.NewGuid().ToString("N"));
	private readonly InMemoryCredentialVault _vault = new();
	private readonly CredentialStore _store;

	/// <summary>Points <see cref="AppLog"/> at a throwaway folder.</summary>
	public CredentialStoreTests()
	{
		AppLog.ConfigureForTesting(_logs);
		_store = new CredentialStore(_vault);
	}

	/// <summary>Restores the log and deletes the throwaway folder.</summary>
	public void Dispose()
	{
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

	// ============================ save ============================

	/// <summary>A saved credential lists and finds without its secret, stored as one entry named by its id.</summary>
	[Fact]
	public void save_then_list_and_find()
	{
		CredentialInfo saved = _store.Save(Token("ZOB GitHub", "ghp_secret", CredentialKind.GitHubToken, CredentialHosts.GitHubDefaults));

		Assert.Equivalent(saved, Assert.Single(_store.List()));
		Assert.Equivalent(saved, _store.Find(saved.Id));
		Assert.Null(_store.Find(Guid.NewGuid()));

		string target = CredentialStore.TargetPrefix + saved.Id.ToString("N");
		Assert.Equal("ZOB GitHub", _vault.UserNames[target]);
		Assert.Contains("ghp_secret", Encoding.UTF8.GetString(_vault.Raw[target]), StringComparison.Ordinal);
	}

	/// <summary>Names, user names and tokens are trimmed; a password is kept exactly as typed.</summary>
	[Fact]
	public void save_trims_everything_but_a_password()
	{
		CredentialInfo login = _store.Save(new CredentialDraft(null, "  Site  ", CredentialKind.UsernamePassword, "  bob ", " p@ss ", Example));
		CredentialInfo token = _store.Save(Token("Api", "  abc  ", CredentialKind.Token, Example));

		Assert.Equal("Site", login.Name);
		Assert.Equal("bob", login.UserName);
		Assert.Equal("Basic " + Base64("bob: p@ss "), Header(login.Id, "https://example.com/"));
		Assert.Equal("Bearer abc", Header(token.Id, "https://example.com/"));
	}

	/// <summary>A token keeps no user name even if one was typed while it was a user name and password.</summary>
	[Fact]
	public void save_drops_the_user_name_of_a_token() =>
		Assert.Null(_store.Save(new CredentialDraft(null, "Api", CredentialKind.Token, "left over", "abc", Example)).UserName);

	/// <summary>Credentials list by name, whatever order they were added in.</summary>
	[Fact]
	public void list_is_by_name()
	{
		_store.Save(Token("zulu", "1", CredentialKind.Token, Example));
		_store.Save(Token("Alpha", "2", CredentialKind.Token, Example));
		_store.Save(Token("mike", "3", CredentialKind.Token, Example));

		Assert.Equal<string>(["Alpha", "mike", "zulu"], _store.List().Select(c => c.Name));
	}

	// ============================ edit ============================

	/// <summary>Editing with the secret left blank keeps the saved one; typing one replaces it.</summary>
	[Fact]
	public void edit_keeps_or_replaces_the_secret()
	{
		CredentialInfo saved = _store.Save(Token("Api", "first", CredentialKind.Token, Example));

		CredentialInfo renamed = _store.Save(new CredentialDraft(saved.Id, "Api renamed", CredentialKind.Token, null, "", ["example.org"]));
		Assert.Equal(saved.Id, renamed.Id);
		Assert.Equal("Bearer first", Header(saved.Id, "https://example.org/"));

		_store.Save(new CredentialDraft(saved.Id, "Api renamed", CredentialKind.Token, null, "second", ["example.org"]));
		Assert.Equal("Bearer second", Header(saved.Id, "https://example.org/"));
		Assert.Single(_store.List());
	}

	/// <summary>Changing what a credential is needs its secret typed again.</summary>
	[Fact]
	public void edit_that_changes_the_kind_needs_the_secret()
	{
		CredentialInfo saved = _store.Save(Token("Api", "abc", CredentialKind.Token, Example));

		Assert.Equal("Enter the password.", _store.Validate(new CredentialDraft(saved.Id, "Api", CredentialKind.UsernamePassword, "bob", null, Example)));
		Assert.Equal("Enter the token.", _store.Validate(new CredentialDraft(saved.Id, "Api", CredentialKind.GitHubToken, null, null, Example)));
	}

	// ============================ validation ============================

	/// <summary>Each rule a credential must meet, with the message the user sees.</summary>
	[Theory]
	[InlineData("", CredentialKind.Token, null, "abc", "example.com", "Give the credential a name.")]
	[InlineData("taken", CredentialKind.Token, null, "abc", "example.com", "There is already a credential called 'taken'.")]
	[InlineData("TAKEN", CredentialKind.Token, null, "abc", "example.com", "There is already a credential called 'TAKEN'.")]
	[InlineData("x", CredentialKind.UsernamePassword, " ", "abc", "example.com", "Enter the user name.")]
	[InlineData("x", CredentialKind.UsernamePassword, "bob", "", "example.com", "Enter the password.")]
	[InlineData("x", CredentialKind.GitHubToken, null, " ", "github.com", "Enter the token.")]
	[InlineData("x", CredentialKind.Token, null, "abc", "", "Name at least one website it may be used with.")]
	public void validate_reports_each_rule(string name, CredentialKind kind, string? user, string secret, string hosts, string expected)
	{
		_store.Save(Token("taken", "abc", CredentialKind.Token, Example));
		CredentialDraft draft = new(null, name, kind, user, secret, hosts.Length == 0 ? [] : [hosts]);

		Assert.Equal(expected, _store.Validate(draft));
		ArgumentException ex = Assert.Throws<ArgumentException>(() => _store.Save(draft));
		Assert.StartsWith(expected, ex.Message, StringComparison.Ordinal);
	}

	/// <summary>A draft with no name at all is asked for one.</summary>
	[Fact]
	public void validate_no_name_asks_for_one() =>
		Assert.Equal("Give the credential a name.", _store.Validate(new CredentialDraft(null, null!, CredentialKind.Token, null, "abc", Example)));

	/// <summary>A credential that would not fit in one Credential Manager entry is refused.</summary>
	[Fact]
	public void validate_refuses_what_is_too_long_to_store() =>
		Assert.StartsWith("That is too long to store.", _store.Validate(Token("Api", new string('x', 3000), CredentialKind.Token, Example)), StringComparison.Ordinal);

	/// <summary>Editing a credential that was removed in the meantime is refused.</summary>
	[Fact]
	public void validate_refuses_editing_a_removed_credential() =>
		Assert.Equal(
			"This credential no longer exists. It may have been removed.",
			_store.Validate(new CredentialDraft(Guid.NewGuid(), "Api", CredentialKind.Token, null, "abc", Example)));

	/// <summary>Renaming a credential to its own name, in another case, is not a clash.</summary>
	[Fact]
	public void validate_allows_a_credential_its_own_name()
	{
		CredentialInfo saved = _store.Save(Token("Api", "abc", CredentialKind.Token, Example));

		Assert.Null(_store.Validate(new CredentialDraft(saved.Id, "API", CredentialKind.Token, null, null, Example)));
	}

	/// <summary>A null draft is a programming error.</summary>
	[Fact]
	public void validate_null_throws()
	{
		Assert.Throws<ArgumentNullException>(() => _store.Validate(null!));
		Assert.Throws<ArgumentNullException>(() => new CredentialStore(null!));
	}

	// ============================ FE-Buddy's GitHub token ============================

	/// <summary>FE-Buddy's own GitHub requests get the chosen GitHub token's secret.</summary>
	[Fact]
	public void get_github_token_gives_a_github_tokens_secret()
	{
		CredentialInfo token = _store.Save(Token("GitHub", "ghp_abc", CredentialKind.GitHubToken, CredentialHosts.GitHubDefaults));

		Assert.Equal("ghp_abc", _store.GetGitHubToken(token.Id));
		Assert.Null(_store.GetGitHubToken(Guid.NewGuid()));
	}

	/// <summary>A credential that is not a GitHub token for GitHub's API never gives one.</summary>
	[Theory]
	[InlineData(CredentialKind.GitHubToken, "example.com")]
	[InlineData(CredentialKind.Token, "github.com")]
	public void get_github_token_ignores_a_credential_that_does_not_qualify(CredentialKind kind, string host)
	{
		CredentialInfo saved = _store.Save(Token("Other", "abc", kind, [host]));

		Assert.Null(_store.GetGitHubToken(saved.Id));
	}

	/// <summary>An entry saved while credentials could be marked for FE-Buddy's requests still reads.</summary>
	[Fact]
	public void an_entry_with_the_old_marker_still_reads()
	{
		Guid id = Guid.NewGuid();
		Plant(id, """{"version":1,"name":"Old","kind":"GitHubToken","hosts":["github.com"],"useForFeBuddyGitHub":true,"secret":"abc"}""");

		Assert.Equal("Old", _store.Find(id)!.Name);
		Assert.Equal("abc", _store.GetGitHubToken(id));
	}

	// ============================ authorize ============================

	/// <summary>A user name and password go as Basic, a token as Bearer, on its own website and its subdomains.</summary>
	[Fact]
	public void authorize_adds_the_right_header()
	{
		CredentialInfo login = _store.Save(new CredentialDraft(null, "Login", CredentialKind.UsernamePassword, "bob", "pa:ss", Example));
		CredentialInfo token = _store.Save(Token("Api", "abc", CredentialKind.GitHubToken, CredentialHosts.GitHubDefaults));

		Assert.Equal("Basic " + Base64("bob:pa:ss"), Header(login.Id, "https://files.example.com/alias.txt"));
		Assert.Equal("Bearer abc", Header(token.Id, "https://raw.githubusercontent.com/org/repo/main/aliases.txt"));
	}

	/// <summary>Off its websites, over plain HTTP, or when it no longer exists, the request is left untouched.</summary>
	[Theory]
	[InlineData("https://evil.net/steal", CredentialUseResult.HostNotAllowed)]
	[InlineData("https://example.com.evil.net/", CredentialUseResult.HostNotAllowed)]
	[InlineData("http://example.com/alias.txt", CredentialUseResult.NotHttps)]
	[InlineData("ftp://example.com/alias.txt", CredentialUseResult.NotHttps)]
	[InlineData("alias.txt", CredentialUseResult.NotHttps)]
	public void authorize_refuses_other_websites_and_plain_http(string url, CredentialUseResult expected)
	{
		CredentialInfo token = _store.Save(Token("Api", "abc", CredentialKind.Token, Example));
		using HttpRequestMessage request = new(HttpMethod.Get, new Uri(url, UriKind.RelativeOrAbsolute));

		Assert.Equal(expected, _store.Authorize(request, token.Id));
		Assert.Null(request.Headers.Authorization);
	}

	/// <summary>An id with no credential behind it - removed, or chosen on another PC - is reported, not guessed at.</summary>
	[Fact]
	public void authorize_unknown_id_is_not_found()
	{
		using HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/");

		Assert.Equal(CredentialUseResult.NotFound, _store.Authorize(request, Guid.NewGuid()));
		Assert.Null(request.Headers.Authorization);
		Assert.Throws<ArgumentNullException>(() => _store.Authorize(null!, Guid.NewGuid()));
	}

	// ============================ delete ============================

	/// <summary>Removing one credential removes only it, and says whether it existed.</summary>
	[Fact]
	public void delete_removes_one()
	{
		CredentialInfo keep = _store.Save(Token("Keep", "1", CredentialKind.Token, Example));
		CredentialInfo drop = _store.Save(Token("Drop", "2", CredentialKind.Token, Example));

		Assert.True(_store.Delete(drop.Id));
		Assert.False(_store.Delete(drop.Id));
		Assert.Equivalent(keep, Assert.Single(_store.List()));
	}

	/// <summary>Removing all removes every FE-Buddy credential and nothing else in the vault.</summary>
	[Fact]
	public void delete_all_removes_only_fe_buddy_entries()
	{
		_store.Save(Token("One", "1", CredentialKind.Token, Example));
		_store.Save(Token("Two", "2", CredentialKind.Token, Example));
		_vault.Write("git:https://github.com", "someone", [1, 2, 3]);

		Assert.Equal(2, _store.DeleteAll());
		Assert.Empty(_store.List());
		Assert.NotNull(_vault.Read("git:https://github.com"));
	}

	/// <summary>Changed is raised for every save and removal, but not for removing nothing.</summary>
	[Fact]
	public void changed_is_raised_for_each_change()
	{
		int raised = 0;
		_store.Changed += (_, _) => raised++;

		CredentialInfo saved = _store.Save(Token("Api", "abc", CredentialKind.Token, Example));
		_store.Delete(saved.Id);
		_store.Delete(saved.Id);
		_store.DeleteAll();

		Assert.Equal(3, raised);
	}

	// ============================ damaged entries ============================

	/// <summary>Entries that cannot be read are skipped rather than breaking the list.</summary>
	[Theory]
	[InlineData("not json")]
	[InlineData("null")]
	[InlineData("""{"version":1,"name":"","kind":"Token","hosts":["example.com"],"secret":"abc"}""")]
	[InlineData("""{"version":1,"name":"No secret","kind":"Token","hosts":["example.com"]}""")]
	[InlineData("""{"version":1,"name":"No hosts","kind":"Token","secret":"abc"}""")]
	[InlineData("""{"version":1,"name":"Odd kind","kind":99,"hosts":["example.com"],"secret":"abc"}""")]
	public void damaged_entries_are_skipped(string json)
	{
		Guid id = Guid.NewGuid();
		Plant(id, json);
		_vault.Plant(CredentialStore.TargetPrefix + "not-a-guid", Encoding.UTF8.GetBytes("{}"));
		CredentialInfo good = _store.Save(Token("Good", "abc", CredentialKind.Token, Example));

		Assert.Equivalent(good, Assert.Single(_store.List()));
		Assert.Null(_store.Find(id));
	}

	/// <summary>The one store over Credential Manager is shared.</summary>
	[Fact]
	public void default_is_one_shared_store() =>
		Assert.Same(CredentialStore.Default, CredentialStore.Default);

	private static CredentialDraft Token(string name, string secret, CredentialKind kind, IReadOnlyList<string> hosts) =>
		new(null, name, kind, null, secret, hosts);

	private static string Base64(string text) => Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

	private string? Header(Guid id, string url)
	{
		using HttpRequestMessage request = new(HttpMethod.Get, url);
		Assert.Equal(CredentialUseResult.Applied, _store.Authorize(request, id));
		AuthenticationHeaderValue header = request.Headers.Authorization!;
		return $"{header.Scheme} {header.Parameter}";
	}

	private void Plant(Guid id, string json) =>
		_vault.Plant(CredentialStore.TargetPrefix + id.ToString("N"), Encoding.UTF8.GetBytes(json));
}
