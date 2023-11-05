using System.Windows.Controls;

using WPF_Template.ViewModels;

namespace WPF_Template.Views;

public partial class InformationPage : Page
{
    public InformationPage(InformationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
