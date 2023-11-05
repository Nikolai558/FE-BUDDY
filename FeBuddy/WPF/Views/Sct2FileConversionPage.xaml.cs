using System.Windows.Controls;

using WPF_UI.ViewModels;

namespace WPF_UI.Views;

public partial class Sct2FileConversionPage : Page
{
    public Sct2FileConversionPage(Sct2FileConversionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
