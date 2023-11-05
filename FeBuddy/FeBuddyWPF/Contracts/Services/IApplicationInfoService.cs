using System;

// This namespace contains the service contracts related to the FeBuddyWPF application.
namespace FeBuddyWPF.Contracts.Services
{
    // This interface provides the contract for an application information service.
    public interface IApplicationInfoService
    {
        // Retrieves the version information of the application.
        Version GetVersion();
    }
}
