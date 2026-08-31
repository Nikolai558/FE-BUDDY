namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>A generated output file, shown in "recent output" lists. Display-only.</summary>
public sealed record OutputFile(string Name, string Size, string When);

/// <summary>A single line in a status checklist (e.g. generator readiness).</summary>
public sealed record StatusRow(string Label, string Detail, StatusKind Kind);

/// <summary>One endpoint in the nav's systems-health popover. Sample data.</summary>
public sealed record HealthRow(string Name, string Detail, StatusKind Kind);

/// <summary>One step in the scripted generation run (AIRAC screen).</summary>
public sealed class RunStep(string label) : Infrastructure.ObservableObject
{
    private StatusKind _state = StatusKind.Idle;

    public string Label { get; } = label;

    /// <summary>Idle = not started, Pending = running, Ok = done.</summary>
    public StatusKind State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }
}

/// <summary>A resource card on the Info screen.</summary>
public sealed record InfoLink(string Glyph, string Title, string Blurb, string Action);

/// <summary>One toggleable output family in the AIRAC cycle build. Sample data.</summary>
public sealed class OutputFamily(string glyph, string name, string description, string filesLabel, bool enabled = true)
    : Infrastructure.ObservableObject
{
    private bool _enabled = enabled;

    public string Glyph { get; } = glyph;
    public string Name { get; } = name;
    public string Description { get; } = description;

    /// <summary>e.g. "2 GeoJSON" / "1 alias".</summary>
    public string FilesLabel { get; } = filesLabel;

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }
}

/// <summary>A file queued for conversion. Sample data.</summary>
public sealed record ConvFile(string Name, string Size);

/// <summary>Severity of a GeoJSON validator finding.</summary>
public enum FindingSeverity { Error, Warn, Info }

/// <summary>One line in the GeoJSON validator report. Sample data.</summary>
public sealed record Finding(FindingSeverity Severity, string Message, string Where);

/// <summary>One toggleable clean-up operation. Sample data.</summary>
public sealed class CleanupOp(string label, string detail, bool enabled = true)
    : Infrastructure.ObservableObject
{
    private bool _enabled = enabled;

    public string Label { get; } = label;
    public string Detail { get; } = detail;

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }
}

/// <summary>An alias / reference text file the cycle produces. Sample data.</summary>
public sealed record AliasArtifact(string FileName, string Description, string CountLabel, string Sample);

/// <summary>A duplicate alias command found across files. Sample data.</summary>
public sealed record DupCommand(string Command, string Files);
