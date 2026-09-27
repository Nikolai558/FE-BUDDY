using System.Globalization;

using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Procedures;
using FeBuddy.Core.Domain.Procedures.Models;
using FeBuddy.Core.Infrastructure.Dtpp;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using DtppAirport = FeBuddy.Core.Infrastructure.Dtpp.Models.DtppMetafileXmlDataModel.Airport;
using DtppRecord = FeBuddy.Core.Infrastructure.Dtpp.Models.DtppMetafileXmlDataModel.Record;

namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// Assembles every <see cref="ProcedureAirport"/> the Procedures sub-service works with: one per
/// airport the FAA d-TPP Metafile lists, joined to NASR <c>APT_BASE</c>/<c>CLS_ARSP</c>, with its
/// chart records folded into <see cref="Procedure"/>s and classified against the previous cycle's
/// metafile.
/// </summary>
/// <remarks>
/// Everything is built once, for the whole metafile, before <c>ProcedureSelection</c> applies the
/// user's facility/ROI/airport/procedure picks - the same "build everything, filter downstream"
/// shape every other AIRAC sub-service builder follows.
/// </remarks>
public static class ProcedureBuilder
{
	/// <summary>
	/// Builds every airport and procedure the metafile lists.
	/// </summary>
	/// <param name="nasr">All parsed NASR CSV data. <c>Apt</c> and <c>ClsArsp</c> must not be null.</param>
	/// <param name="dtpp">The selected cycle's parsed FAA d-TPP Metafile.</param>
	/// <param name="previousDtpp">
	/// The previous cycle's parsed FAA d-TPP Metafile, or <see langword="null"/> when it is not
	/// available - a deleted or re-added procedure's <see cref="Procedure.PreviousPdfName"/> is then
	/// always <see langword="null"/>.
	/// </param>
	/// <returns>Every airport, in metafile file order, plus any messages collected.</returns>
	/// <exception cref="InvalidOperationException">Thrown when <paramref name="nasr"/>.Apt or .ClsArsp has not been parsed.</exception>
	public static ProcedureBuildResult Build(NasrCsvDataCollection nasr, DtppMetafileDataCollection dtpp, DtppMetafileDataCollection? previousDtpp)
	{
		ArgumentNullException.ThrowIfNull(nasr);
		ArgumentNullException.ThrowIfNull(dtpp);

		if (nasr.Apt is null)
		{
			throw new InvalidOperationException(
				"Airport data (APT) has not been parsed. The Procedures sub-service cannot run without it.");
		}

		if (nasr.ClsArsp is null)
		{
			throw new InvalidOperationException(
				"Class airspace data (CLS_ARSP) has not been parsed. The Procedures sub-service cannot run without it.");
		}

		List<ServiceMessage> messages = [];

		// NASR keys APT_BASE on SITE_NO, not ARPT_ID - a duplicated identifier is possible in
		// principle. AirportBuilder already reports that for the Airports sub-service; Procedures
		// just keeps the first, the same tie-break.
		Dictionary<string, AptCsvDataModel.AptBase> aptByIdent = new(StringComparer.OrdinalIgnoreCase);

		foreach (AptCsvDataModel.AptBase row in nasr.Apt.AptBase)
		{
			string id = row.ArptId?.Trim() ?? string.Empty;

			if (id.Length > 0)
			{
				aptByIdent.TryAdd(id, row);
			}
		}

		ILookup<string, ClsArspCsvDataModel.ClsArsp> airspaceByAirport = nasr.ClsArsp.ClsArsp
			.ToLookup(row => row.ArptId?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase);

		ILookup<string, DtppRecord> recordsByAirport = dtpp.Records
			.ToLookup(record => record.AptIdent, StringComparer.OrdinalIgnoreCase);

		ILookup<string, DtppRecord>? previousRecordsByAirport = previousDtpp?.Records
			.ToLookup(record => record.AptIdent, StringComparer.OrdinalIgnoreCase);

		List<ProcedureAirport> airports = new(dtpp.Airports.Count);

		foreach (DtppAirport airportRow in dtpp.Airports)
		{
			aptByIdent.TryGetValue(airportRow.AptIdent, out AptCsvDataModel.AptBase? aptBase);

			ProcedureAirspaceClass airspaceClass = ComputeAirspaceClass(airspaceByAirport[airportRow.AptIdent]);

			IReadOnlyList<DtppRecord> ownRecords = [.. recordsByAirport[airportRow.AptIdent]];

			IReadOnlyList<DtppRecord>? previousOwnRecords = previousRecordsByAirport is null
				? null
				: [.. previousRecordsByAirport[airportRow.AptIdent]];

			airports.Add(new ProcedureAirport
			{
				AptIdent = airportRow.AptIdent,
				IcaoIdent = Normalize(airportRow.IcaoIdent) ?? Normalize(aptBase?.IcaoId),
				Name = airportRow.AirportName,
				City = airportRow.CityName,
				State = airportRow.StateCode,
				IsMilitary = string.Equals(airportRow.Military, "M", StringComparison.OrdinalIgnoreCase),
				Alnum = airportRow.Alnum,
				ResponsibleArtcc = Normalize(aptBase?.RespArtccId),
				AirspaceClass = airspaceClass,
				Latitude = aptBase?.BaseLatDecimal,
				Longitude = aptBase?.BaseLongDecimal,
				Procedures = BuildProcedures(ownRecords, previousOwnRecords),
			});
		}

		return new ProcedureBuildResult(airports, messages);
	}

