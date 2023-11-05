using System;
using System.Windows.Controls;

// This namespace contains the service contracts related to the FeBuddyWPF application.
namespace FeBuddyWPF.Contracts.Services
{
    // This interface defines the contract for a page service.
    public interface IPageService
    {
        // Retrieves the type of the page associated with the specified key.
        Type GetPageType(string key);

        // Retrieves the page associated with the specified key.
        Page GetPage(string key);
    }
}
