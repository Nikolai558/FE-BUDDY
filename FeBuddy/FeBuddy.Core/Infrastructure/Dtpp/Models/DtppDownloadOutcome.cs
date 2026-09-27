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
	/// The FAA has not published this cycle's metafile yet (the download 404'd) - normal for a
	/// cycle whose effective date is more than about 15-18 days away, never an error.
	/// </summary>
	NotYetPublished = 2,
}
