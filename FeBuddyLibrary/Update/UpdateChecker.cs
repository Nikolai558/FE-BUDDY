using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FeBuddyLibrary.Helpers;
using FeBuddy.Versioning;
using Newtonsoft.Json;

namespace FeBuddyLibrary.Update
{
    /// <summary>
    /// Checks GitHub Releases for an update to FE-BUDDY, respecting the caller's
    /// minimum acceptable release channel (see FeBuddy.Versioning.ReleaseChannel).
    ///
    /// This replaces Clowd.Squirrel's GithubSource for MSI-based installs: unlike
    /// Squirrel, it talks to the real GitHub Releases API directly (not a nupkg/
    /// RELEASES-manifest convention), and it understands FE-BUDDY's real semantic
    /// version - including pre-release channels - via FeBuddy.Versioning, the same
    /// library the installer's own version-policy custom action uses. That's a
    /// second consumer of it, proving out the "one implementation of the rule"
    /// goal from that design.
    ///
    /// The selection logic (SelectBestUpdate / SelectLatestStable) is deliberately
    /// separated from the network fetch so it can be exercised directly against a
    /// hand-built release list, without depending on live GitHub state.
    /// </summary>
    public static class UpdateChecker
    {
        private const string ReleasesApiUrl = "https://api.github.com/repos/Nikolai558/FE-BUDDY/releases";

        /// <summary>
        /// Checks for an update. Returns null if none is available (including if
        /// the installed version is already the newest eligible release). Throws
        /// on network/API failures - callers should catch and surface those the
        /// same way Program.cs's existing CheckForUpdates() already does.
        /// </summary>
        /// <param name="installedVersionText">The real installed version, e.g. "2.8.4-beta.1".</param>
        /// <param name="minimumChannel">
        /// The lowest channel the caller will accept. Releases below this channel
        /// are excluded entirely - never offered as a fallback even if nothing at
        /// the requested channel exists yet for the newest in-progress version.
        /// </param>
        public static async Task<UpdateCandidate> CheckForUpdateAsync(
            string installedVersionText,
            ReleaseChannel minimumChannel)
        {
            var installed = ProductVersion.Parse(installedVersionText);
            var releases = await FetchReleasesAsync().ConfigureAwait(false);
            return SelectBestUpdate(releases, installed, minimumChannel);
        }

        /// <summary>
        /// Convenience wrapper for the "revert to latest stable" action - always
        /// returns the newest stable release found, regardless of whether it's
        /// numerically newer or older than what's installed (a revert TO stable
        /// FROM a newer-precedence pre-release, e.g. installed 2.9.0-rc.1 with
        /// latest stable 2.8.4, is exactly the case this action exists for). Note
        /// this does NOT enforce the installer's downgrade rule itself - that only
        /// matters once the result is handed to the MSI, whose own
        /// EnforceVersionPolicy custom action is what actually decides whether
        /// that specific transition is allowed. This method's job is only "what
        /// is the current stable version."
        /// </summary>
        public static async Task<UpdateCandidate> GetLatestStableAsync(string installedVersionText)
        {
            _ = ProductVersion.Parse(installedVersionText); // validates the input; not otherwise used here
            var releases = await FetchReleasesAsync().ConfigureAwait(false);
            return SelectLatestStable(releases);
        }

        /// <summary>
        /// Pure selection logic for CheckForUpdateAsync, exposed separately so it
        /// can be tested against a hand-built release list.
        /// </summary>
        public static UpdateCandidate SelectBestUpdate(
            IEnumerable<GitHubRelease> releases,
            ProductVersion installed,
            ReleaseChannel minimumChannel)
        {
            UpdateCandidate best = null;
            ProductVersion bestVersion = null;

            foreach (var release in releases ?? Enumerable.Empty<GitHubRelease>())
            {
                if (release.Draft)
                {
                    continue;
                }

                if (!ProductVersion.TryParse(NormalizeTag(release.TagName), out var candidateVersion) || candidateVersion is null)
                {
                    Logger.LogMessage("WARNING", $"UpdateChecker: could not parse release tag '{release.TagName}' as a version - skipping.");
                    continue;
                }

                if (candidateVersion.Channel < minimumChannel)
                {
                    // Below the caller's minimum channel - never offered, not even as
                    // a fallback when nothing at the requested channel exists yet.
                    continue;
                }

                if (candidateVersion.ComparePrecedenceTo(installed) <= 0)
                {
                    // Not newer than what's already installed.
                    continue;
                }

                if (bestVersion != null && candidateVersion.ComparePrecedenceTo(bestVersion) <= 0)
                {
                    // Already found something at least as new.
                    continue;
                }

                var msiAsset = FindMsiAsset(release);
                if (msiAsset == null)
                {
                    Logger.LogMessage("WARNING", $"UpdateChecker: release '{release.TagName}' is eligible but has no .msi asset - skipping.");
                    continue;
                }

                bestVersion = candidateVersion;
                best = ToCandidate(candidateVersion, release, msiAsset);
            }

            return best;
        }

        /// <summary>
        /// Pure selection logic for GetLatestStableAsync, exposed separately so it
        /// can be tested against a hand-built release list.
        /// </summary>
        public static UpdateCandidate SelectLatestStable(IEnumerable<GitHubRelease> releases)
        {
            UpdateCandidate best = null;
            ProductVersion bestVersion = null;

            foreach (var release in releases ?? Enumerable.Empty<GitHubRelease>())
            {
                if (release.Draft)
                {
                    continue;
                }

                if (!ProductVersion.TryParse(NormalizeTag(release.TagName), out var candidateVersion) || candidateVersion is null)
                {
                    continue;
                }

                if (candidateVersion.Channel != ReleaseChannel.Stable)
                {
                    continue;
                }

                if (bestVersion != null && candidateVersion.ComparePrecedenceTo(bestVersion) <= 0)
                {
                    continue;
                }

                var msiAsset = FindMsiAsset(release);
                if (msiAsset == null)
                {
                    continue;
                }

                bestVersion = candidateVersion;
                best = ToCandidate(candidateVersion, release, msiAsset);
            }

            return best;
        }

        private static GitHubReleaseAsset FindMsiAsset(GitHubRelease release) =>
            release.Assets?.FirstOrDefault(a =>
                a.Name != null && a.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase));

        private static UpdateCandidate ToCandidate(ProductVersion version, GitHubRelease release, GitHubReleaseAsset msiAsset) =>
            new UpdateCandidate
            {
                Version = version.ToString(),
                ReleaseName = release.Name,
                ReleaseNotes = release.Body,
                MsiDownloadUrl = msiAsset.BrowserDownloadUrl,
                MsiFileName = msiAsset.Name,
                MsiSizeBytes = msiAsset.Size,
            };

        private static async Task<List<GitHubRelease>> FetchReleasesAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesApiUrl);
            // GitHub's API rejects requests with no User-Agent; Accept pins the response shape.
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("FE-BUDDY-Updater", "1.0"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using var response = await SharedHttp.Client.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<List<GitHubRelease>>(json) ?? new List<GitHubRelease>();
        }

        private static string NormalizeTag(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return tagName;
            }

            // Tolerate a "v" prefix (e.g. "v2.8.3") even though this repo's tags
            // don't currently use one - cheap to handle, avoids a surprise later.
            return tagName.Length > 1 && (tagName[0] == 'v' || tagName[0] == 'V') && char.IsDigit(tagName[1])
                ? tagName.Substring(1)
                : tagName;
        }
    }
}
