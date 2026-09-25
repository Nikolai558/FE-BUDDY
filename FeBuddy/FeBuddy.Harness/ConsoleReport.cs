using System.Text.RegularExpressions;

using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Harness;

/// <summary>
/// All console formatting for this harness lives here. <see cref="Program"/> and the runner
/// classes never call <c>Console.Write*</c> directly for airway-related output.
/// </summary>
internal static class ConsoleReport
{
	private static readonly Regex AirwayIdPattern = new(@"^Airway '([^']+)':", RegexOptions.Compiled);

	public static void PrintNasrParseSummary(TimeSpan elapsed)
	{
		Console.WriteLine($"NASR CSV parsing complete. ({elapsed.TotalMilliseconds:N0} ms)");
	}

	public static void PrintAirwayServiceResult(string label, AirwayServiceResult result)
	{
		Console.WriteLine();
		Console.WriteLine($"=== {label} ===");
		Console.WriteLine($"Elapsed:      {result.Elapsed.TotalMilliseconds:N0} ms");
		Console.WriteLine($"Airways built: {result.AirwayCount:N0}");

		if (result.GeojsonFilesWritten.Count == 0)
		{
			Console.WriteLine("GeoJSON files written: (none)");
		}
		else
		{
			Console.WriteLine($"GeoJSON files written: {result.GeojsonFilesWritten.Count}");

			foreach (string file in result.GeojsonFilesWritten)
			{
				int count = result.GeojsonFeatureCountsByFile.TryGetValue(file, out int c) ? c : 0;
				Console.WriteLine($"  {Path.GetFileName(file)} - {count:N0} feature(s)");
				Console.WriteLine($"    {file}");
			}
		}

		if (result.AliasFilePath is not null)
		{
			Console.WriteLine($"Alias file:   {result.AliasFilePath} ({result.AliasAirwayLineCount:N0} airway line(s))");
		}
		else
		{
			Console.WriteLine("Alias file:   (not generated)");
		}

		PrintWarnings(result.Warnings);
	}

	/// <summary>
	/// Prints one Airports run: timing, how many airports were built and how many survived ROI
	/// filtering, every GeoJSON file with its Feature count, the alias file with its command
	/// count, and the run's messages grouped by level.
	/// </summary>
	/// <param name="label">Heading for this run, e.g. "Airports: GeoJSON + Alias".</param>
	/// <param name="result">What <c>AirportService.Run</c> returned.</param>
	public static void PrintAirportServiceResult(string label, AirportServiceResult result)
	{
		Console.WriteLine();
		Console.WriteLine($"=== {label} ===");
		Console.WriteLine($"Elapsed:      {result.Elapsed.TotalMilliseconds:N0} ms");
		Console.WriteLine($"Airports built: {result.AirportCount:N0}");
		Console.WriteLine($"Airports in ROI: {result.AirportsInRoiCount:N0}");

		if (result.GeojsonFilesWritten.Count == 0)
		{
			Console.WriteLine("GeoJSON files written: (none)");
		}
		else
		{
			Console.WriteLine($"GeoJSON files written: {result.GeojsonFilesWritten.Count}");

			foreach (string file in result.GeojsonFilesWritten)
			{
				int count = result.GeojsonFeatureCountsByFile.TryGetValue(file, out int c) ? c : 0;
				Console.WriteLine($"  {Path.GetFileName(file)} - {count:N0} feature(s)");
				Console.WriteLine($"    {file}");
			}
		}

		if (result.AliasFilePath is not null)
		{
			Console.WriteLine($"Alias file:   {result.AliasFilePath} ({result.AliasCommandCount:N0} command(s))");
		}
		else
		{
			Console.WriteLine("Alias file:   (not generated)");
		}

		PrintMessagesByLevel(result.Messages);
	}

	/// <summary>
	/// Prints one Departures run: timing, how many procedures were read and survived each filter
	/// stage, how many GeoJSON files were written (a handful listed; every path with DevMode on -
	/// a full run writes thousands), the alias file with its command count, and the run's
	/// messages grouped by level.
	/// </summary>
	/// <param name="label">Heading for this run, e.g. "Departures: GeoJSON + Alias".</param>
	/// <param name="result">What <c>DepartureService.Run</c> returned.</param>
	public static void PrintDepartureServiceResult(string label, DepartureServiceResult result)
	{
		Console.WriteLine();
		Console.WriteLine($"=== {label} ===");
		Console.WriteLine($"Elapsed:      {result.Elapsed.TotalMilliseconds:N0} ms");
		Console.WriteLine($"Procedures in DP_BASE:          {result.ProcedureCount:N0}");
		Console.WriteLine($"Procedures after type/ARTCC/amendment filters: {result.ProceduresInScopeCount:N0}");
		Console.WriteLine($"Airport + procedure pairs output: {result.AirportProcedureCount:N0}");
		Console.WriteLine($"Pairs skipped (point not found):  {result.SkippedForMissingPointsCount:N0}");

		if (result.GeojsonFilesWritten.Count == 0)
		{
			Console.WriteLine("GeoJSON files written: (none)");
		}
		else
		{
			Console.WriteLine($"GeoJSON files written: {result.GeojsonFilesWritten.Count:N0}");

			const int maxWhenNotVerbose = 6;
			int shown = 0;

			foreach (string file in result.GeojsonFilesWritten)
			{
				if (!DevMode.IsEnabled && shown >= maxWhenNotVerbose)
				{
					Console.WriteLine($"  ... and {result.GeojsonFilesWritten.Count - shown:N0} more (enable DevMode to list every file)");
					break;
				}

				int count = result.GeojsonFeatureCountsByFile.TryGetValue(file, out int c) ? c : 0;
				Console.WriteLine($"  {Path.GetFileName(file)} - {count:N0} feature(s)");
				shown++;
			}
		}

		if (result.AliasFilePath is not null)
		{
			Console.WriteLine($"Alias file:   {result.AliasFilePath} ({result.AliasCommandCount:N0} command(s))");
		}
		else
		{
			Console.WriteLine("Alias file:   (not generated)");
		}

		PrintMessagesByLevel(result.Messages);
	}

