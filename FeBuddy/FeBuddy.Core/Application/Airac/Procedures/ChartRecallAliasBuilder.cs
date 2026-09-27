using System.Globalization;

using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Procedures;
using FeBuddy.Core.Domain.Procedures.ChartRecall;
using FeBuddy.Core.Domain.Procedures.ChartRecall.Models;
using FeBuddy.Core.Infrastructure.Dtpp;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

using DtppRecord = FeBuddy.Core.Infrastructure.Dtpp.Models.DtppMetafileXmlDataModel.Record;

namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// Builds the FAA Chart Recall alias commands: one <c>.OPENURL</c> command per page of every
/// current chart at every airport in the d-TPP Metafile, e.g.
/// <c>.dtwI22Lc .OPENURL https://aeronav.faa.gov/d-tpp/2609/00058IL22L.PDF  ; DETROIT METRO WAYNE COUNTY-ILS OR LOC RWY 22L</c>.
/// </summary>
/// <remarks>
/// <para>
/// The command syntax is FE-Buddy 2.x's, which controllers already know: a period, the airport's
/// FAA identifier in lower case, the chart's code (<see cref="ChartRecallCodes"/>), then a
/// lower-case <c>c</c>. A chart's second and later pages add their page number after the
/// <c>c</c>: <c>.dtwHHOWEc</c>, <c>.dtwHHOWEc2</c>. A minimums sheet many airports share opens at
/// the airport's own page (<c>#nameddest=(DTW)</c>).
/// </para>
/// <para>
/// Every airport in the metafile is covered, whatever the Procedures selection settings say, and a
/// chart the FAA deleted this cycle gets no command. A departure or STAR with no computer code
/// whose name names its own airport (<c>TATALINA FOUR</c> at <c>TATALINA LRRS</c>) uses the
/// airport's identifier as its code (<c>.tljTLJc</c>) - unless two such charts at one airport would
/// then share it, or another chart's code already is the identifier, when it keeps its own name.
/// </para>
/// <para>
/// Two charts that still end up with the same command are both written: FE-Buddy never guesses
/// which one a controller wants. The AIRAC Service's duplicate alias report lists them. Only a
/// line that repeats another exactly - same command, same link - is left out.
/// </para>
/// </remarks>
public static class ChartRecallAliasBuilder
{
	private const string LogSource = "ChartRecallAliasBuilder";

	/// <summary>How many example charts a warning names.</summary>
	private const int ExampleLimit = 5;

	/// <summary>How many "not recognized" warnings one run reports before summing up the rest in one more.</summary>
	private const int UnrecognizedWarningLimit = 20;

	/// <summary>How the summary names the charts each rule leaves out.</summary>
	private static readonly Dictionary<ChartRecallSkipReason, string> SkipReasonLabels = new()
	{
		[ChartRecallSkipReason.HighAltitude] = "HI- approach(es)",
		[ChartRecallSkipReason.Copter] = "COPTER approach(es)",
		[ChartRecallSkipReason.Prm] = "PRM chart(s)",
		[ChartRecallSkipReason.CategoryApproach] = "CAT II/III or SA CAT approach(es)",
		[ChartRecallSkipReason.Converging] = "CONVERGING approach(es)",
		[ChartRecallSkipReason.Aaup] = "AAUP page(s)",
		[ChartRecallSkipReason.NumberedApproach] = "numbered approach(es) such as VOR-1",
		[ChartRecallSkipReason.Gls] = "GLS approach(es) or part(s)",
		[ChartRecallSkipReason.LocNdb] = "LOC/NDB part(s)",
		[ChartRecallSkipReason.AlternateMinimums] = "alternate minimums sheet(s)",
	};

