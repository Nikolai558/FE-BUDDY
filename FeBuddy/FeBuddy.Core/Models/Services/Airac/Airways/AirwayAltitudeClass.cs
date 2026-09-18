namespace FeBuddy.Core.Models.Services.Airac.Airways;

/// <summary>
/// The altitude classification of an airway, derived from the highest
/// <c>MAX_AUTH_ALT</c> found across all of its <c>AWY_SEG_ALT</c> segments.
/// </summary>
/// <remarks>
/// See <c>AirwayClassifier</c> for the exact classification rule. One airway is classified
/// into exactly one of these three values; an airway is never split across files by
/// altitude.
/// </remarks>
public enum AirwayAltitudeClass
{
	/// <summary>Highest segment MaxAuthAlt &gt;= 18,000 feet.</summary>
	High,

	/// <summary>Highest segment MaxAuthAlt &gt; 0 and &lt; 18,000 feet.</summary>
	Low,

	/// <summary>No segment has a usable MaxAuthAlt value (or the highest value is &lt;= 0).</summary>
	Other
}
