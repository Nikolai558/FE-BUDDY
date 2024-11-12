using FeBuddyWPF.Contracts.Views;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FeBuddyWPF.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IShellWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public Frame GetNavigationFrame() => throw new NotImplementedException();

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        private void btnClose_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }
    }
}
