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

/// <summary>One airway designation the user can include / exclude (J, Q, V, T, ...).</summary>
public sealed class DesignationChip(string code, bool enabled = true) : Infrastructure.ObservableObject
{
    private bool _enabled = enabled;

    public string Code { get; } = code;

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }
}

/// <summary>One line in the cycle-diff preview. Kind: Ok = added, Warn = changed, Down = removed.</summary>
public sealed record DiffRow(string Category, string Change, StatusKind Kind);

/// <summary>
/// A numbered display item - a BCG group or a filter. Used both by the map's
/// display visualiser (toggle <see cref="Visible"/>) and by Settings' scheme
/// editor (edit <see cref="Name"/>).
/// </summary>
public sealed class DisplayItem(int number, string name, bool visible = true) : Infrastructure.ObservableObject
{
    private string _name = name;
    private bool _visible = visible;

    public int Number { get; } = number;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public bool Visible
    {
        get => _visible;
        set => SetProperty(ref _visible, value);
    }
}

/// <summary>
/// One CRC ERAM default block (line / symbol / text). Not every field applies to
/// every kind; the editor shows the relevant ones. All sample data.
/// </summary>
public sealed class EramDefault : Infrastructure.ObservableObject
{
    private string _bcg = "8";
    private string _filters = "1";
    private string _style = "Solid";
    private string _thickness = "1";
    private string _size = "1";
    private bool _underline;
    private string _xOffset = "0";
    private string _yOffset = "0";

    public string Bcg { get => _bcg; set => SetProperty(ref _bcg, value); }
    public string Filters { get => _filters; set => SetProperty(ref _filters, value); }
    public string Style { get => _style; set => SetProperty(ref _style, value); }
    public string Thickness { get => _thickness; set => SetProperty(ref _thickness, value); }
    public string Size { get => _size; set => SetProperty(ref _size, value); }
    public bool Underline { get => _underline; set => SetProperty(ref _underline, value); }
    public string XOffset { get => _xOffset; set => SetProperty(ref _xOffset, value); }
    public string YOffset { get => _yOffset; set => SetProperty(ref _yOffset, value); }
}
