
using FeBuddy.Core.Application.Airac.Departures.Models;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// The FE-Buddy custom properties the Departures tab offers, in display order, with their tooltip text.
/// </summary>
/// <remarks>
/// Each property's name comes from Core (<see cref="Core.Application.Airac.FebProperties.Name{TProperty}"/>),
/// the same name <c>DepartureSettingsParser</c> accepts, so only the order and the wording live here.
/// </remarks>
public static class DepartureFebPropertyOptions
{
	/// <summary>Every property, in the order the tab lists them.</summary>
	public static IReadOnlyList<(DepartureFebProperty Property, string Description)> All { get; } =
	[
		(DepartureFebProperty.DpName, "Departure procedure name as NASR publishes it."),
		(DepartureFebProperty.PointId, "Identifier of the point. Symbols and Text files only."),
		(DepartureFebProperty.ArptId, "FAA identifier of the airport this file is for."),
		(DepartureFebProperty.Artcc, "Responsible ARTCC identifier."),
		(DepartureFebProperty.AmendmentNo, "Amendment number of the procedure currently in effect."),
		(DepartureFebProperty.AmendEffDate, "Date the current amendment first became effective."),
		(DepartureFebProperty.Waypoints, "Every point in the procedure, once each. Lines file only."),
	];
}
