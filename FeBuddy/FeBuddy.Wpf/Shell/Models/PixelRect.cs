namespace FeBuddy.Wpf.Shell.Models;

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
