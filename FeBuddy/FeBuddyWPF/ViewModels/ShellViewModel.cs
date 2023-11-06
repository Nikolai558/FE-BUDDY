using FeBuddyWPF.Contracts.Services;
using FeBuddyWPF.MVVMTools.ComponentModel;
using FeBuddyWPF.MVVMTools.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace FeBuddyWPF.ViewModels
{
    public class ShellViewModel: ObservableObject
    {
        private readonly INavigationService _navigationService;
        private ICommand _loadedCommand;
        private ICommand _unloadedCommand;

        public ShellViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }
    }
}
