using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Models;

/// <summary>
/// One levelled message a service emits while running - the replacement for the old flat
/// <c>List&lt;string&gt; Warnings</c> (remediation plan 3.8). The level survives all the way to
/// the GUI's run panel and the Dashboard activity log, so a run reporting 483 routine buffer
/// notices reads as clean rather than as a wall of amber.
/// </summary>
/// <param name="Level">The message's severity.</param>
/// <param name="Source">A short component tag, e.g. <c>"AirwayWaypointBuffer"</c>.</param>
/// <param name="Text">The message text.</param>
public sealed record ServiceMessage(LogLevel Level, string Source, string Text)
{
	/// <summary>
	/// Whether the GUI should also show this message on the run's Review tab. Kept for the few
	/// messages that change what the user expects to find on disk - e.g. "nothing matched your
	/// filters, so no files were written" - rather than the routine per-feature notices.
	/// </summary>
	public bool IsAdvisory { get; init; }
}
