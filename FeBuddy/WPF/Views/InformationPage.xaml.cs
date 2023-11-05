using System.Windows.Controls;

using WPF_UI.ViewModels;

namespace WPF_UI.Views;

public partial class InformationPage : Page
{
    public InformationPage(InformationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
