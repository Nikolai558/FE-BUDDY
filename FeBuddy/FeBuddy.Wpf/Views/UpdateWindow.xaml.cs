using System.Windows;

using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// The modal update window (remediation plan 4.2). Closes itself when its view-model raises
/// <see cref="UpdateWindowViewModel.CloseRequested"/>, owns the "close FE-Buddy with unfinished
/// work?" prompt, and cancels a download in progress when it is closed.
/// </summary>
public partial class UpdateWindow : ChromeWindow
{
    /// <summary>Initializes the window and wires the view-model's close request.</summary>
    public UpdateWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Closed += (_, _) => (DataContext as UpdateWindowViewModel)?.CancelDownload();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is UpdateWindowViewModel oldVm)
        {
            oldVm.CloseRequested -= OnCloseRequested;
            oldVm.ConfirmCloseWithUnfinishedWork = null;
        }

        if (e.NewValue is UpdateWindowViewModel newVm)
        {
            newVm.CloseRequested += OnCloseRequested;
            newVm.ConfirmCloseWithUnfinishedWork = ConfirmCloseWithUnfinishedWork;
        }
    }

    private bool ConfirmCloseWithUnfinishedWork(IReadOnlyList<string> work) =>
        ConfirmWindow.Show(
            this,
            "Close FE-Buddy to update?",
            "FE-Buddy closes while the update installs, and this will be lost:\n\n"
                + string.Join("\n", work.Select(item => "•  " + item)),
            confirmText: "Update anyway");

    private void OnCloseRequested(object? sender, System.EventArgs e) => Close();
}
