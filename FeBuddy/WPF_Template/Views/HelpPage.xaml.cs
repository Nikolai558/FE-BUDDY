using System.Windows.Controls;

using WPF_Template.ViewModels;

namespace WPF_Template.Views;

public partial class HelpPage : Page
{
    public HelpPage(HelpViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
