using FeBuddy.Core.Infrastructure.Credentials;

namespace FeBuddy.UnitTests.Infrastructure.Credentials;

/// <summary>
/// Exercises <see cref="CredentialHosts"/>: which websites a user can name, and which requests a
/// credential may then be sent with.
/// </summary>
public sealed class CredentialHostsTests
{
	/// <summary>Bare hosts and web addresses both become plain lower-case hosts.</summary>
	[Theory]
	[InlineData("github.com", "github.com")]
	[InlineData("  GitHub.COM.  ", "github.com")]
	[InlineData("https://github.com/Org/Repo", "github.com")]
	[InlineData("https://raw.githubusercontent.com/", "raw.githubusercontent.com")]
	[InlineData("*.example.com", "example.com")]
	public void try_parse_normalizes_one_host(string text, string expected)
	{
		Assert.True(CredentialHosts.TryParse(text, out IReadOnlyList<string> hosts, out string? error));

		Assert.Equal(expected, Assert.Single(hosts));
		Assert.Null(error);
	}

	/// <summary>Several hosts in any separator, with duplicates dropped.</summary>
	[Fact]
	public void try_parse_splits_and_drops_duplicates()
	{
		Assert.True(CredentialHosts.TryParse("github.com, githubusercontent.com;GITHUB.com\nexample.org", out IReadOnlyList<string> hosts, out _));

		Assert.Equal<string>(["github.com", "githubusercontent.com", "example.org"], hosts);
	}

	/// <summary>Nothing typed is no hosts, not an error; the store asks for at least one.</summary>
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" , ; ")]
	public void try_parse_blank_is_empty(string? text)
	{
		Assert.True(CredentialHosts.TryParse(text, out IReadOnlyList<string> hosts, out string? error));

		Assert.Empty(hosts);
		Assert.Null(error);
	}

	/// <summary>Anything that is not a website name with a dot is refused, naming the entry.</summary>
	[Theory]
	[InlineData("com")]
	[InlineData("localhost")]
	[InlineData("example.com:8443")]
	[InlineData("bad_host!.com")]
	[InlineData("https://[::1]/")]
	[InlineData("192.168.1.10")]
	[InlineData("https://")]
	[InlineData("*.")]
	[InlineData("1http://example.com")]
	public void try_parse_refuses_what_is_not_a_website(string text)
	{
		Assert.False(CredentialHosts.TryParse($"github.com, {text}", out IReadOnlyList<string> hosts, out string? error));

		Assert.Empty(hosts);
		Assert.Contains(text, error, StringComparison.Ordinal);
	}

	/// <summary>A host covers itself and its subdomains, and nothing that only looks like it.</summary>
	[Theory]
	[InlineData("github.com", true)]
	[InlineData("API.GitHub.com", true)]
	[InlineData("api.github.com.", true)]
	[InlineData("raw.githubusercontent.com", true)]
	[InlineData("notgithub.com", false)]
	[InlineData("github.com.evil.net", false)]
	[InlineData("evil.net", false)]
	[InlineData("", false)]
	public void allows_the_host_and_its_subdomains_only(string host, bool expected) =>
		Assert.Equal(expected, CredentialHosts.Allows(CredentialHosts.GitHubDefaults, host));

	/// <summary>A request with no host is never allowed.</summary>
	[Fact]
	public void allows_no_host_is_false() =>
		Assert.False(CredentialHosts.Allows(CredentialHosts.GitHubDefaults, null!));

	/// <summary>A null list of hosts is a programming error.</summary>
	[Fact]
	public void allows_null_list_throws() =>
		Assert.Throws<ArgumentNullException>(() => CredentialHosts.Allows(null!, "github.com"));
}
