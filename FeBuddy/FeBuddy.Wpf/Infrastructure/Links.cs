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

    /// <summary>The FE-Buddy manual.</summary>
    public const string Manual = "https://github.com/Nikolai558/FE-Buddy-DEV/tree/main/docs/Users/Manual%20HTML";

    /// <summary>The release / change log. FE-Buddy-DEV has no releases (private, dev-only) - this is FE-BUDDY's.</summary>
    public const string ChangeLog = "https://github.com/Nikolai558/FE-BUDDY/releases";

    /// <summary>The issue and feature-request tracker.</summary>
    public const string Issues = "https://github.com/Nikolai558/FE-Buddy-DEV/issues";
}
