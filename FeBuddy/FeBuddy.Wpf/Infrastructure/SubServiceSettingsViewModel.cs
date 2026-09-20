using System.Windows.Input;

using FeBuddy.Core.Helpers;

namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// Base class for every sub-service settings tab. Implements the shared save contract on top of
/// <see cref="ServiceTabViewModel"/>: validate, then write only this menu's own <c>UserConfig</c>
/// subtree, with a one-step undo of the last save.
/// </summary>
/// <remarks>
/// A derived menu:
/// <list type="bullet">
///   <item>returns its subtree path from <see cref="NodePath"/> (e.g. <c>Services.AiracService.Geojson.Airways</c>),</item>
///   <item>returns its rail label from <see cref="ServiceTabViewModel.Title"/>,</item>
///   <item>calls <see cref="LoadFromConfig"/> from its own constructor,</item>
///   <item>calls <see cref="ServiceTabViewModel.MarkDirty"/> from every bound setting's setter,</item>
///   <item>implements <see cref="WriteToConfig"/> (push fields into <see cref="UserConfigFile"/> via <c>TrySetValue</c>)
///         and <see cref="ServiceTabViewModel.BuildReviewSummary"/>, and optionally
///         <see cref="ServiceTabViewModel.Validate"/>.</item>
/// </list>
/// <para>
/// Save and undo are also driven from the tab bar (<see cref="TabbedServiceViewModel"/>), which is
/// why <see cref="Save"/> and <see cref="UndoLastSave"/> are public as well as bound to the
/// commands here - a tab hosted on its own still works exactly the same way.
/// </para>
/// </remarks>
public abstract class SubServiceSettingsViewModel : ServiceTabViewModel
{
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

    /// <summary>Whether <see cref="UndoLastSaveCommand"/> can restore a previous save of this node.</summary>
    public bool CanUndo => UserConfigFile.CanUndo(NodePath);

    /// <summary>Validates, then writes only this menu's <c>UserConfig</c> subtree.</summary>
    public ICommand SaveCommand { get; }

    /// <summary>Restores the one snapshotted previous save of this node (disabled once superseded).</summary>
    public ICommand UndoLastSaveCommand { get; }

    /// <summary>
    /// Validates and persists this menu's subtree. Returns <see langword="false"/> (and toasts
    /// the reason) when validation fails; the offending inputs stay highlighted.
    /// </summary>
    /// <returns><see langword="true"/> if the settings were saved.</returns>
    public override bool Save()
    {
        Revalidate();

        if (ValidationError is { } error)
        {
            Toast.Warn("Cannot save", error);
            return false;
        }

        WriteToConfig();
        UserConfigFile.Save(NodePath);

        ClearDirty();
        OnPropertyChanged(nameof(CanUndo));
        CommandManager.InvalidateRequerySuggested();
        Saved?.Invoke(this, EventArgs.Empty);
        Toast.Success("Saved", $"{Title} settings saved.");
        return true;
    }

    /// <summary>Restores this node's previous save, if a snapshot exists.</summary>
    public void UndoLastSave()
    {
        if (!UserConfigFile.Undo(NodePath))
        {
            return;
        }

        LoadFromConfig();
        ClearDirty();
        OnPropertyChanged(nameof(CanUndo));
        CommandManager.InvalidateRequerySuggested();
        Toast.Info("Reverted", $"{Title} settings reverted to the previous save.");
    }

    /// <summary>
    /// How many of this menu's outputs are currently switched on (GeoJSON, the alias file, and
    /// so on). Overridden by a menu that has more than one output to offer.
    /// </summary>
    protected virtual int EnabledOutputCount => 1;

    /// <summary>
    /// Guards the last remaining output against being switched off.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the output may be switched off; <see langword="false"/> when
    /// it is the last one, in which case the user is told why.
    /// </returns>
    /// <remarks>
    /// A sub-service with every output off would be selected but produce nothing, which reads as
    /// a bug rather than as a choice. Deselecting the sub-service on the General tab is the way
    /// to produce nothing for it, and the message says so.
    /// </remarks>
    protected bool CanTurnOffOutput()
    {
        if (EnabledOutputCount > 1)
        {
            return true;
        }

        Toast.Warn(
            "Keep one output",
            $"{Title} needs at least one output switched on. To produce nothing for {Title}, "
            + "deselect it on the General tab.");

        return false;
    }

    /// <summary>
    /// Puts a rejected toggle back where it was, after <see cref="CanTurnOffOutput"/> has
    /// refused the change.
    /// </summary>
    /// <param name="propertyName">The property the control is bound to.</param>
    /// <remarks>
    /// The notification is posted rather than raised inline: raising it while WPF is still
    /// pushing the new value into the source can leave the control showing the value the
    /// view-model just refused. Posting lets that transfer finish first, so the checkbox
    /// visibly snaps back.
    /// </remarks>
    protected void RestoreRejectedToggle(string propertyName)
    {
        System.Windows.Threading.Dispatcher? dispatcher = System.Windows.Application.Current?.Dispatcher;

        if (dispatcher is null)
        {
            OnPropertyChanged(propertyName);
            return;
        }

        dispatcher.BeginInvoke(() => OnPropertyChanged(propertyName));
    }

    /// <summary>Loads this menu's fields from the in-memory <c>UserConfig</c> dictionary.</summary>
    protected abstract void LoadFromConfig();

    /// <summary>Pushes this menu's fields into the in-memory <c>UserConfig</c> dictionary (via <see cref="UserConfigFile.TrySetValue"/>).</summary>
    protected abstract void WriteToConfig();
}
