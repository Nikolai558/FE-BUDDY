using System;
using System.IO;

namespace FeBuddyLibrary.Update
{
    /// <summary>
    /// Reads facts about the current MSI installation (if any) from the registry state
    /// the installer writes (see FE-BUDDY.Installer\InstallerState.wxs). Shared between
    /// Program.cs's startup update check and any other in-app action that needs the same
    /// facts (e.g. a "revert to latest stable" menu item) - previously duplicated as a
    /// private method in Program.cs.
    /// </summary>
    public static class InstalledProduct
    {
        private const string RegistryKeyPath = @"Software\FE-BUDDY";

        /// <summary>
        /// True if the currently running executable is the one registered as the MSI
        /// install's location - i.e. this process was installed by the MSI, not just
        /// running from Squirrel's per-user location or a dev build output folder.
        /// </summary>
        public static bool IsMsiInstalled()
        {
            var installLocation = ReadRegistryValue("InstallLocation");
            if (string.IsNullOrWhiteSpace(installLocation))
            {
                return false;
            }

            var currentLocation = AppContext.BaseDirectory;

            return string.Equals(
                Path.GetFullPath(currentLocation).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(installLocation).TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>The real installed semantic version (e.g. "2.8.4-alpha.1"), or null if not written (not MSI-installed, or an older install predating this feature).</summary>
        public static string GetProductSemVer() => ReadRegistryValue("ProductSemVer");

        private static string ReadRegistryValue(string name)
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(RegistryKeyPath);
            return key?.GetValue(name) as string;
        }
    }
}
