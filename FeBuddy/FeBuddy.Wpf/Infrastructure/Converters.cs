using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;

namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// Flattens a small subset of Markdown to plain text for a one-way binding into
/// a <see cref="System.Windows.Controls.TextBlock"/> (theme fixes P8): the News
/// feed was showing raw <c>**bold**</c> markers. Strips <c>**</c> / <c>__</c>
/// emphasis and leading <c>#</c> heading marks, and rewrites <c>[label](url)</c>
/// to just <c>label</c>. Everything else, including line breaks, is left as-is.
/// </summary>
public sealed partial class MarkdownToPlainTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string s || s.Length == 0)
        {
            return value ?? string.Empty;
        }

        s = LinkPattern().Replace(s, "$1");
        s = EmphasisPattern().Replace(s, string.Empty);
        s = HeadingPattern().Replace(s, string.Empty);
        return s;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;

    [GeneratedRegex(@"\[([^\]]+)\]\([^)]*\)")]
    private static partial Regex LinkPattern();

    [GeneratedRegex(@"\*\*|__|(?<=\s)\*(?=\S)|(?<=\S)\*(?=\s)")]
    private static partial Regex EmphasisPattern();

    [GeneratedRegex(@"^\s{0,3}#{1,6}\s*", RegexOptions.Multiline)]
    private static partial Regex HeadingPattern();
}

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
/// string -> <see cref="Visibility"/>: <see cref="Visibility.Visible"/> when the string is
/// non-empty, <see cref="Visibility.Collapsed"/> otherwise. Pass ConverterParameter="Invert"
/// to reverse it.
/// </summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hasText = !string.IsNullOrWhiteSpace(value as string);
        if (string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase))
        {
            hasText = !hasText;
        }

        return hasText ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// Two-way inverts a <see cref="bool"/>, e.g. for pairing a checked RadioButton
/// with the negation of the property another RadioButton in the same group binds
/// straight to.
/// </summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
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