	/// <summary>
	/// Prints one Arrivals run: timing, how many procedures were read and survived each filter
	/// stage, how many GeoJSON files were written (a handful listed; every path with DevMode on -
	/// a full run writes thousands), the alias file with its command count, and the run's
	/// messages grouped by level.
	/// </summary>
	/// <param name="label">Heading for this run, e.g. "Arrivals: GeoJSON + Alias".</param>
	/// <param name="result">What <c>ArrivalService.Run</c> returned.</param>
	public static void PrintArrivalServiceResult(string label, ArrivalServiceResult result)
	{
		Console.WriteLine();
		Console.WriteLine($"=== {label} ===");
		Console.WriteLine($"Elapsed:      {result.Elapsed.TotalMilliseconds:N0} ms");
		Console.WriteLine($"Procedures in STAR_BASE:          {result.ProcedureCount:N0}");
		Console.WriteLine($"Procedures after ARTCC/amendment filters: {result.ProceduresInScopeCount:N0}");
		Console.WriteLine($"Airport + procedure pairs output: {result.AirportProcedureCount:N0}");
		Console.WriteLine($"Pairs skipped (point not found):  {result.SkippedForMissingPointsCount:N0}");

		if (result.GeojsonFilesWritten.Count == 0)
		{
			Console.WriteLine("GeoJSON files written: (none)");
		}
		else
		{
			Console.WriteLine($"GeoJSON files written: {result.GeojsonFilesWritten.Count:N0}");

			const int maxWhenNotVerbose = 6;
			int shown = 0;

			foreach (string file in result.GeojsonFilesWritten)
			{
				if (!DevMode.IsEnabled && shown >= maxWhenNotVerbose)
				{
					Console.WriteLine($"  ... and {result.GeojsonFilesWritten.Count - shown:N0} more (enable DevMode to list every file)");
					break;
				}

				int count = result.GeojsonFeatureCountsByFile.TryGetValue(file, out int c) ? c : 0;
				Console.WriteLine($"  {Path.GetFileName(file)} - {count:N0} feature(s)");
				shown++;
			}
		}

		if (result.AliasFilePath is not null)
		{
			Console.WriteLine($"Alias file:   {result.AliasFilePath} ({result.AliasCommandCount:N0} command(s))");
		}
		else
		{
			Console.WriteLine("Alias file:   (not generated)");
		}

		PrintMessagesByLevel(result.Messages);
	}

	/// <summary>
	/// Prints a service's levelled messages grouped by <see cref="LogLevel"/>, most severe
	/// first. Warnings and errors are listed; the routine levels below them are collapsed to a
	/// count so a clean run stays readable. As with the airway warnings above, only the first
	/// few of any group print unless <see cref="DevMode.IsEnabled"/> is set.
	/// </summary>
	/// <param name="messages">Every message the service emitted, in the order it emitted them.</param>
	private static void PrintMessagesByLevel(IReadOnlyList<ServiceMessage> messages)
	{
		if (messages.Count == 0)
		{
			Console.WriteLine("Messages:     none");
			return;
		}

		Console.WriteLine($"Messages:     {messages.Count}");

		bool verbose = DevMode.IsEnabled;
		const int maxWhenNotVerbose = 3;

		var grouped = messages
			.GroupBy(m => m.Level)
			.OrderByDescending(g => g.Key);

		foreach (var group in grouped)
		{
			Console.WriteLine($"  [{group.Key}] ({group.Count()})");

			// Warnings and errors are the point of the list, so they always print. Info,
			// Success and Debug are routine narration and stay behind their count unless
			// developer mode asks for everything.
			if (group.Key is not (LogLevel.Warning or LogLevel.Error) && !verbose)
			{
				continue;
			}

			int shown = 0;

			foreach (ServiceMessage message in group)
			{
				if (!verbose && shown >= maxWhenNotVerbose)
				{
					Console.WriteLine($"    ... and {group.Count() - shown} more (enable DevMode for full detail)");
					break;
				}

				Console.WriteLine($"    - [{message.Source}] {message.Text}");
				shown++;
			}
		}
	}

	private static void PrintWarnings(IReadOnlyList<string> warnings)
	{
		if (warnings.Count == 0)
		{
			Console.WriteLine("Warnings:     none");
			return;
		}

		Console.WriteLine($"Warnings:     {warnings.Count}");

		// Group by the airway ID named at the start of the message ("Airway 'J3': ..."),
		// falling back to a "General" bucket for warnings not tied to one airway (e.g.
		// unrecognized settings keys).
		var grouped = warnings
			.GroupBy(w =>
			{
				Match match = AirwayIdPattern.Match(w);
				return match.Success ? match.Groups[1].Value : "General";
			})
			.OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

		bool verbose = DevMode.IsEnabled;

		foreach (var group in grouped)
		{
			Console.WriteLine($"  [{group.Key}] ({group.Count()})");

			// DevMode prints every warning; otherwise only the first few per group, to keep
			// a full-cycle run's console output readable.
			int shown = 0;
			const int maxWhenNotVerbose = 3;

			foreach (string warning in group)
			{
				if (!verbose && shown >= maxWhenNotVerbose)
				{
					Console.WriteLine($"    ... and {group.Count() - shown} more (enable DevMode for full detail)");
					break;
				}

				Console.WriteLine($"    - {warning}");
				shown++;
			}
		}
	}
}
