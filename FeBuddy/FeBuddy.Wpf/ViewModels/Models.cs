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
