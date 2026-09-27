using FeBuddy.Core.Domain.Procedures.ChartRecall.Models;

namespace FeBuddy.Core.Domain.Procedures.ChartRecall;

/// <summary>
/// Works out the FAA Chart Recall alias command codes for one d-TPP Metafile chart: the part of a
/// command such as <c>.dtwI22Lc</c> between the lower-case airport and the trailing <c>c</c>.
/// </summary>
/// <remarks>
/// <para>By chart type (<c>chart_code</c>):</para>
/// <list type="table">
///   <item><term>IAP</term><description>see <see cref="ApproachCodes"/>: <c>I22L</c>, <c>LY22L</c>, <c>RA</c>, <c>vRIVER19</c>, ...</description></item>
///   <item><term>APD</term><description><c>APD</c></description></item>
///   <item><term>MIN</term><description>
///     <c>TM</c> takeoff minimums, <c>DVA</c> diverse vector area, <c>RM</c> radar minimums - each
///     opening at the airport's own page of the shared PDF; the alternate minimums get no command
///   </description></item>
///   <item><term>HOT</term><description><c>HS</c></description></item>
///   <item><term>LAH</term><description><c>LAHSO</c></description></item>
///   <item><term>DP, ODP, STR</term><description>see <see cref="SidStarCodes"/>: <c>JALEX</c>, <c>GRUUB</c>, ...</description></item>
///   <item><term>DAU</term><description>no command (an attention-all-users page)</description></item>
/// </list>
/// <para>
/// Any other chart type, minimums sheet or approach type is returned as unrecognized, for the
/// caller to warn about. Codes are upper case apart from a charted visual approach's leading
/// <c>v</c>. A continuation page (<c>, CONT.1</c>) gets the same codes as its chart - the caller
/// numbers the pages.
/// </para>
/// </remarks>
public static class ChartRecallCodes
{
	/// <summary>
	/// Works out the codes for one chart.
	/// </summary>
	/// <param name="chartCode">The chart's type (<c>chart_code</c>), e.g. <c>IAP</c>.</param>
	/// <param name="chartName">The chart's name (<c>chart_name</c>); a continuation page's <c>, CONT.n</c> is ignored.</param>
	/// <param name="computerCode">The chart's computer code (<c>faanfd18</c>), read for a DP, ODP or STR only.</param>
	/// <param name="airportName">The name of the airport the chart is listed under, read for a DP, ODP or STR only.</param>
	/// <returns>The codes, or why the chart gets none.</returns>
	public static ChartRecallCodeResult For(string chartCode, string chartName, string? computerCode, string airportName)
	{
		ArgumentNullException.ThrowIfNull(chartCode);
		ArgumentNullException.ThrowIfNull(chartName);
		ArgumentNullException.ThrowIfNull(airportName);

		string name = ProcedureNaming.BaseName(chartName).ToUpperInvariant();
		string type = chartCode.Trim().ToUpperInvariant();

		return type switch
		{
			ProcedureChartTypes.Iap => ApproachCodes.For(name),
			ProcedureChartTypes.Apd => Single("APD"),
			ProcedureChartTypes.Min => ForMinimums(name),
			ProcedureChartTypes.Hot => Single("HS"),
			ProcedureChartTypes.Lah => Single("LAHSO"),
			ProcedureChartTypes.Dp or ProcedureChartTypes.Odp => SidStarCodes.For(isStar: false, name, computerCode, airportName),
			ProcedureChartTypes.Star => SidStarCodes.For(isStar: true, name, computerCode, airportName),
			ProcedureChartTypes.Dau => new ChartRecallCodeResult { SkipReason = ChartRecallSkipReason.Aaup },
			_ => new ChartRecallCodeResult { Unrecognized = [$"the chart type '{type}'"] },
		};
	}

	/// <summary>One of the minimums sheets many airports share, which opens at the airport's own page.</summary>
	private static ChartRecallCodeResult ForMinimums(string name) => name switch
	{
		"TAKEOFF MINIMUMS" => new ChartRecallCodeResult { Codes = ["TM"], OpensAtAirportPage = true },
		"DIVERSE VECTOR AREA" => new ChartRecallCodeResult { Codes = ["DVA"], OpensAtAirportPage = true },
		"RADAR MINIMUMS" => new ChartRecallCodeResult { Codes = ["RM"], OpensAtAirportPage = true },
		"ALTERNATE MINIMUMS" => new ChartRecallCodeResult { SkipReason = ChartRecallSkipReason.AlternateMinimums },
		_ => new ChartRecallCodeResult { Unrecognized = [$"the minimums sheet '{name}'"] },
	};

	private static ChartRecallCodeResult Single(string code) => new() { Codes = [code] };
}
