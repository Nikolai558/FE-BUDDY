namespace FeBuddy.Versioning
{
    /// <summary>
    /// The single, canonical implementation of FE-BUDDY's version-transition rule.
    /// Used by both the MSI's version-policy custom action (FE-BUDDY.Installer.CustomActions)
    /// and, eventually, the in-app update checker - so the rule is defined exactly once.
    ///
    /// Rule: moving to an equal-or-higher-precedence version is always allowed. Moving to a
    /// lower-precedence version (a downgrade) is allowed only when the *currently installed*
    /// version is itself a pre-release - e.g. 2.8.3 -> 2.8.4-alpha.1 -> 2.8.3 is allowed
    /// (opting out of a prerelease chain back to the last stable), but 2.8.3 -> 2.8.4 -> 2.8.3
    /// is not (stable never silently regresses to an older stable release).
    /// </summary>
    public static class UpdatePolicy
    {
        /// <summary>
        /// Is moving from <paramref name="installed"/> to <paramref name="candidate"/> allowed?
        /// </summary>
        public static bool IsTransitionAllowed(ProductVersion? installed, ProductVersion candidate)
        {
            // Nothing currently installed (fresh install) - always allowed.
            if (installed is null)
                return true;

            // Equal or forward precedence (including re-running the same installer) - always allowed.
            if (candidate.ComparePrecedenceTo(installed) >= 0)
                return true;

            // candidate is a genuine downgrade. Only allowed if the installed build was itself
            // a pre-release - i.e. the user is opting out of a prerelease chain back to stable.
            return installed.IsPrerelease;
        }

        /// <summary>
        /// String-based convenience overload for callers holding raw version text
        /// (e.g. registry values, MSI session properties). Unparseable input fails open
        /// (returns true / does not block) - callers should log the raw strings themselves
        /// so a parsing problem is visible without ever bricking an install.
        /// </summary>
        public static bool IsTransitionAllowed(string? installedVersionText, string candidateVersionText)
        {
            ProductVersion? installed = null;
            if (!string.IsNullOrWhiteSpace(installedVersionText) &&
                !ProductVersion.TryParse(installedVersionText, out installed))
            {
                return true;
            }

            if (!ProductVersion.TryParse(candidateVersionText, out var candidate) || candidate is null)
                return true;

            return IsTransitionAllowed(installed, candidate);
        }
    }
}
