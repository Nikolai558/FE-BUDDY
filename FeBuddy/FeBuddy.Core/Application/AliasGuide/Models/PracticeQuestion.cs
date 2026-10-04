namespace FeBuddy.Core.Application.AliasGuide.Models;

/// <summary>What a practice question asks the controller to do.</summary>
public enum PracticeAction
{
	/// <summary>Display an In-Scope Reference card: an airport, a NAVAID or an aircraft operator.</summary>
	DisplayIsr,

	/// <summary>Display an airway's or a procedure's fixes (Data Display).</summary>
	DisplayFixes,

	/// <summary>Recall a chart (Chart Recall).</summary>
	RecallChart,
}

/// <summary>What a practice question is about, which says how its command is built.</summary>
public enum PracticeSubject
{
	/// <summary>An airport's card: <c>.apt</c> and its FAA or ICAO ID.</summary>
	Airport,

	/// <summary>A NAVAID's card: <c>.nav</c> and its ID or name.</summary>
	Navaid,

	/// <summary>An aircraft operator's card: <c>.id</c> and its 3LD or telephony.</summary>
	Operator,

	/// <summary>An airway: its ID, then <c>F</c>.</summary>
	Airway,

	/// <summary>An instrument approach chart.</summary>
	Approach,

	/// <summary>A charted visual approach.</summary>
	Visual,

	/// <summary>A departure procedure: a SID or an obstacle departure.</summary>
	Departure,

	/// <summary>An arrival procedure (STAR).</summary>
	Arrival,

	/// <summary>Another of an airport's charts, such as its airport diagram.</summary>
	OtherChart,
}

/// <summary>
/// One question of the Alias Command Practice: what to do (<see cref="Action"/>) with what
/// (<see cref="Name"/>, <see cref="Detail"/>), and the commands that do it.
/// </summary>
/// <param name="Action">What the controller is asked to do.</param>
/// <param name="Subject">What the question is about.</param>
/// <param name="Name">What the question names, shown large: a chart's name, an airport, a NAVAID, an operator or an airway.</param>
/// <param name="Detail">The line under it: where the chart is, its computer code, its page, or the IDs to use.</param>
/// <param name="Answers">
/// Every command that does it, in command markup (see <c>CommandMarkup</c>), the usual one first:
/// <c>.{a:slc}{t:I}{r:16R}c</c> and <c>.{a:slc}{t:L}{r:16R}c</c> for a chart that is both an ILS and a LOC.
/// </param>
/// <param name="Notes">What is worth knowing about this one, as inline text, shown once it is answered; often none.</param>
/// <param name="Chart">The d-TPP chart a chart or procedure question is about, which its answers are worked out from; otherwise <see langword="null"/>.</param>
public sealed record PracticeQuestion(
	PracticeAction Action,
	PracticeSubject Subject,
	string Name,
	string Detail,
	IReadOnlyList<string> Answers,
	IReadOnlyList<string> Notes,
	PracticeChart? Chart);

/// <summary>A chart as the d-TPP Metafile lists it, for a chart or procedure question.</summary>
/// <param name="ChartCode">Its <c>chart_code</c>, e.g. <c>IAP</c>, <c>DP</c>, <c>STR</c>, <c>APD</c>.</param>
/// <param name="AirportId">Its airport's FAA ID, e.g. <c>SLC</c>.</param>
/// <param name="AirportName">Its airport's name, e.g. <c>SALT LAKE CITY INTL</c>.</param>
/// <param name="ChartName">Its <c>chart_name</c>, a continuation page's <c>, CONT.1</c> included.</param>
/// <param name="ComputerCode">A procedure's computer code, e.g. <c>SLC4.TCH</c>; <see langword="null"/> when it has none, or for any other chart.</param>
public sealed record PracticeChart(string ChartCode, string AirportId, string AirportName, string ChartName, string? ComputerCode);
