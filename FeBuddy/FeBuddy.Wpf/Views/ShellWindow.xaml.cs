using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Wpf.Behaviors;
using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// The main window. See ShellWindow.xaml. Its code-behind is limited to window concerns: the
/// custom caption buttons, keeping the maximised window inside the work area, and the brand
/// mark's clicks. Everything else is data-bound to <see cref="ShellViewModel"/>.
/// </summary>
public partial class ShellWindow : Window
{
	/// <summary>Clicks in a row on the brand mark before it answers.</summary>
	private const int BrandMarkClicks = 7;

	/// <summary>A longer pause than this between clicks starts the count again.</summary>
	private const long BrandMarkClickGapMs = 1_500;

	private int _brandMarkClickCount;
	private long _brandMarkLastClickMs;

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

	private void OnBrandMarkClick(object sender, MouseButtonEventArgs e)
	{
		long now = Environment.TickCount64;
		_brandMarkClickCount = now - _brandMarkLastClickMs <= BrandMarkClickGapMs ? _brandMarkClickCount + 1 : 1;
		_brandMarkLastClickMs = now;

		if (_brandMarkClickCount < BrandMarkClicks)
		{
			return;
		}

		_brandMarkClickCount = 0;
		Toast.Success("You are now a facility engineer.");

		int days = AppEnvironment.GetAiracCycle(AiracCyclePosition.Next).EffectiveDateUtc.DayNumber
			- AppEnvironment.CheckedUtcDate.DayNumber;
		string when = days == 1 ? "tomorrow" : $"in {days} days";

		DispatcherTimer followUp = new() { Interval = TimeSpan.FromSeconds(2) };
		followUp.Tick += (_, _) =>
		{
			followUp.Stop();
			Toast.Info("…oh wait, you already were.", $"Condolences. The next AIRAC cycle is {when}.");
		};
		followUp.Start();
	}
}
