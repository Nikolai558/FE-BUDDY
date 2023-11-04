using System.Windows.Controls;

using WPF_UI.ViewModels;

namespace WPF_UI.Views;

public partial class RWRadarVideoMapConversionPage : Page
{
    public RWRadarVideoMapConversionPage(RWRadarVideoMapConversionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
