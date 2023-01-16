using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FeBuddyWPF.ViewModel
{
    public class MainViewModel: ViewModelBase
    {
        private ViewModelBase _currentChildView;

        public MainViewModel()
        {
            ShowFaaDatToGeojsonViewCommand = new ViewModelCommand(ExecuteShowFaaDatToGeojsonViewCommand);
            ShowAiracDataViewCommand = new ViewModelCommand(ExecuteShowAiracDataViewCommand);
            ShowFaaGeojsonViewCommand = new ViewModelCommand(ExecuteShowFaaGeojsonViewCommand);
            ShowVstarsVeramGeojsonViewCommand = new ViewModelCommand(ExecuteShowVstarsVeramGeojsonViewCommand);
            ShowSctGeojsonViewCommand = new ViewModelCommand(ExecuteShowSctGeojsonViewCommand);
            ShowAliasMaintenanceViewCommand = new ViewModelCommand(ExecuteShowAliasMaintenanceViewCommand);
            ShowMemberActivityTrackingViewCommand = new ViewModelCommand(ExecuteShowMemberActivityTrackingViewCommand);
            ShowAirportTrafficLevelsViewCommand = new ViewModelCommand(ExecuteShowAirportTrafficLevelsViewCommand);
            //ExecuteShowFaaDatToGeojsonViewCommand(null);
        }

        private void ExecuteShowAirportTrafficLevelsViewCommand(object obj)
        {
            throw new NotImplementedException();
        }

        private void ExecuteShowMemberActivityTrackingViewCommand(object obj)
        {
            throw new NotImplementedException();
        }

        private void ExecuteShowAliasMaintenanceViewCommand(object obj)
        {
            throw new NotImplementedException();
        }

        private void ExecuteShowSctGeojsonViewCommand(object obj)
        {
            throw new NotImplementedException();
        }

        private void ExecuteShowVstarsVeramGeojsonViewCommand(object obj)
        {
            throw new NotImplementedException();
        }

        private void ExecuteShowFaaGeojsonViewCommand(object obj)
        {
            throw new NotImplementedException();
        }

        private void ExecuteShowAiracDataViewCommand(object obj)
        {
            CurrentChildView = new AiracDataViewModel();
        }

        private void ExecuteShowFaaDatToGeojsonViewCommand(object obj)
        {
            CurrentChildView = new FaaDatToGeojsonViewModel();
        }

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

    }
}
