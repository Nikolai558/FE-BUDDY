using System.Windows.Media;

namespace FeBuddy.Wpf.Map;

/// <summary>
/// Makes a solid brush and freezes it: a frozen brush can be built off the UI thread (the AIRAC
/// layers are) and drawn from any, and WPF never has to watch it for changes.
/// </summary>
internal static class FrozenBrush
{
	/// <summary>A frozen brush of one colour.</summary>
	/// <param name="color">The colour.</param>
	/// <returns>The brush, already frozen.</returns>
	public static SolidColorBrush Of(Color color)
	{
		SolidColorBrush brush = new(color);
		brush.Freeze();
		return brush;
	}
}
