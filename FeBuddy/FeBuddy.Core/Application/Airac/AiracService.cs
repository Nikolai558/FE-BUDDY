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
using FeBuddy.Core.Application.Settings;
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
/// Stations, Procedures, Telephony) against one cycle's NASR data and gathers the results. Three
/// need more than the NASR cycle: Wx Stations and Telephony read data that is not published per
/// cycle at all - aviationweather.gov's station list and the FAA telephony pages - which the run
/// downloads fresh every time (see <see cref="AiracSharedDataLoader"/>); Procedures also needs the
/// selected cycle's (and the previous cycle's) FAA d-TPP Metafile (see
/// <see cref="AiracCycleDataCache.GetDtppAsync"/>).
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
/// <c>Duplicate_Alias_Commands.txt</c> is written into the cycle folder.
/// </para>
/// </remarks>
public static class AiracService
{
	private const string LogSource = "AiracService";

	/// <summary>The vNAS Alias Upload sub-service's name in progress reports: its tab's title.</summary>
	private const string VnasAliasName = "vNAS Alias Upload";

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
	/// <see cref="AiracSharedDataLoader"/>), falling back on the last good copy.
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
			progress?.Report(new AiracServiceProgress("AIRAC", "Downloading the latest FAA telephony pages"));
			AiracSharedDataLoadResult<TelephonyDataCollection> loaded =
				await AiracSharedDataLoader.LoadTelephonyAsync(cancellationToken).ConfigureAwait(false);

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
			progress?.Report(new AiracServiceProgress(VnasAliasName, "Reading your custom alias files"));
			VnasAliasSettingsParseResult parsed = VnasAliasSettingsParser.Parse(settings.VnasAlias);
			supplementalMessages.AddRange(parsed.Messages);

			customAliasFiles = await AliasSourceLoader
				.LoadAllAsync(parsed.Sources, CredentialStore.Default, cancellationToken: cancellationToken)
				.ConfigureAwait(false);
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
	/// <exception cref="ArgumentException">Thrown when <see cref="AiracServiceSettings.OutputDirectory"/> is blank.</exception>
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

		Stopwatch stopwatch = Stopwatch.StartNew();
		string outputDirectory = settings.CycleOutputDirectory;
		bool anySelected = settings.Airways is not null || settings.Airports is not null
			|| settings.Departures is not null || settings.Arrivals is not null || settings.Navaids is not null
			|| settings.ArtccBoundaries is not null || settings.Fixes is not null || settings.WxStations is not null
			|| settings.Procedures is not null || settings.Telephony is not null
			|| settings.VnasAlias is not null;

		// What getting the downloaded data produced (a fresh copy, an older copy and its age, or none)
		// leads the run's messages, so the Review tab says it before anything built from that data.
		List<ServiceMessage> messages = [.. supplementalData.Messages];

