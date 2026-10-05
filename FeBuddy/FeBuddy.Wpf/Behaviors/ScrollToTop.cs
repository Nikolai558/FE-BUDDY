using System.Windows;
using System.Windows.Controls;

namespace FeBuddy.Wpf.Behaviors;

/// <summary>
/// <c>bhv:ScrollToTop.When="{Binding SelectedTab}"</c> on a ScrollViewer - scrolls it back to the
/// top whenever the bound value changes, so a newly chosen tab opens at its start instead of at the
/// last tab's scroll position.
/// </summary>
/// <remarks>
/// A tabbed screen keeps one page-level ScrollViewer for all its tabs and only swaps what is inside
/// it, so without this the offset carries over from one tab to the next.
/// </remarks>
public static class ScrollToTop
{
	/// <summary>The value whose every change scrolls the ScrollViewer to the top.</summary>
	public static readonly DependencyProperty WhenProperty =
		DependencyProperty.RegisterAttached(
			"When", typeof(object), typeof(ScrollToTop),
			new PropertyMetadata(null, OnWhenChanged));

	/// <summary>Sets the value to follow.</summary>
	/// <param name="element">The ScrollViewer.</param>
	/// <param name="value">The value.</param>
	public static void SetWhen(DependencyObject element, object? value) => element.SetValue(WhenProperty, value);

	/// <summary>Gets the value followed.</summary>
	/// <param name="element">The ScrollViewer.</param>
	/// <returns>The value.</returns>
	public static object? GetWhen(DependencyObject element) => element.GetValue(WhenProperty);

	private static void OnWhenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is ScrollViewer scrollViewer)
		{
			scrollViewer.ScrollToTop();
		}
	}
}
