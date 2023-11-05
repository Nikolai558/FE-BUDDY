// This namespace contains the view model contracts related to the FeBuddyWPF application.
namespace FeBuddyWPF.Contracts.ViewModels
{
    // This interface defines the contract for view models that need to be aware of navigation events.
    public interface INavigationAware
    {
        // Called when the view model is navigated to with a parameter.
        void OnNavigatedTo(object parameter);

        // Called when the view model is navigated away from.
        void OnNavigatedFrom();
    }
}