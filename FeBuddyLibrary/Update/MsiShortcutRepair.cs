using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using FeBuddyLibrary.Helpers;
using Microsoft.Win32;

namespace FeBuddyLibrary.Update
{
    /// <summary>
    /// Puts back the MSI's Start Menu / Desktop shortcuts after Squirrel's uninstaller
    /// (Update.exe --uninstall, invoked by <see cref="SquirrelInstall"/>) has deleted them.
    ///
    /// Squirrel removes shortcuts by path, not by target, so a Squirrel -> MSI migration
    /// takes the freshly-MSI-installed "FE-BUDDY.lnk" files with it (see
    /// docs/SQUIRREL-TO-MSI-MIGRATION.md). This recreates a shortcut named "FE-BUDDY"
    /// pointing at the running (MSI-installed) executable, but only where:
    ///  - the install's saved preference says there should be one
    ///    (HKCU\Software\FE-BUDDY\StartMenuShortcut / DesktopShortcut, written by
    ///    FE-BUDDY.Installer\Shortcuts.wxs; REG_DWORD 1 = create), AND
    ///  - no "FE-BUDDY.lnk" currently exists in either the per-user or the all-users
    ///    location (so a shortcut the MSI put somewhere this code didn't expect, or one
    ///    Squirrel didn't actually touch, is left alone).
    ///
    /// Recreated shortcuts always go in the per-user location - always writable without
    /// elevation, and enough to give the user a working entry regardless of where the MSI
    /// originally placed one.
    /// </summary>
    public static class MsiShortcutRepair
    {
        private const string PrefKeyPath = @"Software\FE-BUDDY";
        private const string ShortcutFileName = "FE-BUDDY.lnk";
        private const string ShortcutDescription = "FE-BUDDY";

        public static void RecreateMissing()
        {
            try
            {
                var exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    Logger.LogMessage("WARNING", "MsiShortcutRepair: could not resolve the running executable path - skipping.");
                    return;
                }

                var workingDir = Path.GetDirectoryName(exePath);

                RepairOne(
                    "Start Menu",
                    PrefIsSet("StartMenuShortcut"),
                    Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                    exePath,
                    workingDir);

                RepairOne(
                    "Desktop",
                    PrefIsSet("DesktopShortcut"),
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                    exePath,
                    workingDir);
            }
            catch (Exception ex)
            {
                Logger.LogMessage("WARNING", "MsiShortcutRepair: unexpected error - " + ex.Message);
            }
        }

        private static bool PrefIsSet(string valueName)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(PrefKeyPath);
                var value = key?.GetValue(valueName);
                return value != null && Convert.ToInt32(value) == 1;
            }
            catch
            {
                return false;
            }
        }

        private static void RepairOne(
            string label,
            bool wanted,
            string perUserFolder,
            string allUsersFolder,
            string exePath,
            string workingDir)
        {
            try
            {
                if (!wanted)
                {
                    return;
                }

                if (ExistsIn(perUserFolder) || ExistsIn(allUsersFolder))
                {
                    // A shortcut is already there - Squirrel didn't take it, or something
                    // else already restored it. Leave it be.
                    return;
                }

                if (string.IsNullOrEmpty(perUserFolder))
                {
                    Logger.LogMessage("WARNING", $"MsiShortcutRepair: no per-user {label} folder to write to - skipping.");
                    return;
                }

                Directory.CreateDirectory(perUserFolder);
                var linkPath = Path.Combine(perUserFolder, ShortcutFileName);
                NativeShortcut.Create(linkPath, exePath, workingDir, ShortcutDescription);
                Logger.LogMessage("INFO", $"MsiShortcutRepair: recreated the {label} shortcut at {linkPath}.");
            }
            catch (Exception ex)
            {
                Logger.LogMessage("WARNING", $"MsiShortcutRepair: could not recreate the {label} shortcut - {ex.Message}");
            }
        }

        private static bool ExistsIn(string folder) =>
            !string.IsNullOrEmpty(folder) && File.Exists(Path.Combine(folder, ShortcutFileName));

        /// <summary>Minimal IShellLink COM interop - just enough to write a .lnk file.</summary>
        private static class NativeShortcut
        {
            public static void Create(string linkPath, string targetPath, string workingDirectory, string description)
            {
                var link = (IShellLinkW)new ShellLink();
                try
                {
                    link.SetPath(targetPath);
                    if (!string.IsNullOrEmpty(workingDirectory))
                    {
                        link.SetWorkingDirectory(workingDirectory);
                    }
                    if (!string.IsNullOrEmpty(description))
                    {
                        link.SetDescription(description);
                    }

                    ((IPersistFile)link).Save(linkPath, false);
                }
                finally
                {
                    Marshal.FinalReleaseComObject(link);
                }
            }

            [ComImport]
            [Guid("00021401-0000-0000-C000-000000000046")]
            private class ShellLink
            {
            }

            [ComImport]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [Guid("000214F9-0000-0000-C000-000000000046")]
            private interface IShellLinkW
            {
                void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, int fFlags);
                void GetIDList(out IntPtr ppidl);
                void SetIDList(IntPtr pidl);
                void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
                void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
                void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
                void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
                void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
                void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
                void GetHotkey(out short pwHotkey);
                void SetHotkey(short wHotkey);
                void GetShowCmd(out int piShowCmd);
                void SetShowCmd(int iShowCmd);
                void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
                void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
                void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
                void Resolve(IntPtr hwnd, int fFlags);
                void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
            }

            [ComImport]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [Guid("0000010b-0000-0000-C000-000000000046")]
            private interface IPersistFile
            {
                void GetClassID(out Guid pClassID);
                [PreserveSig]
                int IsDirty();
                void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);
                void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
                void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
                void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
            }
        }
    }
}
