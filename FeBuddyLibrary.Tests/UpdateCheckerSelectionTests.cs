using System.Collections.Generic;
using FeBuddy.Versioning;
using FeBuddyLibrary.Update;
using Xunit;

namespace FeBuddyLibrary.Tests
{
    /// <summary>
    /// Covers UpdateChecker's pure selection logic (SelectBestUpdate / SelectLatestStable /
    /// SelectLatestForChannel) against hand-built release lists - no network. These decide
    /// whether a user is offered an update at all, and a silent bug here means either
    /// "no updates ever" or "everyone offered a build they didn't ask for".
    /// </summary>
    public class UpdateCheckerSelectionTests
    {
        private static GitHubRelease Release(string tag, bool draft = false, bool withMsi = true)
        {
            var release = new GitHubRelease
            {
                TagName = tag,
                Name = tag,
                Body = $"notes for {tag}",
                Draft = draft,
                Assets = new List<GitHubReleaseAsset>(),
            };

            if (withMsi)
            {
                release.Assets.Add(new GitHubReleaseAsset
                {
                    Id = 1,
                    Name = $"FE-BUDDY-{tag}.msi",
                    BrowserDownloadUrl = $"https://example.test/FE-BUDDY-{tag}.msi",
                    Size = 123,
                });
            }

            return release;
        }

        private static ProductVersion V(string text) => ProductVersion.Parse(text);

        // ---- SelectBestUpdate ------------------------------------------------

        [Fact]
        public void SelectBestUpdate_PicksHighestPrecedence_NotListOrder()
        {
            var releases = new[] { Release("2.8.5"), Release("2.9.0"), Release("2.8.6") };

            var best = UpdateChecker.SelectBestUpdate(releases, V("2.8.4"), ReleaseChannel.Stable);

            Assert.NotNull(best);
            Assert.Equal("2.9.0", best!.Version);
        }

        [Fact]
        public void SelectBestUpdate_ExcludesReleasesBelowMinimumChannel()
        {
            // Installed on stable; the only newer release is an alpha; caller wants Stable only.
            var releases = new[] { Release("2.9.0-alpha.1") };

            Assert.Null(UpdateChecker.SelectBestUpdate(releases, V("2.8.4"), ReleaseChannel.Stable));
        }

        [Fact]
        public void SelectBestUpdate_KeepsReleasesAtOrAboveMinimumChannel()
        {
            var releases = new[] { Release("2.9.0-alpha.1"), Release("2.9.0-beta.1") };

            var best = UpdateChecker.SelectBestUpdate(releases, V("2.8.4"), ReleaseChannel.Beta);

            Assert.NotNull(best);
            Assert.Equal("2.9.0-beta.1", best!.Version);
        }

        [Theory]
        [InlineData("2.8.4")] // same version
        [InlineData("2.8.3")] // older
        public void SelectBestUpdate_IgnoresReleaseNotStrictlyNewerThanInstalled(string releaseTag)
        {
            var releases = new[] { Release(releaseTag) };

            Assert.Null(UpdateChecker.SelectBestUpdate(releases, V("2.8.4"), ReleaseChannel.Stable));
        }

        [Fact]
        public void SelectBestUpdate_SkipsDraftReleases()
        {
            var releases = new[] { Release("2.9.0", draft: true), Release("2.8.5") };

            var best = UpdateChecker.SelectBestUpdate(releases, V("2.8.4"), ReleaseChannel.Stable);

            Assert.NotNull(best);
            Assert.Equal("2.8.5", best!.Version);
        }

        [Fact]
        public void SelectBestUpdate_SkipsReleasesWithNoMsiAsset()
        {
            var releases = new[] { Release("2.9.0", withMsi: false), Release("2.8.5") };

            var best = UpdateChecker.SelectBestUpdate(releases, V("2.8.4"), ReleaseChannel.Stable);

            Assert.NotNull(best);
            Assert.Equal("2.8.5", best!.Version);
        }

        [Fact]
        public void SelectBestUpdate_SkipsUnparseableTags()
        {
            var releases = new[] { Release("not-a-version"), Release("2.8.5") };

            var best = UpdateChecker.SelectBestUpdate(releases, V("2.8.4"), ReleaseChannel.Stable);

            Assert.NotNull(best);
            Assert.Equal("2.8.5", best!.Version);
        }

