namespace FeBuddy.Wpf.Shell.Models;

/// <summary>
/// The main window's sizes in WPF's units (1/96 inch), which each monitor's scaling turns into
/// pixels: so the title bar that must show is as big on a 150% monitor as it looks on a 100% one.
/// </summary>
/// <param name="DefaultWidth">Its width when there is nothing to put back.</param>
/// <param name="DefaultHeight">Its height when there is nothing to put back.</param>
/// <param name="MinWidth">The narrowest it may be.</param>
/// <param name="MinHeight">The shortest it may be.</param>
/// <param name="CaptionHeight">The height of its title bar, all of which must show.</param>
/// <param name="VisibleCaptionWidth">How much of its title bar's width must show, to drag it by.</param>
public sealed record WindowSizes(
	double DefaultWidth,
	double DefaultHeight,
	double MinWidth,
	double MinHeight,
	double CaptionHeight,
	double VisibleCaptionWidth)
{
	/// <summary>
	/// FE-Buddy's main window, as ShellWindow.xaml sets it: 1200 x 780, at least 980 x 640, with a
	/// title bar 46 high (its WindowChrome's CaptionHeight), 100 of whose width must show.
	/// </summary>
	public static WindowSizes Shell { get; } = new(1200, 780, 980, 640, 46, 100);
}
