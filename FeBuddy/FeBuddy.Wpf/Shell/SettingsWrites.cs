using System.IO;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Wpf.Shell;

/// <summary>
/// Every save of the settings files the GUI makes goes through here, so a file that can't be written
/// or read - locked for a moment by antivirus or a sync tool, or in a read-only folder - is reported
/// rather than closing FE-Buddy.
/// </summary>
/// <remarks>
/// On a failure the user is told, and the settings in memory are read again from disk, so nothing
/// takes the change for saved. The page keeps showing what the user chose, so saving again tries again.
/// While a profile change or an import runs (<see cref="SettingsBusy"/>), nothing is saved and the user is
/// asked to wait for it.
/// </remarks>
internal static class SettingsWrites
{
	private const string LogSource = "UserConfig";

	/// <summary>Saves one node (<see cref="UserConfigFile.Save(string)"/>).</summary>
	/// <param name="nodePath">The node, e.g. <c>Services.AiracService.Airways</c>.</param>
	/// <param name="what">What is being saved, for the message, e.g. <c>Airways settings</c>.</param>
	/// <returns><see langword="true"/> when it was saved.</returns>
	internal static bool TrySave(string nodePath, string what) => Try(() => UserConfigFile.Save(nodePath), what);

	/// <summary>
	/// Saves settings every profile shares to <c>Shared.json</c> alone (<see cref="UserConfigFile.SaveShared"/>),
	/// leaving the profile and its undo snapshot as they are.
	/// </summary>
	/// <param name="keys">The shared settings, e.g. <see cref="UserConfigKeys.NewsLastOpen"/>.</param>
	/// <param name="what">What is being saved, for the message.</param>
	/// <returns><see langword="true"/> when they were saved.</returns>
	internal static bool TrySaveShared(string[] keys, string what) => Try(() => UserConfigFile.SaveShared(keys), what);

	/// <summary>Writes every setting in memory (<see cref="UserConfigFile.Write"/>).</summary>
	/// <param name="what">What is being saved, for the message.</param>
	/// <returns><see langword="true"/> when it was written.</returns>
	internal static bool TryWrite(string what) => Try(UserConfigFile.Write, what);

	/// <summary>Restores a node's previous save (<see cref="UserConfigFile.Undo(string)"/>).</summary>
	/// <param name="nodePath">The node.</param>
	/// <param name="what">What is being restored, for the message.</param>
	/// <returns><see langword="true"/> when a previous save was restored.</returns>
	internal static bool TryUndo(string nodePath, string what)
	{
		bool restored = false;
		return Try(() => restored = UserConfigFile.Undo(nodePath), what) && restored;
	}

	private static bool Try(Action write, string what)
	{
		if (SettingsBusy.IsBusy)
		{
			// The profile in use, and the settings in memory, are about to change: a save now could land in the wrong profile.
			Toast.Warn($"Could not save the {what}", "A settings profile change or import is running. Wait for it to finish, then try again.");
			return false;
		}

		try
		{
			write();
			return true;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AppLog.Error(LogSource, $"Could not save the {what}: {ex.Message}");
			Toast.Error($"Could not save the {what}",
				$"{ex.Message} Close anything that may have FE-Buddy's settings open, such as a sync tool, then try again.");

			// So the setting in memory matches the file again, and nothing reads the change as saved.
			UserConfigFile.ReadAll();
			return false;
		}
	}
}
