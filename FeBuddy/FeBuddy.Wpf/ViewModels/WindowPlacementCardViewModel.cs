using System.Windows;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Settings ▸ Main Window: whether FE-Buddy reopens where it was closed, and a button that puts it
/// back at its default. Both take effect at once and take no part in the page's Save - they are this
/// PC's settings (<see cref="MainWindowPlacement"/>), not the profile's.
/// </summary>
public sealed class WindowPlacementCardViewModel : ObservableObject
{
	private readonly Action _moveToDefault;
	private bool _rememberPlacement;

	/// <summary>Creates the card, showing whether remembering is on.</summary>
	/// <param name="moveToDefault">
	/// Moves the main window to its default at once; the main window's own, unless a test stands in.
	/// </param>
	public WindowPlacementCardViewModel(Action? moveToDefault = null)
	{
		_moveToDefault = moveToDefault ?? MoveMainWindowToDefault;
		_rememberPlacement = MainWindowPlacement.IsRemembered;
		ResetCommand = new RelayCommand(Reset);
	}

	/// <summary>
	/// Whether FE-Buddy reopens where it was closed. Saved at once; turned off, FE-Buddy forgets where
	/// it was and opens at its default.
	/// </summary>
	public bool RememberPlacement
	{
		get => _rememberPlacement;
		set
		{
			if (SetProperty(ref _rememberPlacement, value))
			{
				MainWindowPlacement.SetRemembered(value);
			}
		}
	}

	/// <summary>Forgets where the window was and moves it to its default at once.</summary>
	public ICommand ResetCommand { get; }

	private void Reset()
	{
		MainWindowPlacement.Forget();
		_moveToDefault();
	}

	private static void MoveMainWindowToDefault()
	{
		if (Application.Current?.MainWindow is { } window)
		{
			MainWindowPlacement.MoveToDefault(window);
		}
	}
}
