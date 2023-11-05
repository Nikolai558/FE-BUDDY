// This namespace contains the service contracts related to the FeBuddyWPF application.
namespace FeBuddyWPF.Contracts.Services
{
    // This interface defines the contract for a service that handles data restoration and persistence.
    public interface IPersistAndRestoreService
    {
        // Restores previously persisted data.
        void RestoreData();

        // Persists current data for future restoration.
        void PersistData();
    }
}