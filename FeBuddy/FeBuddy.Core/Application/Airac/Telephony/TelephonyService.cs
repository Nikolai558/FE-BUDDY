using System.Diagnostics;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Telephony.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Telephony.Models;

namespace FeBuddy.Core.Application.Airac.Telephony;

/// <summary>
/// Public entry point for the Telephony sub-service: parses settings, reads the FAA telephony pages
/// into operator cards, and writes the <c>Telephony.txt</c> alias file.
/// </summary>
/// <remarks>
/// The only Telephony type <c>AiracService</c> and <c>FeBuddy.Harness</c> call directly; every other
/// type in this folder is a step of this pipeline. Like Wx Stations, its data does not come from a
/// NASR CSV cycle: the FAA telephony pages (JO 7340.2, Chapter 3, Sections 1 and 4) are not
/// published per AIRAC cycle, so every AIRAC Service run downloads the latest copies (see
/// <see cref="Infrastructure.Telephony.TelephonyDownloader"/>) and supplies them here already parsed.
/// </remarks>
public static class TelephonyService
{
	private const string LogSource = "TelephonyService";

	/// <summary>
	/// Runs the full Telephony pipeline: parse settings, read every operator, and write the alias file.
	/// </summary>
	/// <param name="telephonyData">
	/// The parsed FAA telephony pages, or <see langword="null"/> when FE-Buddy has no copy of the
	/// register - the run then writes nothing and says so; the AIRAC Service reports why the
	/// download failed.
	/// </param>
	/// <param name="telephonySettings">The raw Telephony settings dictionary.</param>
	/// <param name="fileNames">The names the user gave files in place of FE-Buddy's, or <see langword="null"/> for none.</param>
	/// <returns>What was built and written, plus timing and every message collected along the way.</returns>
	/// <exception cref="ArgumentException">Thrown when a required setting is missing or invalid.</exception>
	public static TelephonyServiceResult Run(
		TelephonyDataCollection? telephonyData,
		IReadOnlyDictionary<string, string> telephonySettings,
		OutputFileNames? fileNames = null) =>
		Run(telephonyData, telephonySettings, DateOnly.FromDateTime(DateTime.UtcNow), fileNames);

	/// <summary>
	/// Same as <see cref="Run(TelephonyDataCollection?, IReadOnlyDictionary{string, string}, OutputFileNames?)"/>,
	/// with the date a U.S. special call sign's expiration is compared with supplied, so tests do not
	/// depend on the clock.
	/// </summary>
	internal static TelephonyServiceResult Run(
		TelephonyDataCollection? telephonyData,
		IReadOnlyDictionary<string, string> telephonySettings,
		DateOnly today,
		OutputFileNames? fileNames = null)
	{
		ArgumentNullException.ThrowIfNull(telephonySettings);

		Stopwatch stopwatch = Stopwatch.StartNew();
		List<ServiceMessage> messages = [];

		TelephonySettingsParseResult parseResult = TelephonySettingsParser.Parse(telephonySettings);
		messages.AddRange(parseResult.Messages);

		TelephonySettings settings = parseResult.Settings with { FileNames = fileNames ?? OutputFileNames.None };
		string aliasFileName = settings.FileNames.FileName(TelephonyOutputFiles.Alias);

		if (telephonyData is null)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"There was no telephony data to build from, so {aliasFileName} was not written."));

			return Finish(stopwatch, messages, new TelephonyBuildResult([], 0, 0, 0, []), new TelephonyAliasGenerateResult(null, 0, 0));
		}

		TelephonyBuildResult buildResult = TelephonyBuilder.Read(telephonyData, today, settings.VirtualAirlines, settings.IncludeVatsimRadarVirtualAirlines);
		messages.AddRange(buildResult.Messages);

		TelephonyAliasGenerateResult aliasResult = TelephonyAliasWriter.Generate(buildResult.Entries, settings);

		messages.Add(new ServiceMessage(LogLevel.Info, LogSource, SummaryText(buildResult, aliasResult, aliasFileName)));

		if (aliasResult.FilePath is null)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"The FAA telephony pages had no operator with both a designator and a telephony, so {aliasFileName} was not written.")
			{
				IsAdvisory = true
			});
		}

		return Finish(stopwatch, messages, buildResult, aliasResult);
	}

	/// <summary>
	/// The one-line summary of the file: its commands, how many show more than one operator, and
	/// what was left out and why. Virtual airlines are named only when there are some, and the
	/// VATSIM-Radar list's only when it gave some.
	/// </summary>
	private static string SummaryText(TelephonyBuildResult build, TelephonyAliasGenerateResult alias, string aliasFileName)
	{
		int icao = Count(build, TelephonyEntryKind.IcaoAssignment);
		int special = Count(build, TelephonyEntryKind.UsSpecialCallSign);
		int virtualAirlines = Count(build, TelephonyEntryKind.VirtualAirline);
		string fromVatsimRadar = build.VatsimRadarVirtualAirlineCount > 0
			? $" ({build.VatsimRadarVirtualAirlineCount:N0} from the VATSIM-Radar list)"
			: string.Empty;

		string operators = virtualAirlines > 0
			? $"{icao:N0} ICAO operator(s), {special:N0} U.S. special call sign(s) and {virtualAirlines:N0} virtual airline(s){fromVatsimRadar}"
			: $"{icao:N0} ICAO operator(s) and {special:N0} U.S. special call sign(s)";

		return $"{aliasFileName}: {alias.CommandCount:N0} command(s) for {operators}; " +
			$"{alias.MergedCommandCount:N0} command(s) show more than one operator. " +
			$"Left out: {build.NoDesignatorCount:N0} row(s) with no designator, {build.NoTelephonyCount:N0} with no telephony, " +
			$"{build.ExpiredCount:N0} expired U.S. special call sign(s).";
	}

	private static int Count(TelephonyBuildResult build, TelephonyEntryKind kind) => build.Entries.Count(entry => entry.Kind == kind);

	/// <summary>Stops the clock, copies every message to the shared application log, and assembles the result.</summary>
	private static TelephonyServiceResult Finish(
		Stopwatch stopwatch,
		List<ServiceMessage> messages,
		TelephonyBuildResult build,
		TelephonyAliasGenerateResult alias)
	{
		stopwatch.Stop();

		foreach (ServiceMessage message in messages)
		{
			AppLog.Write(message.Level, message.Source, message.Text);
		}

		return new TelephonyServiceResult
		{
			Messages = messages,
			Elapsed = stopwatch.Elapsed,
			IcaoAssignmentCount = Count(build, TelephonyEntryKind.IcaoAssignment),
			SpecialCallSignCount = Count(build, TelephonyEntryKind.UsSpecialCallSign),
			VirtualAirlineCount = Count(build, TelephonyEntryKind.VirtualAirline),
			VatsimRadarVirtualAirlineCount = build.VatsimRadarVirtualAirlineCount,
			NoDesignatorCount = build.NoDesignatorCount,
			NoTelephonyCount = build.NoTelephonyCount,
			ExpiredCount = build.ExpiredCount,
			AliasFilePath = alias.FilePath,
			AliasCommandCount = alias.CommandCount,
			MergedCommandCount = alias.MergedCommandCount,
		};
	}
}
