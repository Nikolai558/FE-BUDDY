using System.Windows;
using FeBuddy.Wpf.Behaviors;
using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// The main window. See ShellWindow.xaml. Its code-behind is limited to window concerns: the
/// custom caption buttons and keeping the maximised window inside the work area. Everything
/// else is data-bound to <see cref="ShellViewModel"/>.
/// </summary>
public partial class ShellWindow : Window
{
	/// <summary>Creates the window and its <see cref="ShellViewModel"/>.</summary>
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
		=> MaxRestoreButton.Content = FindResource(WindowState == WindowState.Maximized ? "Icon.Restore" : "Icon.Maximize");
}
