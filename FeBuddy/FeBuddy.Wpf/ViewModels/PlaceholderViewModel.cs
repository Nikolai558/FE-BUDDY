using FeBuddy.Wpf.Infrastructure;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Stand-in for sections that only exist as navigation right now (Conversions,
/// Facility Files). Keeps the shell honest about what isn't built yet instead of
/// showing a blank page or a MessageBox.
/// </summary>
public sealed class PlaceholderViewModel : ObservableObject
{
    public PlaceholderViewModel(string title, string blurb)
    {
        Title = title;
        Blurb = blurb;
    }

    /// <summary>Parameterless overload so the XAML designer can instantiate it.</summary>
    public PlaceholderViewModel() : this("Section", "Not built yet.") { }

    public string Title { get; }

    public string Blurb { get; }
}
