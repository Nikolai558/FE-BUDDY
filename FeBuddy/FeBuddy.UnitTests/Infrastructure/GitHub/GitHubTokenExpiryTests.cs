using System.ComponentModel;
using System.Globalization;
using System.Net;

using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;
using FeBuddy.Core.Infrastructure.GitHub;
using FeBuddy.Core.Infrastructure.GitHub.Models;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Infrastructure.GitHub;

/// <summary>
/// Covers <see cref="GitHubTokenExpiry"/>: reading GitHub's expiry header, the 57-day warning and
/// its words, and the background check that finds the saved tokens needing attention.
/// </summary>
[Collection("AppLog")]
public sealed class GitHubTokenExpiryTests : IDisposable
{
	/// <summary>Noon UTC, so the calendar days are the same on any PC.</summary>
	private static readonly DateTimeOffset Now = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);

	private readonly string _logs = Path.Combine(Path.GetTempPath(), "FeBuddyTests_TokenExpiry_" + Guid.NewGuid().ToString("N"));
	private readonly InMemoryCredentialVault _vault = new();
	private readonly CredentialStore _store;

	public GitHubTokenExpiryTests()
	{
		AppLog.ConfigureForTesting(_logs);
		_store = new CredentialStore(_vault);
	}

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

	// ============================ the header ============================

	[Theory]
	[InlineData("2026-12-01 05:00:00 UTC", "2026-12-01T05:00:00+00:00")]
	[InlineData("2026-12-01 05:00:00 -0800", "2026-12-01T13:00:00+00:00")]
	[InlineData("2026-12-01 05:00:00 +02:00", "2026-12-01T03:00:00+00:00")]
	public void the_header_is_read_in_the_forms_github_sends(string value, string expected)
	{
		Assert.Equal(DateTimeOffset.Parse(expected, CultureInfo.InvariantCulture), GitHubTokenExpiry.Parse(value));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("  ")]
	[InlineData("never")]
	public void a_missing_or_odd_header_is_no_expiry(string? value)
	{
		Assert.Null(GitHubTokenExpiry.Parse(value));
	}

	[Fact]
	public void the_expiry_is_read_from_githubs_answer()
	{
		using HttpResponseMessage with = new(HttpStatusCode.OK);
		with.Headers.Add("GitHub-Authentication-Token-Expiration", "2026-12-01 05:00:00 UTC");
		using HttpResponseMessage without = new(HttpStatusCode.OK);

		Assert.Equal(new DateTimeOffset(2026, 12, 1, 5, 0, 0, TimeSpan.Zero), GitHubTokenExpiry.Read(with));
		Assert.Null(GitHubTokenExpiry.Read(without));
		Assert.Throws<ArgumentNullException>(() => GitHubTokenExpiry.Read(null!));
	}

	// ============================ the warning ============================

	[Theory]
	[InlineData(57, true)]
	[InlineData(58, false)]
	[InlineData(0, true)]
	[InlineData(-3, true)]
	public void the_warning_starts_57_days_before(int daysLeft, bool soon)
	{
		Assert.Equal(daysLeft, GitHubTokenExpiry.DaysLeft(Now.AddDays(daysLeft), Now));
		Assert.Equal(soon, GitHubTokenExpiry.IsSoon(Now.AddDays(daysLeft), Now));
		Assert.False(GitHubTokenExpiry.IsSoon(null, Now));
	}

	[Theory]
	[InlineData(-3, "expired on ")]
	[InlineData(0, "expires today")]
	[InlineData(1, "expires tomorrow")]
	[InlineData(12, "expires in 12 days, on ")]
	public void when_it_expires_is_put_in_words(int daysLeft, string start)
	{
		DateTimeOffset expires = Now.AddDays(daysLeft);
		string words = GitHubTokenExpiry.DescribeWhen(expires, Now);

		Assert.StartsWith(start, words, StringComparison.Ordinal);

		if (start.EndsWith("on ", StringComparison.Ordinal))
		{
			Assert.EndsWith(expires.ToLocalTime().ToString("d MMM yyyy", CultureInfo.InvariantCulture), words, StringComparison.Ordinal);
		}
	}

	[Fact]
	public void the_warning_names_the_credential_and_what_to_do()
	{
		Assert.Equal(
			$"The GitHub token 'ZOB' expires in 12 days, on {Now.AddDays(12).ToLocalTime():d MMM yyyy}. Make a new token on GitHub and paste it into 'ZOB' in Settings ▸ Credentials.",
			GitHubTokenExpiry.Warning("ZOB", Now.AddDays(12), Now));
		Assert.Null(GitHubTokenExpiry.Warning("ZOB", Now.AddDays(58), Now));
		Assert.Null(GitHubTokenExpiry.Warning("ZOB", null, Now));
	}

	// ============================ the background check ============================

	/// <summary>
	/// Only the saved GitHub tokens for GitHub's API are checked, by name, and only the rejected ones
	/// and the ones expiring soon come back.
	/// </summary>
	[Fact]
	public async Task only_the_tokens_needing_attention_come_back()
	{
		Save("Fine", "ghp_fine");
		Save("Soon", "ghp_soon");
		Save("Revoked", "ghp_revoked");
		Save("Elsewhere", "ghp_elsewhere", ["example.com"]);
		_store.Save(new CredentialDraft(null, "Api key", CredentialKind.Token, null, "abc", CredentialHosts.GitHubDefaults));
		List<string> checkedTokens = [];
		DateTimeOffset soon = DateTimeOffset.UtcNow.AddDays(10);

		using HttpClient client = new(new StubHttpHandler(request =>
		{
			string token = request.Headers.Authorization!.Parameter!;
			checkedTokens.Add(token);

			HttpResponseMessage response = new(token == "ghp_revoked" ? HttpStatusCode.Unauthorized : HttpStatusCode.OK);
			DateTimeOffset expires = token == "ghp_soon" ? soon : DateTimeOffset.UtcNow.AddDays(300);
			response.Headers.Add("GitHub-Authentication-Token-Expiration", expires.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture));
			return response;
		}));

		IReadOnlyList<GitHubTokenStatus> found = await GitHubTokenExpiry.FindTokensNeedingAttentionAsync(_store, client);

		Assert.Equal(["ghp_fine", "ghp_revoked", "ghp_soon"], checkedTokens);
		Assert.Equal(["Revoked", "Soon"], found.Select(t => t.Name));
		Assert.True(found[0] is { IsRejected: true, ExpiresAt: null });
		Assert.False(found[1].IsRejected);
		Assert.Equal(soon.ToUnixTimeSeconds(), found[1].ExpiresAt!.Value.ToUnixTimeSeconds());
	}

	[Fact]
	public async Task no_saved_token_sends_nothing()
	{
		int calls = 0;
		using HttpClient client = new(new StubHttpHandler(_ =>
		{
			calls++;
			return new HttpResponseMessage(HttpStatusCode.OK);
		}));

		Assert.Empty(await GitHubTokenExpiry.FindTokensNeedingAttentionAsync(_store, client));
		Assert.Empty(await GitHubTokenExpiry.FindTokensNeedingAttentionAsync(_store));
		Assert.Equal(0, calls);
		await Assert.ThrowsAsync<ArgumentNullException>(() => GitHubTokenExpiry.FindTokensNeedingAttentionAsync(null!));
	}

	/// <summary>A Credential Manager that can't be read finds nothing and is logged - never thrown.</summary>
	[Fact]
	public async Task a_credential_manager_that_cannot_be_read_finds_nothing()
	{
		Save("Soon", "ghp_soon");
		_vault.Failure = new Win32Exception(1312);
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)));

		Assert.Empty(await GitHubTokenExpiry.FindTokensNeedingAttentionAsync(_store, client));
	}

	/// <summary>A token that can't be read when it is checked is skipped, and the others are still checked.</summary>
	[Fact]
	public async Task a_token_that_cannot_be_read_is_skipped()
	{
		Save("First", "ghp_first");
		Save("Second", "ghp_second");
		int calls = 0;

		using HttpClient client = new(new StubHttpHandler(_ =>
		{
			calls++;
			return new HttpResponseMessage(HttpStatusCode.Unauthorized);
		}));

		// Listing works; reading the first token for its check fails, then Credential Manager recovers.
		CredentialStore flaky = new(new FlakyVault(_vault));
		IReadOnlyList<GitHubTokenStatus> found = await GitHubTokenExpiry.FindTokensNeedingAttentionAsync(flaky, client);

		GitHubTokenStatus only = Assert.Single(found);
		Assert.True(only.IsRejected);
		Assert.Equal(1, calls);
	}

	private void Save(string name, string token, IReadOnlyList<string>? hosts = null) =>
		_store.Save(new CredentialDraft(null, name, CredentialKind.GitHubToken, null, token, hosts ?? CredentialHosts.GitHubDefaults));

	/// <summary>A vault that lists as normal but fails its first read, then works.</summary>
	private sealed class FlakyVault(InMemoryCredentialVault inner) : ICredentialVault
	{
		private bool _failed;

		public void Write(string target, string userName, byte[] secret) => inner.Write(target, userName, secret);

		public byte[]? Read(string target)
		{
			if (!_failed)
			{
				_failed = true;
				throw new Win32Exception(1312);
			}

			return inner.Read(target);
		}

		public bool Delete(string target) => inner.Delete(target);

		public IReadOnlyList<VaultEntry> Enumerate(string prefix) => inner.Enumerate(prefix);
	}
}
