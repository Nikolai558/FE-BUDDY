using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// Attached behaviours for mouse-wheel scrolling. Two independent opt-ins:
///
///   <c>WheelScroll.Amplify="2.5"</c> on a <see cref="ScrollViewer"/> — multiplies
///   each wheel notch so a long page scrolls at a sane pace (WPF's default
///   three-line step feels tiny on the AIRAC / Settings pages).
///
///   <c>WheelScroll.BubbleUp="True"</c> on an inner <see cref="ScrollViewer"/> or
///   an <see cref="ItemsControl"/> that sits inside another scroller — when the
///   inner one is already at its top or bottom edge, the wheel event is
///   re-raised to the parent so the outer page keeps scrolling instead of the
///   gesture dying where the pointer happens to be.
/// </summary>
public static class WheelScroll
{
    // ----------------------------- Amplify ------------------------------

    public static readonly DependencyProperty AmplifyProperty =
        DependencyProperty.RegisterAttached(
            "Amplify", typeof(double), typeof(WheelScroll),
            new PropertyMetadata(0d, OnAmplifyChanged));

    public static double GetAmplify(DependencyObject o) => (double)o.GetValue(AmplifyProperty);
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

        // One notch is Delta 120. ~48 device-independent px per notch, times the
        // requested factor, matches "about 3x the current step" at factor 3.
        e.Handled = true;
        sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta / 120.0 * 48.0 * factor);
    }

    // ----------------------------- BubbleUp -----------------------------

    public static readonly DependencyProperty BubbleUpProperty =
        DependencyProperty.RegisterAttached(
            "BubbleUp", typeof(bool), typeof(WheelScroll),
            new PropertyMetadata(false, OnBubbleUpChanged));

    public static bool GetBubbleUp(DependencyObject o) => (bool)o.GetValue(BubbleUpProperty);
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
