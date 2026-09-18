using FeBuddy.Wpf.Infrastructure;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The AIRAC Service sub-service catalogue: every data topic the service can produce output for,
/// whether or not its backend exists yet.
/// </summary>
/// <remarks>
/// <para>
/// This list is the only place a sub-service is registered. Adding one means adding an entry here
/// and a tab view-model for it; the General tab's picker, the tab rail, the save contract and the
/// Review tab all pick it up with no further changes. The list is expected to grow to roughly
/// twenty entries.
/// </para>
/// <para>
/// <see cref="SubServiceDescriptor.Key"/> is persisted in <c>UserConfig</c> under
/// <c>Services.AiracService.SelectedSubServices</c>, so a key may not be renamed without migrating
/// that value. <see cref="SubServiceDescriptor.IsImplemented"/> is <see langword="false"/> for a
/// sub-service that has no library code behind it yet: its tab opens and explains itself, and it
/// contributes nothing to a run.
/// </para>
/// </remarks>
public static class AiracSubServices
{
    /// <summary>The Airways sub-service key, the one sub-service with a working backend today.</summary>
    public const string AirwaysKey = "Airways";

    /// <summary>Every sub-service, in the order the picker and the tab rail show them.</summary>
    public static IReadOnlyList<SubServiceDescriptor> All { get; } = new[]
    {
        new SubServiceDescriptor("Airports", "Airports", 10, false, () => new PlaceholderSubServiceViewModel("Airports")),
        new SubServiceDescriptor(AirwaysKey, "Airways", 20, true, () => new AirwaysViewModel()),
        new SubServiceDescriptor("Departures", "Departures", 30, false, () => new PlaceholderSubServiceViewModel("Departures")),
    };
}
