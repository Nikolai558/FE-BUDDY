using System.Net;
using System.Text;

using FeBuddy.Core.Application.Airac.ConcatenateAliases;
using FeBuddy.Core.Application.Airac.ConcatenateAliases.Models;
using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;

namespace FeBuddy.UnitTests.Application.Airac.ConcatenateAliases;

/// <summary>
/// Covers <see cref="AliasSourceLoader"/>: reading a custom alias file from this PC or the web; a
/// GitHub file going through the API with its credential; and every way a download can fail - a
/// missing, refused or misplaced credential, a private repository, a web page instead of a file -
/// reported in words the user can act on, never with the secret.
/// </summary>
public sealed class AliasSourceLoaderTests : IDisposable
{
	private const string Secret = "ghp_not_a_real_token";
	private const string AliasText = ".FeUseOnly first line\r\n# a comment\r\n.dtwdv .ECHO DTW\r\n";

	private readonly string _folder = Path.Combine(Path.GetTempPath(), "FeBuddyTests_AliasSources_" + Guid.NewGuid().ToString("N"));
	private readonly CredentialStore _store = new(new InMemoryCredentialVault());

	public AliasSourceLoaderTests() => Directory.CreateDirectory(_folder);

	public void Dispose() => Directory.Delete(_folder, recursive: true);

	// ============================ files on this PC ============================

