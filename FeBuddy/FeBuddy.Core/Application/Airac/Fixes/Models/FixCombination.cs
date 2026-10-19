using System.Diagnostics.CodeAnalysis;

using FeBuddy.Core.Domain.Fixes;

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

	/// <summary>
	/// Reads one <c>Combinations</c> entry: a chart and a fix use joined by <c>+</c>, each tokenized as the
	/// files and the CRC class name spell it, so a hand-edited <c>ENROUTE LOW+wypnt</c> names the same files.
	/// </summary>
	/// <param name="entry">The entry, e.g. <c>ENROUTE-LOW+WYPNT</c>.</param>
	/// <param name="combination">The combination, when the entry is one.</param>
	/// <returns>
	/// <see langword="false"/> when the entry isn't two parts joined by <c>+</c>, or a part has no letter or
	/// digit (it would name no file).
	/// </returns>
	public static bool TryParse(string? entry, [NotNullWhen(true)] out FixCombination? combination)
	{
		combination = null;
		string[] parts = (entry ?? string.Empty).Split('+');

		if (parts.Length != 2)
		{
			return false;
		}

		string chart = FixCharts.Token(parts[0]);
		string fixUse = FixUses.Token(parts[1]);

		if (chart.Length == 0 || fixUse.Length == 0)
		{
			return false;
		}

		combination = new FixCombination(chart, fixUse);
		return true;
	}
}
