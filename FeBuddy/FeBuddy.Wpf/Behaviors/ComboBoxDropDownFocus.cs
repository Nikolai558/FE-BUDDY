using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FeBuddy.Wpf.Behaviors;

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

	/// <summary>Identifies the <c>Enable</c> attached property.</summary>
	public static readonly DependencyProperty EnableProperty =
		DependencyProperty.RegisterAttached(
			"Enable", typeof(bool), typeof(ComboBoxDropDownFocus),
			new PropertyMetadata(false, OnEnableChanged));

	/// <summary>Turns the dropdown wheel fix on or off for a <see cref="ComboBox"/>.</summary>
	/// <param name="o">The combo box.</param>
	/// <param name="value"><see langword="true"/> to scroll its open dropdown with the wheel.</param>
	public static void SetEnable(DependencyObject o, bool value) => o.SetValue(EnableProperty, value);

	/// <summary>Whether the dropdown wheel fix is on for a <see cref="ComboBox"/>.</summary>
	/// <param name="o">The combo box.</param>
	/// <returns><see langword="true"/> when it is on.</returns>
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

		// Mouse.GetPosition stays in WPF's DPI-independent units. Mixing a raw GetCursorPos
		// (physical pixels) with PointToScreen (DPI-scaled) is only right at 100% display scaling.
		Point local = Mouse.GetPosition(scroller);
		if (local.X < 0 || local.Y < 0 || local.X > scroller.ActualWidth || local.Y > scroller.ActualHeight)
		{
			return;
		}

		// VerticalOffset's unit depends on the ScrollViewer: with CanContentScroll on it counts
		// items, with it off device-independent pixels. The ComboBox theme turns it on (the list
		// virtualizes), where a pixel-sized step would jump whole screens of items per notch.
		double notches = e.Delta / 120.0;
		double step = scroller.CanContentScroll
			? notches * Math.Max(1, SystemParameters.WheelScrollLines)   // items per notch
			: notches * PixelsPerNotch;                                  // pixels per notch

		e.Handled = true;
		scroller.ScrollToVerticalOffset(scroller.VerticalOffset - step);
	}
}
