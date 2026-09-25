using System.Globalization;
using System.Windows.Data;

namespace FeBuddy.Wpf.Converters;

/// <summary>
/// Two-way maps an enum property to a <see cref="bool"/> for a single option,
/// so a group of RadioButtons can bind straight to one enum property:
/// <code>
/// IsChecked="{Binding Mode, Converter={StaticResource EnumToBool}, ConverterParameter=HighLow}"
/// </code>
/// Checking a button writes that enum value back; unchecking is ignored (the
/// newly-checked button in the group does the write).
/// </summary>
public sealed class EnumToBooleanConverter : IValueConverter
{
	/// <inheritdoc />
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> value is not null && parameter is string name
		   && string.Equals(value.ToString(), name, StringComparison.Ordinal);

	/// <inheritdoc />
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is true && parameter is string name && targetType.IsEnum)
		{
			return Enum.Parse(targetType, name);
		}

		return Binding.DoNothing;
	}
}
