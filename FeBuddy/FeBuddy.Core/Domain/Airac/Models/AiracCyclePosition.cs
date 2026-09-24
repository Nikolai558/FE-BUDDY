namespace FeBuddy.Core.Domain.Airac.Models;

/// <summary>
/// Which AIRAC cycle relative to today is being referenced.
/// </summary>
/// <remarks>
/// FE-Buddy keeps up to three cycles of NASR data available at once - the previous cycle,
/// the current cycle, and the next (preview) cycle - matching the dev notes' AIRAC data
/// download management rules.
/// </remarks>
public enum AiracCyclePosition
{
	/// <summary>The cycle immediately before the one currently in effect.</summary>
	Previous,

	/// <summary>The cycle in effect as of today (UTC).</summary>
	Current,

	/// <summary>The cycle that becomes effective next (a preview of upcoming data).</summary>
	Next
}