	/// <summary>Groups one airport's records by normalized base name and classifies each group.</summary>
	private static List<Procedure> BuildProcedures(IReadOnlyList<DtppRecord> records, IReadOnlyList<DtppRecord>? previousRecords)
	{
		Dictionary<string, List<DtppRecord>> groupsByName = new(StringComparer.OrdinalIgnoreCase);
		List<string> order = [];

		foreach (DtppRecord record in records)
		{
			string baseName = ProcedureNaming.BaseName(record.ChartName);

			if (!groupsByName.TryGetValue(baseName, out List<DtppRecord>? group))
			{
				group = [];
				groupsByName[baseName] = group;
				order.Add(baseName);
			}

			group.Add(record);
		}

		return [.. order.Select(baseName => BuildProcedure(baseName, groupsByName[baseName], previousRecords))];
	}

	/// <summary>Classifies one base-name group's pages into a single <see cref="Procedure"/>.</summary>
	private static Procedure BuildProcedure(string baseName, List<DtppRecord> group, IReadOnlyList<DtppRecord>? previousRecords)
	{
		List<DtppRecord> mainPages = [.. group.Where(record => !ProcedureNaming.IsContinuation(record.ChartName))];

		bool hasMainDeleted = mainPages.Any(record => HasAction(record, "D"));
		bool hasMainAdded = mainPages.Any(record => HasAction(record, "A"));

		ProcedureChange change;
		DtppRecord reportedRecord;
		DtppRecord? currentRecord;
		DtppRecord? deletedRecord = null;

		if (hasMainDeleted && hasMainAdded)
		{
			// Deleted then re-added within this same metafile: the "A" main page is the chart going
			// forward, the "D" main page is what the previous-cycle lookup falls back to.
			change = ProcedureChange.ReAdded;
			currentRecord = mainPages.First(record => HasAction(record, "A"));
			reportedRecord = currentRecord;
			deletedRecord = mainPages.First(record => HasAction(record, "D"));
		}
		else
		{
			DtppRecord? mainPage = mainPages.Count > 0 ? mainPages[0] : null;

			if (mainPage is not null && !string.IsNullOrEmpty(mainPage.UserAction))
			{
				change = ActionToChange(mainPage.UserAction);
				reportedRecord = mainPage;
				currentRecord = change == ProcedureChange.Deleted ? null : mainPage;
				deletedRecord = change == ProcedureChange.Deleted ? mainPage : null;
			}
			else
			{
				DtppRecord? changedContinuation = group.FirstOrDefault(record =>
					ProcedureNaming.IsContinuation(record.ChartName) && !string.IsNullOrEmpty(record.UserAction));

				if (changedContinuation is not null)
				{
					change = ActionToChange(changedContinuation.UserAction!);
					reportedRecord = changedContinuation;
					currentRecord = mainPage; // the main page persists regardless of the continuation's own change.
					deletedRecord = change == ProcedureChange.Deleted ? changedContinuation : null;
				}
				else
				{
					change = ProcedureChange.None;
					reportedRecord = mainPage ?? group[0];
					currentRecord = mainPage ?? group[0];
				}
			}
		}

		DtppRecord infoRecord = currentRecord ?? reportedRecord;

		string? previousPdfName = change is ProcedureChange.Deleted or ProcedureChange.ReAdded
			? FindPreviousPdfName(previousRecords, baseName, infoRecord.ProcUid)
			: null;

		bool isRemovedFromAirportOnly = deletedRecord is not null
			&& string.Equals(deletedRecord.PdfName, DtppFiles.DeletedFromAirportPdfName, StringComparison.OrdinalIgnoreCase);

		return new Procedure
		{
			Name = baseName,
			ChartCode = group[0].ChartCode,
			ChartSeq = group[0].ChartSeq,
			Pages = [.. group.Select(record => new ProcedurePage(record.ChartName, record.PdfName, record.UserAction))],
			Change = change,
			ReportedName = reportedRecord.ChartName,
			ReportedPdfName = reportedRecord.PdfName,
			CurrentPdfName = currentRecord?.PdfName,
			PreviousPdfName = previousPdfName,
			IsRemovedFromAirportOnly = isRemovedFromAirportOnly,
			ProcUid = infoRecord.ProcUid,
			AmdtNum = Normalize(infoRecord.AmdtNum),
			AmdtDate = ParseAmdtDate(infoRecord.AmdtDate),
			ComputerCode = Normalize(infoRecord.Faanfd18),
			Civil = Normalize(infoRecord.Civil),
		};
	}

