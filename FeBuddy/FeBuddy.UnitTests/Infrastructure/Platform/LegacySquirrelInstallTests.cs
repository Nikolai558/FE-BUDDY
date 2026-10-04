using System.ComponentModel;

using Microsoft.Win32;

using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Platform;
using FeBuddy.Core.Infrastructure.Platform.Models;

namespace FeBuddy.UnitTests.Infrastructure.Platform;

/// <summary>
/// Covers <see cref="LegacySquirrelInstall"/> against a throwaway install folder and a throwaway
/// registry key under <c>HKCU\Software</c> standing in for Squirrel's Installed apps entry. The
/// uninstaller itself is stubbed - except where <see cref="LegacySquirrelInstall.Run"/> is run on
/// cmd.exe - so no test ever runs a real Update.exe.
/// </summary>
[Collection("AppLog")]
public sealed class LegacySquirrelInstallTests : IDisposable
{
	private readonly string _root =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_SquirrelInstall_" + Guid.NewGuid().ToString("N"));

	private readonly string _uninstallKey = @"Software\FeBuddyTests_SquirrelUninstall_" + Guid.NewGuid().ToString("N");
	private readonly string _install;
	private readonly string _updateExe;
	private readonly List<(string File, string Arguments, TimeSpan Timeout)> _runs = [];
	private int? _exitCode = 0;

	/// <summary>Points the lookup, the uninstaller and the log at throwaway places.</summary>
	public LegacySquirrelInstallTests()
	{
		_install = Path.Combine(_root, "LocalAppData", "FE-BUDDY");
		_updateExe = Path.Combine(_install, "Update.exe");

		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		LegacySquirrelInstall.ConfigureForTesting(_install, _uninstallKey, (file, arguments, timeout) =>
		{
			_runs.Add((file, arguments, timeout));
			return _exitCode;
		});
	}

	/// <summary>Restores defaults and cleans up.</summary>
	public void Dispose()
	{
		LegacySquirrelInstall.ConfigureForTesting(null, null, null);
		AppLog.ConfigureForTesting(null);
		Registry.CurrentUser.DeleteSubKeyTree(_uninstallKey, throwOnMissingSubKey: false);

		try
		{
			Directory.Delete(_root, recursive: true);
		}
		catch
		{
			// Best-effort.
		}
	}

	/// <summary>Without Update.exe or an Installed apps entry there is nothing to do, and nothing runs.</summary>
	[Fact]
	public void nothing_is_found_or_run_on_a_pc_without_the_old_install()
	{
		Assert.Equal(LegacySquirrelCleanupResult.NotFound, LegacySquirrelInstall.Remove());
		Assert.Empty(_runs);
	}

