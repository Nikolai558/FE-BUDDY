namespace FeBuddy.Wpf.Shell;

/// <summary>
/// External links surfaced in the UI (Dashboard description box, Info screen). Kept in one
/// place so the Discord link the Dashboard shows and the Info screen's rows never drift.
/// </summary>
public static class Links
{
	/// <summary>The FE-Buddy Discord server invite.</summary>
	public const string Discord = "https://discord.gg/GB46aeauH4";

	/// <summary>The FE-Buddy manual. Pinned to v3-development: the default branch (development) is still v2.x until the 3.0 release.</summary>
	public const string Manual = "https://github.com/Nikolai558/FE-BUDDY/tree/v3-development/docs/Users/Manual%20HTML";

	/// <summary>The release / change log.</summary>
	public const string ChangeLog = "https://github.com/Nikolai558/FE-BUDDY/releases";

	/// <summary>The issue and feature-request tracker.</summary>
	public const string Issues = "https://github.com/Nikolai558/FE-BUDDY/issues";

	/// <summary>GitHub's page for creating a fine-grained personal access token.</summary>
	public const string GitHubNewToken = "https://github.com/settings/personal-access-tokens/new";

	/// <summary>
	/// The step-by-step guide to making that token with the least access FE-Buddy needs. Pinned to
	/// v3-development, like <see cref="Manual"/>.
	/// </summary>
	public const string GitHubTokenGuide = "https://github.com/Nikolai558/FE-BUDDY/blob/v3-development/docs/Users/GitHub-Token-Guide.md";

	/// <summary>The issue tracker with a trailing slash, for turning a <c>#123</c> reference into a link.</summary>
	public const string IssueUrlBase = Issues + "/";
}
