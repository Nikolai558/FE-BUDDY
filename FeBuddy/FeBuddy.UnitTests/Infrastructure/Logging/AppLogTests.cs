using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Infrastructure.Logging;

/// <summary>
/// Shared xUnit collection for tests that touch process-wide static state in
/// <see cref="AppLog"/> (directly, or indirectly through a component that logs). Parallel
/// execution is disabled so these tests do not clobber each other's global log state.
/// </summary>
[CollectionDefinition("AppLog", DisableParallelization = true)]
public sealed class AppLogCollection;

/// <summary>
/// Verifies <see cref="AppLog"/> records in memory, raises <see cref="AppLog.EntryAdded"/>
/// once per entry, gates <see cref="LogLevel.Debug"/> on <see cref="DevMode.IsEnabled"/>,
/// writes to a dated file, and prunes stale files.
/// </summary>
[Collection("AppLog")]
public sealed class AppLogTests : IDisposable
{
	private readonly string _logDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_Logs_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points <see cref="AppLog"/> at a throwaway directory and clears its state.</summary>
	public AppLogTests()
	{
		AppLog.ConfigureForTesting(_logDirectory);
		DevMode.IsEnabled = false;
	}

	/// <summary>Restores defaults and removes the throwaway log directory.</summary>
	public void Dispose()
	{
		AppLog.ConfigureForTesting(null);
		DevMode.IsEnabled = false;

		try
		{
			if (Directory.Exists(_logDirectory))
			{
				Directory.Delete(_logDirectory, recursive: true);
			}
		}
		catch
		{
			// Best-effort cleanup.
		}
	}

	/// <summary>A recorded entry appears in <see cref="AppLog.Entries"/> with its fields intact.</summary>
	[Fact]
	public void write_records_entry_in_memory()
	{
		AppLog.Write(LogLevel.Info, "TestSource", "hello world");

		LogEntry entry = Assert.Single(AppLog.Entries);
		Assert.Equal(LogLevel.Info, entry.Level);
		Assert.Equal("TestSource", entry.Source);
		Assert.Equal("hello world", entry.Message);
		Assert.Equal(DateTimeKind.Utc, entry.UtcTimestamp.Kind);
	}

	/// <summary><see cref="AppLog.EntryAdded"/> fires exactly once per recorded entry.</summary>
	[Fact]
	public void write_raises_entry_added_once_per_entry()
	{
		List<LogEntry> received = [];
		AppLog.EntryAdded += (_, e) => received.Add(e);

		AppLog.Write(LogLevel.Info, "s", "one");
		AppLog.Write(LogLevel.Warning, "s", "two");
		AppLog.Write(LogLevel.Error, "s", "three");

		Assert.Equal(3, received.Count);
		Assert.Equal(["one", "two", "three"], received.Select(e => e.Message));
	}

	/// <summary>A <see cref="LogLevel.Debug"/> entry is dropped entirely when DevMode is off.</summary>
	[Fact]
	public void write_debug_suppressed_when_dev_mode_off()
	{
		DevMode.IsEnabled = false;
		bool raised = false;
		AppLog.EntryAdded += (_, _) => raised = true;

		AppLog.Write(LogLevel.Debug, "s", "diagnostic");

		Assert.Empty(AppLog.Entries);
		Assert.False(raised);
	}

	/// <summary>A <see cref="LogLevel.Debug"/> entry is recorded when DevMode is on.</summary>
	[Fact]
	public void write_debug_recorded_when_dev_mode_on()
	{
		DevMode.IsEnabled = true;

		AppLog.Write(LogLevel.Debug, "s", "diagnostic");

		LogEntry entry = Assert.Single(AppLog.Entries);
		Assert.Equal(LogLevel.Debug, entry.Level);
	}

	/// <summary>Entries reach the dated log file once the sink is started and flushed.</summary>
	[Fact]
	public void start_file_sink_writes_entries_to_dated_file()
	{
		AppLog.StartFileSink();
		AppLog.Write(LogLevel.Warning, "AirwayService", "a warning happened");
		AppLog.FlushForTesting();

		string path = AppLog.GetLogFilePath(DateTime.UtcNow);
		Assert.True(File.Exists(path), $"expected log file at {path}");

		string contents = File.ReadAllText(path);
		Assert.Contains("a warning happened", contents);
		Assert.Contains("WARNING", contents);
		Assert.Contains("AirwayService", contents);
	}

	/// <summary>Files older than the retention window are deleted; recent ones are kept.</summary>
	[Fact]
	public void prune_old_logs_deletes_files_older_than_retention()
	{
		Directory.CreateDirectory(_logDirectory);

		string oldFile = Path.Combine(_logDirectory, "FE-Buddy_2000-01-01.log");
		string recentFile = AppLog.GetLogFilePath(DateTime.UtcNow);
		File.WriteAllText(oldFile, "old");
		File.WriteAllText(recentFile, "recent");

		AppLog.PruneOldLogs(retentionDays: 30);

		Assert.False(File.Exists(oldFile));
		Assert.True(File.Exists(recentFile));
	}

	/// <summary>Before any clear, <see cref="AppLog.DisplayEntries"/> holds every recorded entry.</summary>
	[Fact]
	public void display_entries_match_entries_until_cleared()
	{
		AppLog.Info("s", "one");
		AppLog.Warning("s", "two");

		Assert.Equal(AppLog.Entries, AppLog.DisplayEntries);
	}

