
using FeBuddy.Core.Application.Airac.Arrivals.Models;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// The FE-Buddy custom properties the Arrivals tab offers, in display order, with their tooltip text.
/// </summary>
/// <remarks>
/// Each property's name comes from Core (<see cref="Core.Application.Airac.FebProperties.Name{TProperty}"/>),
/// the same name <c>ArrivalSettingsParser</c> accepts, so only the order and the wording live here.
/// </remarks>
public static class ArrivalFebPropertyOptions
{
	/// <summary>Every property, in the order the tab lists them.</summary>
	public static IReadOnlyList<(ArrivalFebProperty Property, string Description)> All { get; } =
	[
		(ArrivalFebProperty.ArrivalName, "Arrival procedure (STAR) name as NASR publishes it."),
		(ArrivalFebProperty.PointId, "Identifier of the point. Symbols and Text files only."),
		(ArrivalFebProperty.ArptId, "FAA identifier of the airport this file is for."),
		(ArrivalFebProperty.Artcc, "ARTCC responsible for the airport this file is for."),
		(ArrivalFebProperty.AmendmentNo, "Amendment number of the procedure currently in effect."),
		(ArrivalFebProperty.AmendEffDate, "Date the current amendment first became effective."),
		(ArrivalFebProperty.Waypoints, "Every point in the procedure, once each. Lines file only."),
	];
}
