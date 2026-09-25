using System.Windows;
using System.Windows.Input;

using FeBuddy.Wpf.Controls;
using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Views.Models;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// A small themed modal confirm dialog: a title, a message, and a Confirm / Cancel pair - or,
/// through <see cref="ShowChoice"/>, a third choice between them. Used for the "unsaved settings"
/// prompt before a run or a tab change, the "this cycle has already been run" prompt, and any
/// other decision like them.
/// </summary>
public partial class ConfirmWindow : ChromeWindow
{
	private ConfirmWindow(ConfirmViewModel viewModel)
	{
		InitializeComponent();
		DataContext = viewModel;
		viewModel.CloseRequested += (_, _) => Close();
	}

	/// <summary>
	/// Shows the dialog modally and returns whether the user chose the confirm action.
	/// </summary>
	/// <param name="owner">The window to centre on.</param>
	/// <param name="title">The dialog title.</param>
	/// <param name="message">The body text.</param>
	/// <param name="confirmText">The confirm button label.</param>
	/// <param name="cancelText">The cancel button label.</param>
	/// <returns><see langword="true"/> if the user confirmed.</returns>
	public static bool Show(Window? owner, string title, string message, string confirmText, string cancelText = "Cancel") =>
		ShowDialog(owner, new ConfirmViewModel(title, message, confirmText, alternativeText: null, cancelText, confirmIsDefault: false))
			== ConfirmChoice.Confirm;

	/// <summary>
	/// Shows the dialog modally with a third choice, and returns the one the user picked. The
	/// confirm button is the default: Enter picks it.
	/// </summary>
	/// <param name="owner">The window to centre on.</param>
	/// <param name="title">The dialog title.</param>
	/// <param name="message">The body text.</param>
	/// <param name="confirmText">The confirm (primary, default) button label.</param>
	/// <param name="alternativeText">The alternative button label.</param>
	/// <param name="cancelText">The cancel button label.</param>
	/// <returns>The choice; <see cref="ConfirmChoice.Cancel"/> if the window was closed.</returns>
	public static ConfirmChoice ShowChoice(
		Window? owner,
		string title,
		string message,
		string confirmText,
		string alternativeText,
		string cancelText = "Cancel") =>
		ShowDialog(owner, new ConfirmViewModel(title, message, confirmText, alternativeText, cancelText, confirmIsDefault: true));

	private static ConfirmChoice ShowDialog(Window? owner, ConfirmViewModel viewModel)
	{
		ConfirmWindow window = new(viewModel) { Owner = owner };
		window.ShowDialog();
		return viewModel.Choice;
	}

	/// <summary>View-model for <see cref="ConfirmWindow"/>.</summary>
	private sealed class ConfirmViewModel : ObservableObject
	{
		public ConfirmViewModel(string title, string message, string confirmText, string? alternativeText, string cancelText, bool confirmIsDefault)
		{
			Title = title;
			Message = message;
			ConfirmText = confirmText;
			AlternativeText = alternativeText;
			CancelText = cancelText;
			ConfirmIsDefault = confirmIsDefault;

			ConfirmCommand = new RelayCommand(() => Close(ConfirmChoice.Confirm));
			AlternativeCommand = new RelayCommand(() => Close(ConfirmChoice.Alternative));
			CancelCommand = new RelayCommand(() => Close(ConfirmChoice.Cancel));
		}

		public event EventHandler? CloseRequested;

		public string Title { get; }

		public string Message { get; }

		public string ConfirmText { get; }

		public string? AlternativeText { get; }

		public bool HasAlternative => AlternativeText is not null;

		public string CancelText { get; }

		public bool ConfirmIsDefault { get; }

		public ConfirmChoice Choice { get; private set; }

		public ICommand ConfirmCommand { get; }

		public ICommand AlternativeCommand { get; }

		public ICommand CancelCommand { get; }

		private void Close(ConfirmChoice choice)
		{
			Choice = choice;
			CloseRequested?.Invoke(this, EventArgs.Empty);
		}
	}
}
