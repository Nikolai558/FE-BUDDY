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
}
