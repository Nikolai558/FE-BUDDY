using System.ComponentModel;
using System.Diagnostics;

using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Platform.Models;

using Microsoft.Win32;

namespace FeBuddy.Core.Infrastructure.Platform;

/// <summary>
/// The copy of FE-Buddy 2.x that Squirrel installed into <c>%LOCALAPPDATA%\FE-BUDDY</c>. 2.9.x removed
/// it once its MSI copy ran, but FE-Buddy 3 can arrive without 2.9.x's MSI ever running - a Squirrel
/// 2.9.0 that kept declining 2.9.x's installer and took 3.x straight away, or a 2.x user who installs
/// 3.x by hand. That leaves two FE-Buddys on the PC: two entries in Installed apps and two of each
/// shortcut. Uninstalling 3.x first would then delete the old copy's files (that folder is also 2.x's
/// data folder, which 3.x's uninstaller clears) and strand its Installed apps entry.
/// </summary>
/// <remarks>
/// <para>
/// The check is a single <see cref="File.Exists(string)"/> for Squirrel's <c>Update.exe</c>, plus one
/// registry lookup when it is missing, so a PC without the old copy pays nothing noticeable. When it is
/// there, Squirrel's own uninstaller removes it (<c>Update.exe --uninstall</c>), as 2.9.x did - never
/// by deleting the folder by hand, since Squirrel also owns the Installed apps entry. A run that fails
/// or times out is logged and tried again next launch.
/// </para>
/// <para>
/// Squirrel's uninstaller leaves its two shortcuts behind; <see cref="LegacySquirrelShortcuts"/>
/// removes them once the copy they open is gone.
/// </para>
/// </remarks>
public static class LegacySquirrelInstall
{
	private const string LogSource = "Squirrel";

	/// <summary>The Installed apps entry Squirrel registered for 2.x, under <c>HKEY_CURRENT_USER</c>.</summary>
	internal const string DefaultUninstallKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\FE-BUDDY";

	/// <summary>How long launch waits for Squirrel's uninstaller (the same as 2.9.x allowed).</summary>
	internal static readonly TimeSpan UninstallTimeout = TimeSpan.FromSeconds(60);

	private static string? _rootOverride;
	private static string? _uninstallKeyPathOverride;
	private static string? _baseDirectoryOverride;
	private static Func<string, string, TimeSpan, int?> _run = Run;

	/// <summary>Where Squirrel installed 2.x: <c>%LOCALAPPDATA%\FE-BUDDY</c>.</summary>
	internal static string RootDirectory =>
		_rootOverride
		?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FE-BUDDY");

	/// <summary>Squirrel's updater and uninstaller, at the root of the old install.</summary>
	internal static string UpdateExePath => Path.Combine(RootDirectory, "Update.exe");

	/// <summary>The Installed apps entry's key under <c>HKEY_CURRENT_USER</c>.</summary>
	internal static string UninstallKeyPath => _uninstallKeyPathOverride ?? DefaultUninstallKeyPath;

	/// <summary>
	/// Removes the old Squirrel copy if it is still installed, or its Installed apps entry if only
	/// that is left.
	/// </summary>
	/// <returns>What was found and done.</returns>
	public static LegacySquirrelCleanupResult Remove()
	{
		string updateExe = UpdateExePath;

		if (!File.Exists(updateExe))
		{
			return RemoveOrphanedEntry(updateExe)
				? LegacySquirrelCleanupResult.OrphanedEntryRemoved
				: LegacySquirrelCleanupResult.NotFound;
		}

		if (IsRunningFromOldCopy())
		{
			AppLog.Warning(LogSource, $"FE-Buddy is running from '{RootDirectory}', FE-Buddy 2.x's old install folder, so its uninstaller was not run.");
			return LegacySquirrelCleanupResult.Skipped;
		}

		AppLog.Info(LogSource, $"Found FE-Buddy 2.x's old install at '{RootDirectory}'. Running its uninstaller.");

		int? exitCode;
		try
		{
			exitCode = _run(updateExe, "--uninstall", UninstallTimeout);
		}
		catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException or UnauthorizedAccessException)
		{
			AppLog.Warning(LogSource, $"Could not run FE-Buddy 2.x's uninstaller: {ex.Message}. It will be tried again next launch.");
			return LegacySquirrelCleanupResult.UninstallFailed;
		}

