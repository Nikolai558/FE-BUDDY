using FeBuddy.Core.Application.Airac.Procedures.Models;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// The optional <c>Procedures.json</c> fields the Procedures tab offers, in display order, with a
/// short plain-English label for each.
/// </summary>
/// <remarks>
/// Each field's settings name comes from Core
/// (<see cref="Core.Application.Airac.FebProperties.Name{TProperty}"/>), the same name
/// <c>ProcedureSettingsParser</c> accepts for <c>JsonFields</c>, so only the order and the wording
/// live here.
/// </remarks>
public static class ProcedureJsonFieldOptions
{
	/// <summary>Every field, in the order the tab lists them.</summary>
	public static IReadOnlyList<(ProcedureJsonField Field, string Label)> All { get; } =
	[
		(ProcedureJsonField.IcaoId, "ICAO identifier"),
		(ProcedureJsonField.AirportName, "Airport name"),
		(ProcedureJsonField.City, "City"),
		(ProcedureJsonField.State, "State"),
		(ProcedureJsonField.ResponsibleArtcc, "Responsible ARTCC"),
		(ProcedureJsonField.AirspaceClass, "Airspace class"),
		(ProcedureJsonField.Military, "Military airport"),
		(ProcedureJsonField.ChartType, "Chart type"),
		(ProcedureJsonField.ChartUrl, "Chart URL"),
		(ProcedureJsonField.Change, "Change this cycle"),
		(ProcedureJsonField.CompareUrl, "Compare-PDF URL"),
		(ProcedureJsonField.Amendment, "Amendment number"),
		(ProcedureJsonField.AmendmentDate, "Amendment date"),
		(ProcedureJsonField.ProcedureUid, "Procedure ID"),
		(ProcedureJsonField.ComputerCode, "SID/STAR computer code"),
		(ProcedureJsonField.Producer, "Chart producer"),
	];
}
