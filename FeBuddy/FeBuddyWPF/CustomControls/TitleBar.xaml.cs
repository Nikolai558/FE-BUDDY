using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
