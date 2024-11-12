using System.Windows;
using System.Windows.Input;

namespace WPFUI.Views
{
    /// <summary>
    /// Interaction logic for ShellView.xaml
    /// </summary>
    public partial class ShellView : Window
    {
        public ShellView()
        {
            InitializeComponent();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CommandBinding_CanExecute_Close_Min_Max(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBinding_Executed_Close(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(Window.GetWindow(this));
        }

        private void CommandBinding_Executed_Maximized(object sender, ExecutedRoutedEventArgs e)
        {
            if (Window.GetWindow(this).WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(Window.GetWindow(this));
            }
            else
            {
                SystemCommands.MaximizeWindow(Window.GetWindow(this));
            }

        }

        private void CommandBinding_Executed_Minimized(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(Window.GetWindow(this));
        }


    }
}
