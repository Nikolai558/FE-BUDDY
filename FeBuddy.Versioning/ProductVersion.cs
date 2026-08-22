using System;
using Semver;

namespace FeBuddy.Versioning
{
    /// <summary>
    /// A parsed, real FE-BUDDY release version (e.g. "2.8.4-alpha.1").
    /// Wraps <see cref="SemVersion"/> so the rest of the codebase never has to
    /// hand-parse or hand-compare version strings.
    /// </summary>
    public sealed class ProductVersion
    {
        public SemVersion SemVersion { get; }

        public bool IsPrerelease => SemVersion.IsPrerelease;

        public ReleaseChannel Channel
        {
            get
            {
                if (!IsPrerelease)
                    return ReleaseChannel.Stable;

                var tag = SemVersion.PrereleaseIdentifiers.Count > 0
                    ? SemVersion.PrereleaseIdentifiers[0].Value
                    : string.Empty;

                if (tag.Equals("rc", StringComparison.OrdinalIgnoreCase))
                    return ReleaseChannel.ReleaseCandidate;
                if (tag.Equals("beta", StringComparison.OrdinalIgnoreCase))
                    return ReleaseChannel.Beta;

                // Alpha, or anything unrecognized - treat as the earliest/least trusted channel.
                return ReleaseChannel.Alpha;
            }
        }

        private ProductVersion(SemVersion semVersion)
        {
            SemVersion = semVersion;
        }

        /// <summary>
        /// Parses a real FE-BUDDY version string. Throws <see cref="FormatException"/>
        /// if <paramref name="versionText"/> is not a valid, strict SemVer 2.0 string.
        /// </summary>
        public static ProductVersion Parse(string versionText)
        {
            if (string.IsNullOrWhiteSpace(versionText))
                throw new FormatException("Version string is null or empty.");

            return new ProductVersion(SemVersion.Parse(versionText, SemVersionStyles.Strict));
        }

        public static bool TryParse(string? versionText, out ProductVersion? version)
        {
            version = null;

            if (string.IsNullOrWhiteSpace(versionText))
                return false;

            if (!SemVersion.TryParse(versionText, SemVersionStyles.Strict, out var parsed))
                return false;

            version = new ProductVersion(parsed);
            return true;
        }

        /// <summary>
        /// SemVer 2.0 precedence comparison (build metadata ignored, per spec).
        /// Negative: this &lt; other. Zero: equal precedence. Positive: this &gt; other.
        /// </summary>
        public int ComparePrecedenceTo(ProductVersion other) => SemVersion.ComparePrecedenceTo(other.SemVersion);

        public override string ToString() => SemVersion.ToString();
    }
}
