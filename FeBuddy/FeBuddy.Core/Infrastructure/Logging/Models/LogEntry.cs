using FeBuddy.Core.Infrastructure.Configuration;

namespace FeBuddy.Core.Infrastructure.Logging.Models;

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
