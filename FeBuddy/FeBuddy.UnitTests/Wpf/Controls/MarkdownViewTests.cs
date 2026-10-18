using System.Windows.Controls;
using FeBuddy.Wpf.Controls;

namespace FeBuddy.UnitTests.Wpf.Controls;

/// <summary>
/// Covers how often <see cref="MarkdownView"/> works: built once XAML has set its properties, not once
/// per property, and each text parsed once while its links resolve the same way. Each test runs on a
/// WPF thread of its own.
/// </summary>
public sealed class MarkdownViewTests
{
	/// <summary>Nothing is built while XAML is still setting the properties; once it has, the view is built, and again on a change.</summary>
	[Fact]
	public void the_view_is_built_once_its_properties_are_set() => StaThread.Run(() =>
	{
		MarkdownView view = new();
		view.BeginInit();
		view.Markdown = "# News\n\nSee #12.";
		view.IssueUrlBase = "https://github.com/owner/repo/issues/";
		view.HeadingOffset = 1;

		Assert.Null(view.Child);

		view.EndInit();
		StackPanel built = Assert.IsType<StackPanel>(view.Child);
		Assert.Equal(2, built.Children.Count);

		view.Markdown = "Only a paragraph.";
		Assert.NotSame(built, view.Child);
		Assert.Single(Assert.IsType<StackPanel>(view.Child).Children);
	});

	/// <summary>The same text is parsed once; parsed with other link bases, it is parsed again.</summary>
	[Fact]
	public void a_text_is_parsed_once() => StaThread.Run(() =>
	{
		string text = "A [link](docs/a.md) and #5.";

		Assert.Same(MarkdownView.Parse(text, null, "https://example.com/x/"), MarkdownView.Parse(text, null, "https://example.com/x/"));
		Assert.NotSame(MarkdownView.Parse(text, null, "https://example.com/x/"), MarkdownView.Parse(text, "https://example.com/i/", "https://example.com/x/"));
		Assert.Empty(MarkdownView.Parse(null, null, null));
	});
}