		if (exitCode is null)
		{
			AppLog.Warning(LogSource, $"FE-Buddy 2.x's uninstaller did not finish within {UninstallTimeout.TotalSeconds:0} seconds. It will be tried again next launch.");
			return LegacySquirrelCleanupResult.UninstallFailed;
		}

		if (exitCode != 0)
		{
			AppLog.Warning(LogSource, $"FE-Buddy 2.x's uninstaller exited with code {exitCode}. It will be tried again next launch.");
			return LegacySquirrelCleanupResult.UninstallFailed;
		}

		AppLog.Success(LogSource, "Removed FE-Buddy 2.x's old install.");
		return LegacySquirrelCleanupResult.Uninstalled;
	}

	/// <summary>Points the lookup somewhere else. Unit tests only.</summary>
	/// <param name="root">The old install folder, or <see langword="null"/> to restore the default.</param>
	/// <param name="uninstallKeyPath">The Installed apps entry's key under <c>HKEY_CURRENT_USER</c>, or <see langword="null"/> to restore the default.</param>
	/// <param name="run">Stands in for running the uninstaller, or <see langword="null"/> to run it for real.</param>
	/// <param name="baseDirectory">The folder FE-Buddy runs from, or <see langword="null"/> to restore the default.</param>
	internal static void ConfigureForTesting(
		string? root,
		string? uninstallKeyPath,
		Func<string, string, TimeSpan, int?>? run,
		string? baseDirectory = null)
	{
		_rootOverride = root;
		_uninstallKeyPathOverride = uninstallKeyPath;
		_run = run ?? Run;
		_baseDirectoryOverride = baseDirectory;
	}

	/// <summary>Runs a program without a window and waits for it.</summary>
	/// <returns>Its exit code, or <see langword="null"/> when it is still running after <paramref name="timeout"/>.</returns>
	internal static int? Run(string fileName, string arguments, TimeSpan timeout)
	{
		using Process process = Process.Start(new ProcessStartInfo(fileName, arguments)
		{
			UseShellExecute = false,
			CreateNoWindow = true,
		})!;

		return process.WaitForExit(timeout) ? process.ExitCode : null;
	}

	/// <summary>Whether this FE-Buddy runs from inside the old install folder, which the uninstaller deletes.</summary>
	private static bool IsRunningFromOldCopy()
	{
		string baseDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(_baseDirectoryOverride ?? AppContext.BaseDirectory));
		string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(RootDirectory));

		return baseDirectory.Equals(root, StringComparison.OrdinalIgnoreCase)
			|| baseDirectory.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Deletes Squirrel's Installed apps entry when the <c>Update.exe</c> it uninstalls with is gone -
	/// nothing else can remove it then. Only an entry whose uninstall command runs that exact
	/// <c>Update.exe</c> is touched.
	/// </summary>
	/// <returns>Whether an entry was deleted.</returns>
	private static bool RemoveOrphanedEntry(string updateExe)
	{
		string? uninstallCommand;
		using (RegistryKey? entry = Registry.CurrentUser.OpenSubKey(UninstallKeyPath, writable: false))
		{
			uninstallCommand = entry?.GetValue("UninstallString") as string;
		}

		if (uninstallCommand is null || !uninstallCommand.Contains(updateExe, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		Registry.CurrentUser.DeleteSubKeyTree(UninstallKeyPath, throwOnMissingSubKey: false);
		AppLog.Info(LogSource, "Removed FE-Buddy 2.x's Installed apps entry, whose files were already gone.");
		return true;
	}
}
