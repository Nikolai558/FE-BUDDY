using FeBuddy.Wpf.Infrastructure;

using FeBuddy.Core.Models.Services.Airac.Departures;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One selectable FE-Buddy custom property on the Departures tab.
/// </summary>
/// <remarks>
/// The Departures counterpart of <see cref="FebPropertyToggle"/>, which is tied to the Airports
/// property enum.
/// </remarks>
public sealed class DepartureFebPropertyToggle : ObservableObject
{
    private readonly Action _onChanged;
    private bool _isSelected;

    /// <summary>Creates a toggle.</summary>
    /// <param name="property">The property this row controls.</param>
    /// <param name="name">Its name as written to the settings block and the GeoJSON key.</param>
    /// <param name="description">A short plain-English description for the tooltip.</param>
    /// <param name="onChanged">Called when the user ticks or unticks the row.</param>
    public DepartureFebPropertyToggle(DepartureFebProperty property, string name, string description, Action onChanged)
    {
        Property = property;
        Name = name;
        Description = description;
        _onChanged = onChanged;
    }

    /// <summary>The property this row controls.</summary>
    public DepartureFebProperty Property { get; }

    /// <summary>The settings / JSON name, e.g. <c>dpName</c> - written as <c>feb.dpName</c>.</summary>
    public string Name { get; }

    /// <summary>What the property holds, for the tooltip.</summary>
    public string Description { get; }

    /// <summary>The key as it appears in the file, e.g. <c>feb.dpName</c>.</summary>
    public string JsonKey => $"feb.{Name}";

    /// <summary>Whether the user wants this property written.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                _onChanged();
            }
        }
    }
}

/// <summary>
/// The FE-Buddy custom properties a departure Feature can carry, with the names the settings
/// block and the GeoJSON keys use.
/// </summary>
/// <remarks>
/// The names here must match <c>DepartureSettingsParser</c>'s own list exactly - it rejects a
/// name it does not recognize - so this is the GUI-side half of one contract, kept in one place
/// rather than spelled out in XAML.
/// </remarks>
public static class DepartureFebPropertyNames
{
    /// <summary>Every property, in the order the tab lists them.</summary>
    public static IReadOnlyList<(DepartureFebProperty Property, string Name, string Description)> All { get; } = new[]
    {
        (DepartureFebProperty.DpName, "dpName", "Departure procedure name as NASR publishes it."),
        (DepartureFebProperty.ArptId, "arptId", "FAA identifier of the airport this file is for."),
        (DepartureFebProperty.Artcc, "artcc", "Responsible ARTCC identifier."),
        (DepartureFebProperty.AmendmentNo, "amendmentNo", "Amendment number of the procedure currently in effect."),
        (DepartureFebProperty.AmendEffDate, "amendEffDate", "Date the current amendment first became effective."),
        (DepartureFebProperty.Waypoints, "waypoints", "The procedure's point identifiers, in order."),
    };
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
