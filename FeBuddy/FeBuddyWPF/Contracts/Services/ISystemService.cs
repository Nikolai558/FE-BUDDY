// This namespace contains the service contracts related to the FeBuddyWPF application.
namespace FeBuddyWPF.Contracts.Services
{
    // This interface defines the contract for a system service.
    public interface ISystemService
    {
        // Opens the specified URL in the default web browser.
        void OpenInWebBrowser(string url);
    }
}
