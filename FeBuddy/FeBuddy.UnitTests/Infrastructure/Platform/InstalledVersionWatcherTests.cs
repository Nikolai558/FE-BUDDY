using System.Diagnostics;

using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Platform;

namespace FeBuddy.UnitTests.Infrastructure.Platform;

/// <summary>
/// Covers <see cref="InstalledVersionWatcher"/>: a file's version read as the running one is, and an update
/// written over the watched file reported once it settles and Windows Installer has finished, with its version -
/// also when the install folder is deleted and created again, as an update does, or the file is missing for a
/// while, or the folder can't be watched for a moment - but not a write of the same version, not an update rolled
/// back, and not once stopped. Against a throwaway log.
/// </summary>
[Collection("AppLog")]
public sealed class InstalledVersionWatcherTests : IDisposable
{
	private static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(100);

	// Only a guard against hanging: what is waited for comes within a few settles.
	private static readonly TimeSpan HangGuard = TimeSpan.FromSeconds(10);

	// A real assembly with a product version: Core itself.
	private static readonly string Assembly = typeof(AppVersion).Assembly.Location;
	private static readonly string AssemblyVersion = AppVersion.Of(typeof(AppVersion).Assembly);

	// Another, with a different version: the app itself.
	private static readonly string WpfAssembly = typeof(FeBuddy.Wpf.Shell.InstalledUpdate).Assembly.Location;
	private static readonly string WpfVersion = AppVersion.Of(typeof(FeBuddy.Wpf.Shell.InstalledUpdate).Assembly);

	private readonly string _root = Directory.CreateTempSubdirectory("febuddy-watch-").FullName;
	private readonly string _logs = Directory.CreateTempSubdirectory("febuddy-watch-logs-").FullName;

	/// <summary>Points the log at a throwaway folder.</summary>
	public InstalledVersionWatcherTests() => AppLog.ConfigureForTesting(_logs);

	/// <summary>Restores the real log and deletes the throwaway folders.</summary>
	public void Dispose()
	{
		AppLog.ConfigureForTesting(null);

		foreach (string folder in new[] { _root, _logs })
		{
			try
			{
				Directory.Delete(folder, recursive: true);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				// Best-effort cleanup: a copy loaded by a test stays mapped until it is unloaded.
			}
		}
	}

	private string Program => Path.Combine(_root, "FE-BUDDY.dll");

	/// <summary>A watcher whose Windows Installer is <paramref name="installRunning"/>, and whose folder watch is made by <paramref name="createWatcher"/>.</summary>
	private static InstalledVersionWatcher Watch(
		string program,
		string runningVersion,
		Func<bool> installRunning,
		Func<string, FileSystemWatcher>? createWatcher = null,
		int maxMissingChecks = InstalledVersionWatcher.MaxMissingChecks) =>
		new(program, runningVersion, Settle, installRunning, createWatcher ?? (folder => new FileSystemWatcher(folder)), maxMissingChecks);

	[Fact]
	public void a_files_version_is_read_as_the_running_one_is()
	{
		File.Copy(Assembly, Program);
		File.WriteAllText(Path.Combine(_root, "notes.txt"), "not a program");

		Assert.Equal(AssemblyVersion, InstalledVersionWatcher.VersionOf(Program));
		Assert.Null(InstalledVersionWatcher.VersionOf(Path.Combine(_root, "notes.txt")));
		Assert.Null(InstalledVersionWatcher.VersionOf(Path.Combine(_root, "gone.dll")));
	}

	/// <summary>
	/// A running FE-Buddy has its own <c>FE-BUDDY.dll</c> loaded, and Windows' version information answers from that
	/// loaded copy - here, the old version for the new file on disk. The version is read from the file itself.
	/// </summary>
	[Fact]
	public void a_new_file_is_read_as_itself_even_while_the_old_one_is_loaded()
	{
		File.Copy(Assembly, Program);
		System.Runtime.Loader.AssemblyLoadContext context = new("running copy", isCollectible: true);

		try
		{
			context.LoadFromAssemblyPath(Program);
			File.Move(Program, Path.Combine(_root, "5db1f.rbf"));
			File.Copy(WpfAssembly, Program);

			Assert.NotEqual(AssemblyVersion, WpfVersion);
			Assert.Equal(AssemblyVersion, AppVersion.Clean(FileVersionInfo.GetVersionInfo(Program).ProductVersion));
			Assert.Equal(WpfVersion, InstalledVersionWatcher.VersionOf(Program));
		}
		finally
		{
			context.Unload();
		}
	}