	[Fact]
	public async Task a_file_is_read_without_its_byte_order_mark()
	{
		string path = Path.Combine(_folder, "ZOB-Alias.txt");
		File.WriteAllText(path, AliasText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(FileSource(path), _store);

		Assert.True(load.Succeeded);
		Assert.Equal(AliasText, load.Text);
		Assert.Null(load.Problem);
		Assert.Equal(2, load.CommandCount);
	}

	[Fact]
	public async Task a_missing_file_says_so()
	{
		string path = Path.Combine(_folder, "Gone.txt");

		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(FileSource(path), _store);

		Assert.False(load.Succeeded);
		Assert.Equal(0, load.CommandCount);
		Assert.Contains("was not found", load.Problem, StringComparison.Ordinal);
	}

	[Fact]
	public async Task a_file_that_cannot_be_opened_says_why()
	{
		string path = Path.Combine(_folder, "Locked.txt");
		File.WriteAllText(path, AliasText);

		using FileStream locked = new(path, FileMode.Open, FileAccess.Read, FileShare.None);
		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(FileSource(path), _store);

		Assert.Contains("could not be read", load.Problem, StringComparison.Ordinal);
	}

	[Fact]
	public async Task a_file_with_no_alias_command_is_not_an_alias_file()
	{
		string path = Path.Combine(_folder, "Notes.txt");
		File.WriteAllText(path, "just some notes\r\n; and a comment\r\n");

		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(FileSource(path), _store);

		Assert.Contains("does not look like an alias file", load.Problem, StringComparison.Ordinal);
	}

	// ============================ downloads ============================

	[Fact]
	public async Task a_public_github_file_is_downloaded_through_the_api_without_a_credential()
	{
		HttpRequestMessage? sent = null;
		using HttpClient client = Client(request =>
		{
			sent = request;
			return Text(AliasText);
		});

		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(UrlSource("https://github.com/vZOB/facility/blob/main/ZOB-Alias.txt"), _store, client);

		Assert.True(load.Succeeded);
		Assert.Equal("https://api.github.com/repos/vZOB/facility/contents/ZOB-Alias.txt?ref=main", sent!.RequestUri!.AbsoluteUri);
		Assert.Contains("application/vnd.github.raw", sent.Headers.Accept.ToString(), StringComparison.Ordinal);
		Assert.Null(sent.Headers.Authorization);
	}

	[Fact]
	public async Task a_private_github_file_is_downloaded_with_its_token()
	{
		Guid id = SaveGitHubToken();
		string? authorization = null;
		using HttpClient client = Client(request =>
		{
			authorization = request.Headers.Authorization?.ToString();
			return Text(AliasText);
		});

		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(UrlSource("https://raw.githubusercontent.com/o/r/main/a.txt", id), _store, client);

		Assert.True(load.Succeeded);
		Assert.Equal($"Bearer {Secret}", authorization);
	}

	[Fact]
	public async Task another_website_is_downloaded_as_given()
	{
		Uri? sent = null;
		using HttpClient client = Client(request =>
		{
			sent = request.RequestUri;
			return Text(AliasText);
		});

		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(UrlSource("http://example.com/files/a.txt"), _store, client);

		Assert.True(load.Succeeded);
		Assert.Equal("http://example.com/files/a.txt", sent!.AbsoluteUri);
	}

	[Theory]
	[InlineData("ftp://example.com/a.txt")]
	[InlineData("not an address")]
	public async Task an_address_that_is_not_http_is_refused(string url)
	{
		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(UrlSource(url), _store, Client(_ => throw new InvalidOperationException("never sent")));

		Assert.Contains("is not a web address", load.Problem, StringComparison.Ordinal);
	}

	[Fact]
	public async Task a_github_repository_page_is_not_a_file()
	{
		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(
			UrlSource("https://github.com/Nikolai558/test-repo"), _store, Client(_ => throw new InvalidOperationException("never sent")));

		Assert.Contains("a GitHub page, not a file", load.Problem, StringComparison.Ordinal);
	}

	[Fact]
	public async Task a_credential_that_is_not_on_this_pc_is_not_sent_and_the_file_is_not_downloaded()
	{
		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(
			UrlSource("https://github.com/o/r/blob/main/a.txt", Guid.NewGuid()), _store, Client(_ => throw new InvalidOperationException("never sent")));

		Assert.Contains("credential is not on this PC", load.Problem, StringComparison.Ordinal);
	}

	[Fact]
	public async Task a_credential_is_never_sent_over_http()
	{
		Guid id = _store.Save(new CredentialDraft(null, "Example key", CredentialKind.Token, null, Secret, ["example.com"])).Id;

		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(
			UrlSource("http://example.com/a.txt", id), _store, Client(_ => throw new InvalidOperationException("never sent")));

		Assert.Contains("is not https://", load.Problem, StringComparison.Ordinal);
		Assert.Contains("'Example key'", load.Problem, StringComparison.Ordinal);
		Assert.DoesNotContain(Secret, load.Problem, StringComparison.Ordinal);
	}

	[Fact]
	public async Task a_credential_is_never_sent_to_a_website_it_does_not_name()
	{
		Guid id = SaveGitHubToken();

		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(
			UrlSource("https://gitlab.com/o/r/-/raw/main/a.txt", id), _store, Client(_ => throw new InvalidOperationException("never sent")));

		Assert.Contains("is not allowed to be sent to gitlab.com", load.Problem, StringComparison.Ordinal);
	}

	/// <summary>
	/// What to do, and whether it is a matter of access - a credential needed, or one that can't read
	/// the file (GitHub's 404 for a private repository included) - rather than a limit or an error.
	/// </summary>
	[Theory]
	[InlineData(HttpStatusCode.Unauthorized, true, false, "refused the credential 'ZOB GitHub'", true)]
	[InlineData(HttpStatusCode.Unauthorized, false, false, "needs a credential", true)]
	[InlineData(HttpStatusCode.Forbidden, false, true, "limits downloads made without a token", false)]
	[InlineData(HttpStatusCode.TooManyRequests, false, true, "limits downloads made without a token", false)]
	[InlineData(HttpStatusCode.Forbidden, true, true, "download limit for the credential 'ZOB GitHub'", false)]
	[InlineData(HttpStatusCode.Forbidden, true, false, "Contents: Read-only", true)]
	[InlineData(HttpStatusCode.Forbidden, false, false, "It may need a credential", true)]
	[InlineData(HttpStatusCode.NotFound, false, false, "if the repository is private, choose a GitHub credential", true)]
	[InlineData(HttpStatusCode.NotFound, true, false, "or the credential 'ZOB GitHub' cannot see it", true)]
	[InlineData(HttpStatusCode.InternalServerError, true, false, "GitHub answered 500", false)]
	public async Task a_refused_github_download_says_what_to_do(HttpStatusCode status, bool withCredential, bool rateLimited, string expected, bool denied)
	{
		Guid? id = withCredential ? SaveGitHubToken() : null;
		using HttpClient client = Client(_ =>
		{
			HttpResponseMessage response = new(status);

			if (rateLimited)
			{
				response.Headers.Add("x-ratelimit-remaining", "0");
			}

			return response;
		});

		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(UrlSource("https://github.com/o/r/blob/main/a.txt", id), _store, client);

		Assert.False(load.Succeeded);
		Assert.Contains(expected, load.Problem, StringComparison.Ordinal);
		Assert.DoesNotContain(Secret, load.Problem, StringComparison.Ordinal);
		Assert.Equal(denied, load.IsAccessDenied);
	}

	/// <summary>
	/// GitHub's other reasons for 403 are told apart from a missing permission: a short-term limit
	/// on bursts of requests (Retry-After, or 429), and a token not authorized for an organization's
	/// single sign-on.
	/// </summary>
	[Theory]
	[InlineData(HttpStatusCode.Forbidden, "Retry-After", "60", true, "GitHub is limiting how often it can be asked right now", false)]
	[InlineData(HttpStatusCode.TooManyRequests, null, null, false, "GitHub is limiting how often it can be asked right now", false)]
	[InlineData(HttpStatusCode.Forbidden, "X-GitHub-SSO", "required; url=https://github.com/orgs/o/sso", true, "authorized for this organization's single sign-on (SSO)", true)]
	[InlineData(HttpStatusCode.Forbidden, "X-GitHub-SSO", "required; url=https://github.com/orgs/o/sso", false, "It may need a credential", true)]
	public async Task a_github_403_that_is_not_a_missing_permission_says_so(HttpStatusCode status, string? header, string? value, bool withCredential, string expected, bool denied)
	{
		Guid? id = withCredential ? SaveGitHubToken() : null;
		using HttpClient client = Client(_ =>
		{
			HttpResponseMessage response = new(status);

			if (header is not null)
			{
				response.Headers.TryAddWithoutValidation(header, value);
			}

			return response;
		});

		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(UrlSource("https://github.com/o/r/blob/main/a.txt", id), _store, client);

		Assert.Contains(expected, load.Problem, StringComparison.Ordinal);
		Assert.DoesNotContain("Contents: Read-only", load.Problem, StringComparison.Ordinal);
		Assert.Equal(denied, load.IsAccessDenied);
	}

	/// <summary>A Credential Manager that cannot be read fails the one file, with the reason - it never throws.</summary>
	[Fact]
	public async Task a_credential_manager_that_cannot_be_read_fails_the_file()
	{
		InMemoryCredentialVault vault = new();
		CredentialStore store = new(vault);
		Guid id = store.Save(new CredentialDraft(null, "ZOB GitHub", CredentialKind.GitHubToken, null, Secret, CredentialHosts.GitHubDefaults)).Id;
		vault.Failure = new System.ComponentModel.Win32Exception(1312, "A specified logon session does not exist.");

		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(
			UrlSource("https://github.com/o/r/blob/main/a.txt", id), store, Client(_ => throw new InvalidOperationException("never sent")));

		Assert.Equal(
			"Windows Credential Manager could not be read, so its credential could not be used: A specified logon session does not exist.",
			load.Problem);
	}

	/// <summary>A download .NET cannot turn into text - it names a character set .NET does not know - fails the file, with the reason.</summary>
	[Fact]
	public async Task a_download_that_is_not_readable_text_fails_the_file()
	{
		using HttpClient client = Client(_ =>
		{
			HttpResponseMessage response = new(HttpStatusCode.OK) { Content = new ByteArrayContent(Encoding.UTF8.GetBytes(AliasText)) };
			response.Content.Headers.TryAddWithoutValidation("Content-Type", "text/plain; charset=no-such-charset");
			return response;
		});

		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(UrlSource("https://example.com/a.txt"), _store, client);

		Assert.StartsWith("example.com sent the file in a form FE-Buddy cannot read as text: ", load.Problem, StringComparison.Ordinal);
	}

	/// <summary>A download cut off part-way fails the file like one that could not be reached.</summary>
	[Fact]
	public async Task a_download_cut_off_part_way_says_so()
	{
		using HttpClient client = Client(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new FailingStream()) });

		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(UrlSource("https://example.com/a.txt"), _store, client);

