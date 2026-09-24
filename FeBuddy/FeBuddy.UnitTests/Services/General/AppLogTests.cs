using FeBuddy.Core.Configuration;
using FeBuddy.Core.Services.General;

namespace FeBuddy.UnitTests.Services.General;

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
	public void Write_RecordsEntryInMemory()
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
	public void Write_RaisesEntryAddedOncePerEntry()
	{
		List<LogEntry> received = new();
		AppLog.EntryAdded += (_, e) => received.Add(e);

		AppLog.Write(LogLevel.Info, "s", "one");
		AppLog.Write(LogLevel.Warning, "s", "two");
		AppLog.Write(LogLevel.Error, "s", "three");

		Assert.Equal(3, received.Count);
		Assert.Equal(new[] { "one", "two", "three" }, received.Select(e => e.Message));
	}

	/// <summary>A <see cref="LogLevel.Debug"/> entry is dropped entirely when DevMode is off.</summary>
	[Fact]
	public void Write_Debug_SuppressedWhenDevModeOff()
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
	public void Write_Debug_RecordedWhenDevModeOn()
	{
		DevMode.IsEnabled = true;

		AppLog.Write(LogLevel.Debug, "s", "diagnostic");

		LogEntry entry = Assert.Single(AppLog.Entries);
		Assert.Equal(LogLevel.Debug, entry.Level);
	}

	/// <summary>Entries reach the dated log file once the sink is started and flushed.</summary>
	[Fact]
	public void StartFileSink_WritesEntriesToDatedFile()
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
	public void PruneOldLogs_DeletesFilesOlderThanRetention()
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

	/// <summary><see cref="AppLog.Error"/> records at <see cref="LogLevel.Error"/>.</summary>
	[Fact]
	public void Error_RecordsAnErrorEntry()
	{
		AppLog.Error("Test", "boom");

		LogEntry entry = Assert.Single(AppLog.Entries, e => e.Message == "boom");
		Assert.Equal(LogLevel.Error, entry.Level);
	}

	/// <summary>Files that are not FE-Buddy dated logs are never pruned.</summary>
	[Fact]
	public void PruneOldLogs_LeavesFilesItDoesNotRecognize()
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
}
