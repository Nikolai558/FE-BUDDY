using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// <c>ComboBoxDropDownFocus.Enable="True"</c> on a <see cref="ComboBox"/> - makes scrolling
/// the mouse wheel over an open dropdown scroll the dropdown instead of the page behind it.
/// </summary>
/// <remarks>
/// The dropdown's <c>Popup</c> (<c>Themed.ComboBox</c> in <c>Controls.Inputs.xaml</c>) sets
/// <c>AllowsTransparency="True"</c> for its fade-in and rounded corners, which forces Windows to
/// make its host window <c>WS_EX_NOACTIVATE</c> - it can never take real OS focus. Since
/// <c>WM_MOUSEWHEEL</c> is delivered to the focused top-level window and hit-tested against
/// <i>that window's own</i> visual tree, a wheel notch over the popup is actually delivered to
/// whatever sits behind it in the main window - the page keeps scrolling and the dropdown list
/// never sees the event at all. <c>Keyboard.Focus</c> can't fix this: it moves WPF's logical
/// focus but can't move real OS focus onto a no-activate window.
/// <para>
/// So instead of trying to route the event to the popup, this watches every
/// <c>PreviewMouseWheel</c> that reaches any window, and when a themed dropdown is open and the
/// cursor is over its on-screen bounds, scrolls that dropdown's list directly and marks the
/// event handled before the page underneath ever sees it.
/// </para>
/// </remarks>
public static class ComboBoxDropDownFocus
{
	/// <summary>Wheel step for a pixel-scrolling drop-down (<c>CanContentScroll="False"</c>).</summary>
	private const double PixelsPerNotch = 48.0;

	private static ComboBox? _openCombo;

	static ComboBoxDropDownFocus()
	{
		EventManager.RegisterClassHandler(
			typeof(Window), UIElement.PreviewMouseWheelEvent,
			new MouseWheelEventHandler(OnAnyPreviewMouseWheel), handledEventsToo: true);
	}

	public static readonly DependencyProperty EnableProperty =
		DependencyProperty.RegisterAttached(
			"Enable", typeof(bool), typeof(ComboBoxDropDownFocus),
			new PropertyMetadata(false, OnEnableChanged));

	public static void SetEnable(DependencyObject o, bool value) => o.SetValue(EnableProperty, value);

	public static bool GetEnable(DependencyObject o) => (bool)o.GetValue(EnableProperty);

	private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not ComboBox comboBox)
		{
			return;
		}

		comboBox.DropDownOpened -= OnDropDownOpened;
		comboBox.DropDownClosed -= OnDropDownClosed;
		if ((bool)e.NewValue)
		{
			comboBox.DropDownOpened += OnDropDownOpened;
			comboBox.DropDownClosed += OnDropDownClosed;
		}
	}

	private static void OnDropDownOpened(object? sender, EventArgs e) => _openCombo = sender as ComboBox;

	private static void OnDropDownClosed(object? sender, EventArgs e)
	{
		if (ReferenceEquals(_openCombo, sender))
		{
			_openCombo = null;
		}
	}

	private static void OnAnyPreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (_openCombo is not { IsDropDownOpen: true } combo)
		{
			return;
		}

		if (combo.Template?.FindName("PART_DropDownScroll", combo) is not ScrollViewer scroller ||
			!scroller.IsVisible)
		{
			return;
		}

		// Mouse.GetPosition stays in WPF's own DPI-independent units throughout, unlike mixing
		// a raw GetCursorPos (physical pixels) with PointToScreen (already DPI-scaled) - that
		// combination is only correct at exactly 100% display scaling and silently miscomputes
		// the hit-test everywhere else, which is why this ever needed fixing at all.
		Point local = Mouse.GetPosition(scroller);
		if (local.X < 0 || local.Y < 0 || local.X > scroller.ActualWidth || local.Y > scroller.ActualHeight)
		{
			return;
		}

		// VerticalOffset's UNIT depends on the ScrollViewer: with CanContentScroll on it counts
		// items; with it off it counts device-independent pixels. ComboBox's theme style turns
		// CanContentScroll on (the drop-down list virtualizes), so the pixel-sized step this used
		// to apply - 48 per notch - actually moved the list 48 *items*. On any list shorter than
		// that, which is every list in the app, one notch jumped straight from the top to the
		// bottom and the middle of the list could never be seen. Scroll in the unit the
		// ScrollViewer is really using.
		double notches = e.Delta / 120.0;
		double step = scroller.CanContentScroll
			? notches * Math.Max(1, SystemParameters.WheelScrollLines)   // items per notch
			: notches * PixelsPerNotch;                                  // pixels per notch

		e.Handled = true;
		scroller.ScrollToVerticalOffset(scroller.VerticalOffset - step);
	}
}
