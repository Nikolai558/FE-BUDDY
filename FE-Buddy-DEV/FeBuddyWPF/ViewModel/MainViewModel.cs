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


            //ExecuteShowFaaDatToGeojsonViewCommand(null);
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

    }
}
