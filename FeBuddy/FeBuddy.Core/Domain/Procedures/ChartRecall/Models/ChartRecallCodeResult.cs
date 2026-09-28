namespace FeBuddy.Core.Domain.Procedures.ChartRecall.Models;

/// <summary>
/// What <see cref="ChartRecallCodes.For"/> makes of one chart: the codes of its FAA Chart Recall
/// commands, or why it gets none.
/// </summary>
/// <remarks>
/// A chart can have codes and still carry skipped or unrecognized parts: <c>ILS OR LOC/NDB RWY 10</c>
/// gets <c>I10</c> for its ILS part and lists <see cref="ChartRecallSkipReason.LocNdb"/> in
/// <see cref="SkippedParts"/>.
/// </remarks>
public sealed record ChartRecallCodeResult
{
	/// <summary>
	/// The command codes, in the order the commands are written: the part of a command between the
	/// lower-case airport and the trailing <c>c</c>, e.g. <c>I22L</c>, <c>vRIVER19</c>, <c>APD</c>,
	/// <c>JALEX</c>. Empty when the chart gets no command.
	/// </summary>
	public IReadOnlyList<string> Codes { get; init; } = [];

	/// <summary>
	/// Why the whole chart gets no command, when a rule skips it; <see langword="null"/> otherwise.
	/// </summary>
	public ChartRecallSkipReason? SkipReason { get; init; }

	/// <summary>
	/// The known parts of an "OR" chart that get no command although the rest of the chart does,
	/// e.g. <see cref="ChartRecallSkipReason.LocNdb"/>. Empty for most charts.
	/// </summary>
	public IReadOnlyList<ChartRecallSkipReason> SkippedParts { get; init; } = [];

	/// <summary>
	/// What FE-Buddy did not recognize in the chart, each worded to finish the sentence "FE-Buddy
	/// doesn't recognize ...", e.g. <c>the approach type 'SDF'</c>. The caller reports each one as a
	/// warning, so a new FAA chart type is noticed rather than silently left out.
	/// </summary>
	public IReadOnlyList<string> Unrecognized { get; init; } = [];

	/// <summary>
	/// Whether the chart's link should open at the airport's own page of a PDF many airports share
	/// (<c>#nameddest=(DTW)</c>). <see langword="true"/> for the takeoff minimums, diverse vector area
	/// and radar minimums sheets.
	/// </summary>
	public bool OpensAtAirportPage { get; init; }

	/// <summary>
	/// For a departure, obstacle departure or STAR with no usable computer code: whether its name,
	/// once the version, bracketed words, <c>RNAV</c>, <c>OBSTACLE</c> and <c>COPTER</c> are dropped,
	/// names the airport it serves - e.g. <c>TATALINA FOUR (OBSTACLE) (RNAV)</c> at
	/// <c>TATALINA LRRS</c>. The caller then uses the airport's identifier as the code instead of
	/// <see cref="Codes"/>, as long as no other chart at the same airport does the same or already
	/// has the identifier as its code.
	/// </summary>
	public bool NamesItsAirport { get; init; }

	/// <summary>
	/// Whether <see cref="Codes"/> came from the chart name because the chart has no usable computer
	/// code. Only ever <see langword="true"/> for a departure, obstacle departure or STAR.
	/// </summary>
	public bool IsNamedFromChartName { get; init; }
}
