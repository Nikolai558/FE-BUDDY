using System.Windows;
using System.Windows.Threading;

namespace FeBuddy.Wpf.Behaviors;

/// <summary>
/// <c>bhv:BringIntoView.When="{Binding Results.IsRunning}"</c> on an element - scrolls the element
/// into view each time the bound value turns <see langword="true"/>.
/// </summary>
/// <remarks>
/// A conversion's results appear under its Convert card, at the foot of a page that is usually
/// scrolled to that card; without this they would start below the fold. The scroll waits for
/// layout, since the element has often only just been shown.
/// </remarks>
public static class BringIntoView
{
	/// <summary>The value whose every turn to <see langword="true"/> brings the element into view.</summary>
	public static readonly DependencyProperty WhenProperty =
		DependencyProperty.RegisterAttached(
			"When", typeof(bool), typeof(BringIntoView),
			new PropertyMetadata(false, OnWhenChanged));

	/// <summary>Sets the value to follow.</summary>
	/// <param name="element">The element.</param>
	/// <param name="value">The value.</param>
	public static void SetWhen(DependencyObject element, bool value) => element.SetValue(WhenProperty, value);

	/// <summary>Gets the value followed.</summary>
	/// <param name="element">The element.</param>
	/// <returns>The value.</returns>
	public static bool GetWhen(DependencyObject element) => (bool)element.GetValue(WhenProperty);

	private static void OnWhenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is FrameworkElement element && e.NewValue is true)
		{
			element.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => element.BringIntoView());
		}
	}
}
