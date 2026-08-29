namespace FeBuddy.Versioning
{
    /// <summary>
    /// The release channel implied by a version's pre-release tag.
    /// Ordered low-to-high to match SemVer precedence (Stable is highest).
    /// </summary>
    public enum ReleaseChannel
    {
        Alpha = 0,
        Beta = 1,
        ReleaseCandidate = 2,
        Stable = 3,
    }
}
