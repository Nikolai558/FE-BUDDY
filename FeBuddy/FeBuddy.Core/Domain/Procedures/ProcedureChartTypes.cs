namespace FeBuddy.Core.Domain.Procedures;

/// <summary>
/// The d-TPP Metafile <c>chart_code</c> values FE-Buddy knows about, and the default set the
/// Procedures sub-service includes when the user names none.
/// </summary>
public static class ProcedureChartTypes
{
	/// <summary>Instrument approach procedure.</summary>
	public const string Iap = "IAP";

	/// <summary>Takeoff/alternate/radar minimums.</summary>
	public const string Min = "MIN";

	/// <summary>Standard terminal arrival (the FAA's "Metafile XML Definitions" PDF calls this "STAR").</summary>
	public const string Star = "STR";

	/// <summary>Departure procedure.</summary>
	public const string Dp = "DP";

	/// <summary>Airport diagram.</summary>
	public const string Apd = "APD";

	/// <summary>Obstacle departure procedure.</summary>
	public const string Odp = "ODP";

	/// <summary>Hot spots.</summary>
	public const string Hot = "HOT";

	/// <summary>LAHSO (land and hold short operations).</summary>
	public const string Lah = "LAH";

	/// <summary>RNAV DP AAUP.</summary>
	public const string Dau = "DAU";

	/// <summary>Every chart type FE-Buddy recognizes.</summary>
	public static IReadOnlyList<string> All { get; } = [Iap, Min, Star, Dp, Apd, Odp, Hot, Lah, Dau];

	/// <summary>
	/// The chart types the Procedures sub-service includes when <c>ChartTypes</c> is absent or
	/// blank - every kind of procedure a pilot flies, but none of the volume-wide reference sheets
	/// (<see cref="Min"/>, <see cref="Hot"/>, <see cref="Lah"/>).
	/// </summary>
	public static IReadOnlyList<string> Default { get; } = [Iap, Star, Dp, Odp, Dau, Apd];

	/// <summary>Whether <paramref name="chartType"/> is one of <see cref="All"/>, ignoring case.</summary>
	/// <param name="chartType">The chart type to check.</param>
	/// <returns><see langword="true"/> when it is a known chart type.</returns>
	public static bool IsKnown(string chartType) => All.Contains(chartType, StringComparer.OrdinalIgnoreCase);
}
