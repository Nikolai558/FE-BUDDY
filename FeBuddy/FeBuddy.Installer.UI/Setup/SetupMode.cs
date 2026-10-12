namespace FeBuddy.Installer.UI.Setup;

/// <summary>What this run of the MSI is, decided when the window opens (<see cref="SetupContext"/>).</summary>
public enum SetupMode
{
	/// <summary>Nothing installed: a fresh install, with the license and options pages.</summary>
	Install,

	/// <summary>An older or equal FE-Buddy is installed: this MSI replaces it in place.</summary>
	Update,

	/// <summary>
	/// A newer pre-release is installed and this MSI is an older release: going back is allowed
	/// (FeBuddy.Versioning's UpdatePolicy), and replaces it in place.
	/// </summary>
	Rollback,

	/// <summary>
	/// A newer stable release is installed: UpdatePolicy refuses this MSI. The window says why and
	/// installs nothing (the package's LaunchCondition would refuse it anyway).
	/// </summary>
	Blocked,

	/// <summary>This very MSI is already installed: repair it or remove it.</summary>
	Maintenance,

	/// <summary>Started to uninstall (<c>msiexec /x</c>, or Settings ▸ Apps): confirm, then remove.</summary>
	Uninstall,
}
