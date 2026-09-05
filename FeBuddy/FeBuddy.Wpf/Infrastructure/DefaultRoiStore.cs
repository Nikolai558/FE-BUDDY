using FeBuddy.Wpf.Map;

namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// A one-slot shared holder for the "default ROI", so the Map screen can push a
/// box it drew into the Settings screen without the two view-models knowing about
/// each other. Same no-DI pattern as <see cref="Toast"/>. UI-state only - nothing
/// is persisted.
/// </summary>
public static class DefaultRoiStore
{
    public static GeoPoint? SouthWest { get; private set; }

    public static GeoPoint? NorthEast { get; private set; }

    public static bool IsSet => SouthWest is not null && NorthEast is not null;

    /// <summary>Raised after <see cref="Set"/> so a live Settings view-model can refresh.</summary>
    public static event EventHandler? Changed;

    public static void Set(GeoPoint southWest, GeoPoint northEast)
    {
        SouthWest = southWest;
        NorthEast = northEast;
        Changed?.Invoke(null, EventArgs.Empty);
    }
}