	/// <summary>When Update.exe is there, Squirrel's own uninstaller is run on it.</summary>
	[Fact]
	public void the_old_install_is_removed_with_squirrels_uninstaller()
	{
		PlantUpdateExe();

		Assert.Equal(LegacySquirrelCleanupResult.Uninstalled, LegacySquirrelInstall.Remove());

		(string file, string arguments, TimeSpan timeout) = Assert.Single(_runs);
		Assert.Equal(_updateExe, file);
		Assert.Equal("--uninstall", arguments);
		Assert.Equal(TimeSpan.FromSeconds(60), timeout);
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Success && e.Message == "Removed FE-Buddy 2.x's old install.");
	}

	/// <summary>An uninstaller that fails or does not finish is logged, to be tried again next launch.</summary>
	[Theory]
	[InlineData(1, "exited with code 1")]
	[InlineData(null, "did not finish within 60 seconds")]
	public void an_uninstaller_that_fails_or_hangs_is_logged(int? exitCode, string expected)
	{
		PlantUpdateExe();
		_exitCode = exitCode;

		Assert.Equal(LegacySquirrelCleanupResult.UninstallFailed, LegacySquirrelInstall.Remove());
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Warning
			&& e.Message.Contains(expected, StringComparison.Ordinal)
			&& e.Message.EndsWith("It will be tried again next launch.", StringComparison.Ordinal));
	}

	/// <summary>An uninstaller that cannot be started is logged, to be tried again next launch.</summary>
	[Fact]
	public void an_uninstaller_that_cannot_start_is_logged()
	{
		PlantUpdateExe();
		LegacySquirrelInstall.ConfigureForTesting(_install, _uninstallKey, (_, _, _) => throw new Win32Exception("blocked"));

		Assert.Equal(LegacySquirrelCleanupResult.UninstallFailed, LegacySquirrelInstall.Remove());
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Warning
			&& e.Message.StartsWith("Could not run FE-Buddy 2.x's uninstaller: blocked.", StringComparison.Ordinal));
	}

	/// <summary>A FE-Buddy running from inside the old folder never runs the uninstaller that would delete it.</summary>
	[Theory]
	[InlineData("")]
	[InlineData(@"app-2.9.0\")]
	public void the_uninstaller_is_not_run_from_inside_the_old_folder(string subfolder)
	{
		PlantUpdateExe();
		LegacySquirrelInstall.ConfigureForTesting(_install, _uninstallKey, (_, _, _) => throw new InvalidOperationException("must not run"),
			baseDirectory: Path.Combine(_install, subfolder));

		Assert.Equal(LegacySquirrelCleanupResult.Skipped, LegacySquirrelInstall.Remove());
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("so its uninstaller was not run", StringComparison.Ordinal));
	}

	/// <summary>A folder that only starts with the same name (FE-BUDDY-Documents-Demo) is not inside the old one.</summary>
	[Fact]
	public void a_neighbouring_folder_with_a_longer_name_is_not_inside_the_old_one()
	{
		PlantUpdateExe();
		LegacySquirrelInstall.ConfigureForTesting(_install, _uninstallKey, (_, _, _) => 0, baseDirectory: _install + "-Documents-Demo");

		Assert.Equal(LegacySquirrelCleanupResult.Uninstalled, LegacySquirrelInstall.Remove());
	}

	/// <summary>Squirrel's entry is removed once the Update.exe it uninstalls with is gone - nothing else can remove it.</summary>
	[Fact]
	public void an_installed_apps_entry_whose_files_are_gone_is_removed()
	{
		PlantEntry($"\"{_updateExe.ToUpperInvariant()}\" --uninstall");

		Assert.Equal(LegacySquirrelCleanupResult.OrphanedEntryRemoved, LegacySquirrelInstall.Remove());
		Assert.Null(Registry.CurrentUser.OpenSubKey(_uninstallKey));
		Assert.Empty(_runs);
	}

	/// <summary>An entry that uninstalls with anything else, or with nothing, is left alone.</summary>
	[Theory]
	[InlineData(@"""C:\Somewhere\Else\Update.exe"" --uninstall")]
	[InlineData(null)]
	public void an_entry_for_anything_else_is_left_alone(string? uninstallCommand)
	{
		PlantEntry(uninstallCommand);

		Assert.Equal(LegacySquirrelCleanupResult.NotFound, LegacySquirrelInstall.Remove());
		using RegistryKey? entry = Registry.CurrentUser.OpenSubKey(_uninstallKey);
		Assert.NotNull(entry);
	}

	/// <summary>While Update.exe is there its uninstaller runs instead, and removes the entry itself.</summary>
	[Fact]
	public void the_entry_is_left_to_squirrel_while_update_exe_is_there()
	{
		PlantUpdateExe();
		PlantEntry($"\"{_updateExe}\" --uninstall");

		Assert.Equal(LegacySquirrelCleanupResult.Uninstalled, LegacySquirrelInstall.Remove());
		using RegistryKey? entry = Registry.CurrentUser.OpenSubKey(_uninstallKey);
		Assert.NotNull(entry);
	}

	/// <summary>Run returns the program's exit code, or null when it is still running at the timeout.</summary>
	[Fact]
	public void run_returns_the_exit_code_or_null_on_timeout()
	{
		string cmd = Path.Combine(Environment.SystemDirectory, "cmd.exe");

		Assert.Equal(3, LegacySquirrelInstall.Run(cmd, "/c exit 3", TimeSpan.FromSeconds(30)));
		Assert.Null(LegacySquirrelInstall.Run(cmd, "/c ping -n 3 127.0.0.1 >nul", TimeSpan.FromMilliseconds(50)));
	}

	/// <summary>Without an override the lookup covers this user's old install folder and Squirrel's entry.</summary>
	[Fact]
	public void defaults_are_this_users_old_install_and_squirrels_entry()
	{
		LegacySquirrelInstall.ConfigureForTesting(null, null, null);

		Assert.Equal(
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FE-BUDDY"),
			LegacySquirrelInstall.RootDirectory);
		Assert.Equal(Path.Combine(LegacySquirrelInstall.RootDirectory, "Update.exe"), LegacySquirrelInstall.UpdateExePath);
		Assert.Equal(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\FE-BUDDY", LegacySquirrelInstall.UninstallKeyPath);
	}

	private void PlantUpdateExe()
	{
		Directory.CreateDirectory(_install);
		File.WriteAllText(_updateExe, "exe");
	}

	private void PlantEntry(string? uninstallCommand)
	{
		using RegistryKey key = Registry.CurrentUser.CreateSubKey(_uninstallKey);
		key.SetValue("DisplayName", "FE-BUDDY");
		if (uninstallCommand is not null)
		{
			key.SetValue("UninstallString", uninstallCommand);
		}
	}
}
