using System.Windows.Media;

namespace WPF.Constants;
public static class FeBuddyColors
{
  public static Color BorderBackground { get; } = Color.FromRgb(0, 0, 0);
  public static SolidColorBrush BorderBackgroundBrush { get; } = new SolidColorBrush(BorderBackground);
}
