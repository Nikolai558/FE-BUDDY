namespace FeBuddy.Core.Application.Airac.Fixes.Models;

/// <summary>
/// One user-listed chart + fix use combination for <see cref="FixOutputBy.ChartAndFixUse"/>, e.g.
/// <c>ENROUTE-LOW+WYPNT</c>.
/// </summary>
/// <param name="Chart">The chart's token, e.g. <c>ENROUTE-LOW</c> or <c>NO-CHART</c>.</param>
/// <param name="FixUse">The fix use's token, e.g. <c>WYPNT</c>.</param>
public sealed record FixCombination(string Chart, string FixUse)
{
	/// <summary>The combination's group name in its file keys and CRC class name, e.g. <c>ENROUTE-LOW-WYPNT</c>.</summary>
	public string Group => FixOutputFiles.CombinationGroup(Chart, FixUse);
}
