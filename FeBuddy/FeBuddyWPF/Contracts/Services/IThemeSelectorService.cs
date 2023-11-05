using FeBuddyWPF.Models;

// This namespace contains the service contracts related to the FeBuddyWPF application.
namespace FeBuddyWPF.Contracts.Services
{
    // This interface defines the contract for a theme selector service.
    public interface IThemeSelectorService
    {
        // Initializes the theme.
        void InitializeTheme();

        // Sets the specified theme.
        void SetTheme(AppTheme theme);

        // Retrieves the currently set theme.
        AppTheme GetCurrentTheme();
    }
}
