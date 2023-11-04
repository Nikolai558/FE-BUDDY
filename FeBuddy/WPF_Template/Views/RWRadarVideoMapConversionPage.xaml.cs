using System.Windows.Controls;

using WPF_Template.ViewModels;

namespace WPF_Template.Views;

public partial class RWRadarVideoMapConversionPage : Page
{
    public RWRadarVideoMapConversionPage(RWRadarVideoMapConversionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
