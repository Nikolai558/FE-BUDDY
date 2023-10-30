using System.Windows.Controls;

using MahApps.Metro.Controls;

using WPF_UI.Contracts.Views;
using WPF_UI.ViewModels;

namespace WPF_UI.Views;

public partial class ShellWindow : MetroWindow, IShellWindow
{
    public ShellWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public Frame GetNavigationFrame()
        => shellFrame;

    public void ShowWindow()
        => Show();

    public void CloseWindow()
        => Close();
}
