namespace FeBuddy.Wpf.ViewModels;

/// <summary>Release channel the user opts into for updates.</summary>
public enum UpdateChannel
{
    Stable,
    Beta,
    Alpha,
}

/// <summary>Whether the app clips NASR data to a region of interest.</summary>
public enum RoiMode
{
    /// <summary>Set up and use a bounding box.</summary>
    Custom,

    /// <summary>Pull the full NASR dataset, no clipping.</summary>
    AllData,
}

/// <summary>Coordinate decimal places written to GeoJSON.</summary>
public enum CoordPrecision
{
    Five,
    Six,
    Seven,
}

/// <summary>Where the NASR subscription data comes from.</summary>
public enum NasrSource
{
    Faa,
    CustomUrl,
    LocalFile,
}

/// <summary>Visual state for a systems-health / status row.</summary>
public enum StatusKind
{
    Ok,
    Pending,
    Idle,
    Warn,
    Down,
}
