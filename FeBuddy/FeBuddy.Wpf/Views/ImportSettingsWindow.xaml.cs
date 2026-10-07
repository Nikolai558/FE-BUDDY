using System.Windows;

using FeBuddy.Wpf.Controls;
using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// The modal window Settings ▸ Import… asks where a file's settings go in. Shows an
/// <see cref="ImportSettingsViewModel"/> and closes itself when it asks; the view-model's
/// <see cref="ImportSettingsViewModel.Confirmed"/> says which button closed it.
/// </summary>
public partial class ImportSettingsWindow : ChromeWindow
{
	/// <summary>The height the rest of the window takes - title bar, padding, choices and buttons - with room to spare.</summary>
	private const double ReservedHeight = 480;

	/// <summary>The least the summary shrinks to, even on a very short screen.</summary>
	private const double MinimumBodyHeight = 100;

	/// <summary>Creates the window for <paramref name="viewModel"/>.</summary>
	/// <param name="viewModel">The choice to show.</param>
	public ImportSettingsWindow(ImportSettingsViewModel viewModel)
	{
		ArgumentNullException.ThrowIfNull(viewModel);

		InitializeComponent();
		DataContext = viewModel;
		viewModel.CloseRequested += (_, _) => Close();

		// A long summary scrolls rather than pushing the buttons off the bottom of the screen.
		Body.MaxHeight = Math.Max(MinimumBodyHeight, SystemParameters.WorkArea.Height - ReservedHeight);

		Loaded += (_, _) =>
		{
			NameBox.Focus();
			NameBox.SelectAll();
		};
	}

	/// <summary>Asks where the settings go.</summary>
	/// <param name="owner">The window to centre on.</param>
	/// <param name="viewModel">The choice.</param>
	/// <returns><see langword="true"/> when the user chose Import.</returns>
	public static bool Ask(Window? owner, ImportSettingsViewModel viewModel)
	{
		new ImportSettingsWindow(viewModel) { Owner = owner }.ShowDialog();
		return viewModel.Confirmed;
	}
}
