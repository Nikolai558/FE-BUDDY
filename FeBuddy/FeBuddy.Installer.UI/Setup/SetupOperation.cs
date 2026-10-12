namespace FeBuddy.Installer.UI.Setup;

/// <summary>What Windows Installer is asked to do once the user starts it.</summary>
public enum SetupOperation
{
	/// <summary>Install, or replace the installed FE-Buddy (an update or a rollback).</summary>
	Install,

	/// <summary>Reinstall every file of this same version (<c>REINSTALL=ALL</c>).</summary>
	Repair,

	/// <summary>Remove FE-Buddy (<c>REMOVE=ALL</c>).</summary>
	Remove,
}
