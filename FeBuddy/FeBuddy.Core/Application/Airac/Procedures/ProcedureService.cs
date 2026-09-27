using System.Diagnostics;

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
/// <c>Procedure_Changes.md</c> and/or <c>Procedures.json</c>.
/// </summary>
/// <remarks>
/// Unlike every other AIRAC sub-service, its data does not come from the NASR cycle alone - it also
/// needs the selected cycle's FAA d-TPP Metafile (<c>dtpp</c> on <see cref="Run"/>), which the FAA
/// publishes only 15-18 days before the cycle's effective date. A missing metafile is not an error:
/// the run still completes, with an advisory warning and nothing written.
/// </remarks>
public static class ProcedureService
{
	private const string LogSource = "ProcedureService";

	/// <summary>
	/// Runs the full Procedures pipeline: parse settings, build every airport and procedure, select
	/// the included ones, and write the requested documents.
	/// </summary>
	/// <param name="nasr">All parsed NASR CSV data. <c>Apt</c> and <c>ClsArsp</c> must not be null.</param>
	/// <param name="dtpp">
	/// The selected cycle's parsed FAA d-TPP Metafile, or <see langword="null"/> when the FAA has not
	/// published it yet - the run then writes nothing and says why.
	/// </param>
	/// <param name="previousDtpp">
	/// The previous cycle's parsed FAA d-TPP Metafile, or <see langword="null"/> when it is not
	/// available - a deleted or re-added procedure is then listed without a link to its last chart.
	/// </param>
	/// <param name="procedureSettings">The raw Procedures settings dictionary.</param>
	/// <returns>What was built and written, plus timing and every message collected along the way.</returns>
	/// <exception cref="ArgumentException">Thrown when a required setting is missing or invalid.</exception>
	/// <exception cref="InvalidOperationException">Thrown when <paramref name="nasr"/>.Apt or .ClsArsp has not been parsed.</exception>
	public static ProcedureServiceResult Run(
		NasrCsvDataCollection nasr,
		DtppMetafileDataCollection? dtpp,
		DtppMetafileDataCollection? previousDtpp,
		IReadOnlyDictionary<string, string> procedureSettings)
	{
		ArgumentNullException.ThrowIfNull(nasr);
		ArgumentNullException.ThrowIfNull(procedureSettings);

		Stopwatch stopwatch = Stopwatch.StartNew();
		List<ServiceMessage> messages = [];

		ProcedureSettingsParseResult parseResult = ProcedureSettingsParser.Parse(procedureSettings);
		messages.AddRange(parseResult.Messages);

		if (dtpp is null)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				"The FAA has not published the d-TPP metafile for this cycle yet (it is posted 15-18 days before the " +
				"cycle's effective date), so no procedure documents were written.")
			{
				IsAdvisory = true
			});

			return Finish(stopwatch, messages, [], filesWritten: [], newCount: 0, changedCount: 0, deletedCount: 0, reAddedCount: 0, procedureCount: 0);
		}

		ProcedureBuildResult buildResult = ProcedureBuilder.Build(nasr, dtpp, previousDtpp);
		messages.AddRange(buildResult.Messages);

		IReadOnlyList<ProcedureAirport> included = ProcedureSelection.Select(buildResult.Airports, parseResult.Settings, messages);

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

		if (parseResult.Settings.GenerateChangesDocument)
		{
			ProcedureChangesWriteResult changesResult = ProcedureChangesMarkdownWriter.Generate(included, parseResult.Settings, dtpp, previousDtpp);
			messages.AddRange(changesResult.Messages);

			if (changesResult.FilePath is not null)
			{
				filesWritten.Add(changesResult.FilePath);
			}
		}

		if (parseResult.Settings.GenerateProceduresJson)
		{
			ProceduresJsonWriteResult jsonResult = ProceduresJsonWriter.Generate(included, parseResult.Settings, dtpp);
			messages.AddRange(jsonResult.Messages);

			if (jsonResult.FilePath is not null)
			{
				filesWritten.Add(jsonResult.FilePath);
			}
		}

		return Finish(stopwatch, messages, included, filesWritten, newCount, changedCount, deletedCount, reAddedCount, procedureCount);
	}

	/// <summary>Stops the clock, copies every message to the shared application log, and assembles the result.</summary>
	private static ProcedureServiceResult Finish(
		Stopwatch stopwatch,
		List<ServiceMessage> messages,
		IReadOnlyList<ProcedureAirport> included,
		IReadOnlyList<string> filesWritten,
		int newCount,
		int changedCount,
		int deletedCount,
		int reAddedCount,
		int procedureCount)
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
			AirportCount = included.Count,
			ProcedureCount = procedureCount,
			NewCount = newCount,
			ChangedCount = changedCount,
			DeletedCount = deletedCount,
			ReAddedCount = reAddedCount,
			FilesWritten = filesWritten,
		};
	}
}
