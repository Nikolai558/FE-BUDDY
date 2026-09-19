using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// How a tab is presented in the service tab rail.
/// </summary>
public enum ServiceTabStatus
{
    /// <summary>Saved, complete and valid - the rail shows it plain.</summary>
    Ok,

    /// <summary>The user changed something and has not saved it - the rail warns (amber).</summary>
    Unsaved,

    /// <summary>A required value is missing or a value is invalid - the rail alerts (red).</summary>
    Invalid,
}

/// <summary>
/// One tab inside a first-tier service screen (see <see cref="TabbedServiceViewModel"/>): the
/// General tab, one tab per selected sub-service, and the Review tab at the end.
/// </summary>
/// <remarks>
/// The base owns the two things every tab needs and the rail reads: the dirty flag and the
/// validation state, collapsed into <see cref="Status"/>. Validation is continuous rather than
/// save-time, so a tab turns amber the moment the user edits it and red the moment a value goes
/// missing or invalid - which is what makes the rail usable as an at-a-glance checklist when a
/// service has twenty sub-services open.
/// </remarks>
public abstract class ServiceTabViewModel : ObservableObject
{
    private bool _isDirty;
    private string? _validationError;
    private ServiceTabStatus _status = ServiceTabStatus.Ok;

    /// <summary>The tab's label in the rail, e.g. <c>General</c> or <c>Airways</c>.</summary>
    public abstract string Title { get; }

    /// <summary>
    /// Per-field validation messages, keyed by the field key a view passes to
    /// <c>FieldState.Error</c>. Bind as <c>{Binding FieldErrors[SwLat]}</c>.
    /// </summary>
    public ServiceFieldErrors FieldErrors { get; } = new();

    /// <summary>
    /// <see langword="false"/> for a tab the run cannot act on - a placeholder for a sub-service
    /// whose backend does not exist yet. Such a tab is never counted as blocking a run.
    /// </summary>
    public virtual bool IsRunnable => true;

    /// <summary><see langword="true"/> when the tab has edits the user has not saved.</summary>
    public bool IsDirty
    {
        get => _isDirty;
        protected set
        {
            if (SetProperty(ref _isDirty, value))
            {
                UpdateStatus();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>The first validation failure on this tab, or <see langword="null"/> when valid.</summary>
    public string? ValidationError
    {
        get => _validationError;
        private set
        {
            if (SetProperty(ref _validationError, value))
            {
                OnPropertyChanged(nameof(HasValidationError));
                UpdateStatus();
            }
        }
    }

    /// <summary>Whether <see cref="ValidationError"/> is set.</summary>
    public bool HasValidationError => ValidationError is not null;

    /// <summary>How the rail should present this tab. Derived from dirty + validation state.</summary>
    public ServiceTabStatus Status
    {
        get => _status;
        private set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(NeedsAttention));
            }
        }
    }

    /// <summary>Whether the rail should draw this tab in a warning colour.</summary>
    public bool NeedsAttention => Status != ServiceTabStatus.Ok;

    /// <summary>
    /// The tab's contribution to the Review tab: its settings as plain label / value rows.
    /// </summary>
    /// <returns>Zero or more sections, in display order.</returns>
    public abstract IReadOnlyList<ServiceReviewSection> BuildReviewSummary();

    /// <summary>
    /// Persists this tab. The base does nothing and reports success; a settings tab overrides it
    /// (see <see cref="SubServiceSettingsViewModel"/>).
    /// </summary>
    /// <returns><see langword="true"/> when the tab is saved (or has nothing to save).</returns>
    public virtual bool Save() => true;

    /// <summary>
    /// Re-runs validation and recomputes <see cref="Status"/>. Called automatically by
    /// <see cref="MarkDirty"/>; call it directly after loading values from config.
    /// </summary>
    public void Revalidate()
    {
        ServiceValidation validation = new();
        Validate(validation);

        FieldErrors.Replace(validation.FieldErrors);
        ValidationError = validation.Messages.FirstOrDefault();
        UpdateStatus();
    }

    /// <summary>
    /// Collects this tab's validation failures. Add a plain message for a tab-level problem, or
    /// a field message so the offending box highlights. The base reports nothing.
    /// </summary>
    /// <param name="validation">The collector to add failures to.</param>
    protected virtual void Validate(ServiceValidation validation)
    {
    }

