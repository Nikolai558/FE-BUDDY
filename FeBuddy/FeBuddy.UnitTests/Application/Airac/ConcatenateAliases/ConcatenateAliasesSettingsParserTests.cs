using FeBuddy.Core.Application.Airac.ConcatenateAliases;
using FeBuddy.Core.Application.Airac.ConcatenateAliases.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Application.Airac.ConcatenateAliases;

/// <summary>
/// Covers <see cref="ConcatenateAliasesSettingsParser"/>: combining, on unless turned off; numbered custom
/// alias files, each a file on this PC or a web address with an optional credential id, merged in
/// number order and not read at all while combining is off; and the settings it refuses.
/// </summary>
public sealed class ConcatenateAliasesSettingsParserTests
{
	private static readonly Guid CredentialId = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");

	[Fact]
	public void sources_are_read_in_number_order()
	{
		ConcatenateAliasesSettingsParseResult result = ConcatenateAliasesSettingsParser.Parse(Block(
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
		Assert.True(result.Combine);
	}

	[Fact]
	public void keys_match_ignoring_case()
	{
		ConcatenateAliasesSettingsParseResult result = ConcatenateAliasesSettingsParser.Parse(Block(
			("sources.1.url", "https://example.com/a.txt"),
			("SOURCES.1.CREDENTIALID", CredentialId.ToString())));

		Assert.Equal(new AliasSource(1, AliasSourceKind.Url, "https://example.com/a.txt", CredentialId), Assert.Single(result.Sources));
	}

	/// <summary>Uploading FE-Buddy's aliases alone would remove the facility's own from vNAS, so the Review tab says so.</summary>
	[Fact]
	public void no_sources_warns_that_only_fe_buddy_aliases_are_written()
	{
		ConcatenateAliasesSettingsParseResult result = ConcatenateAliasesSettingsParser.Parse(Block(("Sources.1.FilePath", " "), ("Sources.1.Url", "")));

		Assert.Empty(result.Sources);
		ServiceMessage warning = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Warning, warning.Level);
		Assert.True(warning.IsAdvisory);
		Assert.StartsWith("No custom alias files are set, so Combined_Alias.txt holds only FE-Buddy's aliases.", warning.Text, StringComparison.Ordinal);
		Assert.EndsWith("add your facility's alias file on the Concatenate Aliases tab.", warning.Text, StringComparison.Ordinal);
	}

	[Fact]
	public void combining_is_on_unless_turned_off()
	{
		Assert.True(ConcatenateAliasesSettingsParser.CombinesAliasFiles(Block()));
		Assert.True(ConcatenateAliasesSettingsParser.CombinesAliasFiles(Block(("CombineAliasFiles", " "))));
		Assert.True(ConcatenateAliasesSettingsParser.CombinesAliasFiles(Block(("combinealiasfiles", "y"))));
		Assert.False(ConcatenateAliasesSettingsParser.CombinesAliasFiles(Block(("CombineAliasFiles", "N"))));
		Assert.Throws<ArgumentException>(() => ConcatenateAliasesSettingsParser.CombinesAliasFiles(Block(("CombineAliasFiles", "maybe"))));
		Assert.Throws<ArgumentNullException>(() => ConcatenateAliasesSettingsParser.CombinesAliasFiles(null!));
	}

	/// <summary>Off, nothing reads the custom alias files, so they are neither returned nor checked.</summary>
	[Fact]
	public void with_combining_off_the_custom_files_are_not_read()
	{
		ConcatenateAliasesSettingsParseResult result = ConcatenateAliasesSettingsParser.Parse(Block(
			("CombineAliasFiles", "N"),
			("Sources.1.FilePath", "not a full path"),
			("Colour", "x")));

		Assert.False(result.Combine);
		Assert.Empty(result.Sources);
		Assert.Contains("'Colour'", Assert.Single(result.Messages).Text, StringComparison.Ordinal);
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
		ConcatenateAliasesSettingsParseResult result = ConcatenateAliasesSettingsParser.Parse(Block(
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
		ConcatenateAliasesSettingsParseResult result = ConcatenateAliasesSettingsParser.Parse(Block(
			("Sources.1.FilePath", @"C:\a.txt"),
			("Sources.1.Branch", "main")));

		Assert.Single(result.Sources);
		Assert.Contains("'Sources.1.Branch'", Assert.Single(result.Messages).Text, StringComparison.Ordinal);
	}

	[Fact]
	public void a_credential_on_a_file_is_ignored_with_a_notice()
	{
		ConcatenateAliasesSettingsParseResult result = ConcatenateAliasesSettingsParser.Parse(Block(
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
		ArgumentException ex = Assert.Throws<ArgumentException>(() => ConcatenateAliasesSettingsParser.Parse(Block((key1, value1), (key2, value2))));

		Assert.Contains(expected, ex.Message, StringComparison.Ordinal);
		Assert.StartsWith("Custom alias file 1", ex.Message, StringComparison.Ordinal);
	}

	/// <summary>An address with a secret written into it is refused - and the message never repeats the address.</summary>
	[Theory]
	[InlineData("https://raw.githubusercontent.com/o/r/main/a.txt?token=GHSAT0SECRET", "a sign-in token (token=)")]
	[InlineData("https://bob:SECRET@example.com/a.txt", "a user name and password")]
	public void an_address_with_a_secret_in_it_is_refused(string url, string expected)
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => ConcatenateAliasesSettingsParser.Parse(Block(("Sources.1.Url", url))));

		Assert.StartsWith($"Custom alias file 1's web address has {expected} in it.", ex.Message, StringComparison.Ordinal);
		Assert.DoesNotContain("SECRET", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void an_http_address_is_allowed_without_a_credential()
	{
		ConcatenateAliasesSettingsParseResult result = ConcatenateAliasesSettingsParser.Parse(Block(("Sources.1.Url", "http://example.com/a.txt")));

		Assert.Equal(AliasSourceKind.Url, Assert.Single(result.Sources).Kind);
	}

	[Fact]
	public void null_is_refused() =>
		Assert.Throws<ArgumentNullException>(() => ConcatenateAliasesSettingsParser.Parse(null!));

	private static Dictionary<string, string> Block(params (string Key, string Value)[] entries) =>
		entries.ToDictionary(e => e.Key, e => e.Value, StringComparer.OrdinalIgnoreCase);
}
