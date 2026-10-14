using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Wpf.Behaviors;
using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// The main window. See ShellWindow.xaml. Its code-behind is limited to window concerns: the
/// custom caption buttons, keeping the maximised window inside the work area, opening where it was
/// last closed (<see cref="MainWindowPlacement"/>), the brand mark's clicks, and building File
/// Conversions ahead of its first open. Everything else is data-bound to <see cref="ShellViewModel"/>.
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

		// After MaximizeToWorkArea's hook, so a window put back maximized keeps off the taskbar.
		SourceInitialized += (_, _) => MainWindowPlacement.Restore(this);
		StateChanged += OnStateChanged;
		ContentRendered += (_, _) => Dispatcher.InvokeAsync(PrepareFileConversions, DispatcherPriority.ApplicationIdle);
	}

	/// <summary>
	/// Builds the File Conversions screen, then its first page, off screen, while FE-Buddy is idle after
	/// it opens. The first time WPF shows a screen, it loads and compiles every control on it, which
	/// made File Conversions pause on its first open; built here first, it opens at once.
	/// </summary>
	private void PrepareFileConversions()
	{
		if (DataContext is not ShellViewModel { FileConversionsNav.ViewModel: FileConversionsViewModel conversions })
		{
			return;
		}

		Prepare(new FileConversionsView { DataContext = conversions });
		Dispatcher.InvokeAsync(() => Prepare(new DatToGeojsonView { DataContext = conversions.Conversions[0] }), DispatcherPriority.ApplicationIdle);

		void Prepare(FrameworkElement view)
		{
			try
			{
				view.Measure(new Size(Math.Max(ActualWidth, 800), Math.Max(ActualHeight, 600)));
				view.Arrange(new Rect(view.DesiredSize));
			}
			catch (Exception ex)
			{
				// Only a head start: the screen is built again when it is opened.
				AppLog.Warning("Shell", $"Could not build {view.GetType().Name} ahead of its first open: {ex.Message}");
			}
		}
	}

	/// <inheritdoc />
	protected override void OnClosing(CancelEventArgs e)
	{
		base.OnClosing(e);

		// Once nothing has kept the window open, while its handle is still there to ask.
		if (!e.Cancel)
		{
			MainWindowPlacement.Save(this);
		}
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
