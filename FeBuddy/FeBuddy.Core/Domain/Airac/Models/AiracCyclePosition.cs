namespace FeBuddy.Core.Domain.Airac.Models;

/// <summary>
/// Which AIRAC cycle, relative to the one in effect today, is meant.
/// </summary>
/// <remarks>
/// FE-Buddy offers exactly these three cycles of NASR data - previous, current and the next
/// (preview) cycle - and keeps no others.
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
