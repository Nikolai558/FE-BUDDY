using System.Windows;

using FeBuddy.Wpf.Controls;
using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// The modal window that asks for a settings profile's name. Shows a <see cref="ProfileNameViewModel"/>
/// and closes itself when it asks; the name box has the focus, its text selected, from the start.
/// </summary>
public partial class ProfileNameWindow : ChromeWindow
{
	/// <summary>Creates the window for <paramref name="viewModel"/>.</summary>
	/// <param name="viewModel">The question to show.</param>
	public ProfileNameWindow(ProfileNameViewModel viewModel)
	{
		ArgumentNullException.ThrowIfNull(viewModel);

		InitializeComponent();
		DataContext = viewModel;
		viewModel.CloseRequested += (_, _) => Close();

		Loaded += (_, _) =>
		{
			NameBox.Focus();
			NameBox.SelectAll();
		};
	}

	/// <summary>Asks for the name.</summary>
	/// <param name="owner">The window to centre on.</param>
	/// <param name="viewModel">The question.</param>
	/// <returns><see langword="true"/> when the user went ahead; the name is <see cref="ProfileNameViewModel.TrimmedName"/>.</returns>
	public static bool Ask(Window? owner, ProfileNameViewModel viewModel)
	{
		new ProfileNameWindow(viewModel) { Owner = owner }.ShowDialog();
		return viewModel.Confirmed;
	}
}