	/// <summary>
	/// Builds every command from the metafile.
	/// </summary>
	/// <param name="dtpp">The selected cycle's parsed FAA d-TPP Metafile.</param>
	/// <returns>The command lines, the counts behind the summary, and the summary and warning messages.</returns>
	public static ChartRecallBuildResult Build(DtppMetafileDataCollection dtpp)
	{
		ArgumentNullException.ThrowIfNull(dtpp);

		List<ChartRecallAliasLine> lines = [];
		// The command ignoring case, the way CRC matches commands, and the link exactly.
		HashSet<string> writtenLines = new(StringComparer.Ordinal);
		Dictionary<string, int> commandsByChartType = new(StringComparer.OrdinalIgnoreCase);
		Dictionary<ChartRecallSkipReason, int> skippedByReason = [];
		Dictionary<string, List<string>> unrecognizedCharts = new(StringComparer.Ordinal);
		HashSet<string> airportsWithCommands = new(StringComparer.OrdinalIgnoreCase);
		int namedFromChartName = 0;
		int namedAfterAirport = 0;

		// GroupBy keeps the file's order: airports in the order first met, each airport's pages in
		// the FAA's own chart order.
		IEnumerable<IGrouping<string, DtppRecord>> airports = dtpp.Records
			.Where(IsCurrentChartPage)
			.GroupBy(record => record.AptIdent.Trim(), StringComparer.OrdinalIgnoreCase);

		foreach (IGrouping<string, DtppRecord> airport in airports)
		{
			string airportId = airport.Key;
			string airportPrefix = ChartRecallText.LettersAndDigits(airportId).ToLowerInvariant();

			// Every page of a chart shares its codes, so each chart is worked out - and counted - once.
			Dictionary<string, ChartRecallCodeResult> chartResults = new(StringComparer.OrdinalIgnoreCase);

			foreach (DtppRecord record in airport)
			{
				string chartKey = ChartKey(record);

				if (chartResults.ContainsKey(chartKey))
				{
					continue;
				}

				ChartRecallCodeResult result = ChartRecallCodes.For(record.ChartCode, record.ChartName, record.Faanfd18, record.AirportName);
				chartResults[chartKey] = result;

				if (result.SkipReason is { } skipReason)
				{
					Increment(skippedByReason, skipReason);
				}

				foreach (ChartRecallSkipReason skippedPart in result.SkippedParts)
				{
					Increment(skippedByReason, skippedPart);
				}

				foreach (string what in result.Unrecognized)
				{
					if (!unrecognizedCharts.TryGetValue(what, out List<string>? charts))
					{
						charts = [];
						unrecognizedCharts[what] = charts;
					}

					charts.Add($"{airportId} {ProcedureNaming.BaseName(record.ChartName)}");
				}
			}

			// A chart that names its own airport takes the airport's identifier as its code - but
			// only when it is the only one at the airport and no other chart's code already is the
			// identifier (PSC's PASCO ONE has the computer code PSC1.PSC), or two charts would share
			// one command.
			string airportCode = ChartRecallText.LettersAndDigits(airportId);
			bool useAirportId = chartResults.Values.Count(result => result.NamesItsAirport) == 1
				&& !chartResults.Values.Any(result => result.Codes.Contains(airportCode, StringComparer.OrdinalIgnoreCase));
			namedFromChartName += chartResults.Values.Count(result => result.IsNamedFromChartName);
			namedAfterAirport += useAirportId ? 1 : 0;

			foreach (DtppRecord record in airport)
			{
				ChartRecallCodeResult result = chartResults[ChartKey(record)];
				IReadOnlyList<string> codes = useAirportId && result.NamesItsAirport
					? [airportCode]
					: result.Codes;

				if (codes.Count == 0)
				{
					continue;
				}

				int page = ProcedureNaming.PageNumber(record.ChartName);
				string pageSuffix = page > 1 ? page.ToString(CultureInfo.InvariantCulture) : string.Empty;
				string url = DtppFiles.ChartUrl(dtpp.Cycle, record.PdfName.Trim())
					+ (result.OpensAtAirportPage ? $"#nameddest=({airportId})" : string.Empty);
				string comment = $"{record.AirportName.Trim()}-{record.ChartName.Trim()}";

				foreach (string code in codes)
				{
					string command = $".{airportPrefix}{code}c{pageSuffix}";

					if (!writtenLines.Add($"{command.ToUpperInvariant()} {url}"))
					{
						continue;
					}

					lines.Add(new ChartRecallAliasLine(airportId, record.ChartCode.Trim().ToUpperInvariant(), command, url, comment));
					Increment(commandsByChartType, record.ChartCode.Trim().ToUpperInvariant());
					airportsWithCommands.Add(airportId);
				}
			}
		}

		ChartRecallSummary summary = new()
		{
			AirportCount = airportsWithCommands.Count,
			CommandCount = lines.Count,
			CommandsByChartType = commandsByChartType,
			SkippedByReason = skippedByReason,
			NamedFromChartNameCount = namedFromChartName,
			NamedAfterAirportCount = namedAfterAirport,
		};

		List<ServiceMessage> messages = [new ServiceMessage(LogLevel.Info, LogSource, SummaryText(summary))];
		messages.AddRange(UnrecognizedWarnings(unrecognizedCharts));

		return new ChartRecallBuildResult(lines, summary, messages);
	}

