using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeBuddyWPF.Contracts.Activation;
public interface IActivationHandler
{
    bool CanHandle();

    Task HandleAsync();
}
