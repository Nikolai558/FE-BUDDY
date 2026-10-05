using System.Diagnostics;

using FeBuddy.Core.Application.Airac.Airports;
using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Application.Airac.Airways;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Airac.Arrivals;
using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Application.Airac.ArtccBoundaries;
using FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;
using FeBuddy.Core.Application.Airac.Departures;
using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Application.Airac.Fixes;
using FeBuddy.Core.Application.Airac.Fixes.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Navaids;
using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Application.Airac.Telephony;
using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Application.Airac.VnasAlias;
using FeBuddy.Core.Application.Airac.VnasAlias.Models;
using FeBuddy.Core.Application.Airac.WxStations;
using FeBuddy.Core.Application.Airac.WxStations.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Airac;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Telephony.Models;
using FeBuddy.Core.Infrastructure.WxStations.Models;

namespace FeBuddy.Core.Application.Airac;

/// <summary>
/// The AIRAC Service: the GUI calls this once per "Run AIRAC Service". It runs each selected
/// sub-service (Airways, Airports, Departures, Arrivals, NAVAIDs, ARTCC Boundaries, Fixes, Wx
/// Stations, Procedures, Telephony, Concatenate Aliases) against one cycle's NASR data and gathers
/// the results. Four need more than the NASR cycle: Wx Stations and Telephony read data that is
/// not published per cycle at all - aviationweather.gov's station list and the FAA telephony
/// pages - which the run downloads fresh every time (see <see cref="AiracSharedDataLoader"/>);
/// Procedures also needs the selected cycle's (and the previous cycle's) FAA d-TPP Metafile (see
/// <see cref="AiracCycleDataCache.GetDtppAsync"/>); and Concatenate Aliases reads the user's own
/// custom alias files, from this PC or the web (see <see cref="AliasSourceLoader"/>).
/// </summary>
/// <remarks>
/// <para>
/// It never parses NASR data itself. The parsed cycle comes from
/// <see cref="AiracCycleDataCache"/>, which the launch sequence has usually filled already.
/// </para>
/// <para>
/// Every sub-service writes into the same cycle folder
/// (<see cref="AiracServiceSettings.CycleOutputDirectory"/>): the service sets it as each
/// block's <c>OutputDirectory</c>, so the GeoJSON of every sub-service lands in one
/// <c>Geojson</c> folder (see <see cref="AiracOutputPaths"/>).
/// </para>
/// <para>
/// Once every sub-service has run, the alias files the run wrote are checked together for
/// commands more than one line uses (<see cref="DuplicateAliasReport"/>), and
/// <c>Duplicate_Alias_Commands.txt</c> is written into the cycle folder. Then vNAS takes one
/// alias file per facility, so when Concatenate Aliases is in the run and combining is on, every
/// alias file the run wrote is merged, followed by the custom alias files, into
/// <c>Aliases\Combined_Alias.txt</c> (<see cref="VnasAliasFileWriter"/>).
/// </para>
/// <para>
/// A file the user renamed (<see cref="AiracServiceSettings.FileNames"/>) is written under its new
/// name wherever it goes; everything else still knows it by its key, FE-Buddy's name for it.
/// </para>
/// </remarks>
public static class AiracService
{
	private const string LogSource = "AiracService";

	/// <summary>The Concatenate Aliases sub-service's name in progress reports: its tab's title.</summary>
	private const string VnasAliasName = "Concatenate Aliases";

	/// <summary>
	/// Whether the run's cycle folder already holds anything - an earlier run of the same cycle
	/// wrote there. The GUI asks this before a run, to offer to overwrite or delete those files
	/// (<see cref="AiracServiceSettings.ExistingOutput"/>).
	/// </summary>
	/// <param name="settings">The run's settings.</param>
	/// <returns><see langword="true"/> when the cycle folder exists and is not empty.</returns>
	public static bool HasExistingOutput(AiracServiceSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		string directory = settings.CycleOutputDirectory;
		return Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any();
	}

