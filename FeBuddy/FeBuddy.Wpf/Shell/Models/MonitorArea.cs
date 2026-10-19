namespace FeBuddy.Wpf.Shell.Models;

/// <summary>One connected monitor, as Windows reports it, in physical pixels.</summary>
/// <param name="Bounds">The whole monitor.</param>
/// <param name="WorkArea">The part of it outside the taskbar and any other docked bar.</param>
/// <param name="Dpi">Its scaling as a DPI: 96 at 100%, 144 at 150%.</param>
public sealed record MonitorArea(PixelRect Bounds, PixelRect WorkArea, int Dpi);
