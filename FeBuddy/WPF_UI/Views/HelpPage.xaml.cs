using System.Windows.Controls;

using WPF_UI.ViewModels;

namespace WPF_UI.Views;

public partial class HelpPage : Page
{
    public HelpPage(HelpViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
