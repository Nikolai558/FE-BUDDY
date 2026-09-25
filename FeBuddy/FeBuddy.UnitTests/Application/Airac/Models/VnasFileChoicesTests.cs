using FeBuddy.Core.Application.Airac.Models;

namespace FeBuddy.UnitTests.Application.Airac.Models;

/// <summary>
/// Covers <see cref="VnasFileChoices"/>: keys match ignoring case, and a file can only get CRC
/// defaults if it is also marked for vNAS.
/// </summary>
public sealed class VnasFileChoicesTests
{
	[Fact]
	public void none_marks_nothing()
	{
		Assert.Empty(VnasFileChoices.None.UploadFiles);
		Assert.Empty(VnasFileChoices.None.CrcDefaultsFiles);
		Assert.False(VnasFileChoices.None.IsUploaded("Airways.txt"));
	}

	[Fact]
	public void keys_match_ignoring_case()
	{
		VnasFileChoices choices = new(["Airways_High_Lines", "Airways.txt"], ["AIRWAYS_HIGH_LINES"]);

		Assert.True(choices.IsUploaded("airways_high_lines"));
		Assert.True(choices.IsUploaded("AIRWAYS.TXT"));
		Assert.True(choices.HasCrcDefaults("Airways_High_Lines"));
		Assert.False(choices.HasCrcDefaults("Airways.txt"));
	}

	[Fact]
	public void a_crc_defaults_file_must_also_be_marked_for_vnas()
	{
		Assert.Throws<ArgumentException>(() => new VnasFileChoices(["Airways_High_Lines"], ["Airways_Low_Lines"]));
		Assert.Throws<ArgumentNullException>(() => new VnasFileChoices(null!, []));
		Assert.Throws<ArgumentNullException>(() => new VnasFileChoices([], null!));
	}
}
