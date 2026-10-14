using System.Diagnostics;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Procedures.Models;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// Public entry point for the Procedures sub-service: parses settings, builds every airport and
/// procedure from the FAA d-TPP Metafile and NASR data, applies the user's selection, and writes
/// <c>Procedure_Changes.md</c> and/or <c>Procedures.json</c> - and, independently of that selection,
/// the FAA Chart Recall alias file <c>Faa_Chart_Recall.txt</c> for every airport in the metafile.
/// </summary>
/// <remarks>
/// Its data does not come from the NASR cycle alone - it also needs the selected cycle's FAA d-TPP
/// Metafile (<c>dtpp</c> on <see cref="Run"/>), which the FAA publishes only 15-18 days before the
/// cycle's effective date. A missing metafile is not an error: the run still completes, with an
/// advisory warning and nothing written.
/// </remarks>
public static class ProcedureService
{
	private const string LogSource = "ProcedureService";

	/// <summary>
	/// Runs the full Procedures pipeline: parse settings, build every airport and procedure, select
	/// the included ones, write the requested documents, and write the alias file when requested.
	/// </summary>
	/// <param name="nasr">
	/// All parsed NASR CSV data. <c>Apt</c> and <c>ClsArsp</c> must not be null when a document is
	/// requested; the alias file alone reads no NASR data.
	/// </param>
	/// <param name="dtpp">
	/// The selected cycle's parsed FAA d-TPP Metafile, or <see langword="null"/> when the FAA has not
	/// published it yet - the run then writes nothing and says why.
	/// </param>
	/// <param name="previousDtpp">
	/// The previous cycle's parsed FAA d-TPP Metafile, or <see langword="null"/> when it is not
	/// available - a deleted or re-added procedure is then listed without a link to its last chart.
	/// </param>
	/// <param name="procedureSettings">The raw Procedures settings dictionary.</param>
	/// <param name="fileNames">The names the user gave files in place of FE-Buddy's, or <see langword="null"/> for none.</param>
	/// <param name="previousNasr">
	/// The previous cycle's parsed NASR data, for the routes a changed or deleted departure or arrival
	/// had (see <see cref="ProceduresWithFixInFacility"/>), or <see langword="null"/> when it isn't loaded -
	/// this cycle's routes are then the only ones checked.
	/// </param>
	/// <returns>What was built and written, plus timing and every message collected along the way.</returns>
	/// <exception cref="ArgumentException">Thrown when a required setting is missing or invalid.</exception>
	/// <exception cref="InvalidOperationException">
	/// Thrown when a document is requested and <paramref name="nasr"/>.Apt or .ClsArsp has not been parsed -
	/// or, with the procedures-with-a-fix-in-my-facility rule on, its DP, STAR, FIX or NAV.
	/// </exception>
	public static ProcedureServiceResult Run(
		NasrCsvDataCollection nasr,
		DtppMetafileDataCollection? dtpp,
		DtppMetafileDataCollection? previousDtpp,
		IReadOnlyDictionary<string, string> procedureSettings,
		OutputFileNames? fileNames = null,
		NasrCsvDataCollection? previousNasr = null)
	{
		ArgumentNullException.ThrowIfNull(nasr);
		ArgumentNullException.ThrowIfNull(procedureSettings);

		Stopwatch stopwatch = Stopwatch.StartNew();
		List<ServiceMessage> messages = [];

		ProcedureSettingsParseResult parseResult = ProcedureSettingsParser.Parse(procedureSettings);
		messages.AddRange(parseResult.Messages);
		ProcedureSettings settings = parseResult.Settings with { FileNames = fileNames ?? OutputFileNames.None };

		if (dtpp is null)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				"The FAA has not published the d-TPP metafile for this cycle yet (it is posted 15-18 days before the " +
				"cycle's effective date), so no procedure documents or FAA Chart Recall alias file were written.")
			{
				IsAdvisory = true
			});

			return Finish(stopwatch, messages, DocumentOutcome.None, alias: null);
		}

		DocumentOutcome documents = settings.GenerateChangesDocument || settings.GenerateProceduresJson
			? WriteDocuments(nasr, previousNasr, dtpp, previousDtpp, settings, messages)
			: DocumentOutcome.None;

		AliasOutcome? alias = settings.GenerateAliasFile
			? WriteAliasFile(dtpp, settings, messages)
			: null;

		return Finish(stopwatch, messages, documents, alias);
	}

	/// <summary>
	/// The charts whose route has a fix in the user's facility (Feature #367), from this cycle's NASR
	/// routes and, when they're loaded, the previous cycle's; or <see langword="null"/>, with a warning,
	/// when no facility is set.
	/// </summary>
	private static IReadOnlyDictionary<Procedure, FacilityFixMatch>? FindFacilityFixes(
		NasrCsvDataCollection nasr,
		NasrCsvDataCollection? previousNasr,
		IReadOnlyList<ProcedureAirport> airports,
		ProcedureSettings settings,
		List<ServiceMessage> messages)
	{
		if (settings.PrimaryFacility is not { } facility)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				"'IncludeProceduresWithFixInFacility' is on, but no facility is set in Settings ▸ Facility, so departures and " +
				"arrivals at other airports with a fix in it weren't added."));
			return null;
		}

		ProcedureRouteSet? previous = previousNasr is { Dp: not null, Star: not null, Fix: not null, Nav: not null }
			? ProcedureRouteSet.FromNasr(previousNasr)
			: null;

		if (previous is null)
		{
			messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
				"The previous cycle's NASR data isn't loaded, so a changed or deleted departure or arrival at another airport was " +
				$"checked against its route this cycle only, for a fix in {facility}."));
		}

		return ProceduresWithFixInFacility.Find(
			airports, facility, ProcedureRouteSet.FromNasr(nasr), previous, useAirports: settings.Area == SubServiceArea.None);
	}

	/// <summary>Builds, selects and writes the requested documents, counting the changes they report.</summary>
	private static DocumentOutcome WriteDocuments(
		NasrCsvDataCollection nasr,
		NasrCsvDataCollection? previousNasr,
		DtppMetafileDataCollection dtpp,
		DtppMetafileDataCollection? previousDtpp,
		ProcedureSettings settings,
		List<ServiceMessage> messages)
	{
		ProcedureBuildResult buildResult = ProcedureBuilder.Build(nasr, dtpp, previousDtpp);
		messages.AddRange(buildResult.Messages);

		IReadOnlyDictionary<Procedure, FacilityFixMatch>? facilityFixes = settings.IncludeProceduresWithFixInFacility
			? FindFacilityFixes(nasr, previousNasr, buildResult.Airports, settings, messages)
			: null;

		IReadOnlyList<ProcedureAirport> included = ProcedureSelection.Select(buildResult.Airports, settings, messages, facilityFixes);

		int addedForFixes = included.Sum(airport => airport.Procedures.Count(procedure => procedure.IncludedForFacilityFixes is not null));

		if (facilityFixes is not null)
		{
			messages.Add(new ServiceMessage(LogLevel.Info, LogSource, addedForFixes == 0
				? $"No departure or arrival at another airport has a fix in {settings.PrimaryFacility} that isn't already included."
				: $"Added {addedForFixes} departure(s) and arrival(s) at {included.Count(airport => airport.Procedures.Any(procedure => procedure.IncludedForFacilityFixes is not null))} " +
					$"other airport(s), whose route has a fix in {settings.PrimaryFacility}."));
		}

		int newCount = 0;
		int changedCount = 0;
		int deletedCount = 0;
		int reAddedCount = 0;
		int procedureCount = 0;
		bool hasDeletedOrReAdded = false;

		foreach (ProcedureAirport airport in included)
		{
			foreach (Procedure procedure in airport.Procedures)
			{
				procedureCount++;

				switch (procedure.Change)
				{
					case ProcedureChange.New:
						newCount++;
						break;
					case ProcedureChange.Changed:
						changedCount++;
						break;
					case ProcedureChange.Deleted:
						deletedCount++;
						hasDeletedOrReAdded = true;
						break;
					case ProcedureChange.ReAdded:
						reAddedCount++;
						changedCount++;
						hasDeletedOrReAdded = true;
						break;
				}
			}
		}

		if (hasDeletedOrReAdded && previousDtpp is null)
		{
			messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
				"The previous cycle's d-TPP Metafile is not available, so deleted procedures are listed without a link " +
				"to their last chart."));
		}

		List<string> filesWritten = [];

		if (settings.GenerateChangesDocument)
		{
			ProcedureChangesWriteResult changesResult = ProcedureChangesMarkdownWriter.Generate(included, settings, dtpp, previousDtpp);
			messages.AddRange(changesResult.Messages);

			if (changesResult.FilePath is not null)
			{
				filesWritten.Add(changesResult.FilePath);
			}
		}

		if (settings.GenerateProceduresJson)
		{
			ProceduresJsonWriteResult jsonResult = ProceduresJsonWriter.Generate(included, settings, dtpp);
			messages.AddRange(jsonResult.Messages);

			if (jsonResult.FilePath is not null)
			{
				filesWritten.Add(jsonResult.FilePath);
			}
		}

		return new DocumentOutcome(included.Count, procedureCount, newCount, changedCount, deletedCount, reAddedCount, filesWritten);
	}

	/// <summary>Builds and writes <c>Faa_Chart_Recall.txt</c> for every airport in the metafile.</summary>
	private static AliasOutcome WriteAliasFile(DtppMetafileDataCollection dtpp, ProcedureSettings settings, List<ServiceMessage> messages)
	{
		ChartRecallBuildResult buildResult = ChartRecallAliasBuilder.Build(dtpp, settings.FileNames.FileName(ProcedureOutputFiles.Alias));
		messages.AddRange(buildResult.Messages);

		ChartRecallAliasWriteResult writeResult = ChartRecallAliasWriter.Generate(buildResult.Lines, settings);

		return new AliasOutcome(writeResult.FilePath, writeResult.CommandCount, writeResult.FilePath is null ? 0 : buildResult.Summary.AirportCount);
	}

	/// <summary>Stops the clock, copies every message to the shared application log, and assembles the result.</summary>
	private static ProcedureServiceResult Finish(
		Stopwatch stopwatch,
		List<ServiceMessage> messages,
		DocumentOutcome documents,
		AliasOutcome? alias)
	{
		stopwatch.Stop();

		foreach (ServiceMessage message in messages)
		{
			AppLog.Write(message.Level, message.Source, message.Text);
		}

		return new ProcedureServiceResult
		{
			Messages = messages,
			Elapsed = stopwatch.Elapsed,
			AirportCount = documents.AirportCount,
			ProcedureCount = documents.ProcedureCount,
			NewCount = documents.NewCount,
			ChangedCount = documents.ChangedCount,
			DeletedCount = documents.DeletedCount,
			ReAddedCount = documents.ReAddedCount,
			FilesWritten = documents.FilesWritten,
			AliasFilePath = alias?.FilePath,
			AliasCommandCount = alias?.CommandCount ?? 0,
			AliasAirportCount = alias?.AirportCount ?? 0,
		};
	}

	/// <summary>What the documents covered and which were written.</summary>
	private sealed record DocumentOutcome(
		int AirportCount,
		int ProcedureCount,
		int NewCount,
		int ChangedCount,
		int DeletedCount,
		int ReAddedCount,
		IReadOnlyList<string> FilesWritten)
	{
		/// <summary>No document was requested, or there was no metafile to write one from.</summary>
		public static DocumentOutcome None { get; } = new(0, 0, 0, 0, 0, 0, []);
	}

	/// <summary>Where the alias file was written, and what it holds.</summary>
	private sealed record AliasOutcome(string? FilePath, int CommandCount, int AirportCount);
}
