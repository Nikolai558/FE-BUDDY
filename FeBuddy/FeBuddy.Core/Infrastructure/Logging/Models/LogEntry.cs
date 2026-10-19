namespace FeBuddy.Core.Infrastructure.Logging.Models;

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
