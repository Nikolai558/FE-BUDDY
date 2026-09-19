using System.Diagnostics;

namespace FeBuddy.Wpf.Infrastructure;

/// <summary>Opens a URL in the user's default browser, toasting on failure.</summary>
public static class BrowserLauncher
{
    /// <summary>Opens <paramref name="url"/> in the default browser.</summary>
    /// <param name="url">The absolute URL to open.</param>
    public static void Open(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Toast.Warn("Could not open the link", ex.Message);
        }
    }
}
