using System.Windows.Controls;

// This namespace contains the view contracts related to the FeBuddyWPF application.
namespace FeBuddyWPF.Contracts.Views
{
    // This interface defines the contract for the shell window.
    public interface IShellWindow
    {
        // Retrieves the navigation frame of the shell window.
        Frame GetNavigationFrame();

        // Shows the shell window.
        void ShowWindow();

        // Closes the shell window.
        void CloseWindow();
    }
}
