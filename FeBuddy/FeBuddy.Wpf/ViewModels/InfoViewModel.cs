using System.Collections.ObjectModel;
using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.ViewModels.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>Static "about / resources" screen.</summary>
public sealed class InfoViewModel : ObservableObject
{
    public string AboutBody =>
        "FE-Buddy helps VATSIM / VATUSA facility engineers keep sector files current: " +
        "pull the latest FAA NASR cycle, convert legacy video maps, and produce " +
        "ERAM-ready GeoJSON with efficient LineString handling and namespaced " +
        "feb.* properties that won't collide with other tools.";

    public ObservableCollection<InfoLink> Links { get; } =
    [
        // Glyphs are Segoe Fluent code-points (see Theme/Icons.xaml).
        new("", "Manual", "How each tool works, field by field.", "Open manual"),
        new("", "Change log", "What shipped in every release.", "View releases"),
        new("", "Discord", "Ask questions and report problems.", "Join server"),
        new("", "Issues & requests", "Track bugs and feature ideas.", "Open tracker"),
    ];
}
