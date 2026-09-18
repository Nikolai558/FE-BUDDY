using System.Globalization;
using System.Threading.Channels;

using FeBuddy.Core.Configuration;

namespace FeBuddy.Core.Services.General;

/// <summary>
/// Severity of an <see cref="LogEntry"/>, ordered from least to most important.
/// </summary>
/// <remarks>
/// <see cref="Debug"/> entries are only recorded when <see cref="DevMode.IsEnabled"/> is
/// <see langword="true"/>; every other level is always recorded. The GUI's activity-log
/// viewer (Dashboard) filters on these same values, so the level assigned here is the level
/// the user sees.
/// </remarks>
public enum LogLevel
{
	/// <summary>Diagnostic detail useful only while troubleshooting. Gated by <see cref="DevMode.IsEnabled"/>.</summary>
	Debug = 0,

	/// <summary>Routine progress or informational notice. Not a problem.</summary>
	Info = 1,

	/// <summary>An operation completed successfully. Used to punctuate the end of a long task.</summary>
	Success = 2,

	/// <summary>Something unexpected that did not stop the operation, but the user should know about it.</summary>
	Warning = 3,

	/// <summary>An operation failed. The user almost certainly needs to act.</summary>
	Error = 4,
}

/// <summary>
/// A single application log record.
/// </summary>
/// <param name="UtcTimestamp">When the entry was written, in UTC (displayed as a Zulu time in the GUI).</param>
/// <param name="Level">The entry's severity.</param>
/// <param name="Source">
/// A short tag identifying the component that raised the entry, e.g. <c>"AirwayService"</c> or
/// <c>"Launch"</c>. Used for the source column in the activity log.
/// </param>
/// <param name="Message">The human-readable message.</param>
public record LogEntry(DateTime UtcTimestamp, LogLevel Level, string Source, string Message);

/// <summary>
/// Process-wide application log. Every service warning, launch step, and run result flows
/// through here so there is exactly one stream of truth that the log file and the Dashboard
/// activity-log viewer are both views over.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Write(LogLevel, string, string)"/> is safe to call from any thread and never
/// blocks the caller on file I/O - entries are handed to a background writer via an unbounded
/// channel. In-memory recording and <see cref="EntryAdded"/> are synchronous, so a caller that
/// writes an entry and then reads <see cref="Entries"/> sees it immediately.
/// </para>
/// <para>
/// The GUI subscribes to <see cref="EntryAdded"/> and is responsible for marshalling to its
/// dispatcher; the event is raised on whatever thread called <see cref="Write(LogLevel, string, string)"/>.
/// </para>
/// </remarks>
public static class AppLog
{
	private static readonly object _gate = new();
	private static readonly List<LogEntry> _entries = new();

	private static Channel<LogEntry> _fileChannel = CreateChannel();
	private static Task _fileWriterTask = Task.CompletedTask;
	private static string _logDirectory = GetDefaultLogDirectory();
	private static bool _fileSinkStarted;

	/// <summary>
	/// Raised once for every entry that is recorded (after it has been added to
	/// <see cref="Entries"/>). Not raised for <see cref="LogLevel.Debug"/> entries that are
	/// suppressed because <see cref="DevMode.IsEnabled"/> is <see langword="false"/>.
	/// </summary>
	public static event EventHandler<LogEntry>? EntryAdded;

	/// <summary>
	/// Every entry recorded so far, oldest first. Returns a snapshot - safe to enumerate while
	/// other threads continue logging.
	/// </summary>
	public static IReadOnlyList<LogEntry> Entries
	{
		get
		{
			lock (_gate)
			{
				return _entries.ToArray();
			}
		}
	}

	/// <summary>
	/// The folder log files are written to: <c>%APPDATA%\FE-Buddy\Logs</c> by default.
	/// </summary>
	public static string LogDirectory
	{
		get
		{
			lock (_gate)
			{
				return _logDirectory;
			}
		}
	}