		foreach (ServiceMessage message in supplementalData.Messages)
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
			block => AirwayService.Run(nasrData, block),
			result => $"{result.AirwayCount} airway(s), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		AirportServiceResult? airportsResult = await RunSubServiceAsync(
			settings.Airports, "Airports", "Building airport GeoJSON and alias output",
			block => AirportService.Run(nasrData, block),
			result => $"{result.AirportCount} airport(s), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		DepartureServiceResult? departuresResult = await RunSubServiceAsync(
			settings.Departures, "Departures", "Building departure procedure GeoJSON and alias output",
			block => DepartureService.Run(nasrData, block),
			result => $"{result.AirportProcedureCount} airport procedure(s), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		ArrivalServiceResult? arrivalsResult = await RunSubServiceAsync(
			settings.Arrivals, "Arrivals", "Building arrival procedure GeoJSON and alias output",
			block => ArrivalService.Run(nasrData, block),
			result => $"{result.AirportProcedureCount} airport procedure(s), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		NavaidServiceResult? navaidsResult = await RunSubServiceAsync(
			settings.Navaids, "NAVAIDs", "Building NAVAID GeoJSON and alias output",
			block => NavaidService.Run(nasrData, block),
			result => $"{result.NavaidCount} NAVAID(s), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		ArtccBoundaryServiceResult? artccBoundariesResult = await RunSubServiceAsync(
			settings.ArtccBoundaries, "ARTCC Boundaries", "Building ARTCC boundary GeoJSON output",
			block => ArtccBoundaryService.Run(nasrData, block),
			result => $"{result.LocationCount} ARTCC(s), {result.RingCount} boundary line(s), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		FixServiceResult? fixesResult = await RunSubServiceAsync(
			settings.Fixes, "Fixes", "Building fix GeoJSON output",
			block => FixService.Run(nasrData, block),
			result => $"{result.FixCount} fix(es), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		WxStationServiceResult? wxStationsResult = await RunSubServiceAsync(
			settings.WxStations, "Wx Stations", "Building weather station GeoJSON output",
			block => WxStationService.Run(supplementalData.WxStations, block),
			result => $"{result.StationCount} station(s), {result.GeojsonFilesWritten.Count} GeoJSON file(s)").ConfigureAwait(false);

		ProcedureServiceResult? proceduresResult = await RunSubServiceAsync(
			settings.Procedures, "Procedures", "Building procedure publication documents",
			block => ProcedureService.Run(nasrData, supplementalData.Dtpp, supplementalData.PreviousDtpp, block),
			result => $"{result.AirportCount} airport(s), {result.NewCount + result.ChangedCount + result.DeletedCount} change(s), {result.FilesWritten.Count} document(s)"
				+ (result.AliasFilePath is not null ? $", {result.AliasCommandCount} FAA Chart Recall command(s)" : string.Empty)).ConfigureAwait(false);

		TelephonyServiceResult? telephonyResult = await RunSubServiceAsync(
			settings.Telephony, "Telephony", "Building the telephony alias file",
			block => TelephonyService.Run(supplementalData.Telephony, block),
			result => $"{result.AliasCommandCount} command(s), {result.MergedCommandCount} showing more than one operator").ConfigureAwait(false);

		// Every alias file this run wrote, in the order the sub-services ran, checked together for
		// commands two lines share.
		string[] aliasFiles = [.. new[]
		{
			airwaysResult?.AliasFilePath,
			airportsResult?.AliasFilePath,
			departuresResult?.AliasFilePath,
			arrivalsResult?.AliasFilePath,
			navaidsResult?.AliasFilePath,
			proceduresResult?.AliasFilePath,
			telephonyResult?.AliasFilePath,
		}.OfType<string>()];

		DuplicateAliasReportResult? duplicateAliasReport = null;

		if (aliasFiles.Length > 0)
		{
			cancellationToken.ThrowIfCancellationRequested();
			progress?.Report(new AiracServiceProgress("AIRAC", "Checking the alias files for duplicate commands"));

			duplicateAliasReport = await Task.Run(
				() => DuplicateAliasReport.Write(
					aliasFiles, nasrData, settings.SelectedCycle.AiracCycleId, outputDirectory, settings.PrimaryFacility, DateTime.UtcNow),
				cancellationToken).ConfigureAwait(false);

			ServiceMessage reportMessage = DuplicateAliasMessage(duplicateAliasReport, aliasFiles.Length);
			messages.Add(reportMessage);
			AppLog.Write(reportMessage.Level, reportMessage.Source, reportMessage.Text);
		}

		// vNAS takes one alias file, so the alias files marked for vNAS go into vNAS_Alias.txt,
		// below the user's own custom alias files when vNAS Alias Upload is selected.
		string[] vnasAliasFiles = [.. MarkedForVnas(
			(settings.Airways, airwaysResult?.AliasFilePath),
			(settings.Airports, airportsResult?.AliasFilePath),
			(settings.Departures, departuresResult?.AliasFilePath),
			(settings.Arrivals, arrivalsResult?.AliasFilePath),
			(settings.Navaids, navaidsResult?.AliasFilePath),
			(settings.Procedures, proceduresResult?.AliasFilePath),
			(settings.Telephony, telephonyResult?.AliasFilePath))];

		VnasAliasResult? vnasAliasResult = null;

		if (settings.VnasAlias is not null || vnasAliasFiles.Length > 0)
		{
			cancellationToken.ThrowIfCancellationRequested();

			// Reported against the vNAS Alias Upload tab when it is selected; otherwise the file is
			// simply part of the run.
			string step = settings.VnasAlias is not null ? VnasAliasName : "AIRAC";
			progress?.Report(new AiracServiceProgress(step, $"Writing {AiracOutputPaths.VnasAliasFileName}"));

			IReadOnlyList<AliasSourceLoad> customFiles = settings.VnasAlias is not null ? supplementalData.CustomAliasFiles ?? [] : [];

			vnasAliasResult = await Task.Run(
				() => VnasAliasFileWriter.Write(customFiles, vnasAliasFiles, settings.SelectedCycle.AiracCycleId, outputDirectory),
				cancellationToken).ConfigureAwait(false);

			messages.AddRange(vnasAliasResult.Messages);

			// vNAS takes one alias file per facility, so uploading this one would drop the facility's own aliases.
			if (settings.VnasAlias is null && vnasAliasResult.FilePath is not null)
			{
				ServiceMessage onlyFeBuddy = new(LogLevel.Warning, LogSource,
					$"vNAS Alias Upload is not selected, so {AiracOutputPaths.VnasAliasFileName} holds only FE-Buddy's aliases. " +
					"vNAS takes one alias file per facility, so uploading it would remove your facility's own aliases from vNAS. " +
					"To keep them, select vNAS Alias Upload and add your facility's alias file.")
				{ IsAdvisory = true };

				messages.Add(onlyFeBuddy);
				AppLog.Write(onlyFeBuddy.Level, onlyFeBuddy.Source, onlyFeBuddy.Text);
			}

			string summary = vnasAliasResult.FilePath is null
				? $"{AiracOutputPaths.VnasAliasFileName} not written."
				: $"{AiracOutputPaths.VnasAliasFileName}: {vnasAliasResult.CustomCommandCount:N0} custom command(s), " +
					$"then {vnasAliasResult.FeBuddyCommandCount:N0} from {vnasAliasResult.FeBuddyFiles.Count} FE-Buddy alias file(s).";

			progress?.Report(new AiracServiceProgress(step, summary, settings.VnasAlias is not null ? 100 : null));
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
	/// The alias files a sub-service wrote that its settings block marks for vNAS
	/// (<c>UploadToVnas</c> names the alias file), in the order given.
	/// </summary>
	/// <param name="outputs">Each sub-service's settings block and the alias file it wrote, either of which may be <see langword="null"/>.</param>
	/// <returns>The full paths of the alias files marked for vNAS.</returns>
	private static IEnumerable<string> MarkedForVnas(params (IReadOnlyDictionary<string, string>? Block, string? AliasFilePath)[] outputs)
	{
		foreach ((IReadOnlyDictionary<string, string>? block, string? aliasFilePath) in outputs)
		{
			if (block is null || aliasFilePath is null)
			{
				continue;
			}

			// The block's keys match ignoring case, whatever dictionary the caller built.
			Dictionary<string, string> caseInsensitive = new(block, StringComparer.OrdinalIgnoreCase);

			if (SettingsValueReader.StringList(caseInsensitive, SubServiceSettingsReader.UploadToVnasKey)
				.Contains(Path.GetFileName(aliasFilePath), StringComparer.OrdinalIgnoreCase))
			{
				yield return aliasFilePath;
			}
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
			$"one of each. They are listed by ARTCC in {AiracOutputPaths.DuplicateAliasReportFileName} in the cycle folder.")
		{
			IsAdvisory = true
		};
	}
}
