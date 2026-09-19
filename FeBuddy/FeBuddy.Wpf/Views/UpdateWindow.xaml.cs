using System.Windows;

using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// The modal update window (remediation plan 4.2). Closes itself when its view-model raises
/// <see cref="UpdateWindowViewModel.CloseRequested"/>.
/// </summary>
public partial class UpdateWindow : ChromeWindow
{
    /// <summary>Initializes the window and wires the view-model's close request.</summary>
    public UpdateWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is UpdateWindowViewModel oldVm)
        {
            oldVm.CloseRequested -= OnCloseRequested;
        }

        if (e.NewValue is UpdateWindowViewModel newVm)
        {
            newVm.CloseRequested += OnCloseRequested;
        }
    }

    private void OnCloseRequested(object? sender, System.EventArgs e) => Close();
}
