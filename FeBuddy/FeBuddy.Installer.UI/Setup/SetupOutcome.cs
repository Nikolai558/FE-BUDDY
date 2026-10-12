namespace FeBuddy.Installer.UI.Setup;

/// <summary>How the installation ended, for the last page.</summary>
public enum SetupOutcome
{
	/// <summary>Windows Installer finished it.</summary>
	Succeeded,

	/// <summary>The user cancelled it (or declined the administrator prompt), and it was rolled back.</summary>
	Cancelled,

	/// <summary>It failed, and was rolled back.</summary>
	Failed,
}
