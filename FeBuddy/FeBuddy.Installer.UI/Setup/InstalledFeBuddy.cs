using System;

using Microsoft.Win32;

namespace FeBuddy.Installer.UI.Setup;

/// <summary>
/// What the installed FE-Buddy recorded under <c>HKLM\Software\FE-BUDDY</c> (InstallerState.wxs; the
/// same key and value names as 2.x's installer): where it is, its real version and the user's
/// shortcut choices.
/// </summary>
/// <remarks>
/// The window has to read these itself: Windows Installer starts the embedded UI before AppSearch,
/// so the package's own RegistrySearch properties (INSTALLFOLDER, SAVED_*_SHORTCUT,
/// INSTALLEDPRODUCTSEMVER) are still empty then.
/// </remarks>
public sealed class InstalledFeBuddy
{
	private const string KeyPath = @"Software\FE-BUDDY";

	private InstalledFeBuddy(string? installLocation, string? version, bool? startMenuShortcut, bool? desktopShortcut)
	{
		InstallLocation = installLocation;
		Version = version;
		StartMenuShortcut = startMenuShortcut;
		DesktopShortcut = desktopShortcut;
	}

	/// <summary>The install folder, or <see langword="null"/> when none is recorded.</summary>
	public string? InstallLocation { get; }

	/// <summary>The installed real (SemVer) version, e.g. <c>3.0.0-beta.8</c>, or <see langword="null"/>.</summary>
	public string? Version { get; }

	/// <summary>Whether the user kept the Start Menu shortcut, or <see langword="null"/> when not recorded.</summary>
	public bool? StartMenuShortcut { get; }

	/// <summary>Whether the user kept the Desktop shortcut, or <see langword="null"/> when not recorded.</summary>
	public bool? DesktopShortcut { get; }

	/// <summary>The full path of the installed <c>FE-BUDDY.exe</c>, or <see langword="null"/>.</summary>
	public string? ExePath => string.IsNullOrWhiteSpace(InstallLocation)
		? null
		: System.IO.Path.Combine(InstallLocation, "FE-BUDDY.exe");

	/// <summary>Reads the key from the 64-bit view, where the 64-bit MSI writes it.</summary>
	/// <returns>What is recorded; every value is <see langword="null"/> when nothing is.</returns>
	public static InstalledFeBuddy Read()
	{
		try
		{
			using RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
			using RegistryKey? key = hklm.OpenSubKey(KeyPath);
			if (key is not null)
			{
				return new InstalledFeBuddy(
					Text(key, "InstallLocation"),
					Text(key, "ProductSemVer"),
					Flag(key, "StartMenuShortcut"),
					Flag(key, "DesktopShortcut"));
			}
		}
		catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or System.IO.IOException)
		{
			// Unreadable is treated as not recorded: the window falls back to its defaults.
		}

		return new InstalledFeBuddy(null, null, null, null);
	}

	private static string? Text(RegistryKey key, string name)
	{
		string? value = key.GetValue(name) as string;
		return string.IsNullOrWhiteSpace(value) ? null : value!.Trim();
	}

	// InstallerState.wxs writes "1" or "0"; anything else means "not recorded".
	private static bool? Flag(RegistryKey key, string name) => Text(key, name) switch
	{
		"1" => true,
		"0" => false,
		_ => null,
	};
}
