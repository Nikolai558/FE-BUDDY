using FeBuddy.Core.Application.Airac.VnasAlias;
using FeBuddy.Core.Application.Airac.VnasAlias.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Application.Airac.VnasAlias;

/// <summary>
/// Covers <see cref="VnasAliasSettingsParser"/>: numbered custom alias files, each a file on this PC
/// or a web address with an optional credential id, merged in number order; and the settings it
/// refuses.
/// </summary>
public sealed class VnasAliasSettingsParserTests
{
	private static readonly Guid CredentialId = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");

	[Fact]
	public void sources_are_read_in_number_order()
	{
		VnasAliasSettingsParseResult result = VnasAliasSettingsParser.Parse(Block(
			("OutputDirectory", @"C:\Out"),
			("Sources.10.Url", "https://github.com/o/r/blob/main/Late.txt"),
			("Sources.2.Url", " https://github.com/o/r/blob/main/ZOB-Alias.txt "),
			("Sources.2.CredentialId", CredentialId.ToString("N")),
			("Sources.1.FilePath", @"C:\Users\me\ZOB-Alias.txt")));

		Assert.Equal(
			[
				new AliasSource(1, AliasSourceKind.File, @"C:\Users\me\ZOB-Alias.txt"),
				new AliasSource(2, AliasSourceKind.Url, "https://github.com/o/r/blob/main/ZOB-Alias.txt", CredentialId),
				new AliasSource(10, AliasSourceKind.Url, "https://github.com/o/r/blob/main/Late.txt"),
			],
			result.Sources);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void keys_match_ignoring_case()
	{
		VnasAliasSettingsParseResult result = VnasAliasSettingsParser.Parse(Block(
			("sources.1.url", "https://example.com/a.txt"),
			("SOURCES.1.CREDENTIALID", CredentialId.ToString())));

		Assert.Equal(new AliasSource(1, AliasSourceKind.Url, "https://example.com/a.txt", CredentialId), Assert.Single(result.Sources));
	}

	[Fact]
	public void no_sources_notes_that_only_fe_buddy_aliases_are_written()
	{
		VnasAliasSettingsParseResult result = VnasAliasSettingsParser.Parse(Block(("Sources.1.FilePath", " "), ("Sources.1.Url", "")));

		Assert.Empty(result.Sources);
		ServiceMessage note = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Info, note.Level);
		Assert.Contains("holds only FE-Buddy's aliases", note.Text, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("Colour")]
	[InlineData("Sources")]
	[InlineData("Sources.x.Url")]
	[InlineData("Sources.0.Url")]
	[InlineData("Sources.-1.Url")]
	[InlineData("Sources.1")]
	[InlineData("Sources.1.")]
	[InlineData("Sources.1.Url.Extra")]
	public void an_unknown_key_is_ignored_with_a_warning(string key)
	{
		VnasAliasSettingsParseResult result = VnasAliasSettingsParser.Parse(Block(
			(key, "x"),
			("Sources.3.FilePath", @"C:\a.txt")));

		Assert.Single(result.Sources);
		ServiceMessage warning = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Warning, warning.Level);
		Assert.Contains($"'{key}'", warning.Text, StringComparison.Ordinal);
	}

	[Fact]
	public void an_unknown_field_of_a_source_is_ignored_with_a_warning()
	{
		VnasAliasSettingsParseResult result = VnasAliasSettingsParser.Parse(Block(
			("Sources.1.FilePath", @"C:\a.txt"),
			("Sources.1.Branch", "main")));

		Assert.Single(result.Sources);
		Assert.Contains("'Sources.1.Branch'", Assert.Single(result.Messages).Text, StringComparison.Ordinal);
	}

	[Fact]
	public void a_credential_on_a_file_is_ignored_with_a_notice()
	{
		VnasAliasSettingsParseResult result = VnasAliasSettingsParser.Parse(Block(
			("Sources.1.FilePath", @"C:\a.txt"),
			("Sources.1.CredentialId", CredentialId.ToString("N"))));

		Assert.Null(Assert.Single(result.Sources).CredentialId);
		Assert.Equal(LogLevel.Info, Assert.Single(result.Messages).Level);
	}

	[Theory]
	[InlineData("Sources.1.FilePath", @"C:\a.txt", "Sources.1.Url", "https://example.com/a.txt", "both a file path and a web address")]
	[InlineData("Sources.1.CredentialId", "0f8fad5bd9cb469fa16570867728950e", "Sources.9.FilePath", @"C:\b.txt", "has no file path or web address")]
	[InlineData("Sources.1.FilePath", @"a.txt", "Sources.9.FilePath", @"C:\b.txt", "is not a full path")]
	[InlineData("Sources.1.FilePath", @"\a.txt", "Sources.9.FilePath", @"C:\b.txt", "is not a full path")]
	[InlineData("Sources.1.Url", "ftp://example.com/a.txt", "Sources.9.FilePath", @"C:\b.txt", "is not an http:// or https:// address")]
	[InlineData("Sources.1.Url", "example.com/a.txt", "Sources.9.FilePath", @"C:\b.txt", "is not an http:// or https:// address")]
	[InlineData("Sources.1.Url", "https://example.com/a.txt", "Sources.1.CredentialId", "my token", "is not a credential id")]
	public void a_bad_source_is_refused(string key1, string value1, string key2, string value2, string expected)
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => VnasAliasSettingsParser.Parse(Block((key1, value1), (key2, value2))));

		Assert.Contains(expected, ex.Message, StringComparison.Ordinal);
		Assert.StartsWith("Custom alias file 1", ex.Message, StringComparison.Ordinal);
	}

	/// <summary>An address with a secret written into it is refused - and the message never repeats the address.</summary>
	[Theory]
	[InlineData("https://raw.githubusercontent.com/o/r/main/a.txt?token=GHSAT0SECRET", "a sign-in token (token=)")]
	[InlineData("https://bob:SECRET@example.com/a.txt", "a user name and password")]
	public void an_address_with_a_secret_in_it_is_refused(string url, string expected)
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => VnasAliasSettingsParser.Parse(Block(("Sources.1.Url", url))));

		Assert.StartsWith($"Custom alias file 1's web address has {expected} in it.", ex.Message, StringComparison.Ordinal);
		Assert.DoesNotContain("SECRET", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void an_http_address_is_allowed_without_a_credential()
	{
		VnasAliasSettingsParseResult result = VnasAliasSettingsParser.Parse(Block(("Sources.1.Url", "http://example.com/a.txt")));

		Assert.Equal(AliasSourceKind.Url, Assert.Single(result.Sources).Kind);
	}

	[Fact]
	public void null_is_refused() =>
		Assert.Throws<ArgumentNullException>(() => VnasAliasSettingsParser.Parse(null!));

	private static Dictionary<string, string> Block(params (string Key, string Value)[] entries) =>
		entries.ToDictionary(e => e.Key, e => e.Value, StringComparer.OrdinalIgnoreCase);
}
