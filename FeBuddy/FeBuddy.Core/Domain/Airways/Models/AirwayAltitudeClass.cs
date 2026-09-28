namespace FeBuddy.Core.Domain.Airways.Models;

/// <summary>
/// The altitude classification of an airway, derived from the highest
/// <c>MAX_AUTH_ALT</c> found across all of its <c>AWY_SEG_ALT</c> segments.
/// </summary>
/// <remarks>
/// See <c>AirwayClassifier</c> for the exact classification rule. One airway is classified
/// into exactly one of these three values. It does not decide the Airways High and Low files -
/// the user puts each designation in one or both - but the High and Low values also name those
/// files' CRC-ERAM defaults, and in a designation file each airway gets its own class's.
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
