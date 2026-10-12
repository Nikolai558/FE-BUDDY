using System;
using System.IO;
using System.Linq;

using FeBuddy.Versioning;

using WixToolset.Dtf.WindowsInstaller;

namespace FeBuddy.Installer.UI.Setup;

/// <summary>
/// Everything the window needs to know before it opens: what this MSI is, what is installed, and
/// therefore which <see cref="SetupMode"/> it is in. Read once from the session and the machine
/// (<see cref="Read"/>); plain data after that, so the window never touches the session, which is
/// only valid during <c>IEmbeddedUI.Initialize</c>.
/// </summary>
/// <param name="mode">What this run is.</param>
/// <param name="version">This MSI's real version (its PRODUCTSEMVER).</param>
/// <param name="installedVersion">The installed real version, or <see langword="null"/>.</param>
/// <param name="installFolder">The folder to install to: the installed one, or the default.</param>
/// <param name="startMenuShortcut">The Start Menu shortcut's starting choice.</param>
/// <param name="desktopShortcut">The Desktop shortcut's starting choice.</param>
/// <param name="progressOnly">A basic-UI run (<c>/qb</c>, <c>/passive</c>): progress only, nothing to answer.</param>
/// <param name="canCancel">Whether the run may show a Cancel button (not under <c>/qb!</c>).</param>
/// <param name="logFile">The verbose log's path when the run is logging, else <see langword="null"/>.</param>
public sealed class SetupContext(
	SetupMode mode,
	string version,
	string? installedVersion,
	string installFolder,
	bool startMenuShortcut,
	bool desktopShortcut,
	bool progressOnly,
	bool canCancel,
	string? logFile)
{
	/// <summary>The name the window uses for the product.</summary>
	public const string ProductName = "FE-BUDDY";

	/// <summary>What this run is.</summary>
	public SetupMode Mode { get; } = mode;

	/// <summary>This MSI's real version, e.g. <c>3.0.0-beta.9</c>.</summary>
	public string Version { get; } = version;

	/// <summary>The installed real version, or <see langword="null"/> when unknown or none.</summary>
	public string? InstalledVersion { get; } = installedVersion;

	/// <summary>The folder FE-Buddy is installed to, or will be by default.</summary>
	public string InstallFolder { get; } = installFolder;

	/// <summary>The Start Menu shortcut's starting choice: the recorded one, or on.</summary>
	public bool StartMenuShortcut { get; } = startMenuShortcut;

	/// <summary>The Desktop shortcut's starting choice: the recorded one, or on.</summary>
	public bool DesktopShortcut { get; } = desktopShortcut;

	/// <summary>A basic-UI run: the window shows progress and the result, and asks nothing.</summary>
	public bool ProgressOnly { get; } = progressOnly;

	/// <summary>Whether the run may offer Cancel.</summary>
	public bool CanCancel { get; } = canCancel;

	/// <summary>The verbose log's path when the run is logging (<c>/l*v</c>), else <see langword="null"/>.</summary>
	public string? LogFile { get; } = logFile;

	/// <summary>
	/// Reads the session and the machine. The session's AppSearch and FindRelatedProducts have not
	/// run yet, so the installed copy is found here the same way the package finds it: an installed
	/// product with this UpgradeCode, and the values it recorded in the registry.
	/// </summary>
	/// <param name="session">The installer session (valid only during Initialize).</param>
	/// <param name="progressOnly">A basic-UI run.</param>
	/// <param name="canCancel">Whether the run may offer Cancel.</param>
	/// <returns>The context.</returns>
	public static SetupContext Read(Session session, bool progressOnly, bool canCancel)
	{
		string version = session["PRODUCTSEMVER"];
		string productCode = session["ProductCode"];
		string upgradeCode = session["UpgradeCode"];
		string? logFile = NullIfBlank(session["MsiLogFileLocation"]);

		InstalledFeBuddy installed = InstalledFeBuddy.Read();
		string installFolder = installed.InstallLocation ?? DefaultInstallFolder();
		bool startMenu = installed.StartMenuShortcut ?? true;
		bool desktop = installed.DesktopShortcut ?? true;

		SetupMode mode;
		if (string.Equals(session["REMOVE"], "ALL", StringComparison.OrdinalIgnoreCase))
		{
			mode = SetupMode.Uninstall;
		}
		else if (session.EvaluateCondition("Installed"))
		{
			mode = SetupMode.Maintenance;
		}
		else if (IsAnotherVersionInstalled(upgradeCode, productCode))
		{
			mode = UpgradeMode(installed.Version, version);
		}
		else
		{
			mode = SetupMode.Install;

			// Nothing installed means any leftover record is stale: start from the defaults.
			installFolder = DefaultInstallFolder();
			startMenu = true;
			desktop = true;
		}

		return new SetupContext(mode, version, installed.Version, installFolder, startMenu, desktop, progressOnly, canCancel, logFile);
	}

	/// <summary>
	/// Update, rollback or blocked, by FeBuddy.Versioning's UpdatePolicy - the same rule the
	/// package's EnforceVersionPolicy custom action applies, which fails open on an unreadable
	/// version, so this does too.
	/// </summary>
	/// <param name="installedVersion">The installed real version, if known.</param>
	/// <param name="version">This MSI's real version.</param>
	/// <returns>The mode.</returns>
	public static SetupMode UpgradeMode(string? installedVersion, string version)
	{
		if (!UpdatePolicy.IsTransitionAllowed(installedVersion, version))
		{
			return SetupMode.Blocked;
		}

		return ProductVersion.TryParse(installedVersion, out ProductVersion? installed)
			&& ProductVersion.TryParse(version, out ProductVersion? candidate)
			&& candidate!.ComparePrecedenceTo(installed!) < 0
				? SetupMode.Rollback
				: SetupMode.Update;
	}

	/// <summary><c>C:\Program Files\FE-BUDDY\</c> - the package's default INSTALLFOLDER (Folders.wxs).</summary>
	/// <returns>The folder, with a trailing backslash.</returns>
	public static string DefaultInstallFolder() =>
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), ProductName) + Path.DirectorySeparatorChar;

	private static bool IsAnotherVersionInstalled(string upgradeCode, string productCode)
	{
		if (string.IsNullOrWhiteSpace(upgradeCode))
		{
			return false;
		}

		return ProductInstallation.GetRelatedProducts(upgradeCode)
			.Any(p => !string.Equals(p.ProductCode, productCode, StringComparison.OrdinalIgnoreCase));
	}

	private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
