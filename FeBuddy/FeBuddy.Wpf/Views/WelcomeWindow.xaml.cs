using System.Windows;

using FeBuddy.Wpf.Controls;
using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// The one-time welcome to FE-Buddy 3, shown the first time it opens on this PC. Shows a
/// <see cref="WelcomeViewModel"/> and closes itself when it asks.
/// </summary>
public partial class WelcomeWindow : ChromeWindow
{
	/// <summary>Creates the window for <paramref name="viewModel"/>.</summary>
	/// <param name="viewModel">The welcome to show.</param>
	public WelcomeWindow(WelcomeViewModel viewModel)
	{
		ArgumentNullException.ThrowIfNull(viewModel);

		InitializeComponent();
		DataContext = viewModel;
		viewModel.CloseRequested += (_, _) => Close();
	}

	/// <summary>Shows the welcome modally.</summary>
	/// <param name="owner">The window to centre on.</param>
	/// <returns><see langword="true"/> when the user chose to go to Info.</returns>
	public static bool Show(Window? owner)
	{
		WelcomeViewModel viewModel = new();
		new WelcomeWindow(viewModel) { Owner = owner }.ShowDialog();
		return viewModel.GoToInfo;
	}
}
