using System.Windows;

namespace FeBuddy.Wpf.Shell;

/// <summary>
/// Decides where FE-Buddy's main window opens (Feature #364): where it was when it closed, unless
/// that is somewhere it can't be seen or reached on the screens connected now - a monitor unplugged,
/// a resolution lowered, another PC - when it opens at its default instead. Only numbers go in and
/// out, so every rule is tested without a window; <see cref="MainWindowPlacement"/> does the Windows
/// part.
/// </summary>
/// <remarks>
/// <para>
/// Every position and size is in physical screen pixels, the unit Windows' placement and monitor
/// functions use, on every monitor whatever its scaling. The window's own sizes are in WPF's units
/// (<see cref="WindowSizes"/>), and each is turned into pixels with the scaling of the monitor it is
/// measured on.
/// </para>
/// <para>
/// The checks: at least 100 of the title bar's width, and all its height, inside a monitor's work
/// area (its title bar is FE-Buddy's own, so a window whose title bar is off screen can't be dragged
/// back); and the window no bigger than that work area - shrunk to fit, never below its minimum size
/// unless the work area is smaller, and moved fully onto it.
/// </para>
/// </remarks>
public static class WindowPlacementRules
{
	/// <summary>WPF's unit is 1/96 inch, so a DPI of 96 is 100% scaling.</summary>
	public const int StandardDpi = 96;

	// GetWindowPlacement's showCmd for a minimized window, and its flag for one that restores maximized.
	private const int SwShowMinimized = 2;
	private const int SwShowMaximized = 3;
	private const int SwMinimize = 6;
	private const int SwShowMinNoActive = 7;
	private const int WpfRestoreToMaximized = 0x0002;

	/// <summary>Where to put the window as FE-Buddy opens, from where it was when it closed.</summary>
	/// <param name="saved">Where it was, or <see langword="null"/> for none (or none that could be read).</param>
	/// <param name="monitors">The monitors connected now.</param>
	/// <param name="main">The main monitor.</param>
	/// <param name="sizes">The window's sizes.</param>
	/// <returns>Its normal bounds and whether to maximize it.</returns>
	public static WindowPlacementPlan Restore(SavedWindowPlacement? saved, IReadOnlyList<MonitorArea> monitors, MonitorArea main, WindowSizes sizes)
	{
		ArgumentNullException.ThrowIfNull(monitors);
		ArgumentNullException.ThrowIfNull(main);
		ArgumentNullException.ThrowIfNull(sizes);

		if (saved is null)
		{
			return Default(main, sizes);
		}

		// The monitor it was on. Gone, where it was means nothing on these screens.
		MonitorArea? home = MonitorOf(saved.Normal, monitors);

		if (home is null)
		{
			return Default(main, sizes, saved.Maximized);
		}

		// That monitor's scaling changed since: the same size in WPF's units, as Windows keeps a window
		// dragged to a monitor with other scaling.
		PixelRect normal = Rescale(saved.Normal, saved.Dpi, home.Dpi);

		if (saved.Maximized)
		{
			// Maximized on that monitor, with somewhere on it to restore to.
			return new WindowPlacementPlan(TitleBarShows(normal, home, sizes) ? FitOn(normal, home, sizes) : DefaultBounds(home, sizes), Maximized: true);
		}

		MonitorArea? holder = monitors
			.Where(monitor => TitleBarShows(normal, monitor, sizes))
			.MaxBy(monitor => normal.OverlapArea(monitor.WorkArea));

		return holder is null
			? Default(main, sizes)
			: new WindowPlacementPlan(FitOn(normal, holder, sizes), Maximized: false);
	}

	/// <summary>
	/// The default: the window's default size, shrunk to fit if it must, in the middle of the main
	/// monitor's work area.
	/// </summary>
	/// <param name="main">The main monitor.</param>
	/// <param name="sizes">The window's sizes.</param>
	/// <param name="maximized">Whether to maximize it there.</param>
	/// <returns>The default placement.</returns>
	public static WindowPlacementPlan Default(MonitorArea main, WindowSizes sizes, bool maximized = false)
	{
		ArgumentNullException.ThrowIfNull(main);
		ArgumentNullException.ThrowIfNull(sizes);

		return new WindowPlacementPlan(DefaultBounds(main, sizes), maximized);
	}

	/// <summary>
	/// What to keep of the window as it closes, from what <c>GetWindowPlacement</c> reports. Never
	/// minimized: a window closed minimized is kept as it was before - maximized when Windows would
	/// restore it maximized.
	/// </summary>
	/// <param name="showCmd">The placement's <c>showCmd</c>.</param>
	/// <param name="flags">The placement's <c>flags</c>.</param>
	/// <param name="normal">Its normal bounds, in screen pixels (<see cref="FromWorkspace"/>).</param>
	/// <param name="dpi">The scaling of the monitor <paramref name="normal"/> is on.</param>
	/// <returns>What to save.</returns>
	public static SavedWindowPlacement Capture(int showCmd, int flags, PixelRect normal, int dpi)
	{
		bool minimized = showCmd is SwShowMinimized or SwMinimize or SwShowMinNoActive;
		bool maximized = showCmd == SwShowMaximized || (minimized && (flags & WpfRestoreToMaximized) != 0);

		return new SavedWindowPlacement(normal, dpi, maximized);
	}

