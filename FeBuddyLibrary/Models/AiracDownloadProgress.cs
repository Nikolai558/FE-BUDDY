namespace FeBuddyLibrary.Models
{
    /// <summary>
    /// Progress snapshot reported while <see cref="Helpers.DownloadHelpers.DownloadAllFiles"/>
    /// pulls the FAA source files. Mirrors the update-path DownloadProgressInfo, with the
    /// extra per-file context needed when a batch of files is fetched in sequence.
    /// </summary>
    public class AiracDownloadProgress
    {
        /// <summary>Friendly name of the file currently in flight, e.g. "APT.zip".</summary>
        public string FileName { get; set; }

        /// <summary>1-based position of the current file within the batch.</summary>
        public int FileIndex { get; set; }

        /// <summary>Total number of files in the batch.</summary>
        public int FileCount { get; set; }

        public long BytesReceived { get; set; }

        /// <summary>Null when the server didn't send Content-Length.</summary>
        public long? TotalBytes { get; set; }

        /// <summary>Percent of the current file, or null when its size isn't known.</summary>
        public int? CurrentFilePercent =>
            TotalBytes.HasValue && TotalBytes.Value > 0
                ? (int)(BytesReceived * 100 / TotalBytes.Value)
                : (int?)null;

        /// <summary>
        /// Percent across the whole batch: whole files already finished plus the fraction of
        /// the current one (0 when its size isn't known, so the bar simply steps per file).
        /// </summary>
        public int OverallPercent
        {
            get
            {
                if (FileCount <= 0)
                {
                    return 0;
                }

                double fileFraction = TotalBytes.HasValue && TotalBytes.Value > 0
                    ? (double)BytesReceived / TotalBytes.Value
                    : 0d;

                double overall = ((FileIndex - 1) + fileFraction) / FileCount;
                return (int)(overall * 100);
            }
        }
    }
}
