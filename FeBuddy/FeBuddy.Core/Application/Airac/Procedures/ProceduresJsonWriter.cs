using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Domain.Procedures;
using FeBuddy.Core.Domain.Procedures.Models;
using FeBuddy.Core.Infrastructure.Dtpp;
using FeBuddy.Core.Infrastructure.Dtpp.Models;

namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// Writes <c>Procedures.json</c>: every included airport's current procedures (deleted procedures
/// are left out - they belong in <c>Procedure_Changes.md</c> instead), for another tool to consume.
/// </summary>
/// <remarks>
/// Unlike the Markdown document, a shared STAR is written under every included airport that serves
/// it - this is per-airport data, not a change report, so there is no de-duplication.
/// </remarks>
public static class ProceduresJsonWriter
{
	private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

	/// <summary>
	/// Writes <c>Procedures.json</c> for the included airports and procedures.
	/// </summary>
	/// <param name="airports">The included airports (post-selection), each with only its included procedures.</param>
	/// <param name="settings">The parsed settings; <see cref="ProcedureSettings.PrimaryFacility"/> and <see cref="ProcedureSettings.JsonFields"/> are read.</param>
	/// <param name="dtpp">The selected cycle's parsed FAA d-TPP Metafile (its <c>Cycle</c> and effective date).</param>
	/// <returns>The path written, plus any messages collected.</returns>
	public static ProceduresJsonWriteResult Generate(
		IReadOnlyList<ProcedureAirport> airports,
		ProcedureSettings settings,
		DtppMetafileDataCollection dtpp)
	{
		ArgumentNullException.ThrowIfNull(airports);
		ArgumentNullException.ThrowIfNull(settings);
		ArgumentNullException.ThrowIfNull(dtpp);

		IReadOnlyList<(string Facility, IReadOnlyList<ProcedureAirport> Airports)> facilitySections =
			ProcedureOrdering.GroupByFacility(airports, settings.PrimaryFacility);

		JsonObject root = new() { ["cycle"] = dtpp.Cycle };

		if (dtpp.FromEffectiveUtc is { } effective)
		{
			root["effectiveDate"] = DateOnly.FromDateTime(effective).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}

		JsonArray airportsArray = [];

		foreach ((_, IReadOnlyList<ProcedureAirport> facilityAirports) in facilitySections)
		{
			foreach (ProcedureAirport airport in facilityAirports)
			{
				JsonObject? airportObject = BuildAirport(airport, dtpp.Cycle, settings.JsonFields);

				if (airportObject is not null)
				{
					airportsArray.Add(airportObject);
				}
			}
		}

		root["airports"] = airportsArray;

		string directory = AiracOutputPaths.PublicationDocsDirectory(settings.OutputDirectory);
		Directory.CreateDirectory(directory);

		string path = Path.Combine(directory, settings.FileNames.FileName(ProcedureOutputFiles.Json));
		File.WriteAllText(path, root.ToJsonString(SerializerOptions), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

		return new ProceduresJsonWriteResult(path, []);
	}

	/// <summary>
	/// Builds one airport's JSON object, or <see langword="null"/> when every one of its procedures
	/// was deleted (leaving nothing to write for it).
	/// </summary>
	private static JsonObject? BuildAirport(ProcedureAirport airport, string cycle, IReadOnlyCollection<ProcedureJsonField> fields)
	{
		JsonArray proceduresArray = [];

		foreach (Procedure procedure in airport.Procedures)
		{
			if (procedure.Change == ProcedureChange.Deleted)
			{
				continue;
			}

			proceduresArray.Add(BuildProcedure(procedure, cycle, fields));
		}

		if (proceduresArray.Count == 0)
		{
			return null;
		}

		JsonObject airportObject = new() { ["airportId"] = airport.AptIdent };

		AddIfSelected(airportObject, fields, ProcedureJsonField.IcaoId, "icaoId", airport.IcaoIdent);
		AddIfSelected(airportObject, fields, ProcedureJsonField.AirportName, "airportName", airport.Name);
		AddIfSelected(airportObject, fields, ProcedureJsonField.City, "city", airport.City);
		AddIfSelected(airportObject, fields, ProcedureJsonField.State, "state", airport.State);
		AddIfSelected(airportObject, fields, ProcedureJsonField.ResponsibleArtcc, "responsibleArtcc", airport.ResponsibleArtcc);

		if (fields.Contains(ProcedureJsonField.AirspaceClass) && airport.AirspaceClass != ProcedureAirspaceClass.None)
		{
			airportObject["airspaceClass"] = airport.AirspaceClass.ToString();
		}

		if (fields.Contains(ProcedureJsonField.Military))
		{
			airportObject["military"] = airport.IsMilitary;
		}

		airportObject["procedures"] = proceduresArray;

		return airportObject;
	}

	/// <summary>Builds one procedure's JSON object: always <c>name</c>, plus whichever of <paramref name="fields"/> apply and have a value.</summary>
	private static JsonObject BuildProcedure(Procedure procedure, string cycle, IReadOnlyCollection<ProcedureJsonField> fields)
	{
		JsonObject procedureObject = new() { ["name"] = procedure.Name };

		if (fields.Contains(ProcedureJsonField.ChartType))
		{
			procedureObject["chartType"] = procedure.ChartCode;
		}

		if (fields.Contains(ProcedureJsonField.ChartUrl) && procedure.CurrentPdfName is { Length: > 0 } mainPdf)
		{
			procedureObject["chartUrl"] = DtppFiles.ChartUrl(cycle, mainPdf);

			JsonNode[] continuationUrls = [.. procedure.Pages
				.Where(page => ProcedureNaming.IsContinuation(page.ChartName) && !DtppFiles.IsPlaceholderPdf(page.PdfName))
				.Select(page => (JsonNode)DtppFiles.ChartUrl(cycle, page.PdfName))];

			if (continuationUrls.Length > 0)
			{
				procedureObject["continuationUrls"] = new JsonArray(continuationUrls);
			}
		}

		if (fields.Contains(ProcedureJsonField.Change) && procedure.Change != ProcedureChange.None)
		{
			procedureObject["change"] = procedure.Change == ProcedureChange.ReAdded ? "Changed" : procedure.Change.ToString();

			if (procedure.Change == ProcedureChange.ReAdded)
			{
				procedureObject["changeNote"] = "Deleted and re-added in this cycle's d-TPP metafile";
			}
		}

		if (fields.Contains(ProcedureJsonField.CompareUrl) && procedure.Change == ProcedureChange.Changed)
		{
			procedureObject["compareUrl"] = DtppFiles.CompareUrl(cycle, procedure.ReportedPdfName);
		}

		AddIfSelected(procedureObject, fields, ProcedureJsonField.Amendment, "amendment", procedure.AmdtNum);

		if (fields.Contains(ProcedureJsonField.AmendmentDate) && procedure.AmdtDate is { } amendmentDate)
		{
			procedureObject["amendmentDate"] = amendmentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}

		if (fields.Contains(ProcedureJsonField.ProcedureUid) && procedure.ProcUid is { } procedureUid)
		{
			procedureObject["procedureUid"] = procedureUid;
		}

		AddIfSelected(procedureObject, fields, ProcedureJsonField.ComputerCode, "computerCode", procedure.ComputerCode);

		if (fields.Contains(ProcedureJsonField.Producer) && ProducerName(procedure.Civil) is { } producer)
		{
			procedureObject["producer"] = producer;
		}

		return procedureObject;
	}

	private static void AddIfSelected(
		JsonObject target,
		IReadOnlyCollection<ProcedureJsonField> fields,
		ProcedureJsonField field,
		string key,
		string? value)
	{
		if (fields.Contains(field) && value is { Length: > 0 })
		{
			target[key] = value;
		}
	}

	/// <summary>Translates the metafile's civil/military production code to a display producer name.</summary>
	private static string? ProducerName(string? civil) => civil?.Trim().ToUpperInvariant() switch
	{
		"C" => "FAA",
		"D" => "FAA (joint use)",
		"N" => "NGA",
		"H" => "NGA (high altitude)",
		_ => null,
	};
}
