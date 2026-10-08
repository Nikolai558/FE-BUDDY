namespace FeBuddy.Core.Infrastructure.SharedData.Models;

/// <summary>
/// What happened when FE-Buddy tried to download a fresh copy of a data file it keeps one copy of,
/// outside any AIRAC cycle (see <see cref="SharedDataDownload"/>): which copy a run should use, and
/// how old it is.
/// </summary>
/// <param name="FilePath">
/// The copy to use: the fresh download, or the last good copy when the download failed;
/// <see langword="null"/> when there is neither.
/// </param>
/// <param name="DownloadedUtc">When that copy was downloaded, or <see langword="null"/> when there is no copy.</param>
/// <param name="FailureReason">
/// Why the fresh download failed, e.g. the HTTP error; <see langword="null"/> when it succeeded.
/// </param>
public sealed record SharedDataRefreshResult(string? FilePath, DateTime? DownloadedUtc, string? FailureReason)
{
	/// <summary>
	/// Whether the kept copy was used without downloading, because it was new enough
	/// (see <see cref="SharedDataDownload.RefreshAsync(string, string, Action{string, string}, Action{string}, string, TimeSpan?, CancellationToken)"/>).
	/// </summary>
	public bool Reused { get; init; }

	/// <summary>Whether the copy to use was downloaded just now.</summary>
	public bool IsFresh => FilePath is not null && FailureReason is null && !Reused;

	/// <summary>Whether there is a copy to use at all, fresh or not.</summary>
	public bool HasCopy => FilePath is not null;
}
