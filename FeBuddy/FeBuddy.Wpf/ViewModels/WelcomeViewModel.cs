using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The one-time welcome to FE-Buddy 3 (#351, <see cref="Core.Application.Launch.WelcomeNotice"/>):
/// points the user to Info before anything else, and to the Discord. Shown by
/// <see cref="Views.WelcomeWindow"/>, which closes when <see cref="CloseRequested"/> fires.
/// </summary>
public sealed class WelcomeViewModel : ObservableObject
{
	/// <summary>Builds the welcome; its Discord link opens in the browser.</summary>
	public WelcomeViewModel()
		: this(() => BrowserLauncher.Open(Links.Discord))
	{
	}

	/// <summary>Builds the welcome with its own way to open the Discord link. Unit tests.</summary>
	/// <param name="openDiscord">Opens the Discord invite.</param>
	internal WelcomeViewModel(Action openDiscord)
	{
		ArgumentNullException.ThrowIfNull(openDiscord);

		GoToInfoCommand = new RelayCommand(() => Close(goToInfo: true));
		CloseCommand = new RelayCommand(() => Close(goToInfo: false));
		OpenDiscordCommand = new RelayCommand(openDiscord);
	}

	/// <summary>Raised when the window should close.</summary>
	public event EventHandler? CloseRequested;

	/// <summary><see langword="true"/> once the user chose to go to Info.</summary>
	public bool GoToInfo { get; private set; }

	/// <summary>Closes the welcome and opens Info.</summary>
	public ICommand GoToInfoCommand { get; }

	/// <summary>Closes the welcome.</summary>
	public ICommand CloseCommand { get; }

	/// <summary>Opens the FE-Buddy Discord invite; the welcome stays open.</summary>
	public ICommand OpenDiscordCommand { get; }

	private void Close(bool goToInfo)
	{
		GoToInfo = goToInfo;
		CloseRequested?.Invoke(this, EventArgs.Empty);
	}
}
