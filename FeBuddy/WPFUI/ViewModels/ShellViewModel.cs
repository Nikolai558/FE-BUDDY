using Caliburn.Micro;
using System;

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
