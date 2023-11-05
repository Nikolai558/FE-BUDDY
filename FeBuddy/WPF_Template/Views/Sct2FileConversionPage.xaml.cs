using System.Windows.Controls;

using WPF_Template.ViewModels;

namespace WPF_Template.Views;

public partial class Sct2FileConversionPage : Page
{
    public Sct2FileConversionPage(Sct2FileConversionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