	/// <summary>A different version written over the file - moved aside first, as Windows Installer does - is reported once.</summary>
	[Fact]
	public void an_update_written_over_the_file_is_reported_once()
	{
		File.WriteAllText(Program, "the running build");
		using InstalledVersionWatcher watcher = new(Program, "3.0.0-beta.12", Settle);
		List<string> updated = [];
		using ManualResetEventSlim raised = new();
		watcher.Updated += (_, version) =>
		{
			lock (updated)
			{
				updated.Add(version);
			}

			raised.Set();
		};

		File.Move(Program, Path.Combine(_root, "FE-BUDDY.dll.old"));
		File.Copy(Assembly, Program);

		Assert.True(raised.Wait(HangGuard));
		File.Copy(Assembly, Program, overwrite: true);
		Thread.Sleep(Settle * 4);

		Assert.Equal([AssemblyVersion], updated);
		Assert.Equal(AssemblyVersion, watcher.ChangedVersion());
	}

	/// <summary>
	/// An update as Windows Installer runs it: the file in use moved out, the emptied install folder deleted,
	/// then created again and the new file written into it - all under the watch, which follows the new folder.
	/// </summary>
	[Fact]
	public void an_update_that_deletes_and_recreates_the_install_folder_is_reported()
	{
		string folder = Path.Combine(_root, "FE-BUDDY");
		string program = Path.Combine(folder, "FE-BUDDY.dll");
		string configMsi = Directory.CreateDirectory(Path.Combine(_root, "Config.Msi")).FullName;
		Directory.CreateDirectory(folder);
		File.WriteAllText(program, "the running build");

		using InstalledVersionWatcher watcher = new(program, "3.0.0-beta.13", Settle);
		using ManualResetEventSlim raised = new();
		string? reported = null;
		watcher.Updated += (_, version) =>
		{
			reported = version;
			raised.Set();
		};

		File.Move(program, Path.Combine(configMsi, "5db1f.rbf"));
		Directory.Delete(folder);
		Thread.Sleep(Settle * 3);
		Directory.CreateDirectory(folder);
		File.Copy(Assembly, program);

		Assert.True(raised.Wait(HangGuard), "The update wasn't reported.");
		Assert.Equal(AssemblyVersion, reported);
	}

