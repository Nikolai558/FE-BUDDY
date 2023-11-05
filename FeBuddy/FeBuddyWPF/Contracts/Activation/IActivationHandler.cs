using System.Threading.Tasks;

// This namespace groups the classes related to activation in the FeBuddyWPF application.
namespace FeBuddyWPF.Contracts.Activation
{
    // This interface defines the contract for an activation handler.
    public interface IActivationHandler
    {
        // Determines whether the handler can handle the activation.
        bool CanHandle();

        // Handles the activation asynchronously.
        Task HandleAsync();
    }
}