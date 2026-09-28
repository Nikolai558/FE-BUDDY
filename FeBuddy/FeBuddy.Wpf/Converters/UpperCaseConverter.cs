using System.Globalization;
using System.Windows.Data;

namespace FeBuddy.Wpf.Converters;

/// <summary>
/// One-way string -> upper case in the binding's culture. Lets a title be written in normal case
/// at the call site while SectionHeader decides how every title is cased.
/// </summary>
public sealed class UpperCaseConverter : IValueConverter
{
	/// <inheritdoc />
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> (value as string)?.ToUpper(culture) ?? string.Empty;

	/// <inheritdoc />
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> Binding.DoNothing;
}
