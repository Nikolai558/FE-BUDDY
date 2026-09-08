using System.Windows.Input;

using FEBuddyLibrary.Helpers;

namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// Base class for every sub-service settings menu (remediation plan 5.3). Implements the
/// shared save contract: a dirty flag, <see cref="SaveCommand"/> that validates then writes
/// only this menu's own <c>UserConfig</c> subtree, and a one-step <see cref="UndoLastSaveCommand"/>.
/// </summary>
/// <remarks>
/// A derived menu:
/// <list type="bullet">
///   <item>returns its subtree path from <see cref="NodePath"/> (e.g. <c>Services.AiracService.Geojson.Airways</c>),</item>
///   <item>calls <see cref="LoadFromConfig"/> from its own constructor,</item>
///   <item>calls <see cref="MarkDirty"/> from every bound setting's setter,</item>
///   <item>implements <see cref="WriteToConfig"/> (push fields into <see cref="UserConfigFile"/> via <c>TrySetValue</c>) and, optionally, <see cref="Validate"/>.</item>
/// </list>
/// </remarks>
public abstract class SubServiceSettingsViewModel : ObservableObject
{
    private bool _isDirty;
    private string? _validationError;

    /// <summary>Wires the Save / Undo commands. Derived constructors should call <see cref="LoadFromConfig"/> after their fields exist.</summary>
    protected SubServiceSettingsViewModel()
    {
        SaveCommand = new RelayCommand(() => Save(), () => IsDirty);
        UndoLastSaveCommand = new RelayCommand(UndoLastSave, () => CanUndo);
    }

    /// <summary>Raised after a successful <see cref="Save"/>.</summary>
    public event EventHandler? Saved;

    /// <summary>The dotted <c>UserConfig</c> path this menu owns, e.g. <c>Services.AiracService.Geojson.Airways</c>.</summary>
    public abstract string NodePath { get; }

    /// <summary>The breadcrumb tail, shown as <c>AIRAC Service › {BreadcrumbTitle}</c>.</summary>
    public abstract string BreadcrumbTitle { get; }

    /// <summary><see langword="true"/> when the menu has unsaved edits.</summary>
    public bool IsDirty
    {
        get => _isDirty;
        protected set
        {
            if (SetProperty(ref _isDirty, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>The last validation failure message, or <see langword="null"/> when valid.</summary>
    public string? ValidationError
    {
        get => _validationError;
        private set => SetProperty(ref _validationError, value);
    }

    /// <summary>Whether <see cref="UndoLastSaveCommand"/> can restore a previous save of this node.</summary>
    public bool CanUndo => UserConfigFile.CanUndo(NodePath);

    /// <summary>Validates, then writes only this menu's <c>UserConfig</c> subtree.</summary>
    public ICommand SaveCommand { get; }

    /// <summary>Restores the one snapshotted previous save of this node (disabled once superseded).</summary>
    public ICommand UndoLastSaveCommand { get; }

    /// <summary>
    /// Validates and persists this menu's subtree. Returns <see langword="false"/> (and toasts
    /// the reason) when validation fails.
    /// </summary>
    /// <returns><see langword="true"/> if the settings were saved.</returns>
    public bool Save()
    {
        string? error = Validate();
        ValidationError = error;

        if (error is not null)
        {
            Toast.Warn("Cannot save", error);
            return false;
        }

        WriteToConfig();
        UserConfigFile.Save(NodePath);

        IsDirty = false;
        OnPropertyChanged(nameof(CanUndo));
        CommandManager.InvalidateRequerySuggested();
        Saved?.Invoke(this, EventArgs.Empty);
        Toast.Success("Saved", $"{BreadcrumbTitle} settings saved.");
        return true;
    }

    /// <summary>Loads this menu's fields from the in-memory <c>UserConfig</c> dictionary.</summary>
    protected abstract void LoadFromConfig();

    /// <summary>Pushes this menu's fields into the in-memory <c>UserConfig</c> dictionary (via <see cref="UserConfigFile.TrySetValue"/>).</summary>
    protected abstract void WriteToConfig();

    /// <summary>Validates the current field values. Returns <see langword="null"/> when valid, otherwise a message.</summary>
    protected virtual string? Validate() => null;

    /// <summary>Marks the menu dirty. Call from every bound setting's setter.</summary>
    protected void MarkDirty() => IsDirty = true;

    private void UndoLastSave()
    {
        if (!UserConfigFile.Undo(NodePath))
        {
            return;
        }

        LoadFromConfig();
        IsDirty = false;
        OnPropertyChanged(nameof(CanUndo));
        CommandManager.InvalidateRequerySuggested();
        Toast.Info("Reverted", $"{BreadcrumbTitle} settings reverted to the previous save.");
    }
}
