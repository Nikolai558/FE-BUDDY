using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Models;

/// <summary>
/// Common result shape shared by every FE-Buddy service's top-level entry point.
/// </summary>
/// <remarks>
/// <para>
/// Each service (e.g. <c>AirwayServiceResult</c>) derives from this record and adds its own
/// fields, such as the files written and per-item counts.
/// </para>
/// <para>
/// <see cref="Messages"/> is always complete, whatever <see cref="DevMode"/> says: a service
/// that skips a bad record must never let that go unreported. Summarizing many similar messages
/// is the caller's job (the GUI, or <c>FeBuddy.Harness</c>'s console report), not the library's.
/// </para>
/// </remarks>
public abstract record ServiceResult
{
	/// <summary>
	/// Every levelled message the service emitted while running. The service still completed;
	/// the caller presents these grouped by level. Each service also copies them to
	/// <see cref="AppLog"/>.
	/// </summary>
	public required IReadOnlyList<ServiceMessage> Messages { get; init; }

	/// <summary>
	/// The text of every <see cref="LogLevel.Warning"/> and <see cref="LogLevel.Error"/> in
	/// <see cref="Messages"/> - what a plain-text report lists. Use <see cref="Messages"/> for
	/// anything level-aware.
	/// </summary>
	public IReadOnlyList<string> Warnings =>
		[.. Messages
			.Where(m => m.Level is LogLevel.Warning or LogLevel.Error)
			.Select(m => m.Text)];

	/// <summary>Total wall-clock time the service took to run.</summary>
	public required TimeSpan Elapsed { get; init; }
}
