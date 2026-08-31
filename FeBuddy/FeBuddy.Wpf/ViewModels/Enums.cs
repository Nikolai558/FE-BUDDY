namespace FeBuddy.Wpf.ViewModels;

/// <summary>How the airway GeoJSON generator should split its output files.</summary>
public enum AirwayOutputMode
{
    /// <summary>Do not generate GeoJSON from airway data.</summary>
    None,

    /// <summary>Airways_High / Airways_Low / Airways_Other by max authorised altitude.</summary>
    HighLow,

    /// <summary>One file per airway designation (Airways_J, Airways_V, ...).</summary>
    Designation,
}

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

/// <summary>External source format on the Conversions screen. vSTARS/vERAM/DXF retired in 3.0.</summary>
public enum ConvKind
{
    Dat,
    Kml,
    Sct2,
}

/// <summary>GeoJSON Tools screen mode.</summary>
public enum GjMode
{
    Validate,
    Cleanup,
}

/// <summary>Coordinate decimal places written to GeoJSON.</summary>
public enum CoordPrecision
{
    Five,
    Six,
    Seven,
}

/// <summary>Phase of the scripted "generate GeoJSON" run on the AIRAC screen.</summary>
public enum GenPhase
{
    Idle,
    Running,
    Complete,
    Cancelled,
}

/// <summary>Visual state for a <see cref="Models.StatusRow"/> or a systems-health row.</summary>
public enum StatusKind
{
    Ok,
    Pending,
    Idle,
    Warn,
    Down,
}
