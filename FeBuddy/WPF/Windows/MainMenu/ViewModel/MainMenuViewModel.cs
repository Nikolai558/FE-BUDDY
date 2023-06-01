using System.Windows;
using System.Windows.Input;
using WPF.BaseClasses;

namespace WPF.Windows.MainMenu.ViewModel
{
  /// <summary>
  /// Main Menu for FE Buddy that has all the features offered by FE Buddy
  /// </summary>
  public class MainMenuViewModel : ViewModelBase
  {
    // ------- Private Properties ------- 
    private ViewModelBase _currentChildView;

    // ------- Public Properties ------- 
    public ViewModelBase CurrentChildView
    {
      get { return _currentChildView; }
      set { _currentChildView = value; OnPropertyChanged(nameof(CurrentChildView)); }
    }

    public ICommand ShowFaaDatToGeojsonViewCommand { get; }
    public ICommand ShowAiracDataViewCommand { get; }
    public ICommand ShowFaaGeojsonViewCommand { get; }
    public ICommand ShowVstarsVeramGeojsonViewCommand { get; }
    public ICommand ShowSctGeojsonViewCommand { get; }
    public ICommand ShowAliasMaintenanceViewCommand { get; }
    public ICommand ShowMemberActivityTrackingViewCommand { get; }
    public ICommand ShowAirportTrafficLevelsViewCommand { get; }

    // ------- Constructor  ------- 
    public MainMenuViewModel()
    {
      ShowFaaDatToGeojsonViewCommand = new ViewModelCommand(ExecuteShowFaaDatToGeojsonViewCommand);
      ShowAiracDataViewCommand = new ViewModelCommand(ExecuteShowAiracDataViewCommand);
      ShowFaaGeojsonViewCommand = new ViewModelCommand(ExecuteShowFaaGeojsonViewCommand);
      ShowVstarsVeramGeojsonViewCommand = new ViewModelCommand(ExecuteShowVstarsVeramGeojsonViewCommand);
      ShowSctGeojsonViewCommand = new ViewModelCommand(ExecuteShowSctGeojsonViewCommand);
      ShowAliasMaintenanceViewCommand = new ViewModelCommand(ExecuteShowAliasMaintenanceViewCommand);
      ShowMemberActivityTrackingViewCommand = new ViewModelCommand(ExecuteShowMemberActivityTrackingViewCommand);
      ShowAirportTrafficLevelsViewCommand = new ViewModelCommand(ExecuteShowAirportTrafficLevelsViewCommand);
    }

    // ------- Functions to Call and Show Different Child views (I.E Features of FE Buddy)  ------- 
    private void ExecuteShowAirportTrafficLevelsViewCommand(object obj)
    {
      MessageBox.Show("This Menu has not been implemented yet.");
      //throw new NotImplementedException();
    }

    private void ExecuteShowMemberActivityTrackingViewCommand(object obj)
    {
      MessageBox.Show("This Menu has not been implemented yet.");
      //throw new NotImplementedException();
    }

    private void ExecuteShowAliasMaintenanceViewCommand(object obj)
    {
      MessageBox.Show("This Menu has not been implemented yet.");
      //throw new NotImplementedException();
    }

    private void ExecuteShowSctGeojsonViewCommand(object obj)
    {
      MessageBox.Show("This Menu has not been implemented yet.");
      //throw new NotImplementedException();
    }

    private void ExecuteShowVstarsVeramGeojsonViewCommand(object obj)
    {
      MessageBox.Show("This Menu has not been implemented yet.");
      //throw new NotImplementedException();
    }

    private void ExecuteShowFaaGeojsonViewCommand(object obj)
    {
      MessageBox.Show("This Menu has not been implemented yet.");
      //throw new NotImplementedException();
    }

    private void ExecuteShowAiracDataViewCommand(object obj)
    {
      MessageBox.Show("This Menu has not been implemented yet.");
      //throw new NotImplementedException();
    }

    private void ExecuteShowFaaDatToGeojsonViewCommand(object obj)
    {
      MessageBox.Show("This Menu has not been implemented yet.");
      //throw new NotImplementedException();
    }
  }
}
