using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WPF.ViewModel;
class MainWindow: ViewModelBase
{
  // ------- Private Properties -------
  private ViewModelBase _currentView;

  // ------- Public Properties ------- 
  public ViewModelBase CurrentView 
  { 
    get { return _currentView; } 
    set { _currentView = value; OnPropertyChanged(nameof(CurrentView));} 
  }

  // ------- Commands -------
  public ICommand ShowStartingView { get; }

  // ------- Constructor  ------- 
  public MainWindow()
  {
    ShowStartingView = new ViewModelCommand(ShowStartingViewExecute);

    CurrentView = new StartingView.StartingViewModel();
  }

  // ------- Functions to Call and Show Different Windows  ------- 
  private void ShowStartingViewExecute(object obj)
  {
    CurrentView = new StartingView.StartingViewModel();
  }
}
