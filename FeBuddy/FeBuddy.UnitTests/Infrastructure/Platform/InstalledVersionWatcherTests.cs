using FeBuddy.Core.Infrastructure.Platform;

namespace FeBuddy.UnitTests.Infrastructure.Platform;

/// <summary>
/// Covers <see cref="InstalledVersionWatcher"/>: a file's version read as the running one is, and an update
/// written over the watched file reported once it settles, with its version - but not a write of the same
/// version, and not once stopped.
/// </summary>
public sealed class InstalledVersionWatcherTests : IDisposable
{
	private static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(100);

	// A real assembly with a product version: Core itself.
	private static readonly string Assembly = typeof(AppVersion).Assembly.Location;
	private static readonly string AssemblyVersion = AppVersion.Of(typeof(AppVersion).Assembly);

	private readonly string _root = Directory.CreateTempSubdirectory("febuddy-watch-").FullName;

	/// <summary>Deletes the throwaway folder.</summary>
	public void Dispose()
	{
		try
		{
			Directory.Delete(_root, recursive: true);
		}
		catch (IOException)
		{
			// Best-effort cleanup.
		}
	}

	private string Program => Path.Combine(_root, "FE-BUDDY.dll");

	[Fact]
	public void a_files_version_is_read_as_the_running_one_is()
	{
		File.Copy(Assembly, Program);
		File.WriteAllText(Path.Combine(_root, "notes.txt"), "not a program");

		Assert.Equal(AssemblyVersion, InstalledVersionWatcher.VersionOf(Program));
		Assert.Null(InstalledVersionWatcher.VersionOf(Path.Combine(_root, "notes.txt")));
		Assert.Null(InstalledVersionWatcher.VersionOf(Path.Combine(_root, "gone.dll")));
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

		Assert.True(raised.Wait(TimeSpan.FromSeconds(10)));
		File.Copy(Assembly, Program, overwrite: true);
		Thread.Sleep(Settle * 4);

		Assert.Equal([AssemblyVersion], updated);
		Assert.Equal(AssemblyVersion, watcher.ChangedVersion());
	}

	/// <summary>The same version written again (a repair) isn't an update; nor is anything once the watcher is stopped.</summary>
	[Fact]
	public void the_same_version_or_a_stopped_watcher_reports_nothing()
	{
		File.Copy(Assembly, Program);
		using InstalledVersionWatcher watcher = new(Program, AssemblyVersion, Settle);
		int updates = 0;
		watcher.Updated += (_, _) => Interlocked.Increment(ref updates);

		File.Copy(Assembly, Program, overwrite: true);
		Thread.Sleep(Settle * 5);
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
