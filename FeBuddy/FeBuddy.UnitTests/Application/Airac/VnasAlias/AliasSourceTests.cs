using FeBuddy.Core.Application.Airac.VnasAlias.Models;

namespace FeBuddy.UnitTests.Application.Airac.VnasAlias;

/// <summary>Covers how messages name a custom alias file: its number and file name, never the whole address.</summary>
public sealed class AliasSourceTests
{
	[Theory]
	[InlineData(AliasSourceKind.File, @"C:\Users\me\ZOB-Alias.txt", "ZOB-Alias.txt")]
	[InlineData(AliasSourceKind.Url, "https://github.com/o/r/blob/main/My%20Aliases.txt?ref=x", "My Aliases.txt")]
	[InlineData(AliasSourceKind.Url, "https://example.com/folder/", "folder")]
	[InlineData(AliasSourceKind.Url, "https://example.com", "example.com")]
	[InlineData(AliasSourceKind.Url, "not an address", "not an address")]
	public void the_file_name_is_the_last_part_of_its_path_or_address(AliasSourceKind kind, string location, string expected)
	{
		AliasSource source = new(3, kind, location);

		Assert.Equal(expected, source.FileName);
		Assert.Equal($"custom alias file 3 ({expected})", source.DisplayName);
	}
}
