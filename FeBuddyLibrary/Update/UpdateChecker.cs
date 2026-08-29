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
        // Exposed (not private) because UpdateInstaller needs the same owner/repo to build
        // the authenticated releases-assets API URL for its own unauthenticated-then-token
        // fallback.
        public const string RepoOwner = "Nikolai558";
        public const string RepoName = "FE-BUDDY";

        private const string ReleasesApiUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases";

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
        /// Convenience wrapper for the Squirrel -> MSI migration trigger: returns the
        /// newest release at or above <paramref name="minimumChannel"/> that has a .msi
        /// asset, with NO comparison against any installed version. A Squirrel install
        /// migrating to the MSI is often on the exact same version number as the latest
        /// release (e.g. both 2.8.4), so "is it newer" - the rule CheckForUpdateAsync
        /// applies - would wrongly return nothing. Here the question is only "what is the
        /// current MSI release for this channel."
        /// </summary>
        public static async Task<UpdateCandidate> GetLatestForChannelAsync(ReleaseChannel minimumChannel)
        {
            var releases = await FetchReleasesAsync().ConfigureAwait(false);
            return SelectLatestForChannel(releases, minimumChannel);
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

        /// <summary>
        /// Pure selection logic for GetLatestForChannelAsync, exposed separately so it
        /// can be tested against a hand-built release list. Same shape as
        /// SelectLatestStable, but keeps any release whose channel is >= minimumChannel
        /// (not just exactly Stable) and never looks at an installed version.
        /// </summary>
        public static UpdateCandidate SelectLatestForChannel(
            IEnumerable<GitHubRelease> releases,
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
                    continue;
                }

                if (candidateVersion.Channel < minimumChannel)
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
                MsiAssetId = msiAsset.Id,
                MsiFileName = msiAsset.Name,
                MsiSizeBytes = msiAsset.Size,
            };

        /// <summary>
        /// Tries the releases list unauthenticated first (the normal, expected path for
        /// FE-BUDDY's real public repo). Only if that fails, and only if
        /// FEBUDDY_GITHUB_TOKEN is set, retries once with it attached - see GitHubAuth for
        /// why this is a fallback rather than always-sent.
        /// </summary>
        private static async Task<List<GitHubRelease>> FetchReleasesAsync()
        {
            try
            {
                return await FetchReleasesAsync(authToken: null).ConfigureAwait(false);
            }
            catch (HttpRequestException) when (GitHubAuth.GetOptionalToken() is { } token)
            {
                Logger.LogMessage("INFO", $"UpdateChecker: unauthenticated release check failed, retrying with {GitHubAuth.EnvironmentVariableName}.");
                return await FetchReleasesAsync(authToken: token).ConfigureAwait(false);
            }
        }

        private static async Task<List<GitHubRelease>> FetchReleasesAsync(string authToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesApiUrl);
            // GitHub's API rejects requests with no User-Agent; Accept pins the response shape.
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("FE-BUDDY-Updater", "1.0"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            if (authToken != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            }

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
