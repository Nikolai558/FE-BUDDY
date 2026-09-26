using FeBuddy.Core.Infrastructure.Dtpp;

namespace FeBuddy.UnitTests.Infrastructure.Dtpp;

/// <summary>Covers <see cref="DtppFiles"/>: the download URL format and its argument check.</summary>
public sealed class DtppFilesTests
{
	[Fact]
	public void download_url_is_built_from_the_cycle_id() =>
		Assert.Equal("https://aeronav.faa.gov/d-tpp/2609/xml_data/d-tpp_Metafile.xml", DtppFiles.DownloadUrl("2609"));

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	public void download_url_rejects_a_blank_cycle_id(string cycleId) =>
		Assert.Throws<ArgumentException>(() => DtppFiles.DownloadUrl(cycleId));

	[Fact]
	public void download_url_rejects_a_null_cycle_id() =>
		Assert.Throws<ArgumentNullException>(() => DtppFiles.DownloadUrl(null!));

	[Fact]
	public void file_name_is_the_faa_published_name() =>
		Assert.Equal("d-tpp_Metafile.xml", DtppFiles.FileName);
}
