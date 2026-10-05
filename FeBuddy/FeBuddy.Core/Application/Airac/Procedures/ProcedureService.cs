using System.Diagnostics;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Application.Models;
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
	/// <returns>What was built and written, plus timing and every message collected along the way.</returns>
	/// <exception cref="ArgumentException">Thrown when a required setting is missing or invalid.</exception>
	/// <exception cref="InvalidOperationException">
	/// Thrown when a document is requested and <paramref name="nasr"/>.Apt or .ClsArsp has not been parsed.
	/// </exception>
	public static ProcedureServiceResult Run(
		NasrCsvDataCollection nasr,
		DtppMetafileDataCollection? dtpp,
		DtppMetafileDataCollection? previousDtpp,
		IReadOnlyDictionary<string, string> procedureSettings,
		OutputFileNames? fileNames = null)
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
			? WriteDocuments(nasr, dtpp, previousDtpp, settings, messages)
			: DocumentOutcome.None;

		AliasOutcome? alias = settings.GenerateAliasFile
			? WriteAliasFile(dtpp, settings, messages)
			: null;

		return Finish(stopwatch, messages, documents, alias);
	}

	/// <summary>Builds, selects and writes the requested documents, counting the changes they report.</summary>
	private static DocumentOutcome WriteDocuments(
		NasrCsvDataCollection nasr,
		DtppMetafileDataCollection dtpp,
		DtppMetafileDataCollection? previousDtpp,
		ProcedureSettings settings,
		List<ServiceMessage> messages)
	{
		ProcedureBuildResult buildResult = ProcedureBuilder.Build(nasr, dtpp, previousDtpp);
		messages.AddRange(buildResult.Messages);

		IReadOnlyList<ProcedureAirport> included = ProcedureSelection.Select(buildResult.Airports, settings, messages);

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
