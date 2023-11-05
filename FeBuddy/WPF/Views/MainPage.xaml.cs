using System.Windows.Controls;

using WPF_UI.ViewModels;

namespace WPF_UI.Views;

public partial class MainPage : Page
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
