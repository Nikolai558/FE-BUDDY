using WixToolset.Dtf.WindowsInstaller;

namespace FeBuddy.Installer.UI.Setup;

/// <summary>What the user chose, handed to Windows Installer as properties when the install starts.</summary>
/// <param name="Operation">Install (or update), repair or remove.</param>
/// <param name="InstallFolder">The install folder (applied to fresh installs only).</param>
/// <param name="StartMenuShortcut">Whether to create the Start Menu shortcut.</param>
/// <param name="DesktopShortcut">Whether to create the Desktop shortcut.</param>
public sealed record SetupChoices(SetupOperation Operation, string InstallFolder, bool StartMenuShortcut, bool DesktopShortcut)
{
	/// <summary>
	/// Sets the session's properties, as the stock dialogs' Publish events did (FeBuddy.UI.wxs and
	/// InstallOptionsDlg.wxs): the shortcut choices in CREATE_*_SHORTCUT (the components'
	/// conditions) and SAVED_*_SHORTCUT (what InstallerState.wxs records for the next update), and
	/// the folder in INSTALLFOLDER.
	/// </summary>
	/// <param name="session">The installer session (valid only during Initialize).</param>
	/// <param name="mode">The run's mode: only a fresh install takes the folder - an update keeps
	/// the installed folder, which AppSearch reads back from the registry anyway.</param>
	public void ApplyTo(Session session, SetupMode mode)
	{
		switch (Operation)
		{
			case SetupOperation.Remove:
				session["REMOVE"] = "ALL";
				return;

			case SetupOperation.Repair:
				session["REINSTALL"] = "ALL";
				return;
		}

		if (mode == SetupMode.Install)
		{
			session["INSTALLFOLDER"] = InstallFolder;
		}

		// An empty property is "off": the components are conditioned on the property being set.
		session["CREATE_STARTMENU_SHORTCUT"] = StartMenuShortcut ? "1" : string.Empty;
		session["CREATE_DESKTOP_SHORTCUT"] = DesktopShortcut ? "1" : string.Empty;
		session["SAVED_STARTMENU_SHORTCUT"] = StartMenuShortcut ? "1" : "0";
		session["SAVED_DESKTOP_SHORTCUT"] = DesktopShortcut ? "1" : "0";
	}
}
