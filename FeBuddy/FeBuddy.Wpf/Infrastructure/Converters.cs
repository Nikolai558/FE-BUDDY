using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// bool -> <see cref="Visibility"/>. Pass ConverterParameter="Invert" to map
/// <see langword="true"/> to <see cref="Visibility.Collapsed"/> instead.
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var flag = value is true;
        if (IsInvert(parameter))
        {
            flag = !flag;
        }

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var visible = value is Visibility.Visible;
        return IsInvert(parameter) ? !visible : visible;
    }

    private static bool IsInvert(object? parameter)
        => string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase);
}

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
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null && parameter is string name
           && string.Equals(value.ToString(), name, StringComparison.Ordinal);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is string name && targetType.IsEnum)
        {
            return Enum.Parse(targetType, name);
        }

        return Binding.DoNothing;
    }
}