	/// <summary>
	/// Runs every selected sub-service, resolving the parsed cycle data for
	/// <see cref="AiracServiceSettings.SelectedCycle"/> from <see cref="AiracCycleDataCache.Instance"/>
	/// (awaiting an in-flight parse rather than starting a second one). When Procedures is selected
	/// its d-TPP Metafiles come from the cache the same way; when Wx Stations or Telephony is
	/// selected, the latest copy of its data is downloaded first (see
	/// <see cref="AiracSharedDataLoader"/>), falling back on the last good copy; and when Concatenate
	/// Aliases is selected and combining, its custom alias files are read first (see
	/// <see cref="AliasSourceLoader"/>) - one that cannot be read is left out of <c>Combined_Alias.txt</c>
	/// with a warning.
	/// </summary>
	/// <param name="settings">The run's cross-cutting choices and per-sub-service settings blocks.</param>
	/// <param name="progress">Optional per-sub-service progress for the run panel.</param>
	/// <param name="cancellationToken">Cancels the run.</param>
	/// <returns>The aggregated result.</returns>
	public static async Task<AiracServiceResult> RunAsync(
		AiracServiceSettings settings,
		IProgress<AiracServiceProgress>? progress = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(settings);

		progress?.Report(new AiracServiceProgress("AIRAC", $"Loading parsed data for cycle {settings.SelectedCycle.AiracCycleId}"));
		NasrCsvDataCollection nasrData = await AiracCycleDataCache.Instance
			.GetAsync(settings.SelectedCycle.AiracCycleId, cancellationToken)
			.ConfigureAwait(false);

		List<ServiceMessage> supplementalMessages = [];
		WxStationDataCollection? wxStationData = null;
		TelephonyDataCollection? telephonyData = null;

		if (settings.WxStations is not null)
		{
			progress?.Report(new AiracServiceProgress("AIRAC", "Downloading the latest Wx station data"));
			AiracSharedDataLoadResult<WxStationDataCollection> loaded =
				await AiracSharedDataLoader.LoadWxStationsAsync(cancellationToken).ConfigureAwait(false);

			wxStationData = loaded.Data;
			supplementalMessages.AddRange(loaded.Messages);
		}

		if (settings.Telephony is not null)
		{
			bool includeVatsimRadar = TelephonySettingsParser.IncludesVatsimRadarList(settings.Telephony);

			progress?.Report(new AiracServiceProgress("AIRAC", includeVatsimRadar
				? "Downloading the latest FAA telephony pages and VATSIM-Radar Virtual Airline List"
				: "Downloading the latest FAA telephony pages"));
			AiracSharedDataLoadResult<TelephonyDataCollection> loaded =
				await AiracSharedDataLoader.LoadTelephonyAsync(includeVatsimRadar, cancellationToken).ConfigureAwait(false);

			telephonyData = loaded.Data;
			supplementalMessages.AddRange(loaded.Messages);
		}

		DtppMetafileDataCollection? dtppData = null;
		DtppMetafileDataCollection? previousDtppData = null;

		if (settings.Procedures is not null)
		{
			progress?.Report(new AiracServiceProgress("AIRAC", $"Loading d-TPP Metafile data for cycle {settings.SelectedCycle.AiracCycleId}"));
			dtppData = await AiracCycleDataCache.Instance
				.GetDtppAsync(settings.SelectedCycle.AiracCycleId, cancellationToken)
				.ConfigureAwait(false);

			// The previous cycle's metafile is what links a procedure the selected cycle deletes
			// back to the chart it last had (see AiracSupplementalData.PreviousDtpp) - it may not be
			// one of the three cycles the cache tracks, in which case GetDtppAsync just answers null.
			AiracCycleInfo previousCycle = AiracCycleResolver.GetCycle(AiracCyclePosition.Previous, asOfUtc: settings.SelectedCycle.EffectiveDateUtc);

			progress?.Report(new AiracServiceProgress("AIRAC", $"Loading d-TPP Metafile data for cycle {previousCycle.AiracCycleId}"));
			previousDtppData = await AiracCycleDataCache.Instance
				.GetDtppAsync(previousCycle.AiracCycleId, cancellationToken)
				.ConfigureAwait(false);
		}

		IReadOnlyList<AliasSourceLoad>? customAliasFiles = null;

		if (settings.VnasAlias is not null)
		{
			VnasAliasSettingsParseResult parsed = VnasAliasSettingsParser.Parse(settings.VnasAlias);
			supplementalMessages.AddRange(parsed.Messages);

			if (parsed.Combine)
			{
				progress?.Report(new AiracServiceProgress(VnasAliasName, "Reading your custom alias files"));

				customAliasFiles = await AliasSourceLoader
					.LoadAllAsync(parsed.Sources, CredentialStore.Default, cancellationToken: cancellationToken)
					.ConfigureAwait(false);
			}
		}

		AiracSupplementalData supplementalData = new()
		{
			WxStations = wxStationData,
			Telephony = telephonyData,
			Dtpp = dtppData,
			PreviousDtpp = previousDtppData,
			CustomAliasFiles = customAliasFiles,
			Messages = supplementalMessages,
		};

		return await RunAsync(settings, nasrData, supplementalData, progress, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Runs every selected sub-service against the supplied parsed cycle data, with no Wx station
	/// data: a selected Wx Stations sub-service writes nothing, saying it had no data. Kept for
	/// callers from before Wx Stations existed.
	/// </summary>
	/// <param name="settings">The run's cross-cutting choices and per-sub-service settings blocks.</param>
	/// <param name="nasrData">
	/// The parsed NASR CSV data for <see cref="AiracServiceSettings.SelectedCycle"/>.
	/// </param>
	/// <param name="progress">Optional per-sub-service progress for the run panel.</param>
	/// <param name="cancellationToken">Cancels before the next sub-service starts.</param>
	/// <returns>The aggregated result: each sub-service's result plus a combined warning list.</returns>
	/// <exception cref="ArgumentException">Thrown when <see cref="AiracServiceSettings.OutputDirectory"/> is blank.</exception>
	/// <exception cref="IOException">
	/// Thrown when <see cref="ExistingOutputAction.DeleteExisting"/> cannot delete the cycle
	/// folder (a file in it is open elsewhere, say). Nothing is written in that case, but the
	/// files deleted before the failure stay deleted.
	/// </exception>
	public static Task<AiracServiceResult> RunAsync(
		AiracServiceSettings settings,
		NasrCsvDataCollection nasrData,
		IProgress<AiracServiceProgress>? progress = null,
		CancellationToken cancellationToken = default) =>
		RunAsync(settings, nasrData, wxStationData: null, progress, cancellationToken);

	/// <summary>
	/// Runs every selected sub-service against the supplied parsed cycle data and Wx station data.
	/// Delegates to the overload that also accepts a d-TPP Metafile, wrapping
	/// <paramref name="wxStationData"/> in an <see cref="AiracSupplementalData"/> with everything
	/// else left <see langword="null"/>. Kept for callers from before Procedures existed.
	/// </summary>
	/// <param name="settings">The run's cross-cutting choices and per-sub-service settings blocks.</param>
	/// <param name="nasrData">
	/// The parsed NASR CSV data for <see cref="AiracServiceSettings.SelectedCycle"/>.
	/// </param>
	/// <param name="wxStationData">
	/// The parsed Wx station data, or <see langword="null"/> when there is none. Only read when
	/// <see cref="AiracServiceSettings.WxStations"/> is not <see langword="null"/>.
	/// </param>
	/// <param name="progress">Optional per-sub-service progress for the run panel.</param>
	/// <param name="cancellationToken">Cancels before the next sub-service starts.</param>
	/// <returns>The aggregated result: each sub-service's result plus a combined warning list.</returns>
	/// <exception cref="ArgumentException">Thrown when <see cref="AiracServiceSettings.OutputDirectory"/> is blank.</exception>
	/// <exception cref="IOException">
	/// Thrown when <see cref="ExistingOutputAction.DeleteExisting"/> cannot delete the cycle
	/// folder (a file in it is open elsewhere, say). Nothing is written in that case, but the
	/// files deleted before the failure stay deleted.
	/// </exception>
	public static Task<AiracServiceResult> RunAsync(
		AiracServiceSettings settings,
		NasrCsvDataCollection nasrData,
		WxStationDataCollection? wxStationData,
		IProgress<AiracServiceProgress>? progress = null,
		CancellationToken cancellationToken = default) =>
		RunAsync(settings, nasrData, new AiracSupplementalData { WxStations = wxStationData }, progress, cancellationToken);

	/// <summary>
	/// Runs every selected sub-service against the supplied parsed cycle data and supplemental
	/// data (Wx station data, the FAA telephony pages and, when Procedures is selected, the FAA
	/// d-TPP Metafile). This is the real implementation every other <c>RunAsync</c> overload
	/// delegates to.
	/// </summary>
	/// <param name="settings">The run's cross-cutting choices and per-sub-service settings blocks.</param>
	/// <param name="nasrData">
	/// The parsed NASR CSV data for <see cref="AiracServiceSettings.SelectedCycle"/>.
	/// </param>
	/// <param name="supplementalData">
	/// The Wx station, telephony and d-TPP Metafile data the selected sub-services need beyond the
	/// NASR data, and what getting it produced for the Review tab (added to the run's messages).
	/// </param>
	/// <param name="progress">Optional per-sub-service progress for the run panel.</param>
	/// <param name="cancellationToken">Cancels before the next sub-service starts.</param>
	/// <returns>The aggregated result: each sub-service's result plus a combined warning list.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when <see cref="AiracServiceSettings.OutputDirectory"/> is blank, or a new name in
	/// <see cref="AiracServiceSettings.FileNames"/> can't be used. Nothing is deleted or written then.
	/// </exception>
	/// <exception cref="IOException">
	/// Thrown when <see cref="ExistingOutputAction.DeleteExisting"/> cannot delete the cycle
	/// folder (a file in it is open elsewhere, say). Nothing is written in that case, but the
	/// files deleted before the failure stay deleted.
	/// </exception>
	public static async Task<AiracServiceResult> RunAsync(
		AiracServiceSettings settings,
		NasrCsvDataCollection nasrData,
		AiracSupplementalData supplementalData,
		IProgress<AiracServiceProgress>? progress = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(settings);
		ArgumentNullException.ThrowIfNull(nasrData);
		ArgumentNullException.ThrowIfNull(supplementalData);
		ArgumentException.ThrowIfNullOrWhiteSpace(settings.OutputDirectory, nameof(settings));

		// Before anything is deleted or written: a new name that can't be used stops the run here.
		OutputFileNamesParseResult fileNamesParse = OutputFileNamesParser.Parse(settings.FileNames);
		OutputFileNames fileNames = fileNamesParse.FileNames;
		string combinedAliasFileName = fileNames.FileName(AiracOutputPaths.CombinedAliasFileName);

		Stopwatch stopwatch = Stopwatch.StartNew();
		string outputDirectory = settings.CycleOutputDirectory;
		bool anySelected = settings.Airways is not null || settings.Airports is not null
			|| settings.Departures is not null || settings.Arrivals is not null || settings.Navaids is not null
			|| settings.ArtccBoundaries is not null || settings.Fixes is not null || settings.WxStations is not null
			|| settings.Procedures is not null || settings.Telephony is not null
			|| settings.VnasAlias is not null;

		// What getting the downloaded data produced (a fresh copy, an older copy and its age, or none)
		// leads the run's messages, so the Review tab says it before anything built from that data.
		List<ServiceMessage> messages = [.. supplementalData.Messages, .. fileNamesParse.Messages];

		foreach (ServiceMessage message in messages)
		{
			AppLog.Write(message.Level, message.Source, message.Text);
		}

		// Only when there is something to write: a run with nothing selected must not empty the
		// folder and leave it that way.
		if (anySelected && settings.ExistingOutput == ExistingOutputAction.DeleteExisting && Directory.Exists(outputDirectory))
		{
			progress?.Report(new AiracServiceProgress("AIRAC", $"Deleting the files already in {outputDirectory}"));

			// Off the caller's thread: a Departures or Arrivals run leaves thousands of files, and
			// the GUI awaits this from its UI thread.
			await Task.Run(() => Directory.Delete(outputDirectory, recursive: true), cancellationToken).ConfigureAwait(false);
			AppLog.Info(LogSource, $"Deleted the earlier output in '{outputDirectory}' before the run.");
		}

		AirwayServiceResult? airwaysResult = await RunSubServiceAsync(
			settings.Airways, "Airways", "Building airway GeoJSON and alias output",
			block => AirwayService.Run(nasrData, block, fileNames),
			result => $"{result.AirwayCount} airway(s), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		AirportServiceResult? airportsResult = await RunSubServiceAsync(
			settings.Airports, "Airports", "Building airport GeoJSON and alias output",
			block => AirportService.Run(nasrData, block, fileNames),
			result => $"{result.AirportCount} airport(s), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		DepartureServiceResult? departuresResult = await RunSubServiceAsync(
			settings.Departures, "Departures", "Building departure procedure GeoJSON and alias output",
			block => DepartureService.Run(nasrData, block, fileNames),
			result => $"{result.AirportProcedureCount} airport procedure(s), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		ArrivalServiceResult? arrivalsResult = await RunSubServiceAsync(
			settings.Arrivals, "Arrivals", "Building arrival procedure GeoJSON and alias output",
			block => ArrivalService.Run(nasrData, block, fileNames),
			result => $"{result.AirportProcedureCount} airport procedure(s), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		NavaidServiceResult? navaidsResult = await RunSubServiceAsync(
			settings.Navaids, "NAVAIDs", "Building NAVAID GeoJSON and alias output",
			block => NavaidService.Run(nasrData, block, fileNames),
			result => $"{result.NavaidCount} NAVAID(s), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		ArtccBoundaryServiceResult? artccBoundariesResult = await RunSubServiceAsync(
			settings.ArtccBoundaries, "ARTCC Boundaries", "Building ARTCC boundary GeoJSON output",
			block => ArtccBoundaryService.Run(nasrData, block, fileNames),
			result => $"{result.LocationCount} ARTCC(s), {result.RingCount} boundary line(s), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		FixServiceResult? fixesResult = await RunSubServiceAsync(
			settings.Fixes, "Fixes", "Building fix GeoJSON output",
			block => FixService.Run(nasrData, block, fileNames),
			result => $"{result.FixCount} fix(es), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		WxStationServiceResult? wxStationsResult = await RunSubServiceAsync(
			settings.WxStations, "Wx Stations", "Building weather station GeoJSON output",
			block => WxStationService.Run(supplementalData.WxStations, block, fileNames),
			result => $"{result.StationCount} station(s), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		ProcedureServiceResult? proceduresResult = await RunSubServiceAsync(
			settings.Procedures, "Procedures", "Building procedure publication documents",
			block => ProcedureService.Run(nasrData, supplementalData.Dtpp, supplementalData.PreviousDtpp, block, fileNames),
			result => $"{result.AirportCount} airport(s), {result.NewCount + result.ChangedCount + result.DeletedCount} change(s), {result.FilesWritten.Count} document(s)"
				+ (result.AliasFilePath is not null ? $", {result.AliasCommandCount} FAA Chart Recall command(s)" : string.Empty)).ConfigureAwait(false);

		TelephonyServiceResult? telephonyResult = await RunSubServiceAsync(
			settings.Telephony, "Telephony", "Building the telephony alias file",
			block => TelephonyService.Run(supplementalData.Telephony, block, fileNames),
			result => $"{result.AliasCommandCount} command(s), {result.MergedCommandCount} showing more than one operator").ConfigureAwait(false);

		// Every alias file this run wrote, in the order the sub-services ran: checked together for
		// commands two lines share, then combined. Each is known by its key - FE-Buddy's name for it -
		// whatever the user named it.
		(string Key, string? Path)[] aliasOutputs =
		[
			(AirwayOutputFiles.Alias, airwaysResult?.AliasFilePath),
			(AirportOutputFiles.Alias, airportsResult?.AliasFilePath),
			(DepartureOutputFiles.Alias, departuresResult?.AliasFilePath),
			(ArrivalOutputFiles.Alias, arrivalsResult?.AliasFilePath),
			(NavaidOutputFiles.Alias, navaidsResult?.AliasFilePath),
			(ProcedureOutputFiles.Alias, proceduresResult?.AliasFilePath),
			(TelephonyOutputFiles.Alias, telephonyResult?.AliasFilePath),
		];

		AliasFileWritten[] aliasFiles = [.. aliasOutputs
			.Where(output => output.Path is not null)
			.Select(output => new AliasFileWritten(output.Key, output.Path!))];

		DuplicateAliasReportResult? duplicateAliasReport = null;

		if (aliasFiles.Length > 0)
		{
			cancellationToken.ThrowIfCancellationRequested();
			progress?.Report(new AiracServiceProgress("AIRAC", "Checking the alias files for duplicate commands"));

			duplicateAliasReport = await Task.Run(
				() => DuplicateAliasReport.Write(
					aliasFiles, nasrData, settings.SelectedCycle.AiracCycleId, outputDirectory, settings.PrimaryFacility, DateTime.UtcNow,
					fileNames.FileName(AiracOutputPaths.DuplicateAliasReportFileName)),
				cancellationToken).ConfigureAwait(false);

			ServiceMessage reportMessage = DuplicateAliasMessage(duplicateAliasReport, aliasFiles.Length);
			messages.Add(reportMessage);
			AppLog.Write(reportMessage.Level, reportMessage.Source, reportMessage.Text);
		}

		// vNAS takes one alias file, so every alias file the run wrote goes into Combined_Alias.txt, then
		// the user's own custom alias files - while Concatenate Aliases is in the run and combining.
		bool combine = settings.VnasAlias is not null && VnasAliasSettingsParser.CombinesAliasFiles(settings.VnasAlias);
		VnasAliasResult? vnasAliasResult = null;

		if (combine)
		{
			cancellationToken.ThrowIfCancellationRequested();
			progress?.Report(new AiracServiceProgress(VnasAliasName, $"Writing {combinedAliasFileName}"));

			IReadOnlyList<AliasSourceLoad> customFiles = supplementalData.CustomAliasFiles ?? [];
			string[] feBuddyFiles = [.. aliasFiles.Select(file => file.FilePath)];

			vnasAliasResult = await Task.Run(
				() => VnasAliasFileWriter.Write(customFiles, feBuddyFiles, settings.SelectedCycle.AiracCycleId, outputDirectory, combinedAliasFileName),
				cancellationToken).ConfigureAwait(false);

			messages.AddRange(vnasAliasResult.Messages);

			string summary = vnasAliasResult.FilePath is null
				? $"{combinedAliasFileName} not written."
				: $"{combinedAliasFileName}: {vnasAliasResult.FeBuddyCommandCount:N0} command(s) from {vnasAliasResult.FeBuddyFiles.Count} " +
					$"FE-Buddy alias file(s), then {vnasAliasResult.CustomCommandCount:N0} custom command(s).";

			progress?.Report(new AiracServiceProgress(VnasAliasName, summary, 100));
		}
		else
		{
			if (settings.VnasAlias is not null)
			{
				progress?.Report(new AiracServiceProgress(VnasAliasName, $"Combining is off, so {combinedAliasFileName} was not written.", 100));
			}

			// Combined_Alias.txt is built from the run's alias files, so one an earlier run left is out of
			// date once this run rewrites them without writing a new one. A run that writes no alias
			// file leaves it alone, like any other earlier file ("Overwrite files").
			if (aliasFiles.Length > 0)
			{
				(string sentence, bool failed) = VnasAliasFileWriter.DeleteEarlierFile(outputDirectory, combinedAliasFileName);

				if (sentence.Length > 0)
				{
					string why = settings.VnasAlias is null ? "Concatenate Aliases is not in the run" : "Combining is off on the Concatenate Aliases tab";
					ServiceMessage deleted = new(failed ? LogLevel.Warning : LogLevel.Info, LogSource,
						$"{why}, so {combinedAliasFileName} was not written.{sentence}")
					{ IsAdvisory = true };

					messages.Add(deleted);
					AppLog.Write(deleted.Level, deleted.Source, deleted.Text);
				}
			}
		}

		if (!anySelected)
		{
			const string message = "AIRAC Service run requested with no sub-service selected; nothing to do.";
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource, message));
			AppLog.Warning(LogSource, message);
		}

		stopwatch.Stop();

		return new AiracServiceResult
		{
			Messages = messages,
			Elapsed = stopwatch.Elapsed,
			OutputDirectory = outputDirectory,
			DuplicateAliasReport = duplicateAliasReport,
			Airways = airwaysResult,
			Airports = airportsResult,
			Departures = departuresResult,
			Arrivals = arrivalsResult,
			Navaids = navaidsResult,
			ArtccBoundaries = artccBoundariesResult,
			Fixes = fixesResult,
			WxStations = wxStationsResult,
			Procedures = proceduresResult,
			Telephony = telephonyResult,
			VnasAlias = vnasAliasResult,
		};

		// Runs one sub-service if it was selected (its settings block is not null), reporting
		// start and finish to the run panel and the log. Null when it was not selected.
		async Task<TResult?> RunSubServiceAsync<TResult>(
			IReadOnlyDictionary<string, string>? block,
			string name,
			string startMessage,
			Func<IReadOnlyDictionary<string, string>, TResult> run,
			Func<TResult, string> summarize)
			where TResult : ServiceResult
		{
			if (block is null)
			{
				return null;
			}

			cancellationToken.ThrowIfCancellationRequested();

			AppLog.Info(LogSource, $"AIRAC Service: running {name} for cycle {settings.SelectedCycle.AiracCycleId}.");
			progress?.Report(new AiracServiceProgress(name, startMessage));

			// Keys match ignoring case whatever dictionary the caller built. Every sub-service
			// writes into the one cycle folder, whatever the block said.
			Dictionary<string, string> caseInsensitive = new(block, StringComparer.OrdinalIgnoreCase)
			{
				["OutputDirectory"] = outputDirectory,
			};

			TResult result = await Task.Run(() => run(caseInsensitive), cancellationToken).ConfigureAwait(false);

			messages.AddRange(result.Messages);

			string summary = $"{name} complete: {summarize(result)}.";
			progress?.Report(new AiracServiceProgress(name, summary, 100));
			AppLog.Success(LogSource, summary);

			return result;
		}
	}

	/// <summary>
	/// What the run panel says about the duplicate alias report: an advisory warning when there is
	/// something to fix, otherwise a note that the files were checked.
	/// </summary>
	private static ServiceMessage DuplicateAliasMessage(DuplicateAliasReportResult report, int fileCount)
	{
		if (report.Duplicates.Count == 0)
		{
			return new ServiceMessage(LogLevel.Info, LogSource,
				$"No duplicate alias commands in the {fileCount} alias file(s) this run wrote.");
		}

		return new ServiceMessage(LogLevel.Warning, LogSource,
			$"{report.Duplicates.Count:N0} alias command(s) are used by more than one line of this run's alias files, so CRC can only run " +
			$"one of each. They are listed by ARTCC in {Path.GetFileName(report.FilePath)} in the cycle folder.")
		{
			IsAdvisory = true
		};
	}
}