	/// <summary>
	/// Records a log entry: adds it to <see cref="Entries"/>, raises <see cref="EntryAdded"/>,
	/// and queues it for the log file. Never throws and never blocks on file I/O.
	/// </summary>
	/// <param name="level">The entry's severity.</param>
	/// <param name="source">A short component tag, e.g. <c>"AirwayService"</c>.</param>
	/// <param name="message">The message text.</param>
	public static void Write(LogLevel level, string source, string message)
	{
		// DevMode gates Debug entries only - they are neither recorded, raised, nor written.
		if (level == LogLevel.Debug && !DevMode.IsEnabled)
		{
			return;
		}

		LogEntry entry = new(DateTime.UtcNow, level, source ?? string.Empty, message ?? string.Empty);

		lock (_gate)
		{
			_entries.Add(entry);
		}

		// Fire-and-forget onto the background writer. TryWrite on an unbounded channel only
		// fails if the channel has been completed, which never happens in normal operation.
		_fileChannel.Writer.TryWrite(entry);

		try
		{
			EntryAdded?.Invoke(null, entry);
		}
		catch
		{
			// A misbehaving subscriber must never break logging for everyone else.
		}
	}

	/// <summary>
	/// Convenience overload for <see cref="LogLevel.Debug"/>.
	/// </summary>
	/// <param name="source">A short component tag.</param>
	/// <param name="message">The message text.</param>
	public static void Debug(string source, string message) => Write(LogLevel.Debug, source, message);

	/// <summary>
	/// Convenience overload for <see cref="LogLevel.Info"/>.
	/// </summary>
	/// <param name="source">A short component tag.</param>
	/// <param name="message">The message text.</param>
	public static void Info(string source, string message) => Write(LogLevel.Info, source, message);

	/// <summary>
	/// Convenience overload for <see cref="LogLevel.Success"/>.
	/// </summary>
	/// <param name="source">A short component tag.</param>
	/// <param name="message">The message text.</param>
	public static void Success(string source, string message) => Write(LogLevel.Success, source, message);

	/// <summary>
	/// Convenience overload for <see cref="LogLevel.Warning"/>.
	/// </summary>
	/// <param name="source">A short component tag.</param>
	/// <param name="message">The message text.</param>
	public static void Warning(string source, string message) => Write(LogLevel.Warning, source, message);

	/// <summary>
	/// Convenience overload for <see cref="LogLevel.Error"/>.
	/// </summary>
	/// <param name="source">A short component tag.</param>
	/// <param name="message">The message text.</param>
	public static void Error(string source, string message) => Write(LogLevel.Error, source, message);

	/// <summary>
	/// Starts the background file sink and prunes log files older than
	/// <paramref name="retentionDays"/> days. Call once from the launch sequence. Calling it
	/// again is a no-op except that pruning runs again.
	/// </summary>
	/// <param name="retentionDays">How many days of log files to keep. Older files are deleted.</param>
	public static void StartFileSink(int retentionDays = 30)
	{
		lock (_gate)
		{
			if (!_fileSinkStarted)
			{
				_fileSinkStarted = true;
				_fileWriterTask = Task.Run(() => ConsumeAsync(_fileChannel.Reader, _logDirectory));
			}
		}

		PruneOldLogs(retentionDays);
	}

	/// <summary>
	/// Deletes <c>FE-Buddy_*.log</c> files in <see cref="LogDirectory"/> whose date is older
	/// than <paramref name="retentionDays"/> days. Best-effort - never throws.
	/// </summary>
	/// <param name="retentionDays">How many days of log files to keep.</param>
	public static void PruneOldLogs(int retentionDays = 30)
	{
		try
		{
			string directory = LogDirectory;

			if (!Directory.Exists(directory))
			{
				return;
			}

			DateTime cutoffUtc = DateTime.UtcNow.Date.AddDays(-Math.Abs(retentionDays));

			foreach (string file in Directory.EnumerateFiles(directory, "FE-Buddy_*.log"))
			{
				DateTime fileDate = TryParseLogFileDate(file, out DateTime parsed)
					? parsed
					: File.GetLastWriteTimeUtc(file).Date;

				if (fileDate < cutoffUtc)
				{
					try
					{
						File.Delete(file);
					}
					catch
					{
						// A locked or already-removed file is not worth failing launch over.
					}
				}
			}
		}
		catch
		{
			// Pruning is housekeeping; a failure here must not affect anything else.
		}
	}

