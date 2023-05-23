using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPF.BaseClasses;
using WPF.Windows.StartingWindow.ViewModel;

namespace WPF.Windows.MainWindow.ViewModel
{
    /// <summary>
    /// Main Window for FE Buddy. This will hold ALL Sub Windows / Views
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        // ------- Private Properties ------- 
        private ViewModelBase _currentWindowView;

        // ------- Public Properties ------- 
        public ViewModelBase CurrentWindowView
        {
            get { return _currentWindowView; }
            set { _currentWindowView = value; OnPropertyChanged(nameof(CurrentWindowView)); }
        }

        public ICommand ShowStartingWindow { get; }
        public ICommand ShowSettingsWindow { get; }
        public ICommand ShowProgramMsgWindow { get; }
        public ICommand ShowMainMenuWindow { get; }

        // ------- Constructor  ------- 
        public MainWindowViewModel()
        {
            ShowStartingWindow = new ViewModelCommand(ExecuteShowStartingWindowCommand);
            ShowSettingsWindow = new ViewModelCommand(ExecuteShowSettingsWindowCommand);
            ShowProgramMsgWindow = new ViewModelCommand(ExecuteShowProgramMsgWindowCommand);
            ShowMainMenuWindow = new ViewModelCommand(ExecuteShowMainMenuWindowCommand);
            CurrentWindowView = new ProgStartingViewModel();

        }

        // ------- Functions to Call and Show Different Windows  ------- 
        private void ExecuteShowStartingWindowCommand(object obj)
        {
            CurrentWindowView = new ProgStartingViewModel();
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
