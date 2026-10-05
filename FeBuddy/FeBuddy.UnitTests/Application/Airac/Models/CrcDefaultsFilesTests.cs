using FeBuddy.Core.Application.Airac.Models;

namespace FeBuddy.UnitTests.Application.Airac.Models;

/// <summary>Covers <see cref="CrcDefaultsFiles"/>: none by default, and keys match ignoring case.</summary>
public sealed class CrcDefaultsFilesTests
{
	[Fact]
	public void none_marks_nothing()
	{
		Assert.Empty(CrcDefaultsFiles.None.Files);
		Assert.False(CrcDefaultsFiles.None.HasCrcDefaults("Airways_High_Lines"));
	}

	[Fact]
	public void keys_match_ignoring_case()
	{
		CrcDefaultsFiles files = new(["AIRWAYS_HIGH_LINES"]);

		Assert.True(files.HasCrcDefaults("Airways_High_Lines"));
		Assert.False(files.HasCrcDefaults("Airways_Low_Lines"));
		Assert.Throws<ArgumentNullException>(() => new CrcDefaultsFiles(null!));
	}
}
