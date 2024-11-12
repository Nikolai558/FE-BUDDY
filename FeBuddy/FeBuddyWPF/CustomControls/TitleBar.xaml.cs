using System.Windows;
using System.Windows.Controls;

namespace FeBuddyWPF.CustomControls
{
    /// <summary>
    /// Interaction logic for TitleBar.xaml
    /// </summary>
    public partial class TitleBar : UserControl
    {
        public TitleBar()
        {
            InitializeComponent();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
