using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeBuddyWPF.Contracts.Services
{
    public interface IDialogService
    {
        void ShowMessage(string message, string title = "Info");
    }
}
