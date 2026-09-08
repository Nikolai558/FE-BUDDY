using System.Windows;

using FeBuddy.Wpf.Infrastructure;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// A small themed modal confirm dialog: a title, a message, and a Confirm / Cancel pair.
/// Reused for the "unsaved settings" prompt before a run (remediation plan 5.3 / 7.9) and
/// any other yes/no decision.
/// </summary>
public partial class ConfirmWindow : Window
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
    public static bool Show(Window? owner, string title, string message, string confirmText, string cancelText = "Cancel")
    {
        ConfirmViewModel vm = new(title, message, confirmText, cancelText);
        ConfirmWindow window = new(vm) { Owner = owner };
        window.ShowDialog();
        return vm.Confirmed;
    }

    /// <summary>View-model for <see cref="ConfirmWindow"/>.</summary>
    private sealed class ConfirmViewModel(string title, string message, string confirmText, string cancelText) : ObservableObject
    {
        public event EventHandler? CloseRequested;

        public string Title { get; } = title;

        public string Message { get; } = message;

        public string ConfirmText { get; } = confirmText;

        public string CancelText { get; } = cancelText;

        public bool Confirmed { get; private set; }

        public System.Windows.Input.ICommand ConfirmCommand => new RelayCommand(() =>
        {
            Confirmed = true;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        });

        public System.Windows.Input.ICommand CancelCommand => new RelayCommand(() =>
            CloseRequested?.Invoke(this, EventArgs.Empty));
    }
}
