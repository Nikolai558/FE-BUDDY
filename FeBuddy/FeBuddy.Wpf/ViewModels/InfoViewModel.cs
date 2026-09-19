using System.Collections.ObjectModel;
using System.Windows.Input;

using FeBuddy.Wpf.Infrastructure;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// SYSTEM ▸ Info: real resource links only (remediation plan Phase 10). The About menu is
/// gone (redundant with the Dashboard description box); Discord moved to the Dashboard.
/// </summary>
public sealed class InfoViewModel : ObservableObject
{
    /// <summary>One Info resource row.</summary>
    /// <param name="Title">The link title.</param>
    /// <param name="Blurb">A one-line description.</param>
    /// <param name="Url">The link target.</param>
    public sealed record Resource(string Title, string Blurb, string Url);

    public InfoViewModel()
    {
        OpenCommand = new RelayCommand<string>(BrowserLauncher.Open);
    }

    /// <summary>Parameter is the URL to open.</summary>
    public ICommand OpenCommand { get; }

    public ObservableCollection<Resource> Resources { get; } =
    [
        new("Manual", "How each tool works, field by field.", Links.Manual),
        new("Change log", "What shipped in every release.", Links.ChangeLog),
        new("Issues & requests", "Track bugs and feature ideas.", Links.Issues),
    ];
}
