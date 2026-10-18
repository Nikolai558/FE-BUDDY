namespace FeBuddy.Core.Infrastructure.Dtpp.Models;

/// <summary>
/// What <see cref="DtppDownloader.EnsureCycleHasMetafileAsync"/> did for one cycle folder.
/// </summary>
public enum DtppDownloadOutcome
{
	/// <summary>The cycle folder already had the metafile; nothing was downloaded.</summary>
	AlreadyPresent = 0,

	/// <summary>The metafile was downloaded and placed in the cycle folder.</summary>
	Downloaded = 1,

	/// <summary>
	/// The FAA's server has no Metafile for this cycle (the download 404'd): normal for the next cycle
	/// until the FAA posts it, approximately 20 days prior to its effective date. Never an error.
	/// </summary>
	NotYetPublished = 2,
}
