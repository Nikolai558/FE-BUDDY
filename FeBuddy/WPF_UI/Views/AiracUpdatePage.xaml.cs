using System.Windows.Controls;

using WPF_UI.ViewModels;

namespace WPF_UI.Views;

public partial class AiracUpdatePage : Page
{
    public AiracUpdatePage(AiracUpdateViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
