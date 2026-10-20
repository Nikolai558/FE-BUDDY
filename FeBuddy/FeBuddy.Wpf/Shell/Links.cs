namespace FeBuddy.Wpf.Shell;

/// <summary>
/// External links surfaced in the UI (Dashboard description box, Info screen, the first-launch
/// welcome). Kept in one place so the Discord link the Dashboard and the welcome show and the Info
/// screen's rows never drift.
/// </summary>
public static class Links
{
	/// <summary>The FE-Buddy Discord server invite.</summary>
	public const string Discord = "https://discord.gg/GB46aeauH4";

	/// <summary>The FE-Buddy User Guide. Pinned to v3-development by name, like <c>GitHubRepository.Branch</c>, so changing the default branch never moves it.</summary>
	public const string UserGuide = "https://github.com/Nikolai558/FE-BUDDY/blob/v3-development/docs/Users/User-Guide.md";

	/// <summary>The FAQ and troubleshooting page. Pinned to v3-development, like <see cref="UserGuide"/>.</summary>
	public const string FaqAndTroubleshooting = "https://github.com/Nikolai558/FE-BUDDY/blob/v3-development/docs/Users/FAQ-and-Troubleshooting.md";

	/// <summary>The release / change log.</summary>
	public const string ChangeLog = "https://github.com/Nikolai558/FE-BUDDY/releases";

	/// <summary>The issue and feature-request tracker.</summary>
	public const string Issues = "https://github.com/Nikolai558/FE-BUDDY/issues";

	/// <summary>GitHub's page for creating a fine-grained personal access token.</summary>
	public const string GitHubNewToken = "https://github.com/settings/personal-access-tokens/new";

	/// <summary>
	/// The step-by-step guide to making that token with the least access FE-Buddy needs. Pinned to
	/// v3-development, like <see cref="UserGuide"/>.
	/// </summary>
	public const string GitHubTokenGuide = "https://github.com/Nikolai558/FE-BUDDY/blob/v3-development/docs/Users/GitHub-Token-Guide.md";

	/// <summary>The guide's "If something goes wrong" section: what each refusal means, and the fix.</summary>
	public const string GitHubTokenGuideTroubleshooting = GitHubTokenGuide + "#if-something-goes-wrong";

	/// <summary>The issue tracker with a trailing slash, for turning a <c>#123</c> reference into a link.</summary>
	public const string IssueUrlBase = Issues + "/";

	/// <summary>What the antimeridian (AM), the 180° meridian, is: for the question an ROI that may cross it asks.</summary>
	public const string Antimeridian = "https://en.wikipedia.org/wiki/180th_meridian";
}
