using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace WPFUI.ViewModels
{
    public class ShellViewModel : Conductor<Object>
    {
        private void ShowAiracDataViewCommand()
        {
            ActivateItemAsync(new AiracDataViewModel());
        }
    }
}
