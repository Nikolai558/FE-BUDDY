using FeBuddy.Wpf.Infrastructure;

using FeBuddy.Core.Application.Airac.Departures.Models;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// The FE-Buddy custom properties a departure Feature can carry, with the names the settings
/// block and the GeoJSON keys use.
/// </summary>
/// <remarks>
/// The names here must match <c>DepartureSettingsParser</c>'s own list exactly - it rejects a
/// name it does not recognize - so this is the GUI-side half of one contract, kept in one place
/// rather than spelled out in XAML. The enum value beside each name is the Core property
/// the parser maps it to.
/// </remarks>
public static class DepartureFebPropertyNames
{
	/// <summary>Every property, in the order the tab lists them.</summary>
	public static IReadOnlyList<(DepartureFebProperty Property, string Name, string Description)> All { get; } =
	[
		(DepartureFebProperty.DpName, "dpName", "Departure procedure name as NASR publishes it."),
		(DepartureFebProperty.PointId, "pointId", "Identifier of the point. Symbols and Text files only."),
		(DepartureFebProperty.ArptId, "arptId", "FAA identifier of the airport this file is for."),
		(DepartureFebProperty.Artcc, "artcc", "Responsible ARTCC identifier."),
		(DepartureFebProperty.AmendmentNo, "amendmentNo", "Amendment number of the procedure currently in effect."),
		(DepartureFebProperty.AmendEffDate, "amendEffDate", "Date the current amendment first became effective."),
		(DepartureFebProperty.Waypoints, "waypoints", "Every point in the procedure, once each. Lines file only."),
	];
}

/// <summary>
/// One ARTCC in the Departures tab's ARTCC filter. The <b>selected</b> set is what gets
/// persisted to <c>ArtccFilter</c>; none selected means every ARTCC.
/// </summary>
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
