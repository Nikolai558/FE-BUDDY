using System.Globalization;

namespace FeBuddy.Wpf.Shell.Models;

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
