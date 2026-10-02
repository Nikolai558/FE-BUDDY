using System.Windows;
using System.Windows.Controls;

using FeBuddy.Wpf.Controls;

using FeBuddy.Core.Infrastructure.Markdown;

namespace FeBuddy.Wpf.Behaviors;

/// <summary>
/// <c>bhv:InlineMarkdown.Text="Only what **you** pick, in `Upload_to_vNAS`."</c> on a
/// <see cref="TextBlock"/> - one line of inline Markdown (<c>**bold**</c>, <c>*italic*</c>,
/// <c>`code`</c>, links), shown the way <see cref="MarkdownView"/> shows a paragraph's text but in
/// the TextBlock's own style. The What's New page uses it for its table cells and lists.
/// </summary>
/// <remarks>
/// Set it instead of <c>Text</c>: it replaces the TextBlock's inlines. For text whose only markup
/// is <c>`code`</c>, <see cref="InlineCode"/> is lighter.
/// </remarks>
public static class InlineMarkdown
{
	/// <summary>The text, as inline Markdown.</summary>
	public static readonly DependencyProperty TextProperty =
		DependencyProperty.RegisterAttached(
			"Text", typeof(string), typeof(InlineMarkdown),
			new PropertyMetadata(null, OnTextChanged));

	/// <summary>Sets a TextBlock's text, as inline Markdown.</summary>
	/// <param name="textBlock">The TextBlock.</param>
	/// <param name="value">The text, or <see langword="null"/> for none.</param>
	public static void SetText(TextBlock textBlock, string? value)
	{
		ArgumentNullException.ThrowIfNull(textBlock);
		textBlock.SetValue(TextProperty, value);
	}

	/// <summary>Gets a TextBlock's text, as set, markup included.</summary>
	/// <param name="textBlock">The TextBlock.</param>
	/// <returns>The text, or <see langword="null"/>.</returns>
	public static string? GetText(TextBlock textBlock)
	{
		ArgumentNullException.ThrowIfNull(textBlock);
		return (string?)textBlock.GetValue(TextProperty);
	}

	private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not TextBlock textBlock)
		{
			return;
		}

		textBlock.Inlines.Clear();

		foreach (var span in MarkdownParser.ParseInlines(e.NewValue as string ?? string.Empty))
		{
			textBlock.Inlines.Add(MarkdownView.BuildInline(span));
		}
	}
}
