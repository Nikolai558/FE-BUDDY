namespace FeBuddy.Core.Domain.Procedures.ChartRecall.Models;

/// <summary>
/// Why FE-Buddy deliberately makes no FAA Chart Recall command for a chart, or for one part of an
/// "OR" chart.
/// </summary>
/// <remarks>
/// A chart skipped for one of these reasons is a kind FE-Buddy knows and leaves out on purpose. A
/// chart FE-Buddy does not recognize at all is not skipped this way: it is reported as a warning
/// instead (<see cref="ChartRecallCodeResult.Unrecognized"/>), so a new FAA chart type is noticed
/// and can be taught to FE-Buddy.
/// </remarks>
public enum ChartRecallSkipReason
{
	/// <summary>
	/// A high-altitude approach: the name starts with <c>HI-</c>, e.g. <c>HI-ILS OR LOC RWY 15</c>.
	/// The whole chart is skipped, its "OR" parts included.
	/// </summary>
	HighAltitude,

	/// <summary>
	/// A helicopter approach: the name starts with <c>COPTER</c>, e.g. <c>COPTER ILS OR LOC RWY 30</c>.
	/// The whole chart is skipped, its "OR" parts included.
	/// </summary>
	Copter,

	/// <summary>
	/// A precision runway monitor approach, or its attention-all-users page: the name holds the word
	/// <c>PRM</c>, e.g. <c>ILS PRM RWY 09R</c>, <c>PRM AAUP</c>.
	/// </summary>
	Prm,

	/// <summary>
	/// A category II/III or special-authorization approach: a bracket holding <c>CAT</c>, e.g.
	/// <c>ILS RWY 04R (SA CAT I)</c>, <c>ILS RWY 22L (CAT II - III)</c>.
	/// </summary>
	CategoryApproach,

	/// <summary>A converging approach, e.g. <c>CONVERGING ILS RWY 17C</c>.</summary>
	Converging,

	/// <summary>
	/// An attention-all-users page: an approach name ending in <c>AAUP</c>, or an RNAV departure
	/// AAUP (chart type <c>DAU</c>).
	/// </summary>
	Aaup,

	/// <summary>A numbered approach, e.g. <c>VOR-1 RWY 14L</c>.</summary>
	NumberedApproach,

	/// <summary>A GLS approach, or the GLS part of an "OR" chart.</summary>
	Gls,

	/// <summary>
	/// The LOC/NDB part of an "OR" chart, e.g. <c>ILS OR LOC/NDB RWY 10</c>. The chart's other parts
	/// still get their commands.
	/// </summary>
	LocNdb,

	/// <summary>The alternate minimums sheet (chart type <c>MIN</c>).</summary>
	AlternateMinimums,
}
