using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Procedures.ChartRecall.Models;

namespace FeBuddy.Core.Application.Airac.Procedures.Models;

/// <summary>
/// One line of <c>FAA_CHART_RECALL.txt</c>: a command that opens one chart page, e.g.
/// <c>.dtwI22Lc .OPENURL https://aeronav.faa.gov/d-tpp/2609/00058IL22L.PDF  ; DETROIT METRO WAYNE COUNTY-ILS OR LOC RWY 22L</c>.
/// </summary>
/// <param name="AirportId">The FAA identifier of the airport the chart is listed under, e.g. <c>DTW</c>.</param>
/// <param name="ChartCode">The chart's type (<c>chart_code</c>), e.g. <c>IAP</c>.</param>
/// <param name="Command">The command name, period included, e.g. <c>.dtwI22Lc</c> or <c>.dtwHHOWEc2</c>.</param>
/// <param name="Url">The chart page's link, e.g. <c>https://aeronav.faa.gov/d-tpp/2609/00058IL22L.PDF</c>.</param>
/// <param name="Comment">What the line opens: the airport name, a hyphen and the chart name as the FAA prints it.</param>
public sealed record ChartRecallAliasLine(string AirportId, string ChartCode, string Command, string Url, string Comment)
{
	/// <summary>The whole line, without a line ending - two spaces before the <c>;</c>, as FE-Buddy 2.x wrote it.</summary>
	public string Text => $"{Command} .OPENURL {Url}  ; {Comment}";
}

/// <summary>
/// The counts behind the FAA Chart Recall summary message: what got a command, what was left out
/// on purpose, and how departures and STARs with no computer code were named.
/// </summary>
public sealed record ChartRecallSummary
{
	/// <summary>How many airports got at least one command.</summary>
	public required int AirportCount { get; init; }

	/// <summary>How many commands (lines) were made.</summary>
	public required int CommandCount { get; init; }

	/// <summary>How many commands each chart type (<c>chart_code</c>) got, e.g. <c>IAP</c> 11,933.</summary>
	public required IReadOnlyDictionary<string, int> CommandsByChartType { get; init; }

	/// <summary>
	/// How many charts, or "OR" parts of charts, each rule left without a command. A chart counts
	/// once, however many pages it has.
	/// </summary>
	public required IReadOnlyDictionary<ChartRecallSkipReason, int> SkippedByReason { get; init; }

	/// <summary>How many departures, obstacle departures and STARs had no usable computer code, so were named from their chart name.</summary>
	public required int NamedFromChartNameCount { get; init; }

	/// <summary>How many of those <see cref="NamedFromChartNameCount"/> charts named their own airport, so use the airport's identifier.</summary>
	public required int NamedAfterAirportCount { get; init; }
}

/// <summary>The outcome of building the FAA Chart Recall commands from a d-TPP Metafile.</summary>
/// <param name="Lines">Every command line, in metafile order: airport by airport, each airport's charts in the FAA's order.</param>
/// <param name="Summary">The counts behind the summary message.</param>
/// <param name="Messages">The summary message, plus a warning for everything FE-Buddy did not recognize.</param>
public sealed record ChartRecallBuildResult(
	IReadOnlyList<ChartRecallAliasLine> Lines,
	ChartRecallSummary Summary,
	IReadOnlyList<ServiceMessage> Messages);

/// <summary>The outcome of writing <c>FAA_CHART_RECALL.txt</c>.</summary>
/// <param name="FilePath">The path written, or <see langword="null"/> when there was no command to write.</param>
/// <param name="CommandCount">How many commands were written.</param>
public sealed record ChartRecallAliasWriteResult(string? FilePath, int CommandCount);
