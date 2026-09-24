namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// External links surfaced in the UI (Dashboard description box, Info screen). Kept in one
/// place so the Discord link the Dashboard shows and the Info screen's rows never drift.
/// </summary>
public static class Links
{
    // TODO(owner): replace with the real FE-Buddy Discord invite.
    /// <summary>The FE-Buddy Discord server invite.</summary>
    public const string Discord = "https://discord.gg/febuddy";

    /// <summary>The FE-Buddy manual. Pinned to v3-development: the default branch (development) is still v2.x until the 3.0 release.</summary>
    public const string Manual = "https://github.com/Nikolai558/FE-BUDDY/tree/v3-development/docs/Users/Manual%20HTML";

    /// <summary>The release / change log.</summary>
    public const string ChangeLog = "https://github.com/Nikolai558/FE-BUDDY/releases";

    /// <summary>The issue and feature-request tracker.</summary>
    public const string Issues = "https://github.com/Nikolai558/FE-BUDDY/issues";

    /// <summary>The issue tracker with a trailing slash, for turning a <c>#123</c> reference into a link.</summary>
    public const string IssueUrlBase = Issues + "/";
}
