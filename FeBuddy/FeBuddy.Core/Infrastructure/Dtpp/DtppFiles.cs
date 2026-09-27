namespace FeBuddy.Core.Infrastructure.Dtpp;

/// <summary>
/// The FAA d-TPP Metafile's file name and download source.
/// </summary>
public static class DtppFiles
{
	/// <summary>
	/// The file name, as published by the FAA - placed alongside the NASR CSVs in an AIRAC
	/// cycle's folder: <c>%APPDATA%\FE-Buddy\AiracCycles\&lt;cycleId&gt;\d-tpp_Metafile.xml</c>.
	/// </summary>
	public const string FileName = "d-tpp_Metafile.xml";

	/// <summary>
	/// The placeholder <see cref="Models.DtppMetafileXmlDataModel.Record.PdfName"/> the FAA uses
	/// on a deleted chart record.
	/// </summary>
	public const string DeletedChartPdfName = "DELETED_JOB.PDF";

	/// <summary>
	/// The placeholder <see cref="Models.DtppMetafileXmlDataModel.Record.PdfName"/> the FAA uses
	/// when an airport it used to serve no longer has this chart at all.
	/// </summary>
	public const string DeletedFromAirportPdfName = "DEL_APT_SERVED.PDF";

	/// <summary>
	/// Builds the FAA's download URL for a cycle's d-TPP Metafile, e.g.
	/// <c>https://aeronav.faa.gov/d-tpp/2609/xml_data/d-tpp_Metafile.xml</c>.
	/// </summary>
	/// <remarks>
	/// Unlike the NASR 28-day subscription CSVs, the FAA publishes this file only about 15-18
	/// days before the cycle's effective date, so a caller building a URL for the next cycle much
	/// earlier than that should expect the download to 404 until the FAA catches up.
	/// </remarks>
	/// <param name="airacCycleId">The AIRAC cycle id, e.g. <c>2609</c>.</param>
	/// <returns>The absolute download URL.</returns>
	public static string DownloadUrl(string airacCycleId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(airacCycleId);
		return $"https://aeronav.faa.gov/d-tpp/{airacCycleId}/xml_data/d-tpp_Metafile.xml";
	}

	/// <summary>
	/// Builds the FAA's download URL for one chart PDF, e.g.
	/// <c>https://aeronav.faa.gov/d-tpp/2609/05620R9.PDF</c>.
	/// </summary>
	/// <param name="airacCycleId">The AIRAC cycle id, e.g. <c>2609</c>.</param>
	/// <param name="pdfName">
	/// The chart's <see cref="Models.DtppMetafileXmlDataModel.Record.PdfName"/>, e.g.
	/// <c>05620R9.PDF</c>.
	/// </param>
	/// <returns>The absolute download URL.</returns>
	public static string ChartUrl(string airacCycleId, string pdfName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(airacCycleId);
		ArgumentException.ThrowIfNullOrWhiteSpace(pdfName);
		return $"https://aeronav.faa.gov/d-tpp/{airacCycleId}/{pdfName}";
	}

	/// <summary>
	/// Builds the FAA's download URL for a chart's compare PDF - the prior cycle's version of the
	/// same chart with changes marked - e.g.
	/// <c>https://aeronav.faa.gov/d-tpp/2609/compare_pdf/05620R9_cmp.pdf</c>.
	/// </summary>
	/// <param name="airacCycleId">The AIRAC cycle id, e.g. <c>2609</c>.</param>
	/// <param name="pdfName">
	/// The chart's <see cref="Models.DtppMetafileXmlDataModel.Record.PdfName"/>, e.g.
	/// <c>05620R9.PDF</c>.
	/// </param>
	/// <returns>The absolute download URL.</returns>
	public static string CompareUrl(string airacCycleId, string pdfName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(airacCycleId);
		ArgumentException.ThrowIfNullOrWhiteSpace(pdfName);
		string nameWithoutExtension = Path.GetFileNameWithoutExtension(pdfName);
		return $"https://aeronav.faa.gov/d-tpp/{airacCycleId}/compare_pdf/{nameWithoutExtension}_cmp.pdf";
	}

	/// <summary>
	/// Whether <paramref name="pdfName"/> is one of the FAA's placeholder names for a deleted
	/// chart record (<see cref="DeletedChartPdfName"/> or <see cref="DeletedFromAirportPdfName"/>)
	/// rather than a real chart PDF, checked case-insensitively.
	/// </summary>
	/// <param name="pdfName">The chart's <see cref="Models.DtppMetafileXmlDataModel.Record.PdfName"/>.</param>
	/// <returns><see langword="true"/> when the name is a deletion placeholder.</returns>
	public static bool IsPlaceholderPdf(string pdfName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(pdfName);
		return string.Equals(pdfName, DeletedChartPdfName, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(pdfName, DeletedFromAirportPdfName, StringComparison.OrdinalIgnoreCase);
	}
}
