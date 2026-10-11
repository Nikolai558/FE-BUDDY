using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Data;

using FeBuddy.Wpf.Behaviors;

namespace FeBuddy.Wpf.Converters;

/// <summary>
/// One-way: puts each file name in a message - a word ending in a file extension FE-Buddy writes or
/// reads, such as <c>Combined_Alias.txt</c> - between backticks, so <c>bhv:InlineCode.Text</c> shows
/// it as code. For the run messages on the Review tab, which FE-Buddy's library writes as plain text.
/// </summary>
/// <remarks>
/// A name already between backticks is left as it is, and so is one inside a path or a web address
/// (<c>Aliases\Airways.txt</c>, <c>…/main/ZOB.txt</c>): only part of it would stand out.
/// </remarks>
public sealed partial class FileNamesAsCodeConverter : IValueConverter
{
	/// <inheritdoc />
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> value is string text ? Mark(text) : string.Empty;

	/// <inheritdoc />
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> Binding.DoNothing;

	/// <summary>Puts each file name in <paramref name="text"/> between backticks.</summary>
	/// <param name="text">The message.</param>
	/// <returns>The message, its file names marked as code.</returns>
	internal static string Mark(string text)
	{
		ArgumentNullException.ThrowIfNull(text);

		StringBuilder marked = new(text.Length + 16);

		foreach ((string part, bool isCode) in InlineCode.Split(text))
		{
			marked.Append(isCode ? $"`{part}`" : FileName().Replace(part, "`$0`"));
		}

		return marked.ToString();
	}

	// A word of letters, digits, '_' and '-', with dots inside, ending in a known extension - not
	// straight after a path separator, a dot, a backtick or '>' (a placeholder such as <Airport ID>).
	[GeneratedRegex(@"(?<![\w`/\\.>-])[\w-]+(?:\.[\w-]+)*\.(?:txt|geojson|md|json|xml|dat|sct2?|csv|log)\b(?![`/\\])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex FileName();
}
