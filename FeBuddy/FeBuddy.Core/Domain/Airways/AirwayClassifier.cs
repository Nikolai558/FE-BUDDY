using System.Text.RegularExpressions;

using FeBuddy.Core.Domain.Airways.Models;

namespace FeBuddy.Core.Domain.Airways;

/// <summary>
/// Classifies an airway as High, Low, or Other based on the highest <c>MAX_AUTH_ALT</c>
/// found across its segments.
/// </summary>
/// <remarks>
/// Classification comes from the published altitudes, not the airway's name: a name's letter
/// (J, Q, V, T...) is only a convention, and some airways do not follow it. Each airway lands
/// in exactly one class - it is never split across files by altitude.
/// </remarks>
public static class AirwayClassifier
{
	/// <summary>The <c>MAX_AUTH_ALT</c> (feet MSL) at or above which an airway is <see cref="AirwayAltitudeClass.High"/>: the base of Class A airspace.</summary>
	public const int HighAltitudeThresholdFeet = 18000;

	/// <summary>The value used for <see cref="Airway.Designation"/> when an <c>AWY_ID</c> has no leading letters.</summary>
	public const string UnknownDesignation = "Unknown";

	private static readonly Regex LeadingLettersPattern = new("^[A-Za-z]+", RegexOptions.Compiled);

	/// <summary>
	/// Derives an airway's designation from its <c>AWY_ID</c>: the leading letters before the
	/// first digit, upper-cased. <c>J3</c> -&gt; <c>J</c>, <c>V23</c> -&gt; <c>V</c>,
	/// <c>AT1</c> -&gt; <c>AT</c>, <c>Q100</c> -&gt; <c>Q</c>, <c>T295</c> -&gt; <c>T</c>.
	/// </summary>
	/// <remarks>
	/// The designation is deliberately <b>not</b> <c>AWY_BASE.AWY_DESIGNATION</c>: that field
	/// is a route category, and using it filed RNAV airways under <c>RN</c> even though their
	/// IDs start with <c>Q</c> or <c>T</c>. This one derived value feeds file naming, the
	/// designation filter, and the GUI's designation list.
	/// </remarks>
	/// <param name="awyId">The airway ID (<c>AWY_BASE.AWY_ID</c>).</param>
	/// <returns>
	/// The upper-cased leading-letter designation, or <see cref="UnknownDesignation"/> when the
	/// ID has no leading letters (which should not occur in real NASR data).
	/// </returns>
	public static string DeriveDesignation(string? awyId)
	{
		if (string.IsNullOrWhiteSpace(awyId))
		{
			return UnknownDesignation;
		}

		Match match = LeadingLettersPattern.Match(awyId.Trim());
		return match.Success ? match.Value.ToUpperInvariant() : UnknownDesignation;
	}

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
