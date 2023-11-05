using System.Windows.Controls;

using WPF_Template.ViewModels;

namespace WPF_Template.Views;

public partial class AiracUpdatePage : Page
{
    public AiracUpdatePage(AiracUpdateViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
