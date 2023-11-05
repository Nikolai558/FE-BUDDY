using System;
using System.Windows.Controls;

// This namespace contains the service contracts related to navigation in the FeBuddyWPF application.
namespace FeBuddyWPF.Contracts.Services
{
    // This interface defines the contract for a navigation service.
    public interface INavigationService
    {
        // An event that is raised when navigation occurs, providing the page key as a string.
        event EventHandler<string> Navigated;

        // Indicates whether the service can navigate back to the previous page.
        bool CanGoBack { get; }

        // Initializes the navigation service with the specified shell frame.
        void Initialize(Frame shellFrame);

        // Navigates to a specified page key with an optional parameter and an option to clear the navigation history.
        bool NavigateTo(string pageKey, object parameter = null, bool clearNavigation = false);

        // Navigates back to the previous page.
        void GoBack();

        // Unsubscribes the navigation event.
        void UnsubscribeNavigation();

        // Clears the navigation history.
        void CleanNavigation();
    }
}
