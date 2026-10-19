using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Shell;

using FeBuddy.Wpf.Behaviors;
using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.Shell.Models;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// A <see cref="Window"/> that wears the app's own chrome — a dark titlebar with
/// the shared caption buttons and a 1px window border — instead of the stock
/// Windows title bar. The child dialogs (ROI picker, Confirm, Update) derive from this so
/// they match the shell.
/// <para>
/// The look lives in <c>Theme/Controls.Window.xaml</c>'s <c>ChromeWindowStyle</c>;
/// this class just installs the <see cref="WindowChrome"/>, applies that style, and
/// hooks the three named caption buttons (<c>PART_Minimize</c>, <c>PART_MaxRestore</c>,
/// <c>PART_Close</c>) from the template.
/// </para>
/// <para>
/// The style is wired up explicitly via <see cref="FrameworkElement.SetResourceReference"/>
/// rather than through <c>DefaultStyleKeyProperty</c>'s implicit theme-style lookup: a plain
/// <c>Window</c> subclass's default style is resolved through WPF's theme/generic dictionary
/// path, not through the ordinary <c>Application.Resources</c> lookup that every other implicit
/// style in this app relies on - on this app's target framework that lookup silently comes back
/// empty (<c>Style</c> stays <see langword="null"/>, so nothing ever paints and the window
/// renders solid black). Applying the style by key sidesteps that path entirely.
/// </para>
/// </summary>
public class ChromeWindow : Window
{
	/// <summary>Initializes the window and installs the custom chrome.</summary>
	public ChromeWindow()
	{
		SetResourceReference(StyleProperty, "ChromeWindowStyle");

		WindowStyle = WindowStyle.None;
		WindowChrome.SetWindowChrome(this, new WindowChrome
		{
			CaptionHeight = 40,
			ResizeBorderThickness = new Thickness(6),
			CornerRadius = new CornerRadius(0),
			GlassFrameThickness = new Thickness(0),
			UseAeroCaptionButtons = false,
		});
		MaximizeToWorkArea.Attach(this);
	}

	/// <summary>
	/// The work area, in WPF's units, of the screen this window's owner is on - the screen a dialog
	/// opens on, over its owner - or of this window's own screen when it has no owner. Unlike
	/// <see cref="SystemParameters.WorkArea"/>, always the main screen's, it follows FE-Buddy onto
	/// another screen. Ask once the owner is set: from <see cref="Window.SourceInitialized"/>, which
	/// comes before a window sized to its content is measured, or before the window is shown.
	/// </summary>
	/// <returns>The work area; the main screen's when the screen can't be read.</returns>
	protected Rect OwnerWorkArea()
	{
		MonitorArea? monitor = NativeMonitors.Of(new WindowInteropHelper(Owner ?? this).Handle);

		return monitor is null ? SystemParameters.WorkArea : WindowPlacementRules.ToDips(monitor.WorkArea, monitor.Dpi);
	}

	/// <inheritdoc />
	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		Hook("PART_Minimize", () => WindowState = WindowState.Minimized);
		Hook("PART_MaxRestore", () => WindowState =
			WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized);
		Hook("PART_Close", Close);
	}

	private void Hook(string partName, Action action)
	{
		if (GetTemplateChild(partName) is ButtonBase button)
		{
			button.Click += (_, _) => action();
		}
	}
}
