using FeBuddy.Core.Application.Airac.ConcatenateAliases;
using FeBuddy.Core.Application.Airac.ConcatenateAliases.Models;

namespace FeBuddy.UnitTests.Application.Airac.ConcatenateAliases;

/// <summary>Covers <see cref="AliasSourceRules"/>: what the tab shows under a custom alias row, and the run refuses it by.</summary>
public sealed class AliasSourceRulesTests
{
	[Theory]
	[InlineData(AliasSourceKind.File, "", false, "Choose the file.")]
	[InlineData(AliasSourceKind.File, "Alias.txt", false, "Give the file's full path")]
	[InlineData(AliasSourceKind.File, @"C:\Users\me\Alias.txt", true, null)]
	[InlineData(AliasSourceKind.Url, " ", false, "Enter the file's web address.")]
	[InlineData(AliasSourceKind.Url, "ftp://example.com/a.txt", false, "Enter a web address starting with https://.")]
	[InlineData(AliasSourceKind.Url, "https://github.com/Nikolai558/test-repo", false, "This is a GitHub page, not a file.")]
	[InlineData(AliasSourceKind.Url, "https://example.com/a.txt?token=abc", false, "This address has")]
	[InlineData(AliasSourceKind.Url, "http://example.com/a.txt", true, "A credential is only ever sent to an https:// address.")]
	[InlineData(AliasSourceKind.Url, "http://example.com/a.txt", false, null)]
	[InlineData(AliasSourceKind.Url, "http://github.com/vZOB/facility/blob/main/aliases/ZOB-Alias.txt", true, null)]
	[InlineData(AliasSourceKind.Url, "https://example.com/a.txt", true, null)]
	public void a_row_is_checked_before_anything_is_read(AliasSourceKind kind, string location, bool hasCredential, string? problem)
	{
		string? found = AliasSourceRules.Problem(kind, location, hasCredential);

		if (problem is null)
		{
			Assert.Null(found);
		}
		else
		{
			Assert.StartsWith(problem, found, StringComparison.Ordinal);
		}
	}
}
