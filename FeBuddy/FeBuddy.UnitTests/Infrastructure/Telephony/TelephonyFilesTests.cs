using FeBuddy.Core.Infrastructure.Telephony;

namespace FeBuddy.UnitTests.Infrastructure.Telephony;

/// <summary>
/// Covers <see cref="TelephonyFiles"/>: the two kept file names, the FAA URLs they are downloaded
/// from, and that the kept copies' paths land under the shared Telephony folder.
/// </summary>
public sealed class TelephonyFilesTests
{
	[Fact]
	public void file_names_are_the_documented_names()
	{
		Assert.Equal("telephony_register.html", TelephonyFiles.RegisterFileName);
		Assert.Equal("us_special_call_signs.html", TelephonyFiles.SpecialCallSignsFileName);
	}

	[Fact]
	public void urls_are_the_faa_chapter_3_pages()
	{
		Assert.Equal(
			"https://www.faa.gov/air_traffic/publications/atpubs/cnt_html/chap3_section_1.html",
			TelephonyFiles.RegisterUrl);
		Assert.Equal(
			"https://www.faa.gov/air_traffic/publications/atpubs/cnt_html/chap3_section_4.html",
			TelephonyFiles.SpecialCallSignsUrl);
	}

	[Fact]
	public void shared_directory_ends_with_the_telephony_folder() =>
		Assert.EndsWith(Path.Combine("FE-Buddy", "Telephony"), TelephonyFiles.SharedDirectory, StringComparison.Ordinal);

	[Fact]
	public void register_file_path_ends_with_the_telephony_register_html_path() =>
		Assert.EndsWith(
			Path.Combine("FE-Buddy", "Telephony", "telephony_register.html"),
			TelephonyFiles.RegisterFilePath,
			StringComparison.Ordinal);

	[Fact]
	public void special_call_signs_file_path_ends_with_the_us_special_call_signs_html_path() =>
		Assert.EndsWith(
			Path.Combine("FE-Buddy", "Telephony", "us_special_call_signs.html"),
			TelephonyFiles.SpecialCallSignsFilePath,
			StringComparison.Ordinal);
}