	/// <summary>
	/// While the file is missing mid-install it is looked for again: the install folder is deleted and created again,
	/// its first check finds nothing, and the new file written later is still found.
	/// </summary>
	[Fact]
	public void a_file_missing_mid_install_is_looked_for_again()
	{
		string folder = Path.Combine(_root, "FE-BUDDY");
		string program = Path.Combine(folder, "FE-BUDDY.dll");
		string configMsi = Directory.CreateDirectory(Path.Combine(_root, "Config.Msi")).FullName;
		Directory.CreateDirectory(folder);
		File.WriteAllText(program, "the running build");
		int checks = 0;

		using InstalledVersionWatcher watcher = Watch(program, "3.0.0-beta.13", () =>
		{
			Interlocked.Increment(ref checks);
			return false;
		});

		using ManualResetEventSlim raised = new();
		watcher.Updated += (_, _) => raised.Set();

		File.Move(program, Path.Combine(configMsi, "5db1f.rbf"));
		Directory.Delete(folder);
		int before = Volatile.Read(ref checks);
		Directory.CreateDirectory(folder);
		Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref checks) > before, HangGuard), "The new folder wasn't checked.");
		File.Copy(Assembly, program);

		Assert.True(raised.Wait(HangGuard), "The update wasn't reported.");
	}

	/// <summary>
	/// While Windows Installer is still at work - the files around the new one still coming - the update isn't
	/// reported, however often it is looked at; once the install has finished, it is.
	/// </summary>
	[Fact]
	public void an_update_is_not_reported_while_an_install_is_running()
	{
		File.Copy(Assembly, Program);
		int asked = 0;
		bool installing = true;

		using InstalledVersionWatcher watcher = Watch(Program, AssemblyVersion, () =>
		{
			Interlocked.Increment(ref asked);
			return Volatile.Read(ref installing);
		});

		using ManualResetEventSlim raised = new();
		string? reported = null;
		watcher.Updated += (_, version) =>
		{
			reported = version;
			raised.Set();
		};

		File.Move(Program, Path.Combine(_root, "5db1f.rbf"));
		File.Copy(WpfAssembly, Program);

		Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref asked) >= 3, HangGuard), "The install wasn't asked about.");
		Assert.False(raised.IsSet);

		Volatile.Write(ref installing, false);

		Assert.True(raised.Wait(HangGuard), "The update wasn't reported once the install finished.");
		Assert.Equal(WpfVersion, reported);
	}

	/// <summary>An update rolled back - cancelled, or failed - before it finished leaves the running version, so nothing is reported.</summary>
	[Fact]
	public void a_rolled_back_update_is_not_reported()
	{
		File.Copy(Assembly, Program);
		string rollbackCopy = Path.Combine(_root, "5db1f.rbf");
		int asked = 0;
		bool installing = true;

		using InstalledVersionWatcher watcher = Watch(Program, AssemblyVersion, () =>
		{
			Interlocked.Increment(ref asked);
			return Volatile.Read(ref installing);
		});

		int updates = 0;
		watcher.Updated += (_, _) => Interlocked.Increment(ref updates);

		File.Move(Program, rollbackCopy);
		File.Copy(WpfAssembly, Program);
		Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref asked) >= 2, HangGuard), "The install wasn't asked about.");

		File.Delete(Program);
		File.Move(rollbackCopy, Program);
		Volatile.Write(ref installing, false);

		Assert.True(
			SpinWait.SpinUntil(() => AppLog.Entries.Any(e => e.Message == $"FE-BUDDY.dll was written again, still v{AssemblyVersion}."), HangGuard),
			"The check after the rollback didn't run.");
		Assert.Equal(0, Volatile.Read(ref updates));
	}

	/// <summary>
	/// After the missing file has been looked for as many times as it may be, a new install folder still starts the
	/// looking over: an uninstall, then an install later, is followed.
	/// </summary>
	[Fact]
	public void a_folder_recreated_after_the_missing_checks_ran_out_is_still_followed()
	{
		string folder = Path.Combine(_root, "FE-BUDDY");
		string program = Path.Combine(folder, "FE-BUDDY.dll");
		string configMsi = Directory.CreateDirectory(Path.Combine(_root, "Config.Msi")).FullName;
		Directory.CreateDirectory(folder);
		File.Copy(Assembly, program);
		int checks = 0;

		using InstalledVersionWatcher watcher = Watch(program, AssemblyVersion, () =>
		{
			Interlocked.Increment(ref checks);
			return false;
		}, maxMissingChecks: 3);

		using ManualResetEventSlim raised = new();
		watcher.Updated += (_, _) => raised.Set();

		File.Move(program, Path.Combine(configMsi, "5db1f.rbf"));
		Directory.Delete(folder);

		// The first check finds nothing, then three more do, and the looking stops.
		Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref checks) >= 4, HangGuard), "The missing file wasn't looked for.");
		Thread.Sleep(Settle * 3);

		int before = Volatile.Read(ref checks);
		Directory.CreateDirectory(folder);
		Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref checks) > before, HangGuard), "The new folder wasn't checked.");
		File.Copy(WpfAssembly, program);

		Assert.True(raised.Wait(HangGuard), "The update wasn't reported.");
	}

	/// <summary>A folder that can't be watched for a moment - gone again as its watch starts - doesn't stop the checks that follow.</summary>
	[Fact]
	public void a_watch_that_fails_to_start_once_does_not_stop_the_checks()
	{
		File.Copy(Assembly, Program);
		int checks = 0;
		int watches = 0;

		using InstalledVersionWatcher watcher = Watch(
			Program,
			AssemblyVersion,
			() =>
			{
				Interlocked.Increment(ref checks);
				return false;
			},
			folder => Interlocked.Increment(ref watches) == 2
				? throw new FileNotFoundException("The folder went as its watch started.")
				: new FileSystemWatcher(folder));

		using ManualResetEventSlim raised = new();
		watcher.Updated += (_, _) => raised.Set();

		// Missing, so the folder is watched again once the file is back - and that watch fails to start.
		File.Delete(Program);
		Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref checks) >= 1, HangGuard), "The missing file wasn't looked for.");
		File.Copy(WpfAssembly, Program);

		Assert.True(raised.Wait(HangGuard), "The update wasn't reported.");
		Assert.True(Volatile.Read(ref watches) >= 3);
	}

	/// <summary>The same version written again (a repair) isn't an update, and is logged as what it is; nor is anything once the watcher is stopped.</summary>
	[Fact]
	public void the_same_version_or_a_stopped_watcher_reports_nothing()
	{
		File.Copy(Assembly, Program);
		using InstalledVersionWatcher watcher = new(Program, AssemblyVersion, Settle);
		int updates = 0;
		watcher.Updated += (_, _) => Interlocked.Increment(ref updates);

		File.Copy(Assembly, Program, overwrite: true);
		Assert.True(SpinWait.SpinUntil(() => AppLog.Entries.Any(e => e.Message == $"FE-BUDDY.dll was written again, still v{AssemblyVersion}."), HangGuard));
		Assert.DoesNotContain(AppLog.Entries, e => e.Message.Contains("on disk", StringComparison.Ordinal));
		Assert.Null(watcher.ChangedVersion());

		InstalledVersionWatcher stopped = new(Program, "0.0.1", Settle);
		stopped.Updated += (_, _) => Interlocked.Increment(ref updates);
		stopped.Dispose();
		File.Copy(Assembly, Program, overwrite: true);
		Thread.Sleep(Settle * 5);

		Assert.Equal(0, updates);
		Assert.Throws<ArgumentException>(() => new InstalledVersionWatcher(" ", "3.0.0"));
	}
}
