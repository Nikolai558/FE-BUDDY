using System.Runtime.InteropServices;

using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Core.Infrastructure.Platform;

/// <summary>
/// The shortcuts FE-Buddy 2.8.x and earlier left behind. Those versions were installed by Squirrel
/// into <c>%LOCALAPPDATA%\FE-BUDDY</c>, which put an <c>FE-BUDDY.lnk</c> on the user's Desktop and in
/// their Start menu. Moving to the MSI removes that copy of FE-Buddy but not the two shortcuts: they
/// sit beside the MSI's own and, once the copy is gone, open nothing. Nothing else removes them - the
/// uninstaller only knows the MSI's shortcuts.
/// </summary>
/// <remarks>
/// Only a shortcut with exactly that name, in exactly those two folders, pointing at the old copy's
/// <c>FE-BUDDY.exe</c> while that file does not exist, is deleted. A shortcut the user made or
/// renamed, or one that still opens something, is left alone.
/// </remarks>
public static class LegacySquirrelShortcuts
{
	private const string LogSource = "Shortcuts";

	/// <summary>The name Squirrel gave both shortcuts.</summary>
	internal const string ShortcutFileName = "FE-BUDDY.lnk";

	private static IReadOnlyList<string>? _foldersOverride;
	private static string? _squirrelExeOverride;

	// The COM object that reads shortcuts. Unavailable where Windows Script Host is turned off by policy.
	private static string _shellProgId = "WScript.Shell";

	/// <summary>The old copy's executable, which the shortcuts point at: <c>%LOCALAPPDATA%\FE-BUDDY\FE-BUDDY.exe</c>.</summary>
	internal static string SquirrelExePath =>
		_squirrelExeOverride
		?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FE-BUDDY", "FE-BUDDY.exe");

	/// <summary>Where Squirrel put the shortcuts: this user's Desktop and Start menu Programs folder.</summary>
	internal static IReadOnlyList<string> Folders =>
		_foldersOverride
		?? [
			Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
			Environment.GetFolderPath(Environment.SpecialFolder.Programs),
		];

	/// <summary>
	/// Deletes the old shortcuts that no longer open anything. Best-effort: a shortcut that cannot be
	/// read or deleted is logged and left.
	/// </summary>
	/// <returns>How many shortcuts were deleted.</returns>
	public static int RemoveDead()
	{
		string squirrelExe = Path.GetFullPath(SquirrelExePath);
		if (File.Exists(squirrelExe))
		{
			// The old copy is still installed, so its shortcuts still work.
			return 0;
		}

		int removed = 0;

		foreach (string folder in Folders)
		{
			if (string.IsNullOrEmpty(folder))
			{
				continue;
			}

			string shortcut = Path.Combine(folder, ShortcutFileName);
			if (!File.Exists(shortcut))
			{
				continue;
			}

			string? target = ReadTarget(shortcut);
			if (string.IsNullOrEmpty(target)
				|| !string.Equals(Path.GetFullPath(target), squirrelExe, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			try
			{
				File.Delete(shortcut);
				removed++;
				AppLog.Info(LogSource, $"Deleted '{shortcut}', a shortcut FE-Buddy 2.8.x left that no longer opens anything.");
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				AppLog.Warning(LogSource, $"Could not delete the old shortcut '{shortcut}': {ex.Message}");
			}
		}

		return removed;
	}

	/// <summary>Points the lookup somewhere else. Unit tests only.</summary>
	/// <param name="folders">The folders to look in, or <see langword="null"/> to restore the default.</param>
	/// <param name="squirrelExe">The old executable's path, or <see langword="null"/> to restore the default.</param>
	/// <param name="shellProgId">The COM object that reads shortcuts, or <see langword="null"/> to restore the default.</param>
	internal static void ConfigureForTesting(IReadOnlyList<string>? folders, string? squirrelExe, string? shellProgId = null)
	{
		_foldersOverride = folders;
		_squirrelExeOverride = squirrelExe;
		_shellProgId = shellProgId ?? "WScript.Shell";
	}

	/// <summary>The path a shortcut points at, through the Windows Script Host's shell object; <see langword="null"/> when it cannot be read.</summary>
	private static string? ReadTarget(string shortcut)
	{
		object? shell = null;

		try
		{
			shell = Activator.CreateInstance(Type.GetTypeFromProgID(_shellProgId, throwOnError: true)!);
			dynamic link = ((dynamic)shell!).CreateShortcut(shortcut);
			return (string)link.TargetPath;
		}
		catch (Exception ex) when (ex is COMException or ArgumentException or InvalidCastException)
		{
			AppLog.Warning(LogSource, $"Could not read the shortcut '{shortcut}': {ex.Message}");
			return null;
		}
		finally
		{
			if (shell is not null)
			{
				Marshal.FinalReleaseComObject(shell);
			}
		}
	}
}
