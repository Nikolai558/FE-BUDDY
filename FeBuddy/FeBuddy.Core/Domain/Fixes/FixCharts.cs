namespace FeBuddy.Core.Domain.Fixes;

/// <summary>
/// The NASR <c>CHARTS</c> vocabulary (<c>FIX_BASE.CHARTS</c>) and the rules FE-Buddy applies to
/// it: parsing the comma-separated list, and the file-naming/CRC-class token for each chart.
/// </summary>
public static class FixCharts
{
	/// <summary>The token for a fix whose <c>CHARTS</c> is empty.</summary>
	public const string NoChart = "NO-CHART";

	/// <summary>
	/// Parses <c>FIX_BASE.CHARTS</c> into the chart names it lists.
	/// </summary>
	/// <param name="charts">The raw, comma-separated <c>CHARTS</c> value, or <see langword="null"/>.</param>
	/// <returns>
	/// The chart names, trimmed, with empty entries dropped and duplicates removed (ignoring
	/// case, keeping the first spelling), in the order NASR lists them. Empty when
	/// <paramref name="charts"/> is null, blank, or holds nothing usable.
	/// </returns>
	public static IReadOnlyList<string> Parse(string? charts)
	{
		if (string.IsNullOrWhiteSpace(charts))
		{
			return [];
		}

		List<string> result = [];
		HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

		foreach (string part in charts.Split(','))
		{
			string trimmed = part.Trim();

			if (trimmed.Length > 0 && seen.Add(trimmed))
			{
				result.Add(trimmed);
			}
		}

		return result;
	}

	/// <summary>The file-name/CRC-class token for a chart name, e.g. <c>ENROUTE LOW</c> to <c>ENROUTE-LOW</c>.</summary>
	/// <param name="chart">The NASR chart name.</param>
	/// <returns>The token.</returns>
	public static string Token(string chart) => FixTokens.Token(chart);

	/// <summary>A fix's chart tokens, or <see cref="NoChart"/> when it has none.</summary>
	/// <param name="charts">The fix's chart names, as parsed by <see cref="Parse"/>.</param>
	/// <returns>The tokens, in the order given, or a single <see cref="NoChart"/> entry.</returns>
	public static IReadOnlyList<string> TokensFor(IReadOnlyList<string> charts)
	{
		ArgumentNullException.ThrowIfNull(charts);

		return charts.Count == 0 ? [NoChart] : [.. charts.Select(Token)];
	}
}
