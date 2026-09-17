using System.Globalization;

using FEBuddyLibrary.Helpers;
using FEBuddyLibrary.Models.Services.General;

namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// The one saved default ROI (<c>Services.AiracService.DefaultRoi</c> in <c>UserConfig.json</c>),
/// read and written by both Settings and the Map page's inline editor.
/// </summary>
/// <remarks>
/// Each section's view-model is built once and kept alive for the rest of the session (see
/// <see cref="NavItem"/>), so a plain "read the file in the constructor" load - what both view
/// models did before this existed - goes stale the moment the *other* section changes it: set it
/// in Settings, switch to Map, and the old value (or none) is still showing. Routing every read
/// and write through here and raising <see cref="Changed"/> on every write means whichever
/// view-model didn't make the change still hears about it.
/// </remarks>
public static class DefaultRoiStore
{
    private const string Node = "Services.AiracService.DefaultRoi";
    private const string FilterKey = $"{Node}.FilterByRoi";
    private const string SwLatKey = $"{Node}.DefaultCoordindates.SwLat";
    private const string SwLonKey = $"{Node}.DefaultCoordindates.SwLon";
    private const string NeLatKey = $"{Node}.DefaultCoordindates.NeLat";
    private const string NeLonKey = $"{Node}.DefaultCoordindates.NeLon";

    /// <summary>Raised after <see cref="Set"/> or <see cref="Clear"/> writes to disk.</summary>
    public static event EventHandler? Changed;

    /// <summary>Reads the current default ROI, or <see langword="null"/> when none is set.</summary>
    public static RegionOfInterest? Load()
    {
        // Clear() only flips this flag - it leaves the last-drawn coordinates in place (in case
        // the user re-enables the ROI and wants them back), so they must not be trusted alone.
        if (!string.Equals(UserConfigFile.GetValue(FilterKey), "true", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (double.TryParse(UserConfigFile.GetValue(SwLatKey), NumberStyles.Float, CultureInfo.InvariantCulture, out double swLat)
            && double.TryParse(UserConfigFile.GetValue(SwLonKey), NumberStyles.Float, CultureInfo.InvariantCulture, out double swLon)
            && double.TryParse(UserConfigFile.GetValue(NeLatKey), NumberStyles.Float, CultureInfo.InvariantCulture, out double neLat)
            && double.TryParse(UserConfigFile.GetValue(NeLonKey), NumberStyles.Float, CultureInfo.InvariantCulture, out double neLon))
        {
            return new RegionOfInterest(swLat, swLon, neLat, neLon);
        }

        return null;
    }

    public static void Set(RegionOfInterest roi)
    {
        UserConfigFile.TrySetValue(FilterKey, "true");
        UserConfigFile.TrySetValue(SwLatKey, roi.SwLat.ToString(CultureInfo.InvariantCulture));
        UserConfigFile.TrySetValue(SwLonKey, roi.SwLon.ToString(CultureInfo.InvariantCulture));
        UserConfigFile.TrySetValue(NeLatKey, roi.NeLat.ToString(CultureInfo.InvariantCulture));
        UserConfigFile.TrySetValue(NeLonKey, roi.NeLon.ToString(CultureInfo.InvariantCulture));
        UserConfigFile.Save(Node);
        Changed?.Invoke(null, EventArgs.Empty);
    }

    public static void Clear()
    {
        UserConfigFile.TrySetValue(FilterKey, "false");
        UserConfigFile.Save(Node);
        Changed?.Invoke(null, EventArgs.Empty);
    }
}
