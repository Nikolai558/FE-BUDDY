using System.Diagnostics;

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
	/// <returns>What was built and written, plus timing and every message collected along the way.</returns>
	/// <exception cref="ArgumentException">Thrown when a required setting is missing or invalid.</exception>
	public static TelephonyServiceResult Run(TelephonyDataCollection? telephonyData, IReadOnlyDictionary<string, string> telephonySettings) =>
		Run(telephonyData, telephonySettings, DateOnly.FromDateTime(DateTime.UtcNow));

	/// <summary>
	/// Same as <see cref="Run(TelephonyDataCollection?, IReadOnlyDictionary{string, string})"/>, with the
	/// date a U.S. special call sign's expiration is compared with supplied, so tests do not depend on
	/// the clock.
	/// </summary>
	internal static TelephonyServiceResult Run(
		TelephonyDataCollection? telephonyData,
		IReadOnlyDictionary<string, string> telephonySettings,
		DateOnly today)
	{
		ArgumentNullException.ThrowIfNull(telephonySettings);

		Stopwatch stopwatch = Stopwatch.StartNew();
		List<ServiceMessage> messages = [];

		TelephonySettingsParseResult parseResult = TelephonySettingsParser.Parse(telephonySettings);
		messages.AddRange(parseResult.Messages);

		if (telephonyData is null)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				"There was no telephony data to build from, so Telephony.txt was not written."));

			return Finish(stopwatch, messages, new TelephonyBuildResult([], 0, 0, 0, []), new TelephonyAliasGenerateResult(null, 0, 0));
		}

		TelephonyBuildResult buildResult = TelephonyBuilder.Read(telephonyData, today);
		messages.AddRange(buildResult.Messages);

		TelephonyAliasGenerateResult aliasResult = TelephonyAliasWriter.Generate(buildResult.Entries, parseResult.Settings);

		messages.Add(new ServiceMessage(LogLevel.Info, LogSource, SummaryText(buildResult, aliasResult)));

		if (aliasResult.FilePath is null)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				"The FAA telephony pages had no operator with both a designator and a telephony, so Telephony.txt was not written.")
			{
				IsAdvisory = true
			});
		}

		return Finish(stopwatch, messages, buildResult, aliasResult);
	}

	/// <summary>
	/// The one-line summary of the file: its commands, how many show more than one operator, and
	/// what was left out and why.
	/// </summary>
	private static string SummaryText(TelephonyBuildResult build, TelephonyAliasGenerateResult alias)
	{
		int icao = build.Entries.Count(entry => entry.Kind == TelephonyEntryKind.IcaoAssignment);
		int special = build.Entries.Count - icao;

		return $"{TelephonyOutputFiles.Alias}: {alias.CommandCount:N0} command(s) for {icao:N0} ICAO operator(s) and " +
			$"{special:N0} U.S. special call sign(s); {alias.MergedCommandCount:N0} command(s) show more than one operator. " +
			$"Left out: {build.NoDesignatorCount:N0} row(s) with no designator, {build.NoTelephonyCount:N0} with no telephony, " +
			$"{build.ExpiredCount:N0} expired U.S. special call sign(s).";
	}

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

		int icao = build.Entries.Count(entry => entry.Kind == TelephonyEntryKind.IcaoAssignment);

		return new TelephonyServiceResult
		{
			Messages = messages,
			Elapsed = stopwatch.Elapsed,
			IcaoAssignmentCount = icao,
			SpecialCallSignCount = build.Entries.Count - icao,
			NoDesignatorCount = build.NoDesignatorCount,
			NoTelephonyCount = build.NoTelephonyCount,
			ExpiredCount = build.ExpiredCount,
			AliasFilePath = alias.FilePath,
			AliasCommandCount = alias.CommandCount,
			MergedCommandCount = alias.MergedCommandCount,
		};
	}
}
