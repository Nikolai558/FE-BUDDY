namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>One row in the shell's systems-health popover (Internet / AIRAC data / Updates).</summary>
public sealed record HealthRow(string Name, string Detail, StatusKind Kind);


/// <summary>
/// A numbered display item - a BCG group or a filter. Used both by the map's
/// display visualiser (toggle <see cref="Visible"/>) and by Settings' scheme
/// editor (edit <see cref="Name"/>).
/// </summary>
public sealed class DisplayItem(int number, string name, bool visible = true) : Infrastructure.ObservableObject
{
    private string _name = name;
    private bool _visible = visible;

    public int Number { get; } = number;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public bool Visible
    {
        get => _visible;
        set => SetProperty(ref _visible, value);
    }
}