	/// <summary>
	/// <see cref="AppLog.ClearDisplay"/> empties the display only: <see cref="AppLog.Entries"/> keeps
	/// everything, and entries written afterwards show up on the display.
	/// </summary>
	[Fact]
	public void clear_display_drops_earlier_entries_from_display_only()
	{
		AppLog.Info("s", "before one");
		AppLog.Warning("s", "before two");

		AppLog.ClearDisplay();

		Assert.Empty(AppLog.DisplayEntries);
		Assert.Equal(2, AppLog.Entries.Count);

		AppLog.Success("s", "after");

		LogEntry shown = Assert.Single(AppLog.DisplayEntries);
		Assert.Equal("after", shown.Message);
		Assert.Equal(["before one", "before two", "after"], AppLog.Entries.Select(e => e.Message));
	}

	/// <summary>A second clear moves the display start up to the newest entry again.</summary>
	[Fact]
	public void clear_display_twice_starts_from_the_latest_clear()
	{
		AppLog.Info("s", "one");
		AppLog.ClearDisplay();
		AppLog.Info("s", "two");
		AppLog.ClearDisplay();
		AppLog.Info("s", "three");

		Assert.Equal(["three"], AppLog.DisplayEntries.Select(e => e.Message));
	}

	/// <summary>Clearing the display never touches the log file: entries before and after reach it.</summary>
	[Fact]
	public void clear_display_leaves_the_log_file_unchanged()
	{
		AppLog.StartFileSink();
		AppLog.Info("s", "before the clear");
		AppLog.ClearDisplay();
		AppLog.Info("s", "after the clear");
		AppLog.FlushForTesting();

		string contents = File.ReadAllText(AppLog.GetLogFilePath(DateTime.UtcNow));
		Assert.Contains("before the clear", contents);
		Assert.Contains("after the clear", contents);
	}

	/// <summary><see cref="AppLog.ConfigureForTesting"/> resets the display start with the entries.</summary>
	[Fact]
	public void configure_for_testing_resets_the_display_start()
	{
		AppLog.Info("s", "one");
		AppLog.ClearDisplay();

		AppLog.ConfigureForTesting(_logDirectory);
		AppLog.Info("s", "fresh");

		Assert.Equal(["fresh"], AppLog.DisplayEntries.Select(e => e.Message));
	}

	/// <summary><see cref="AppLog.Error"/> records at <see cref="LogLevel.Error"/>.</summary>
	[Fact]
	public void error_records_an_error_entry()
	{
		AppLog.Error("Test", "boom");

		LogEntry entry = Assert.Single(AppLog.Entries, e => e.Message == "boom");
		Assert.Equal(LogLevel.Error, entry.Level);
	}

	/// <summary>Files that are not FE-Buddy dated logs are never pruned.</summary>
	[Fact]
	public void prune_old_logs_leaves_files_it_does_not_recognize()
	{
		Directory.CreateDirectory(_logDirectory);

		string notes = Path.Combine(_logDirectory, "notes.log");
		string badDate = Path.Combine(_logDirectory, "FE-Buddy_yesterday.log");
		File.WriteAllText(notes, "keep");
		File.WriteAllText(badDate, "keep");

		AppLog.PruneOldLogs(retentionDays: 30);

		Assert.True(File.Exists(notes));
		Assert.True(File.Exists(badDate));
	}

	/// <summary>Following the display returns what it holds since the last clear, and calls back for each later entry.</summary>
	[Fact]
	public void following_the_display_returns_what_it_holds_and_calls_back_for_later_entries()
	{
		AppLog.Info("Test", "before the clear");
		AppLog.ClearDisplay();
		AppLog.Info("Test", "one");
		List<string> later = [];

		IReadOnlyList<LogEntry> held = AppLog.FollowDisplay((_, entry) => later.Add(entry.Message));
		AppLog.Info("Test", "two");

		Assert.Equal(["one"], held.Select(entry => entry.Message));
		Assert.Equal(["two"], later);
		Assert.Throws<ArgumentNullException>(() => AppLog.FollowDisplay(null!));
	}

	/// <summary>
	/// An entry logged on another thread while the display is followed is in the snapshot or the callbacks,
	/// exactly once: none missed between the two, none in both.
	/// </summary>
	[Fact]
	public async Task an_entry_logged_while_following_starts_is_seen_exactly_once()
	{
		for (int attempt = 0; attempt < 20; attempt++)
		{
			AppLog.ConfigureForTesting(_logDirectory);
			using ManualResetEventSlim go = new();
			System.Collections.Concurrent.ConcurrentQueue<LogEntry> later = new();

			Task logging = Task.Run(() =>
			{
				go.Wait();
				for (int i = 0; i < 200; i++)
				{
					AppLog.Info("Test", $"entry {i}");
				}
			});

			go.Set();
			IReadOnlyList<LogEntry> held = AppLog.FollowDisplay((_, entry) => later.Enqueue(entry));
			await logging;

			List<LogEntry> seen = [.. held, .. later];
			Assert.Equal(200, seen.Count);
			Assert.Equal(200, seen.Distinct(ReferenceEqualityComparer.Instance).Count());
		}
	}
}
