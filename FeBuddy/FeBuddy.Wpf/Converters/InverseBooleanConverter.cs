using System.Globalization;
using System.Windows.Data;

namespace FeBuddy.Wpf.Converters;

/// <summary>
/// Two-way inverts a <see cref="bool"/>, e.g. for pairing a checked RadioButton
/// with the negation of the property another RadioButton in the same group binds
/// straight to.
/// </summary>
public sealed class InverseBooleanConverter : IValueConverter
{
	/// <inheritdoc />
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> value is bool b && !b;

	/// <inheritdoc />
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> value is bool b && !b;
}
