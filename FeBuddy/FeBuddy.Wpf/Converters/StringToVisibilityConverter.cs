using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FeBuddy.Wpf.Converters;

/// <summary>
/// string -> <see cref="Visibility"/>: <see cref="Visibility.Visible"/> when the string is
/// non-empty, <see cref="Visibility.Collapsed"/> otherwise.
/// </summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
	/// <inheritdoc />
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

	/// <inheritdoc />
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> Binding.DoNothing;
}
