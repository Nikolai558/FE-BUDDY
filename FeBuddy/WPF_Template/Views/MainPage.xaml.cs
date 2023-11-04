using System.Windows.Controls;

using WPF_Template.ViewModels;

namespace WPF_Template.Views;

public partial class MainPage : Page
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
