using System.Globalization;

namespace FeBuddy.Wpf.Shell;

/// <summary>
/// A rectangle in whole physical screen pixels: the unit Windows' placement and monitor functions use,
/// whatever each monitor's scaling.
/// </summary>
/// <param name="Left">Its left edge.</param>
/// <param name="Top">Its top edge.</param>
/// <param name="Width">Its width.</param>
/// <param name="Height">Its height.</param>
public readonly record struct PixelRect(int Left, int Top, int Width, int Height)
{
	/// <summary>Its right edge, just outside it.</summary>
	public int Right => Left + Width;

	/// <summary>Its bottom edge, just outside it.</summary>
	public int Bottom => Top + Height;

	/// <summary>How much of it <paramref name="other"/> covers, in square pixels; 0 when they do not meet.</summary>
	/// <param name="other">The other rectangle.</param>
	/// <returns>The area they share.</returns>
	public long OverlapArea(PixelRect other)
	{
		long width = Math.Min(Right, other.Right) - (long)Math.Max(Left, other.Left);
		long height = Math.Min(Bottom, other.Bottom) - (long)Math.Max(Top, other.Top);

		return width > 0 && height > 0 ? width * height : 0;
	}

	/// <summary>The same rectangle moved by <paramref name="x"/> and <paramref name="y"/>.</summary>
	/// <param name="x">How far right.</param>
	/// <param name="y">How far down.</param>
	/// <returns>The moved rectangle.</returns>
	public PixelRect Offset(int x, int y) => this with { Left = Left + x, Top = Top + y };
}

/// <summary>One connected monitor, as Windows reports it, in physical pixels.</summary>
/// <param name="Bounds">The whole monitor.</param>
/// <param name="WorkArea">The part of it outside the taskbar and any other docked bar.</param>
/// <param name="Dpi">Its scaling as a DPI: 96 at 100%, 144 at 150%.</param>
public sealed record MonitorArea(PixelRect Bounds, PixelRect WorkArea, int Dpi);

/// <summary>Where to put the main window.</summary>
/// <param name="Normal">Its normal (restored) bounds, in physical screen pixels.</param>
/// <param name="Maximized">Whether to maximize it, on the monitor <paramref name="Normal"/> is on.</param>
public sealed record WindowPlacementPlan(PixelRect Normal, bool Maximized);

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

/// <summary>
/// What FE-Buddy keeps of where its main window was when it closed
/// (<c>General.MainWindowPlacement</c>): its normal bounds and whether it was maximized. Never
/// minimized: a window closed minimized is kept as it was before.
/// </summary>
/// <param name="Normal">Its normal (restored) bounds, in physical screen pixels; it maximizes on the monitor they are on.</param>
/// <param name="Dpi">The scaling of the monitor those bounds were on, as a DPI.</param>
/// <param name="Maximized">Whether it was maximized.</param>
public sealed record SavedWindowPlacement(PixelRect Normal, int Dpi, bool Maximized)
{
	/// <summary>The furthest from 0 a saved number may be: far beyond any screen, and safe to add up.</summary>
	private const int MaxValue = 1_000_000;

	private const string NormalState = "Normal";
	private const string MaximizedState = "Maximized";

	/// <summary>The setting's text: <c>left,top,width,height,dpi,state</c>, e.g. <c>2120,140,1800,1170,144,Maximized</c>.</summary>
	/// <returns>The text to save.</returns>
	public string Format() => string.Create(
		CultureInfo.InvariantCulture,
		$"{Normal.Left},{Normal.Top},{Normal.Width},{Normal.Height},{Dpi},{(Maximized ? MaximizedState : NormalState)}");

	/// <summary>
	/// Reads the setting's text (<see cref="Format"/>). Anything else - missing, cut short, a word
	/// for a number, a size or DPI of 0 or less, a number far off any screen - is <see langword="null"/>,
	/// which means the default.
	/// </summary>
	/// <param name="text">The saved text, or <see langword="null"/>.</param>
	/// <returns>The placement, or <see langword="null"/> when there is none to read.</returns>
	public static SavedWindowPlacement? Parse(string? text)
	{
		string[] parts = (text ?? string.Empty).Split(',');

		if (parts.Length != 6)
		{
			return null;
		}

		int[] numbers = new int[5];

		for (int i = 0; i < numbers.Length; i++)
		{
			if (!int.TryParse(parts[i].Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out numbers[i])
				|| numbers[i] is < -MaxValue or > MaxValue)
			{
				return null;
			}
		}

		bool? maximized = parts[5].Trim() switch
		{
			NormalState => false,
			MaximizedState => true,
			_ => null,
		};

		if (numbers[2] <= 0 || numbers[3] <= 0 || numbers[4] <= 0 || maximized is null)
		{
			return null;
		}

		return new SavedWindowPlacement(new PixelRect(numbers[0], numbers[1], numbers[2], numbers[3]), numbers[4], maximized.Value);
	}
}
