namespace FEBuddyLibrary.Models.Services.Airac;

/// <summary>Which stage a NASR cycle download is currently in.</summary>
public enum AiracDownloadPhase
{
	/// <summary>Already downloaded and extracted in a previous run; nothing to do.</summary>
	AlreadyAvailable,

	/// <summary>Downloading the cycle's zip file from the FAA.</summary>
	Downloading,

	/// <summary>Extracting the downloaded zip into the local cycle cache.</summary>
	Extracting,

	/// <summary>The cycle's data is downloaded, extracted, and ready to use.</summary>
	Complete
}

/// <summary>
/// A progress update from <c>NasrCycleDownloadService.EnsureCycleAvailableAsync</c>.
/// </summary>
/// <param name="Phase">Which stage the download is in.</param>
/// <param name="PercentComplete">
/// 0-100 while <see cref="Phase"/> is <see cref="AiracDownloadPhase.Downloading"/> and the
/// server reported a content length; otherwise <see langword="null"/> (the FAA server does
/// not always send a Content-Length header, and extraction has no meaningful percentage).
/// </param>
public sealed record AiracDownloadProgress(AiracDownloadPhase Phase, double? PercentComplete);
