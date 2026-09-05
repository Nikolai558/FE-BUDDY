using System.Windows;
using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// Code-behind is limited to what is genuinely a window concern: the custom
/// caption buttons and the maximise padding fix. Everything else is data-bound.
/// </summary>
public partial class ShellWindow : Window
{
    // Glyphs for the maximise button in each state (Segoe Fluent: Maximize / Restore).
    private const string MaximizeGlyph = "";
    private const string RestoreGlyph = "";

    public ShellWindow()
    {
        InitializeComponent();
        DataContext = new ShellViewModel();
        StateChanged += OnStateChanged;
    }

    private void OnMinimize(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void OnMaxRestore(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private void OnStateChanged(object? sender, EventArgs e)
    {
        var maximized = WindowState == WindowState.Maximized;

        // Without WindowChrome's non-client area, a maximised window would push
        // ~8px of content past every screen edge. Pad it back.
        RootBorder.Padding = maximized ? new Thickness(8) : new Thickness(0);
        MaxRestoreButton.Content = maximized ? RestoreGlyph : MaximizeGlyph;
    }
}