        [Fact]
        public void SelectBestUpdate_NormalizesLeadingVOnTag()
        {
            var releases = new[] { Release("v2.9.0") };

            var best = UpdateChecker.SelectBestUpdate(releases, V("2.8.4"), ReleaseChannel.Stable);

            Assert.NotNull(best);
            Assert.Equal("2.9.0", best!.Version);
        }

        [Fact]
        public void SelectBestUpdate_NoEligibleReleases_ReturnsNull()
        {
            Assert.Null(UpdateChecker.SelectBestUpdate(new GitHubRelease[0], V("2.8.4"), ReleaseChannel.Stable));
            Assert.Null(UpdateChecker.SelectBestUpdate(null, V("2.8.4"), ReleaseChannel.Stable));
        }

        [Fact]
        public void SelectBestUpdate_PopulatesCandidateFromReleaseAndMsiAsset()
        {
            var best = UpdateChecker.SelectBestUpdate(new[] { Release("2.9.0") }, V("2.8.4"), ReleaseChannel.Stable);

            Assert.NotNull(best);
            Assert.Equal("2.9.0", best!.Version);
            Assert.Equal("2.9.0", best.ReleaseName);
            Assert.Equal("notes for 2.9.0", best.ReleaseNotes);
            Assert.Equal("https://example.test/FE-BUDDY-2.9.0.msi", best.MsiDownloadUrl);
            Assert.Equal("FE-BUDDY-2.9.0.msi", best.MsiFileName);
            Assert.Equal(1, best.MsiAssetId);
            Assert.Equal(123, best.MsiSizeBytes);
        }

        // ---- SelectLatestStable -------------------------------------------------

        [Fact]
        public void SelectLatestStable_ReturnsNewestStable_IgnoringPrereleases()
        {
            var releases = new[] { Release("2.8.4"), Release("2.9.0-rc.1"), Release("2.8.6") };

            var best = UpdateChecker.SelectLatestStable(releases);

            Assert.NotNull(best);
            Assert.Equal("2.8.6", best!.Version);
        }

        [Fact]
        public void SelectLatestStable_NoStableRelease_ReturnsNull()
        {
            var releases = new[] { Release("2.9.0-rc.1"), Release("2.9.0-beta.2") };

            Assert.Null(UpdateChecker.SelectLatestStable(releases));
        }

        [Fact]
        public void SelectLatestStable_SkipsStableReleaseWithNoMsiAsset()
        {
            var releases = new[] { Release("2.9.0", withMsi: false), Release("2.8.6") };

            var best = UpdateChecker.SelectLatestStable(releases);

            Assert.NotNull(best);
            Assert.Equal("2.8.6", best!.Version);
        }

        // ---- SelectLatestForChannel -------------------------------------------

        [Fact]
        public void SelectLatestForChannel_ReturnsNewestAtOrAboveChannel_WithNoInstalledComparison()
        {
            // Stable outranks the rc even though the rc shares the version number.
            var releases = new[] { Release("2.8.4"), Release("2.8.4-rc.1") };

            var best = UpdateChecker.SelectLatestForChannel(releases, ReleaseChannel.ReleaseCandidate);

            Assert.NotNull(best);
            Assert.Equal("2.8.4", best!.Version);
        }

        [Fact]
        public void SelectLatestForChannel_ExcludesReleasesBelowChannel()
        {
            var releases = new[] { Release("2.9.0-beta.1") };

            Assert.Null(UpdateChecker.SelectLatestForChannel(releases, ReleaseChannel.ReleaseCandidate));
        }

        [Fact]
        public void SelectLatestForChannel_ReturnsReleaseEvenWhenItMatchesTheInstalledVersion()
        {
            // The Squirrel -> MSI migration case: latest release == currently running version.
            var releases = new[] { Release("2.8.4") };

            var best = UpdateChecker.SelectLatestForChannel(releases, ReleaseChannel.Stable);

            Assert.NotNull(best);
            Assert.Equal("2.8.4", best!.Version);
        }
    }
}
