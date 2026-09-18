using System.Text.RegularExpressions;

using FeBuddy.Core.Configuration;
using FeBuddy.Core.Models.Services.Airac.Airways;

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