		Assert.StartsWith("Could not reach example.com: ", load.Problem, StringComparison.Ordinal);
	}

	[Fact]
	public async Task a_forbidden_download_from_another_website_does_not_mention_github_tokens()
	{
		Guid id = _store.Save(new CredentialDraft(null, "Example key", CredentialKind.Token, null, Secret, ["example.com"])).Id;
		using HttpClient client = Client(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));

		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(UrlSource("https://example.com/a.txt", id), _store, client);

		Assert.Equal("example.com does not let the credential 'Example key' read it.", load.Problem);
		Assert.True(load.IsAccessDenied);
	}

	[Fact]
	public async Task a_missing_file_on_another_website_says_404()
	{
		using HttpClient client = Client(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(UrlSource("https://example.com/a.txt"), _store, client);

		Assert.Contains("example.com could not find it (404 Not Found)", load.Problem, StringComparison.Ordinal);
		Assert.False(load.IsAccessDenied);
	}

	[Theory]
	[InlineData("text/html", ".not really html", "https://raw.githubusercontent.com/o/r/main/a.txt", "(on GitHub, the file's Raw link)")]
	[InlineData("text/plain", "\uFEFF  <!DOCTYPE html><html></html>", "https://example.com/a.txt", "Use the address of the file itself.")]
	[InlineData("text/plain", "\r\n<html><body>Sign in</body></html>", "https://example.com/a.txt", "sent a web page")]
	public async Task a_web_page_is_not_an_alias_file(string mediaType, string body, string url, string expected)
	{
		using HttpClient client = Client(_ => new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(body, Encoding.UTF8, mediaType),
		});

		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(UrlSource(url), _store, client);

		Assert.Contains(expected, load.Problem, StringComparison.Ordinal);
	}

	[Fact]
	public async Task a_download_with_no_content_type_is_read()
	{
		using HttpClient client = Client(_ =>
		{
			HttpResponseMessage response = new(HttpStatusCode.OK) { Content = new ByteArrayContent(Encoding.UTF8.GetBytes(AliasText)) };
			response.Content.Headers.ContentType = null;
			return response;
		});

		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(UrlSource("https://example.com/a.txt"), _store, client);

		Assert.True(load.Succeeded);
	}

	[Fact]
	public async Task a_website_that_cannot_be_reached_says_so()
	{
		using HttpClient client = Client(_ => throw new HttpRequestException("No such host is known."));

		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(UrlSource("https://example.com/a.txt"), _store, client);

		Assert.Equal("Could not reach example.com: No such host is known.", load.Problem);
	}

	[Fact]
	public async Task a_download_that_times_out_says_so()
	{
		using HttpClient client = Client(_ => throw new TaskCanceledException("timed out"));

		AliasSourceLoad load = await AliasSourceLoader.LoadAsync(UrlSource("https://example.com/a.txt"), _store, client);

		Assert.Contains("did not answer within 30 seconds", load.Problem, StringComparison.Ordinal);
	}

	[Fact]
	public async Task cancelling_the_download_cancels()
	{
		using CancellationTokenSource cancelled = new();
		await cancelled.CancelAsync();
		using HttpClient client = Client(_ => throw new TaskCanceledException());

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			AliasSourceLoader.LoadAsync(UrlSource("https://example.com/a.txt"), _store, client, cancelled.Token));
	}

	// ============================ several ============================

	[Fact]
	public async Task every_source_is_loaded_in_order_and_one_token_serves_two_of_them()
	{
		string path = Path.Combine(_folder, "Local.txt");
		File.WriteAllText(path, ".local .ECHO local");
		Guid id = SaveGitHubToken();
		List<string?> authorizations = [];

		using HttpClient client = Client(request =>
		{
			authorizations.Add(request.Headers.Authorization?.Parameter);
			return Text(".web .ECHO web");
		});

		IReadOnlyList<AliasSourceLoad> loads = await AliasSourceLoader.LoadAllAsync(
			[
				UrlSource("https://github.com/one/r/blob/main/a.txt", id, 1),
				FileSource(path, 2),
				UrlSource("https://github.com/two/r/blob/main/b.txt", number: 3),
				UrlSource("https://github.com/three/r/blob/main/c.txt", id, 4),
			],
			_store,
			client);

		Assert.Equal([1, 2, 3, 4], loads.Select(l => l.Source.Number));
		Assert.All(loads, l => Assert.True(l.Succeeded));
		Assert.Equal([Secret, null, Secret], authorizations);
	}

	[Fact]
	public async Task files_alone_need_no_client()
	{
		string path = Path.Combine(_folder, "Local.txt");
		File.WriteAllText(path, ".local .ECHO local");

		IReadOnlyList<AliasSourceLoad> loads = await AliasSourceLoader.LoadAllAsync([FileSource(path)], _store);

		Assert.True(Assert.Single(loads).Succeeded);
	}

	[Fact]
	public async Task nulls_are_refused()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(() => AliasSourceLoader.LoadAsync(null!, _store));
		await Assert.ThrowsAsync<ArgumentNullException>(() => AliasSourceLoader.LoadAsync(FileSource("C:\\a.txt"), null!));
		await Assert.ThrowsAsync<ArgumentNullException>(() => AliasSourceLoader.LoadAllAsync(null!, _store));
		await Assert.ThrowsAsync<ArgumentNullException>(() => AliasSourceLoader.LoadAllAsync([], null!));
	}

	private static AliasSource FileSource(string path, int number = 1) => new(number, AliasSourceKind.File, path);

	private static AliasSource UrlSource(string url, Guid? credentialId = null, int number = 1) => new(number, AliasSourceKind.Url, url, credentialId);

	private Guid SaveGitHubToken() =>
		_store.Save(new CredentialDraft(null, "ZOB GitHub", CredentialKind.GitHubToken, null, Secret, CredentialHosts.GitHubDefaults)).Id;

	private static HttpClient Client(Func<HttpRequestMessage, HttpResponseMessage> responder) => new(new StubHttpHandler(responder));

	private static HttpResponseMessage Text(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body) };

	/// <summary>A body whose connection drops as soon as it is read.</summary>
	private sealed class FailingStream : MemoryStream
	{
		public override int Read(byte[] buffer, int offset, int count) => throw new IOException("The connection was reset.");

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
			throw new IOException("The connection was reset.");

		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
			throw new IOException("The connection was reset.");
	}
}
