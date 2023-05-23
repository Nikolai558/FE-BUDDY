using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPF.BaseClasses;

namespace WPF.Windows.MainWindow.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ViewModelBase _currentWindowView;

        public ViewModelBase CurrentWindowView
        {
            get { return _currentWindowView; }
            set { _currentWindowView = value; OnPropertyChanged(nameof(CurrentWindowView)); }
        }

        public ICommand ShowStartingWindow { get; }
        public ICommand ShowSettingsWindow { get; }
        public ICommand ShowProgramMsgWindow { get; }
        public ICommand ShowMainMenuWindow { get; }

        public MainWindowViewModel()
        {
            ShowStartingWindow = new ViewModelCommand(ExecuteShowStartingWindowCommand);
            ShowSettingsWindow = new ViewModelCommand(ExecuteShowSettingsWindowCommand);
            ShowProgramMsgWindow = new ViewModelCommand(ExecuteShowProgramMsgWindowCommand);
            ShowMainMenuWindow = new ViewModelCommand(ExecuteShowMainMenuWindowCommand);
            //CurrentWindowView = new StartingWindowViewModel
        }

        private void ExecuteShowStartingWindowCommand(object obj)
        {
            //CurrentWindowView = new StartingWindowViewModel
            MessageBox.Show("This Menu has not been implemented yet.");
        }

        private void ExecuteShowSettingsWindowCommand(object obj)
        {
            //CurrentWindowView = new SettingsWindowViewModel
            MessageBox.Show("This Menu has not been implemented yet.");
        }

        private void ExecuteShowProgramMsgWindowCommand(object obj)
        {
            //CurrentWindowView = new ProgramMsgWindowViewModel
            MessageBox.Show("This Menu has not been implemented yet.");
        }

        private void ExecuteShowMainMenuWindowCommand(object obj)
        {
            //CurrentWindowView = new MainMenuWindowViewModel
            MessageBox.Show("This Menu has not been implemented yet.");
        }
    }
}
