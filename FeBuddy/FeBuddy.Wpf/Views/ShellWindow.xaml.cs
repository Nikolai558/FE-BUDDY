using System.Windows;
using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// Code-behind is limited to what is genuinely a window concern: the custom
/// caption buttons and keeping the maximised window inside the work area.
/// Everything else is data-bound.
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
		MaximizeToWorkArea.Attach(this);
		StateChanged += OnStateChanged;
	}

	private void OnMinimize(object sender, RoutedEventArgs e)
		=> WindowState = WindowState.Minimized;

	private void OnMaxRestore(object sender, RoutedEventArgs e)
		=> WindowState = WindowState == WindowState.Maximized
			? WindowState.Normal
			: WindowState.Maximized;

	private void OnClose(object sender, RoutedEventArgs e) => Close();

	// No maximised padding is needed: MaximizeToWorkArea sizes the maximised window to the
	// monitor's work area, so nothing overshoots the screen edges or sits under the taskbar.
	private void OnStateChanged(object? sender, EventArgs e)
		=> MaxRestoreButton.Content = WindowState == WindowState.Maximized ? RestoreGlyph : MaximizeGlyph;
}
