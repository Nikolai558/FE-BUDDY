using FeBuddy.Wpf.Infrastructure;

using FeBuddy.Core.Models.Services.Airac.Airports;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One selectable FE-Buddy custom property on the Airports tab.
/// </summary>
public sealed class FebPropertyToggle : ObservableObject
{
    private readonly Action _onChanged;
    private bool _isSelected;

    /// <summary>Creates a toggle.</summary>
    /// <param name="property">The property this row controls.</param>
    /// <param name="name">Its name as written to the settings block and the GeoJSON key.</param>
    /// <param name="description">A short plain-English description for the tooltip.</param>
    /// <param name="onChanged">Called when the user ticks or unticks the row.</param>
    public FebPropertyToggle(AirportFebProperty property, string name, string description, Action onChanged)
    {
        Property = property;
        Name = name;
        Description = description;
        _onChanged = onChanged;
    }

    /// <summary>The property this row controls.</summary>
    public AirportFebProperty Property { get; }

    /// <summary>The settings / JSON name, e.g. <c>faaId</c> - written as <c>feb.faaId</c>.</summary>
    public string Name { get; }

    /// <summary>What the property holds, for the tooltip.</summary>
    public string Description { get; }

    /// <summary>The key as it appears in the file, e.g. <c>feb.faaId</c>.</summary>
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
/// The FE-Buddy custom properties an airport Feature can carry, with the names the settings
/// block and the GeoJSON keys use.
/// </summary>
/// <remarks>
/// The names here must match <c>AirportSettingsParser</c>'s own list exactly - it rejects a name
/// it does not recognize - so this is the GUI-side half of one contract, kept in one place
/// rather than spelled out in XAML.
/// </remarks>
public static class AirportFebPropertyNames
{
    /// <summary>Every property, in the order the tab lists them.</summary>
    public static IReadOnlyList<(AirportFebProperty Property, string Name, string Description)> All { get; } = new[]
    {
        (AirportFebProperty.FaaId, "faaId", "FAA identifier, e.g. SEA. Not written to the Text file, which already labels it."),
        (AirportFebProperty.IcaoId, "icaoId", "ICAO identifier, e.g. KSEA. Blank for airports that have none."),
        (AirportFebProperty.Name, "name", "Airport name. Not written to the Text file, which already labels it."),
        (AirportFebProperty.Elev, "elev", "Field elevation in feet. Blank when NASR publishes none."),
        (AirportFebProperty.RespArtcc, "respArtcc", "Responsible ARTCC identifier."),
        (AirportFebProperty.TfcPtrnAlt, "tfcPtrnAlt", "Traffic pattern altitude in feet."),
        (AirportFebProperty.FssId, "fssId", "Tie-in Flight Service Station identifier."),
        (AirportFebProperty.TwrType, "twrType", "Tower type, e.g. TWR or No-TWR."),
    };
}
