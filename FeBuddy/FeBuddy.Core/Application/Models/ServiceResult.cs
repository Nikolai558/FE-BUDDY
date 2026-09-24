using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Models;

/// <summary>
/// Common result shape shared by every FE-Buddy service's top-level entry point.
/// </summary>
/// <remarks>
/// Individual services (e.g. <c>AirwayServiceResult</c>) derive from this record and add
/// service-specific fields such as file paths written and per-item counts.
///
/// <para>
/// <see cref="Warnings"/> is always the complete list, regardless of
/// <see cref="DevMode"/>. A service that skips a bad record and continues
/// (see the Airways service's mid-airway unresolved-waypoint handling) must never let that
/// problem go unreported just because developer mode is off — silently dropping warnings
/// would defeat the purpose of collecting them in the first place. Trimming detail for
/// display (e.g. summarizing many similar warnings) is a presentation concern and belongs in
/// the caller (<c>FeBuddy.Harness</c>'s <c>ConsoleReport</c> today, the GUI later), not in the
/// library.
/// </para>
/// </remarks>
public abstract record ServiceResult
{
	/// <summary>
	/// Every levelled message the service emitted while running (remediation plan 3.8). The
	/// service still completed; the caller presents these grouped by level. Also mirrored to
	/// <see cref="AppLog"/> by the service's top-level entry point.
	/// </summary>
	public required IReadOnlyList<ServiceMessage> Messages { get; init; }

	/// <summary>
	/// Backwards-compatible view of <see cref="Messages"/>: just the text of the
	/// <see cref="LogLevel.Warning"/> and <see cref="LogLevel.Error"/>
	/// entries. Prefer <see cref="Messages"/> for anything level-aware.
	/// </summary>
	public IReadOnlyList<string> Warnings =>
		[.. Messages
			.Where(m => m.Level is LogLevel.Warning or LogLevel.Error)
			.Select(m => m.Text)];

	/// <summary>Total wall-clock time the service took to run.</summary>
	public required TimeSpan Elapsed { get; init; }
}
