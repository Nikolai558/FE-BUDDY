using FeBuddy.Core.Infrastructure.Credentials;

namespace FeBuddy.UnitTests.Infrastructure.Credentials;

/// <summary>
/// Covers <see cref="UrlSecrets"/>: a user name and password before the host, or a sign-in token in
/// the query, is found; an ordinary address is left alone.
/// </summary>
public sealed class UrlSecretsTests
{
	[Theory]
	[InlineData("https://bob:hunter2@example.com/a.txt", "a user name and password")]
	[InlineData("https://bob@example.com/a.txt", "a user name and password")]
	[InlineData("https://raw.githubusercontent.com/o/r/main/a.txt?token=GHSAT0AAA", "a sign-in token (token=)")]
	[InlineData("https://example.com/a.txt?plain=1&TOKEN=x", "a sign-in token (TOKEN=)")]
	[InlineData("https://example.com/a.txt?%74oken=x", "a sign-in token (token=)")]
	[InlineData("https://bucket.s3.amazonaws.com/a.txt?X-Amz-Signature=abc&X-Amz-Expires=60", "a sign-in token (X-Amz-Signature=)")]
	[InlineData("https://example.com/a.txt?access_token", "a sign-in token (access_token=)")]
	public void a_secret_in_an_address_is_found(string url, string expected) =>
		Assert.Equal(expected, UrlSecrets.Describe(new Uri(url)));

	[Theory]
	[InlineData("https://github.com/o/r/blob/main/a.txt")]
	[InlineData("https://github.com/o/r/blob/main/a.txt?plain=1")]
	[InlineData("https://www.dropbox.com/scl/fi/abc/a.txt?rlkey=xyz&dl=1")]
	[InlineData("https://example.com/a.txt?&")]
	[InlineData("https://example.com/tokens/a.txt?name=token")]
	public void an_ordinary_address_has_none(string url) =>
		Assert.Null(UrlSecrets.Describe(new Uri(url)));

	[Fact]
	public void a_relative_address_has_none_and_null_is_refused()
	{
		Assert.Null(UrlSecrets.Describe(new Uri("a.txt?token=x", UriKind.Relative)));
		Assert.Throws<ArgumentNullException>(() => UrlSecrets.Describe(null!));
	}
}
