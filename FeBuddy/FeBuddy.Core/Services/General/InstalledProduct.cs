using Microsoft.Win32;

namespace FeBuddy.Core.Services.General;

/// <summary>
/// What the FE-Buddy installer recorded about the MSI install, read from
/// <c>HKLM\Software\FE-BUDDY</c> (written by FeBuddy.Installer's InstallerState.wxs - the same key
/// and value names FE-Buddy 2.x's installer uses, so a 3.x install upgrades a 2.x one in place).
/// </summary>
/// <remarks>
/// Every read returns <see langword="null"/> when there is no install, the value is missing, or
/// the registry cannot be read. The running version itself comes from <see cref="AppVersion"/>,
/// not from here: the registry describes what the installer put in the install folder, which is
/// only the running copy when <see cref="IsMsiInstalled"/> says so.
/// </remarks>
public static class InstalledProduct
{
	internal const string RegistryKeyPath = @"Software\FE-BUDDY";

	/// <summary>Reads one value under <see cref="RegistryKeyPath"/>. Unit tests replace it.</summary>
	internal static Func<string, string?> ReadValue { get; set; } = ReadRegistryValue;

	/// <summary>The install folder, e.g. <c>C:\Program Files\FE-BUDDY\</c>.</summary>
	public static string? InstallLocation => ReadValue("InstallLocation");

	/// <summary>The real version the installer installed, e.g. <c>3.0.0-alpha.1</c>.</summary>
	public static string? ProductSemVer => ReadValue("ProductSemVer");

	/// <summary>The install's MSI ProductCode GUID, for <c>msiexec /x</c>.</summary>
	public static string? ProductCode => ReadValue("ProductCode");

	/// <summary>
	/// <see langword="true"/> when <paramref name="baseDirectory"/> is the MSI's install folder - this
	/// copy was installed by the MSI, rather than being a dev build or a copy run from elsewhere.
	/// </summary>
	/// <param name="baseDirectory">The running copy's folder (<see cref="AppContext.BaseDirectory"/>).</param>
	/// <returns><see langword="true"/> for the MSI-installed copy.</returns>
	public static bool IsMsiInstalled(string baseDirectory)
	{
		string? installLocation = InstallLocation;
		if (string.IsNullOrWhiteSpace(installLocation) || string.IsNullOrWhiteSpace(baseDirectory))
		{
			return false;
		}

		try
		{
			return string.Equals(
				Path.GetFullPath(baseDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
				Path.GetFullPath(installLocation).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
				StringComparison.OrdinalIgnoreCase);
		}
		catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
		{
			return false;
		}
	}

	// The MSI is x64, so it writes the 64-bit view; read that view whatever this process is.
	private static string? ReadRegistryValue(string name)
	{
		try
		{
			using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
			using RegistryKey? key = baseKey.OpenSubKey(RegistryKeyPath);
			return key?.GetValue(name) as string;
		}
		catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
		{
			return null;
		}
	}
}
