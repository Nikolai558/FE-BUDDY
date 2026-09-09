using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Shell;

namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// A <see cref="Window"/> that wears the app's own chrome — a dark titlebar with
/// the shared caption buttons and a 1px window border — instead of the stock
/// Windows title bar (theme fixes P6). The child dialogs (ROI picker, Confirm,
/// Update) derive from this so they stop looking foreign against the shell.
///
/// <para>
/// The look lives in the implicit style in <c>Theme/Controls.Window.xaml</c>;
/// this class just installs the <see cref="WindowChrome"/> and hooks the three
/// named caption buttons (<c>PART_Minimize</c>, <c>PART_MaxRestore</c>,
/// <c>PART_Close</c>) from the template.
/// </para>
/// </summary>
public class ChromeWindow : Window
{
    static ChromeWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ChromeWindow), new FrameworkPropertyMetadata(typeof(ChromeWindow)));
    }

    /// <summary>Initializes the window and installs the custom chrome.</summary>
    public ChromeWindow()
    {
        WindowStyle = WindowStyle.None;
        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = 40,
            ResizeBorderThickness = new Thickness(6),
            CornerRadius = new CornerRadius(0),
            GlassFrameThickness = new Thickness(0),
            UseAeroCaptionButtons = false,
        });
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
