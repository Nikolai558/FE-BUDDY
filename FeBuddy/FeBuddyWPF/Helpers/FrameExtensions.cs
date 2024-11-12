using System.Windows.Controls;
using System.Windows;

// This namespace contains the helper classes related to the FeBuddyWPF application.
namespace FeBuddyWPF.Helpers
{
    // This static class provides extension methods for the Frame class.
    public static class FrameExtensions
    {
        // Retrieves the data context from the Frame's content if available.
        public static object GetDataContext(this Frame frame)
        {
            if (frame.Content is FrameworkElement element)
            {
                return element.DataContext;
            }

            return null;
        }

        // Clears the navigation history of the Frame.
        public static void CleanNavigation(this Frame frame)
        {
            while (frame.CanGoBack)
            {
                frame.RemoveBackEntry();
            }
        }
    }
}