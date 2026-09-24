using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FeBuddy.Wpf.Behaviors;

/// <summary>
/// Attached behaviours for mouse-wheel scrolling. Two independent opt-ins:
/// <list type="bullet">
/// <item><c>WheelScroll.Amplify="2.5"</c> on a <see cref="ScrollViewer"/> multiplies each wheel
/// notch so a long page scrolls at a sane pace (WPF's default three-line step feels tiny on the
/// AIRAC and Settings pages). It stands aside while the pointer is over a nested scroller that
/// can still move.</item>
/// <item><c>WheelScroll.BubbleUp="True"</c> on an inner <see cref="ScrollViewer"/> or
/// <see cref="ItemsControl"/> inside another scroller: once the inner one is at its top or bottom
/// edge, the wheel event is re-raised to the parent so the outer page keeps scrolling.</item>
/// </list>
/// </summary>
public static class WheelScroll
{
	// ----------------------------- Amplify ------------------------------

	/// <summary>Identifies the <c>Amplify</c> attached property.</summary>
	public static readonly DependencyProperty AmplifyProperty =
		DependencyProperty.RegisterAttached(
			"Amplify", typeof(double), typeof(WheelScroll),
			new PropertyMetadata(0d, OnAmplifyChanged));

	/// <summary>Gets the wheel multiplier for a <see cref="ScrollViewer"/>.</summary>
	/// <param name="o">The scroll viewer.</param>
	/// <returns>The multiplier; 0 or less means off.</returns>
	public static double GetAmplify(DependencyObject o) => (double)o.GetValue(AmplifyProperty);

	/// <summary>Sets the wheel multiplier for a <see cref="ScrollViewer"/>.</summary>
	/// <param name="o">The scroll viewer.</param>
	/// <param name="v">The multiplier; 0 or less turns it off.</param>
	public static void SetAmplify(DependencyObject o, double v) => o.SetValue(AmplifyProperty, v);

	private static void OnAmplifyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
	{
		if (o is not ScrollViewer sv) return;
		sv.PreviewMouseWheel -= AmplifyHandler;
		if ((double)e.NewValue > 0) sv.PreviewMouseWheel += AmplifyHandler;
	}

	private static void AmplifyHandler(object sender, MouseWheelEventArgs e)
	{
		if (e.Handled || sender is not ScrollViewer sv) return;
		double factor = GetAmplify(sv);
		if (factor <= 0 || sv.ScrollableHeight <= 0) return;

		// PreviewMouseWheel tunnels, so this outer scroller sees the wheel before any scroller
		// nested inside it; leave the event alone while the one under the pointer can still move.
		if (InnerCanScroll(e.OriginalSource as DependencyObject, sv, e.Delta)) return;

		// One notch is Delta 120. ~48 device-independent px per notch, times the
		// requested factor, matches "about 3x the current step" at factor 3.
		e.Handled = true;
		sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta / 120.0 * 48.0 * factor);
	}

	private static bool InnerCanScroll(DependencyObject? d, ScrollViewer outer, int delta)
	{
		while (d is not null && d != outer)
		{
			if (d is ScrollViewer inner && inner.ScrollableHeight > 0)
			{
				bool canUp = delta > 0 && inner.VerticalOffset > 0.5;
				bool canDown = delta < 0 && inner.VerticalOffset < inner.ScrollableHeight - 0.5;
				if (canUp || canDown) return true;
			}
			d = d is Visual ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
		}
		return false;
	}

	// ----------------------------- BubbleUp -----------------------------

	/// <summary>Identifies the <c>BubbleUp</c> attached property.</summary>
	public static readonly DependencyProperty BubbleUpProperty =
		DependencyProperty.RegisterAttached(
			"BubbleUp", typeof(bool), typeof(WheelScroll),
			new PropertyMetadata(false, OnBubbleUpChanged));

	/// <summary>Gets whether an inner scroller passes the wheel to its parent at its edges.</summary>
	/// <param name="o">The inner scroller or items control.</param>
	/// <returns><see langword="true"/> when it does.</returns>
	public static bool GetBubbleUp(DependencyObject o) => (bool)o.GetValue(BubbleUpProperty);

	/// <summary>Sets whether an inner scroller passes the wheel to its parent at its edges.</summary>
	/// <param name="o">The inner scroller or items control.</param>
	/// <param name="v"><see langword="true"/> to pass it on.</param>
	public static void SetBubbleUp(DependencyObject o, bool v) => o.SetValue(BubbleUpProperty, v);

	private static void OnBubbleUpChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
	{
		if (o is not UIElement el) return;
		el.PreviewMouseWheel -= BubbleHandler;
		if ((bool)e.NewValue) el.PreviewMouseWheel += BubbleHandler;
	}

	private static void BubbleHandler(object sender, MouseWheelEventArgs e)
	{
		if (e.Handled || sender is not DependencyObject d) return;

		var sv = sender as ScrollViewer ?? FindDescendantScrollViewer(d);
		if (sv is null) return;

		bool atTop = sv.VerticalOffset <= 0.5;
		bool atBottom = sv.VerticalOffset >= sv.ScrollableHeight - 0.5;
		bool wantsPastTop = e.Delta > 0 && atTop;
		bool wantsPastBottom = e.Delta < 0 && atBottom;
		if (!wantsPastTop && !wantsPastBottom) return;

		e.Handled = true;
		if (VisualTreeHelper.GetParent(d) is UIElement parent)
		{
			parent.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
			{
				RoutedEvent = UIElement.MouseWheelEvent,
				Source = sender,
			});
		}
	}

	private static ScrollViewer? FindDescendantScrollViewer(DependencyObject root)
	{
		if (root is ScrollViewer sv) return sv;
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var found = FindDescendantScrollViewer(VisualTreeHelper.GetChild(root, i));
			if (found is not null) return found;
		}
		return null;
	}
}
