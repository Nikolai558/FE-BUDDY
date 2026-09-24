using FeBuddy.Wpf.Infrastructure;

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

/// <summary>
/// One ARTCC in the Departures tab's ARTCC filter. The <b>selected</b> set is what gets
/// persisted to <c>ArtccFilter</c>; none selected means every ARTCC.
/// </summary>
/// <param name="artcc">The ARTCC identifier.</param>
/// <param name="isSelected">Whether it starts selected.</param>
/// <param name="onChanged">Called when the user ticks or unticks it.</param>
public sealed class ArtccToggle(string artcc, bool isSelected, Action onChanged) : ObservableObject
{
	private bool _isSelected = isSelected;

	/// <summary>The ARTCC identifier, e.g. <c>ZSE</c>.</summary>
	public string Artcc { get; } = artcc;

	/// <summary><see langword="true"/> to include this ARTCC's departures.</summary>
	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			if (SetProperty(ref _isSelected, value))
			{
				onChanged();
			}
		}
	}
}