	/// <summary>
	/// Finds the chart's PDF name in the previous cycle's metafile: same airport (the caller already
	/// scoped <paramref name="previousRecords"/> to it) and same normalized base name, main page
	/// first; failing that, the same <see cref="DtppMetafileXmlDataModel.Record.ProcUid"/>.
	/// </summary>
	private static string? FindPreviousPdfName(IReadOnlyList<DtppRecord>? previousRecords, string baseName, int? procUid)
	{
		if (previousRecords is null)
		{
			return null;
		}

		DtppRecord? byName = previousRecords.FirstOrDefault(record =>
			!ProcedureNaming.IsContinuation(record.ChartName)
			&& ProcedureNaming.BaseName(record.ChartName).Equals(baseName, StringComparison.OrdinalIgnoreCase));

		if (byName is not null)
		{
			return byName.PdfName;
		}

		if (procUid is { } uid)
		{
			DtppRecord? byProcUid = previousRecords.FirstOrDefault(record => record.ProcUid == uid);

			if (byProcUid is not null)
			{
				return byProcUid.PdfName;
			}
		}

		return null;
	}

	private static ProcedureAirspaceClass ComputeAirspaceClass(IEnumerable<ClsArspCsvDataModel.ClsArsp> rows)
	{
		bool hasB = false;
		bool hasC = false;
		bool hasD = false;
		bool hasE = false;

		foreach (ClsArspCsvDataModel.ClsArsp row in rows)
		{
			hasB |= IsYes(row.ClassBAirspace);
			hasC |= IsYes(row.ClassCAirspace);
			hasD |= IsYes(row.ClassDAirspace);
			hasE |= IsYes(row.ClassEAirspace);
		}

		if (hasB) return ProcedureAirspaceClass.B;
		if (hasC) return ProcedureAirspaceClass.C;
		if (hasD) return ProcedureAirspaceClass.D;
		if (hasE) return ProcedureAirspaceClass.E;
		return ProcedureAirspaceClass.None;
	}

	private static bool HasAction(DtppRecord record, string action) =>
		string.Equals(record.UserAction, action, StringComparison.OrdinalIgnoreCase);

	private static ProcedureChange ActionToChange(string action) => action.ToUpperInvariant() switch
	{
		"A" => ProcedureChange.New,
		"C" => ProcedureChange.Changed,
		"D" => ProcedureChange.Deleted,
		_ => ProcedureChange.None,
	};

	private static bool IsYes(string? value) => value is not null && value.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase);

	private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

	private static DateOnly? ParseAmdtDate(string? value) =>
		DateOnly.TryParseExact(value?.Trim(), "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
			? date
			: null;
}