	/// <summary>The monitor a rectangle is most on, or <see langword="null"/> when it is on none of them.</summary>
	/// <param name="bounds">The rectangle.</param>
	/// <param name="monitors">The monitors.</param>
	/// <returns>The monitor, or <see langword="null"/>.</returns>
	public static MonitorArea? MonitorOf(PixelRect bounds, IReadOnlyList<MonitorArea> monitors)
	{
		ArgumentNullException.ThrowIfNull(monitors);

		return monitors
			.Where(monitor => bounds.OverlapArea(monitor.Bounds) > 0)
			.MaxBy(monitor => bounds.OverlapArea(monitor.Bounds));
	}

	/// <summary>
	/// Screen coordinates to the workspace coordinates <c>SetWindowPlacement</c> takes for a normal
	/// window: shifted by how far its monitor's work area starts in from the monitor's edge - by a
	/// taskbar on the left or the top. Mixing the two up makes a window creep each launch.
	/// </summary>
	/// <param name="screen">The rectangle in screen coordinates.</param>
	/// <param name="monitors">The monitors.</param>
	/// <returns>The rectangle in workspace coordinates.</returns>
	public static PixelRect ToWorkspace(PixelRect screen, IReadOnlyList<MonitorArea> monitors) =>
		MonitorOf(screen, monitors) is { } monitor
			? screen.Offset(monitor.Bounds.Left - monitor.WorkArea.Left, monitor.Bounds.Top - monitor.WorkArea.Top)
			: screen;

	/// <summary>The workspace coordinates <c>GetWindowPlacement</c> reports, back to screen coordinates (<see cref="ToWorkspace"/>).</summary>
	/// <param name="workspace">The rectangle in workspace coordinates.</param>
	/// <param name="monitors">The monitors.</param>
	/// <returns>The rectangle in screen coordinates.</returns>
	public static PixelRect FromWorkspace(PixelRect workspace, IReadOnlyList<MonitorArea> monitors) =>
		MonitorOf(workspace, monitors) is { } monitor
			? workspace.Offset(monitor.WorkArea.Left - monitor.Bounds.Left, monitor.WorkArea.Top - monitor.Bounds.Top)
			: workspace;

	/// <summary>
	/// A monitor's work area in WPF's units for a window at that monitor's scaling: what a dialog on
	/// that monitor sizes itself to.
	/// </summary>
	/// <param name="workArea">The work area, in physical pixels.</param>
	/// <param name="dpi">The monitor's scaling, as a DPI; 0 or less is taken as 100%.</param>
	/// <returns>The work area in WPF's units.</returns>
	public static Rect ToDips(PixelRect workArea, int dpi)
	{
		double scale = StandardDpi / (double)(dpi > 0 ? dpi : StandardDpi);

		return new Rect(workArea.Left * scale, workArea.Top * scale, workArea.Width * scale, workArea.Height * scale);
	}

	/// <summary>The default size, shrunk to fit if it must, in the middle of the monitor's work area.</summary>
	private static PixelRect DefaultBounds(MonitorArea monitor, WindowSizes sizes)
	{
		PixelRect work = monitor.WorkArea;
		int width = Math.Min(Pixels(sizes.DefaultWidth, monitor.Dpi), work.Width);
		int height = Math.Min(Pixels(sizes.DefaultHeight, monitor.Dpi), work.Height);

		return new PixelRect(work.Left + ((work.Width - width) / 2), work.Top + ((work.Height - height) / 2), width, height);
	}

	/// <summary>
	/// The bounds made to fit the monitor's work area: no bigger than it, no smaller than the window's
	/// minimum size unless the work area is smaller still, and moved fully onto it.
	/// </summary>
	private static PixelRect FitOn(PixelRect bounds, MonitorArea monitor, WindowSizes sizes)
	{
		PixelRect work = monitor.WorkArea;
		int width = Math.Clamp(bounds.Width, Math.Min(Pixels(sizes.MinWidth, monitor.Dpi), work.Width), work.Width);
		int height = Math.Clamp(bounds.Height, Math.Min(Pixels(sizes.MinHeight, monitor.Dpi), work.Height), work.Height);

		return new PixelRect(
			Math.Clamp(bounds.Left, work.Left, work.Right - width),
			Math.Clamp(bounds.Top, work.Top, work.Bottom - height),
			width,
			height);
	}

	/// <summary>Whether all the title bar's height, and enough of its width to drag it by, are inside the monitor's work area.</summary>
	private static bool TitleBarShows(PixelRect bounds, MonitorArea monitor, WindowSizes sizes)
	{
		PixelRect work = monitor.WorkArea;
		long shownWidth = Math.Min(bounds.Right, work.Right) - (long)Math.Max(bounds.Left, work.Left);

		return bounds.Top >= work.Top
			&& bounds.Top + Pixels(sizes.CaptionHeight, monitor.Dpi) <= work.Bottom
			&& shownWidth >= Pixels(sizes.VisibleCaptionWidth, monitor.Dpi);
	}

	/// <summary>The bounds with their size taken from one scaling to another; their top-left corner stays.</summary>
	private static PixelRect Rescale(PixelRect bounds, int fromDpi, int toDpi) =>
		fromDpi <= 0 || toDpi <= 0 || fromDpi == toDpi
			? bounds
			: bounds with
			{
				Width = (int)Math.Round(bounds.Width * (double)toDpi / fromDpi),
				Height = (int)Math.Round(bounds.Height * (double)toDpi / fromDpi),
			};

	/// <summary>A size in WPF's units, in pixels at <paramref name="dpi"/>.</summary>
	private static int Pixels(double dips, int dpi) => (int)Math.Round(dips * dpi / StandardDpi);
}
