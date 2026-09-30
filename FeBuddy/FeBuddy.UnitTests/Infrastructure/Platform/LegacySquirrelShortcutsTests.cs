using System.Runtime.InteropServices;

using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Platform;

namespace FeBuddy.UnitTests.Infrastructure.Platform;

/// <summary>
/// Covers <see cref="LegacySquirrelShortcuts"/>: deleting the Desktop and Start menu shortcuts
/// FE-Buddy 2.8.x left, and only those.
/// </summary>
[Collection("AppLog")]
public sealed class LegacySquirrelShortcutsTests : IDisposable
{
	private readonly string _root =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_SquirrelShortcuts_" + Guid.NewGuid().ToString("N"));

	private readonly string _desktop;
	private readonly string _programs;
	private readonly string _squirrelExe;

	/// <summary>Points the lookup and the log at a throwaway folder.</summary>
	public LegacySquirrelShortcutsTests()
	{
		_desktop = Directory.CreateDirectory(Path.Combine(_root, "Desktop")).FullName;
		_programs = Directory.CreateDirectory(Path.Combine(_root, "Programs")).FullName;
		_squirrelExe = Path.Combine(_root, "LocalAppData", "FE-BUDDY", "FE-BUDDY.exe");

		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		LegacySquirrelShortcuts.ConfigureForTesting([_desktop, "", _programs], _squirrelExe);
	}

	/// <summary>Restores defaults and cleans up.</summary>
	public void Dispose()
	{
		LegacySquirrelShortcuts.ConfigureForTesting(null, null);
		AppLog.ConfigureForTesting(null);

		try
		{
			foreach (string file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
			{
				File.SetAttributes(file, FileAttributes.Normal);
			}

			Directory.Delete(_root, recursive: true);
		}
		catch
		{
			// Best-effort.
		}
	}

	/// <summary>Both of 2.8.x's shortcuts are deleted once the copy they open is gone.</summary>
	[Fact]
	public void dead_shortcuts_on_the_desktop_and_start_menu_are_deleted()
	{
		string onDesktop = CreateShortcut(_desktop, "FE-BUDDY.lnk", _squirrelExe);
		string inStartMenu = CreateShortcut(_programs, "FE-BUDDY.lnk", _squirrelExe.ToUpperInvariant());

		Assert.Equal(2, LegacySquirrelShortcuts.RemoveDead());
		Assert.False(File.Exists(onDesktop));
		Assert.False(File.Exists(inStartMenu));
		Assert.Contains(AppLog.Entries, e => e.Message.StartsWith("Deleted '", StringComparison.Ordinal));
	}

	/// <summary>While the old copy is still installed its shortcuts work, so they are kept.</summary>
	[Fact]
	public void shortcuts_are_kept_while_the_old_copy_is_still_installed()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(_squirrelExe)!);
		File.WriteAllText(_squirrelExe, "exe");
		string onDesktop = CreateShortcut(_desktop, "FE-BUDDY.lnk", _squirrelExe);

		Assert.Equal(0, LegacySquirrelShortcuts.RemoveDead());
		Assert.True(File.Exists(onDesktop));
	}

	/// <summary>A shortcut to anything else - the MSI's copy, say - or under another name is left alone.</summary>
	[Fact]
	public void other_shortcuts_are_left_alone()
	{
		string toMsiCopy = CreateShortcut(_desktop, "FE-BUDDY.lnk", Path.Combine(_root, "Program Files", "FE-BUDDY", "FE-BUDDY.exe"));
		string renamed = CreateShortcut(_programs, "My FE-BUDDY.lnk", _squirrelExe);

		Assert.Equal(0, LegacySquirrelShortcuts.RemoveDead());
		Assert.True(File.Exists(toMsiCopy));
		Assert.True(File.Exists(renamed));
	}

	/// <summary>A shortcut with no target, or one that is not a shortcut at all, is left alone.</summary>
	[Fact]
	public void shortcuts_without_a_readable_target_are_left_alone()
	{
		string noTarget = Path.Combine(_desktop, "FE-BUDDY.lnk");
		SaveShortcut(noTarget, target: null);
		string notAShortcut = Path.Combine(_programs, "FE-BUDDY.lnk");
		File.WriteAllText(notAShortcut, "not a shortcut");

		Assert.Equal(0, LegacySquirrelShortcuts.RemoveDead());
		Assert.True(File.Exists(noTarget));
		Assert.True(File.Exists(notAShortcut));
	}

	/// <summary>Where shortcuts cannot be read (Windows Script Host turned off), the failure is logged and nothing is deleted.</summary>
	[Fact]
	public void shortcuts_are_left_alone_when_they_cannot_be_read()
	{
		string onDesktop = CreateShortcut(_desktop, "FE-BUDDY.lnk", _squirrelExe);
		LegacySquirrelShortcuts.ConfigureForTesting([_desktop], _squirrelExe, "FeBuddyTests.NoSuchComObject");

		Assert.Equal(0, LegacySquirrelShortcuts.RemoveDead());
		Assert.True(File.Exists(onDesktop));
		Assert.Contains(AppLog.Entries, e => e.Message.StartsWith("Could not read the shortcut", StringComparison.Ordinal));
	}

	/// <summary>A dead shortcut that cannot be deleted is logged, and the rest are still deleted.</summary>
	[Fact]
	public void a_shortcut_that_cannot_be_deleted_is_logged_and_skipped()
	{
		string readOnly = CreateShortcut(_desktop, "FE-BUDDY.lnk", _squirrelExe);
		File.SetAttributes(readOnly, FileAttributes.ReadOnly);
		string inStartMenu = CreateShortcut(_programs, "FE-BUDDY.lnk", _squirrelExe);

		Assert.Equal(1, LegacySquirrelShortcuts.RemoveDead());
		Assert.True(File.Exists(readOnly));
		Assert.False(File.Exists(inStartMenu));
		Assert.Contains(AppLog.Entries, e => e.Level == Core.Infrastructure.Logging.Models.LogLevel.Warning
			&& e.Message.StartsWith("Could not delete the old shortcut", StringComparison.Ordinal));
	}

	/// <summary>Without an override the lookup covers this user's Desktop and Start menu and the old install folder.</summary>
	[Fact]
	public void defaults_are_this_users_desktop_and_start_menu()
	{
		LegacySquirrelShortcuts.ConfigureForTesting(null, null);

		Assert.Equal(
			[Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Environment.GetFolderPath(Environment.SpecialFolder.Programs)],
			LegacySquirrelShortcuts.Folders);
		Assert.Equal(
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FE-BUDDY", "FE-BUDDY.exe"),
			LegacySquirrelShortcuts.SquirrelExePath);
	}

	private static string CreateShortcut(string folder, string name, string target)
	{
		string path = Path.Combine(folder, name);
		SaveShortcut(path, target);
		return path;
	}

	private static void SaveShortcut(string path, string? target)
	{
		object shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell", throwOnError: true)!)!;
		try
		{
			dynamic link = ((dynamic)shell).CreateShortcut(path);
			if (target is not null)
			{
				link.TargetPath = target;
			}

			link.Save();
		}
		finally
		{
			Marshal.FinalReleaseComObject(shell);
		}
	}
}
