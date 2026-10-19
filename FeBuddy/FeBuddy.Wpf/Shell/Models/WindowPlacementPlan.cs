namespace FeBuddy.Wpf.Shell.Models;

/// <summary>Where to put the main window.</summary>
/// <param name="Normal">Its normal (restored) bounds, in physical screen pixels.</param>
/// <param name="Maximized">Whether to maximize it, on the monitor <paramref name="Normal"/> is on.</param>
public sealed record WindowPlacementPlan(PixelRect Normal, bool Maximized);
