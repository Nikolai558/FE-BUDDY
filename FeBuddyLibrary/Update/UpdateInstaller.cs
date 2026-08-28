using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using FeBuddyLibrary.Helpers;

namespace FeBuddyLibrary.Update
{
    /// <summary>
    /// Downloads an UpdateCandidate's .msi and launches it. Deliberately does not wait for
    /// the installer to finish, or do anything silent/background - per the agreed design,
    /// the MSI runs its own normal interactive UI, and the caller is expected to exit the
    /// app immediately after LaunchInstaller returns (the MSI can't safely replace this
    /// process's own files while it's still running - see the file-locking discussion this
    /// design came out of).
    /// </summary>
    public static class UpdateInstaller
    {
        public static async Task<string> DownloadAsync(
            UpdateCandidate candidate,
            string destinationDirectory,
            IProgress<DownloadProgressInfo> progress,
            CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(destinationDirectory);
            var destinationPath = Path.Combine(destinationDirectory, candidate.MsiFileName);

            using var request = new HttpRequestMessage(HttpMethod.Get, candidate.MsiDownloadUrl);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("FE-BUDDY-Updater", "1.0"));

            using var response = await SharedHttp.Client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength
                ?? (candidate.MsiSizeBytes > 0 ? candidate.MsiSizeBytes : (long?)null);

            using var httpStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;
            while ((bytesRead = await httpStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                totalRead += bytesRead;
                progress?.Report(new DownloadProgressInfo { BytesReceived = totalRead, TotalBytes = totalBytes });
            }

            Logger.LogMessage("INFO", $"UpdateInstaller: downloaded {candidate.Version} to {destinationPath} ({totalRead} bytes)");
            return destinationPath;
        }

        /// <summary>
        /// Launches the downloaded MSI in its normal interactive installer UI, elevated.
        /// Throws System.ComponentModel.Win32Exception (NativeErrorCode 1223) if the user
        /// declines the UAC prompt - callers should handle that as a cancellation, not a
        /// generic failure.
        /// </summary>
        public static void LaunchInstaller(string msiPath)
        {
            Logger.LogMessage("INFO", $"UpdateInstaller: launching {msiPath}");

            var psi = new ProcessStartInfo
            {
                FileName = "msiexec",
                Arguments = $"/i \"{msiPath}\"",
                UseShellExecute = true,
                Verb = "runas",
            };

            Process.Start(psi);
        }
    }
}
