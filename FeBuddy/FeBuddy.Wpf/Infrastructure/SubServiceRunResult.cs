using System.Windows.Input;

using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Services.General;

namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// A sub-service run's messages that share one heading - a severity such as <c>Warning</c>, or
/// a subject such as an airway ID - so the Review tab does not show one flat wall of text.
/// </summary>
/// <param name="Title">The heading: the severity or subject these messages share.</param>
/// <param name="Level">The highest severity among them, which orders the groups.</param>
/// <param name="Messages">The message texts, in the order the service emitted them.</param>
public sealed record SubServiceMessageGroup(string Title, LogLevel Level, IReadOnlyList<string> Messages)
{
    /// <summary>How many messages are in this group.</summary>
    public int Count => Messages.Count;
}

/// <summary>
/// One sub-service's block on the Review tab after a finished run: what it produced, and its
/// warnings and routine notices, each behind its own toggle.
/// </summary>
/// <remarks>
/// Errors and advisories (<see cref="ServiceMessage.IsAdvisory"/>) are left out: the Review tab
/// already lists them in its own ERRORS and ADVISORIES cards, for every sub-service at once.
/// The warnings and routine notices both start collapsed on every run, like the Dashboard
/// activity log - a run can report hundreds of them.
/// </remarks>
public sealed class SubServiceRunResult : ObservableObject
{
    private bool _isAttentionExpanded;
    private bool _isInfoExpanded;

    /// <summary>Builds a sub-service's block from its run messages.</summary>
    /// <param name="name">The sub-service's name, as the tab rail shows it.</param>
    /// <param name="summary">One line of what the run produced, e.g. its counts.</param>
    /// <param name="messages">Every message the sub-service emitted.</param>
    /// <param name="groupKey">
    /// The heading each message is grouped under, e.g. the airway it names; <see langword="null"/>
    /// groups by severity.
    /// </param>
    public SubServiceRunResult(
        string name,
        string summary,
        IEnumerable<ServiceMessage> messages,
        Func<ServiceMessage, string>? groupKey = null)
    {
        Name = name;
        Summary = summary;

        ServiceMessage[] shown = messages
            .Where(m => !m.IsAdvisory && m.Level != LogLevel.Error)
            .ToArray();

        Func<ServiceMessage, string> key = groupKey ?? (m => m.Level.ToString());

        Attention = Group(shown.Where(m => m.Level >= LogLevel.Warning), key);
        Info = Group(shown.Where(m => m.Level < LogLevel.Warning), key);
        AttentionMessageCount = Attention.Sum(g => g.Count);
        InfoMessageCount = Info.Sum(g => g.Count);

        ToggleAttentionCommand = new RelayCommand(() => IsAttentionExpanded = !IsAttentionExpanded);
        ToggleInfoCommand = new RelayCommand(() => IsInfoExpanded = !IsInfoExpanded);
    }

    /// <summary>The sub-service's name.</summary>
    public string Name { get; }

    /// <summary>One line of what the run produced.</summary>
    public string Summary { get; }

    /// <summary>The warnings, grouped; shown behind <see cref="ToggleAttentionCommand"/>.</summary>
    public IReadOnlyList<SubServiceMessageGroup> Attention { get; }

    /// <summary>Whether there is any warning to show.</summary>
    public bool HasAttention => Attention.Count > 0;

    /// <summary>How many warnings there are, across every group.</summary>
    public int AttentionMessageCount { get; }

    /// <summary>Whether the warnings are expanded.</summary>
    public bool IsAttentionExpanded
    {
        get => _isAttentionExpanded;
        set { if (SetProperty(ref _isAttentionExpanded, value)) OnPropertyChanged(nameof(AttentionToggleLabel)); }
    }

    /// <summary>The label on the warnings toggle.</summary>
    public string AttentionToggleLabel => IsAttentionExpanded
        ? "Hide warnings"
        : $"Show {AttentionMessageCount} warning(s)";

    /// <summary>Expands or collapses the warnings.</summary>
    public ICommand ToggleAttentionCommand { get; }

    /// <summary>The routine notices, grouped; shown behind <see cref="ToggleInfoCommand"/>.</summary>
    public IReadOnlyList<SubServiceMessageGroup> Info { get; }

    /// <summary>Whether there is any routine notice to show.</summary>
    public bool HasInfo => Info.Count > 0;

    /// <summary>How many routine notices there are, across every group.</summary>
    public int InfoMessageCount { get; }

    /// <summary>Whether the routine notices are expanded.</summary>
    public bool IsInfoExpanded
    {
        get => _isInfoExpanded;
        set { if (SetProperty(ref _isInfoExpanded, value)) OnPropertyChanged(nameof(InfoToggleLabel)); }
    }

    /// <summary>The label on the routine-notices toggle.</summary>
    public string InfoToggleLabel => IsInfoExpanded
        ? "Hide routine messages"
        : $"Show {InfoMessageCount} routine message(s)";

    /// <summary>Expands or collapses the routine notices.</summary>
    public ICommand ToggleInfoCommand { get; }

    private static IReadOnlyList<SubServiceMessageGroup> Group(
        IEnumerable<ServiceMessage> messages,
        Func<ServiceMessage, string> key) =>
        messages
            .GroupBy(key)
            .Select(g => new SubServiceMessageGroup(g.Key, g.Max(m => m.Level), g.Select(m => m.Text).ToArray()))
            .OrderByDescending(g => g.Level)
            .ThenBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
