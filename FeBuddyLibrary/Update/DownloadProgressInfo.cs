namespace FeBuddyLibrary.Update
{
    /// <summary>Progress snapshot reported while UpdateInstaller.DownloadAsync runs.</summary>
    public class DownloadProgressInfo
    {
        public long BytesReceived { get; set; }

        /// <summary>Null if the server didn't report a size and GitHub's asset size wasn't known either.</summary>
        public long? TotalBytes { get; set; }

        public int? PercentComplete =>
            TotalBytes.HasValue && TotalBytes.Value > 0
                ? (int)(BytesReceived * 100 / TotalBytes.Value)
                : null;
    }
}