	/// <summary>
	/// The full path of the log file for a given UTC date:
	/// <c>%APPDATA%\FE-Buddy\Logs\FE-Buddy_yyyy-MM-dd.log</c>.
	/// </summary>
	/// <param name="utcDate">The date whose log file path is wanted.</param>
	/// <returns>The absolute log file path.</returns>
	public static string GetLogFilePath(DateTime utcDate) =>
		Path.Combine(LogDirectory, $"FE-Buddy_{utcDate:yyyy-MM-dd}.log");

	/// <summary>
	/// Points the log directory somewhere else and resets in-memory state. For unit tests only,
	/// so a test never writes into the real <c>%APPDATA%</c> tree.
	/// </summary>
	/// <param name="logDirectory">
	/// The directory to write log files into, or <see langword="null"/> to restore the default.
	/// </param>
	internal static void ConfigureForTesting(string? logDirectory)
	{
		FlushForTesting();

		lock (_gate)
		{
			_entries.Clear();
			EntryAdded = null;
			_logDirectory = logDirectory ?? GetDefaultLogDirectory();
			_fileChannel = CreateChannel();
			_fileWriterTask = Task.CompletedTask;
			_fileSinkStarted = false;
		}
	}

	/// <summary>
	/// Completes the background writer and waits for every queued entry to reach the file.
	/// For unit tests that need to assert on file contents.
	/// </summary>
	internal static void FlushForTesting()
	{
		Channel<LogEntry> channel;
		Task writer;

		lock (_gate)
		{
			channel = _fileChannel;
			writer = _fileWriterTask;
		}

		channel.Writer.TryComplete();

		try
		{
			writer.Wait(TimeSpan.FromSeconds(5));
		}
		catch
		{
			// Nothing actionable in a test-teardown flush failure.
		}
	}

	private static Channel<LogEntry> CreateChannel() =>
		Channel.CreateUnbounded<LogEntry>(new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = false,
		});

	private static async Task ConsumeAsync(ChannelReader<LogEntry> reader, string directory)
	{
		await foreach (LogEntry entry in reader.ReadAllAsync().ConfigureAwait(false))
		{
			try
			{
				Directory.CreateDirectory(directory);

				string path = Path.Combine(directory, $"FE-Buddy_{entry.UtcTimestamp:yyyy-MM-dd}.log");
				string line = string.Format(
					CultureInfo.InvariantCulture,
					"{0:yyyy-MM-dd HH:mm:ss}Z  {1,-7}  {2,-24}  {3}{4}",
					entry.UtcTimestamp,
					entry.Level.ToString().ToUpperInvariant(),
					entry.Source,
					entry.Message,
					Environment.NewLine);

				await File.AppendAllTextAsync(path, line).ConfigureAwait(false);
			}
			catch
			{
				// The log file is a convenience; losing a line to an I/O error must not crash
				// the writer loop or the app.
			}
		}
	}

	private static string GetDefaultLogDirectory() =>
		Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"FE-Buddy",
			"Logs");

	private static bool TryParseLogFileDate(string filePath, out DateTime date)
	{
		string name = Path.GetFileNameWithoutExtension(filePath);
		const string prefix = "FE-Buddy_";

		if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
			&& DateTime.TryParseExact(
				name[prefix.Length..],
				"yyyy-MM-dd",
				CultureInfo.InvariantCulture,
				DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
				out date))
		{
			date = date.Date;
			return true;
		}

		date = default;
		return false;
	}
}
