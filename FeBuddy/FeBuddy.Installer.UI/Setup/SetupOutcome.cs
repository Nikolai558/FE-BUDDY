namespace FeBuddy.Installer.UI.Setup;

/// <summary>How the installation ended, for the last page.</summary>
public enum SetupOutcome
{
	/// <summary>Windows Installer finished it.</summary>
	Succeeded,

	/// <summary>The user cancelled it, and it was rolled back.</summary>
	Cancelled,

	/// <summary>It failed, and was rolled back.</summary>
	Failed,

	/// <summary>
	/// It never ran and nothing changed: Windows Installer ended without saying how, most often because
	/// its administrator prompt was declined (<see cref="SetupOutcomeRules"/>).
	/// </summary>
	NotStarted,
}
