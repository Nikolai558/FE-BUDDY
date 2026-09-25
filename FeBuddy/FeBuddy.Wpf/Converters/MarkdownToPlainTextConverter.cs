using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace FeBuddy.Wpf.Converters;

/// <summary>
/// Flattens a small subset of Markdown to plain text for a one-way binding into
/// a <see cref="System.Windows.Controls.TextBlock"/>, so a News preview never shows raw
/// <c>**bold**</c> markers. Strips <c>**</c> / <c>__</c>
/// emphasis and leading <c>#</c> heading marks, and rewrites <c>[label](url)</c>
/// to just <c>label</c>. Everything else, including line breaks, is left as-is.
/// </summary>
public sealed partial class MarkdownToPlainTextConverter : IValueConverter
{
	/// <inheritdoc />
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

	/// <inheritdoc />
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> Binding.DoNothing;

	[GeneratedRegex(@"\[([^\]]+)\]\([^)]*\)")]
	private static partial Regex LinkPattern();

	[GeneratedRegex(@"\*\*|__|(?<=\s)\*(?=\S)|(?<=\S)\*(?=\s)")]
	private static partial Regex EmphasisPattern();

	[GeneratedRegex(@"^\s{0,3}#{1,6}\s*", RegexOptions.Multiline)]
	private static partial Regex HeadingPattern();
}