	/// <summary>
	/// A page of a chart that is in this cycle: not deleted, a real PDF rather than the FAA's deletion
	/// placeholder, and listed under an airport.
	/// </summary>
	private static bool IsCurrentChartPage(DtppRecord record) =>
		!string.Equals(record.UserAction?.Trim(), "D", StringComparison.OrdinalIgnoreCase)
		&& !string.IsNullOrWhiteSpace(record.PdfName)
		&& !DtppFiles.IsPlaceholderPdf(record.PdfName.Trim())
		&& !string.IsNullOrWhiteSpace(record.AptIdent);

	/// <summary>What identifies a chart within its airport: its type and its base name, which all its pages share.</summary>
	private static string ChartKey(DtppRecord record) => $"{record.ChartCode.Trim()}|{ProcedureNaming.BaseName(record.ChartName)}";

	private static void Increment<TKey>(Dictionary<TKey, int> counts, TKey key)
		where TKey : notnull =>
		counts[key] = counts.GetValueOrDefault(key) + 1;

	/// <summary>The one-line summary of the file: what got commands, what was left out and why.</summary>
	private static string SummaryText(ChartRecallSummary summary)
	{
		string text = $"{ProcedureOutputFiles.Alias}: {summary.CommandCount:N0} command(s) for {summary.AirportCount:N0} airport(s)";

		if (summary.CommandsByChartType.Count > 0)
		{
			text += " - " + string.Join(", ", summary.CommandsByChartType
				.OrderByDescending(pair => pair.Value)
				.ThenBy(pair => pair.Key, StringComparer.Ordinal)
				.Select(pair => $"{pair.Key} {pair.Value:N0}"));
		}

		text += ".";

		if (summary.SkippedByReason.Count > 0)
		{
			text += " No command, by rule: " + string.Join(", ", summary.SkippedByReason
				.OrderByDescending(pair => pair.Value)
				.ThenBy(pair => pair.Key)
				.Select(pair => $"{pair.Value:N0} {SkipReasonLabel(pair.Key)}")) + ".";
		}

		if (summary.NamedFromChartNameCount > 0)
		{
			text += $" {summary.NamedFromChartNameCount:N0} departure(s)/STAR(s) had no computer code, so were named from the chart name " +
				$"({summary.NamedAfterAirportCount:N0} of them after their airport).";
		}

		return text;
	}

	/// <summary>How the summary names the charts a rule leaves out; a rule added without a label shows its name.</summary>
	private static string SkipReasonLabel(ChartRecallSkipReason reason) => SkipReasonLabels.GetValueOrDefault(reason, reason.ToString());

	/// <summary>One warning per thing FE-Buddy did not recognize, naming a few of the charts it affects.</summary>
	private static IEnumerable<ServiceMessage> UnrecognizedWarnings(Dictionary<string, List<string>> unrecognizedCharts)
	{
		int reported = 0;

		foreach ((string what, List<string> charts) in unrecognizedCharts)
		{
			if (reported == UnrecognizedWarningLimit)
			{
				int remaining = unrecognizedCharts.Count - UnrecognizedWarningLimit;
				yield return new ServiceMessage(LogLevel.Warning, LogSource,
					$"FAA Chart Recall: {remaining:N0} more thing(s) FE-Buddy doesn't recognize also left charts without a command. " +
					"Please report them to the FE-Buddy developers.");
				yield break;
			}

			string examples = string.Join(", ", charts.Take(ExampleLimit)) + (charts.Count > ExampleLimit ? ", ..." : string.Empty);

			yield return new ServiceMessage(LogLevel.Warning, LogSource,
				$"FAA Chart Recall: FE-Buddy doesn't recognize {what}, so {charts.Count:N0} chart(s) got no command for it ({examples}). " +
				"Please report it to the FE-Buddy developers, so it can be given a command or listed as a known type.");

			reported++;
		}
	}
}
