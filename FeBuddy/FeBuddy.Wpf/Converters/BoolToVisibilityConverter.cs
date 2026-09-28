using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FeBuddy.Wpf.Converters;

/// <summary>
/// bool -> <see cref="Visibility"/>. Pass ConverterParameter="Invert" to map
/// <see langword="true"/> to <see cref="Visibility.Collapsed"/> instead.
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
	/// <inheritdoc />
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var flag = value is true;
		if (IsInvert(parameter))
		{
			flag = !flag;
		}

		return flag ? Visibility.Visible : Visibility.Collapsed;
	}

	/// <inheritdoc />
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var visible = value is Visibility.Visible;
		return IsInvert(parameter) ? !visible : visible;
	}

	private static bool IsInvert(object? parameter)
		=> string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase);
}
