using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

using FeBuddy.Wpf.Shell;

namespace FeBuddy.Wpf.Behaviors;

/// <summary>
/// <c>bhv:InlineCode.Text="Files are written to `Geojson\` in the cycle's folder."</c> on a
/// <see cref="TextBlock"/> - shows the text with every part between backticks in the look
/// Markdown gives <c>`code`</c>: the mono font on a faint panel. Folder names and paths in
/// descriptions use it, so they stand out from the words around them. A Markdown link,
/// <c>[HERE](https://docs.virtualnas.net/)</c>, outside the backticks shows as a link that opens in
/// the browser.
/// </summary>
/// <remarks>
/// <para>
/// Set it instead of <c>Text</c>: it replaces the TextBlock's inlines. Only a backtick and a
/// <c>[text](https://...)</c> link are special, so a name such as <c>Combined_Alias.txt</c> needs no
/// escaping, and a backtick with no partner shows as it is. Line breaks (<c>&amp;#xA;</c> in XAML,
/// <c>\n</c> in code) work as they do in <c>Text</c>.
/// </para>
/// <para>
/// Code text is sized from the TextBlock's own font size, at the ratio <c>MarkdownView</c> uses
/// (13 against its 14.5), so it suits a caption as well as body text. <see cref="ApplyLook"/> is the
/// one place the look itself is set; <c>MarkdownView</c> uses it too.
/// </para>
/// </remarks>
public static class InlineCode
{
	/// <summary>The code text's size against the text around it: MarkdownView's 13 against its 14.5.</summary>
	private const double SizeRatio = 13.0 / 14.5;

	/// <summary>The text, with each part to show as code between backticks.</summary>
	public static readonly DependencyProperty TextProperty =
		DependencyProperty.RegisterAttached(
			"Text", typeof(string), typeof(InlineCode),
			new PropertyMetadata(null, OnTextChanged));

	/// <summary>Sets a TextBlock's text, with each part to show as code between backticks.</summary>
	/// <param name="textBlock">The TextBlock.</param>
	/// <param name="value">The text, or <see langword="null"/> for none.</param>
	public static void SetText(TextBlock textBlock, string? value)
	{
		ArgumentNullException.ThrowIfNull(textBlock);
		textBlock.SetValue(TextProperty, value);
	}

	/// <summary>Gets a TextBlock's text, as set, backticks included.</summary>
	/// <param name="textBlock">The TextBlock.</param>
	/// <returns>The text, or <see langword="null"/>.</returns>
	public static string? GetText(TextBlock textBlock)
	{
		ArgumentNullException.ThrowIfNull(textBlock);
		return (string?)textBlock.GetValue(TextProperty);
	}

	/// <summary>
	/// Gives text the code look: the mono font on the faint <c>Brush.Stroke</c> panel, in the
	/// primary text colour.
	/// </summary>
	/// <param name="element">The run or span.</param>
	/// <param name="primaryText">
	/// <see langword="false"/> to leave the colour to what holds it - a link's own colour, say.
	/// </param>
	public static void ApplyLook(TextElement element, bool primaryText = true)
	{
		ArgumentNullException.ThrowIfNull(element);

		element.SetResourceReference(TextElement.FontFamilyProperty, "Font.Mono");
		element.SetResourceReference(TextElement.BackgroundProperty, "Brush.Stroke");

		if (primaryText)
		{
			element.SetResourceReference(TextElement.ForegroundProperty, "Brush.Text.Primary");
		}
	}

	/// <summary>
	/// Splits text at its backticks: the parts between a pair are code, the rest plain. A backtick
	/// with no partner, or a pair with nothing between, stays in the plain text as written.
	/// </summary>
	/// <param name="text">The text.</param>
	/// <returns>The parts, in order, none of them empty.</returns>
	internal static IReadOnlyList<(string Text, bool IsCode)> Split(string text)
	{
		ArgumentNullException.ThrowIfNull(text);

		List<(string Text, bool IsCode)> parts = [];
		int plainStart = 0;
		int searchFrom = 0;

		while (text.IndexOf('`', searchFrom) is int open and >= 0)
		{
			int close = text.IndexOf('`', open + 1);

			if (close < 0)
			{
				break;
			}

			if (close == open + 1)
			{
				// Nothing between: two backticks, written as they are.
				searchFrom = close + 1;
				continue;
			}

			if (open > plainStart)
			{
				parts.Add((text[plainStart..open], false));
			}

			parts.Add((text[(open + 1)..close], true));
			plainStart = close + 1;
			searchFrom = plainStart;
		}

		if (plainStart < text.Length)
		{
			parts.Add((text[plainStart..], false));
		}

		return parts;
	}

	/// <summary>
	/// Splits plain text at its Markdown links, <c>[text](https://...)</c>: each link's text and
	/// address, and the plain text around them. Only an <c>http</c> or <c>https</c> address makes a
	/// link; anything else stays in the plain text as written.
	/// </summary>
	/// <param name="text">The plain text: a part <see cref="Split"/> did not mark as code.</param>
	/// <returns>The parts, in order, none of them empty; <c>Url</c> is <see langword="null"/> for plain text.</returns>
	internal static IReadOnlyList<(string Text, string? Url)> SplitLinks(string text)
	{
		ArgumentNullException.ThrowIfNull(text);

		List<(string Text, string? Url)> parts = [];
		int plainStart = 0;
		int searchFrom = 0;

		while (text.IndexOf('[', searchFrom) is int open and >= 0)
		{
			int closeText = text.IndexOf("](", open + 1, StringComparison.Ordinal);
			int closeUrl = closeText < 0 ? -1 : text.IndexOf(')', closeText + 2);

			if (closeUrl < 0)
			{
				break;
			}

			string label = text[(open + 1)..closeText];
			string url = text[(closeText + 2)..closeUrl];

			if (label.Length == 0 || label.Contains('[') || !Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
				|| (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
			{
				searchFrom = open + 1;
				continue;
			}

			if (open > plainStart)
			{
				parts.Add((text[plainStart..open], null));
			}

			parts.Add((label, url));
			plainStart = closeUrl + 1;
			searchFrom = plainStart;
		}

		if (plainStart < text.Length)
		{
			parts.Add((text[plainStart..], null));
		}

		return parts;
	}

	private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not TextBlock textBlock)
		{
			return;
		}

		textBlock.Inlines.Clear();

		foreach ((string part, bool isCode) in Split(e.NewValue as string ?? string.Empty))
		{
			if (!isCode)
			{
				foreach ((string plain, string? url) in SplitLinks(part))
				{
					textBlock.Inlines.Add(url is null ? new Run(plain) : Link(plain, url));
				}

				continue;
			}

			Run run = new(part);
			ApplyLook(run);
			run.SetBinding(TextElement.FontSizeProperty, new Binding(nameof(TextBlock.FontSize))
			{
				Source = textBlock,
				Converter = CodeSize.Instance,
			});

			textBlock.Inlines.Add(run);
		}
	}

	/// <summary>A link in the accent colour that opens <paramref name="url"/> in the browser.</summary>
	private static Hyperlink Link(string text, string url)
	{
		Hyperlink link = new(new Run(text)) { ToolTip = url };
		link.SetResourceReference(TextElement.ForegroundProperty, "Brush.Accent");
		link.Click += (_, _) => BrowserLauncher.Open(url);
		return link;
	}

	/// <summary>The code text's font size from the TextBlock's.</summary>
	private sealed class CodeSize : IValueConverter
	{
		public static CodeSize Instance { get; } = new();

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
			value is double size ? size * SizeRatio : DependencyProperty.UnsetValue;

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
			throw new NotSupportedException();
	}
}