    /// <summary>Marks the tab dirty and re-validates. Call from every bound setting's setter.</summary>
    protected void MarkDirty()
    {
        IsDirty = true;
        Revalidate();
    }

    /// <summary>Clears the dirty flag (after a successful save or a reload) and re-validates.</summary>
    protected void ClearDirty()
    {
        IsDirty = false;
        Revalidate();
    }

    private void UpdateStatus() =>
        Status = HasValidationError || FieldErrors.Any
            ? ServiceTabStatus.Invalid
            : IsDirty
                ? ServiceTabStatus.Unsaved
                : ServiceTabStatus.Ok;
}

/// <summary>
/// Collects a tab's validation failures during <see cref="ServiceTabViewModel.Revalidate"/>.
/// </summary>
public sealed class ServiceValidation
{
    private readonly List<string> _messages = new();
    private readonly Dictionary<string, string> _fieldErrors = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Every failure message, in the order they were added.</summary>
    public IReadOnlyList<string> Messages => _messages;

    /// <summary>Failures that belong to a specific input, keyed by field key.</summary>
    public IReadOnlyDictionary<string, string> FieldErrors => _fieldErrors;

    /// <summary>Whether anything failed.</summary>
    public bool HasErrors => _messages.Count > 0;

    /// <summary>Records a tab-level failure with no single input to blame.</summary>
    /// <param name="message">The message shown above the tab's content.</param>
    public void Add(string message) => _messages.Add(message);

    /// <summary>
    /// Records a failure against one input, so that box highlights and carries the message as its
    /// tool-tip. The first failure recorded for a key wins.
    /// </summary>
    /// <param name="fieldKey">The key the view passes to <c>FieldState.Error</c>, e.g. <c>SwLat</c>.</param>
    /// <param name="message">The message for that input.</param>
    public void AddField(string fieldKey, string message)
    {
        if (_fieldErrors.TryAdd(fieldKey, message))
        {
            _messages.Add(message);
        }
    }

    /// <summary>
    /// Records a failure against one input when <paramref name="value"/> is blank - the
    /// "required field with nothing in it" case.
    /// </summary>
    /// <param name="fieldKey">The field key.</param>
    /// <param name="value">The current value.</param>
    /// <param name="message">The message for that input.</param>
    /// <returns><see langword="true"/> when the value was present.</returns>
    public bool RequireValue(string fieldKey, string? value, string message)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        AddField(fieldKey, message);
        return false;
    }
}

/// <summary>
/// An indexable, bindable view over a tab's per-field validation messages.
/// </summary>
/// <remarks>
/// A plain dictionary cannot be bound to by key and re-read when it changes; this raises the
/// <c>Item[]</c> change so every <c>{Binding FieldErrors[Whatever]}</c> refreshes at once.
/// </remarks>
public sealed class ServiceFieldErrors : INotifyPropertyChanged
{
    private IReadOnlyDictionary<string, string> _map =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Whether any field currently has a validation message.</summary>
    public bool Any => _map.Count > 0;

    /// <summary>The message for a field key, or <see langword="null"/> when that field is fine.</summary>
    /// <param name="fieldKey">The field key.</param>
    public string? this[string fieldKey] =>
        fieldKey is not null && _map.TryGetValue(fieldKey, out string? message) ? message : null;

    /// <summary>Replaces the whole set and notifies every key binding.</summary>
    /// <param name="map">The new field-to-message map.</param>
    internal void Replace(IReadOnlyDictionary<string, string> map)
    {
        _map = map;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Any)));
    }
}

/// <summary>One label / value line on the Review tab.</summary>
/// <param name="Label">What the setting is called.</param>
/// <param name="Value">Its current value, already formatted for display.</param>
public sealed record ServiceReviewRow(string Label, string Value);

/// <summary>A titled group of <see cref="ServiceReviewRow"/> on the Review tab.</summary>
/// <param name="Title">The group heading, usually the tab's title.</param>
/// <param name="Rows">The rows, in display order.</param>
/// <param name="Note">Optional line under the heading, e.g. why a tab contributes nothing.</param>
public sealed record ServiceReviewSection(string Title, IReadOnlyList<ServiceReviewRow> Rows, string? Note = null);
