using FEBuddyLibrary.Models.Services.Airways;

namespace FEBuddyLibrary.Services.Airways;

/// <summary>
/// Classifies an airway as High, Low, or Other based on the highest <c>MAX_AUTH_ALT</c>
/// found across its segments.
/// </summary>
/// <remarks>
/// See Decisions Log 10.2 in the build plan: classification is intentionally based on
/// altitude data rather than airway ID naming conventions (old FE-Buddy checked whether the
/// ID contained "Q" or "J"), and one airway is always classified into exactly one class.
/// </remarks>
public static class AirwayClassifier
{
	/// <summary>Feet AGL/MSL at or above which an airway is classified as <see cref="AirwayAltitudeClass.High"/>.</summary>
	public const int HighAltitudeThresholdFeet = 18000;

	/// <summary>
	/// Determines an airway's altitude classification from its normalized segments.
	/// </summary>
	/// <param name="segments">The airway's normalized segments.</param>
	/// <returns>
	/// The classification and the highest <c>MaxAuthAlt</c> found (or <see langword="null"/>
	/// when no segment carried a usable value).
	/// <list type="bullet">
	///   <item>Highest MaxAuthAlt &gt;= 18,000 -&gt; <see cref="AirwayAltitudeClass.High"/></item>
	///   <item>Highest MaxAuthAlt &gt; 0 and &lt; 18,000 -&gt; <see cref="AirwayAltitudeClass.Low"/></item>
	///   <item>No segment has a usable value, or the highest is &lt;= 0 -&gt; <see cref="AirwayAltitudeClass.Other"/></item>
	/// </list>
	/// </returns>
	public static (AirwayAltitudeClass AltitudeClass, int? MaxAuthAlt) Classify(
		IEnumerable<AirwaySegment> segments)
	{
		ArgumentNullException.ThrowIfNull(segments);

		int? highestMaxAuthAlt = null;

		foreach (AirwaySegment segment in segments)
		{
			if (segment.MaxAuthAlt is not int altitude)
			{
				continue;
			}

			highestMaxAuthAlt = highestMaxAuthAlt is null
				? altitude
				: Math.Max(highestMaxAuthAlt.Value, altitude);
		}

		if (highestMaxAuthAlt is null || highestMaxAuthAlt.Value <= 0)
		{
			return (AirwayAltitudeClass.Other, highestMaxAuthAlt);
		}

		return highestMaxAuthAlt.Value >= HighAltitudeThresholdFeet
			? (AirwayAltitudeClass.High, highestMaxAuthAlt)
			: (AirwayAltitudeClass.Low, highestMaxAuthAlt);
	}
}
