using FeBuddy.Core.Infrastructure.Dtpp;

namespace FeBuddy.UnitTests.Infrastructure.Dtpp;

/// <summary>
/// Covers <see cref="DtppFiles"/>: the download/chart/compare URL formats, the placeholder PDF
/// names, and their argument checks.
/// </summary>
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

	// ---- ChartUrl ----

	[Fact]
	public void chart_url_is_built_from_the_cycle_id_and_pdf_name() =>
		Assert.Equal("https://aeronav.faa.gov/d-tpp/2609/05620R9.PDF", DtppFiles.ChartUrl("2609", "05620R9.PDF"));

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	public void chart_url_rejects_a_blank_cycle_id(string cycleId) =>
		Assert.Throws<ArgumentException>(() => DtppFiles.ChartUrl(cycleId, "05620R9.PDF"));

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	public void chart_url_rejects_a_blank_pdf_name(string pdfName) =>
		Assert.Throws<ArgumentException>(() => DtppFiles.ChartUrl("2609", pdfName));

	[Fact]
	public void chart_url_rejects_null_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => DtppFiles.ChartUrl(null!, "05620R9.PDF"));
		Assert.Throws<ArgumentNullException>(() => DtppFiles.ChartUrl("2609", null!));
	}

	// ---- CompareUrl ----

	[Fact]
	public void compare_url_strips_the_pdf_extension_and_appends_cmp() =>
		Assert.Equal("https://aeronav.faa.gov/d-tpp/2609/compare_pdf/05620R9_cmp.pdf", DtppFiles.CompareUrl("2609", "05620R9.PDF"));

	[Fact]
	public void compare_url_handles_a_pdf_name_with_no_extension() =>
		Assert.Equal("https://aeronav.faa.gov/d-tpp/2609/compare_pdf/AKTO_cmp.pdf", DtppFiles.CompareUrl("2609", "AKTO"));

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	public void compare_url_rejects_a_blank_cycle_id(string cycleId) =>
		Assert.Throws<ArgumentException>(() => DtppFiles.CompareUrl(cycleId, "05620R9.PDF"));

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	public void compare_url_rejects_a_blank_pdf_name(string pdfName) =>
		Assert.Throws<ArgumentException>(() => DtppFiles.CompareUrl("2609", pdfName));

	[Fact]
	public void compare_url_rejects_null_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => DtppFiles.CompareUrl(null!, "05620R9.PDF"));
		Assert.Throws<ArgumentNullException>(() => DtppFiles.CompareUrl("2609", null!));
	}

	// ---- placeholder PDF names ----

	[Fact]
	public void placeholder_pdf_names_are_the_faa_published_names()
	{
		Assert.Equal("DELETED_JOB.PDF", DtppFiles.DeletedChartPdfName);
		Assert.Equal("DEL_APT_SERVED.PDF", DtppFiles.DeletedFromAirportPdfName);
	}

	[Theory]
	[InlineData("DELETED_JOB.PDF")]
	[InlineData("deleted_job.pdf")]
	[InlineData("DEL_APT_SERVED.PDF")]
	[InlineData("del_apt_served.pdf")]
	public void is_placeholder_pdf_is_true_for_either_placeholder_case_insensitively(string pdfName) =>
		Assert.True(DtppFiles.IsPlaceholderPdf(pdfName));

	[Theory]
	[InlineData("05620R9.PDF")]
	[InlineData("AKTO.PDF")]
	public void is_placeholder_pdf_is_false_for_a_real_chart(string pdfName) =>
		Assert.False(DtppFiles.IsPlaceholderPdf(pdfName));

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	public void is_placeholder_pdf_rejects_a_blank_pdf_name(string pdfName) =>
		Assert.Throws<ArgumentException>(() => DtppFiles.IsPlaceholderPdf(pdfName));

	[Fact]
	public void is_placeholder_pdf_rejects_a_null_pdf_name() =>
		Assert.Throws<ArgumentNullException>(() => DtppFiles.IsPlaceholderPdf(null!));
}
