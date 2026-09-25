using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FeBuddy.Wpf.Converters;

/// <summary>
/// string -> <see cref="Visibility"/>: <see cref="Visibility.Visible"/> when the string is
/// non-empty, <see cref="Visibility.Collapsed"/> otherwise. Pass ConverterParameter="Invert"
/// to reverse it.
/// </summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
	/// <inheritdoc />
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var hasText = !string.IsNullOrWhiteSpace(value as string);
		if (string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase))
		{
			hasText = !hasText;
		}

		return hasText ? Visibility.Visible : Visibility.Collapsed;
	}

	/// <inheritdoc />
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> Binding.DoNothing;
}
