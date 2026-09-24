using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;

using FeBuddy.Core.Infrastructure.Configuration;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

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
///         and <see cref="ServiceTabViewModel.BuildPreviewSummary"/>, and optionally
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
	private Dictionary<string, string>? _captureBuffer;
	private SavedStateSnapshot? _savedState;

	/// <summary>Wires the Save / Undo commands. Derived constructors should call <see cref="LoadFromConfig"/> after their fields exist.</summary>
	protected SubServiceSettingsViewModel()
	{
		SaveCommand = new RelayCommand(() => Save(), () => IsDirty);
		UndoLastSaveCommand = new RelayCommand(UndoLastSave, () => CanUndo);
		RevertChangesCommand = new RelayCommand(RevertChanges, () => IsDirty);
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
	/// Throws away every unsaved edit on this tab, putting it back to the state it was last
	/// saved (or loaded) at. Distinct from <see cref="UndoLastSaveCommand"/>, which steps the
	/// saved settings themselves back one save.
	/// </summary>
	public ICommand RevertChangesCommand { get; }

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

	/// <inheritdoc />
	/// <remarks>
	/// Compares the tab's current values against the snapshot taken when it was last saved or
	/// loaded, so changing a setting and changing it straight back leaves the tab clean rather
	/// than permanently marked as edited.
	/// </remarks>
	protected override void MarkDirty()
	{
		IsDirty = _savedState is null || !MatchesSavedState();
		Revalidate();
	}

	/// <inheritdoc />
	protected override void ClearDirty()
	{
		_savedState = SavedStateSnapshot.Of(CaptureCurrentValues());
		IsDirty = false;
		Revalidate();
	}

	/// <summary>
	/// Re-takes the clean-state snapshot after a list this tab's settings depend on has been
	/// populated. Cycle-dependent lists (airway designations, and so on) arrive well after the
	/// tab is built, so the snapshot taken at construction describes a tab whose lists were
	/// still empty; without this, toggling such a row and toggling it straight back would leave
	/// the tab marked as edited.
	/// </summary>
	/// <remarks>
	/// Deliberately does nothing while the tab has unsaved edits - re-snapshotting then would
	/// adopt those edits as the saved state and lose the warning.
	/// </remarks>
	protected void ResyncSavedState()
	{
		if (!IsDirty)
		{
			ClearDirty();
		}
	}

	/// <summary>Discards every unsaved edit, reloading the tab from what was last saved.</summary>
	public void RevertChanges()
	{
		if (!IsDirty)
		{
			return;
		}

		LoadFromConfig();
		OnReloadedFromConfig();
		ClearDirty();
		CommandManager.InvalidateRequerySuggested();
		Toast.Info("Changes discarded", $"{Title} is back to its last saved settings.");
	}

	/// <summary>Restores this node's previous save, if a snapshot exists.</summary>
	public void UndoLastSave()
	{
		if (!UserConfigFile.Undo(NodePath))
		{
			return;
		}

		LoadFromConfig();
		OnReloadedFromConfig();
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

	/// <summary>
	/// Writes one of this menu's values, relative to <see cref="NodePath"/>.
	/// </summary>
	/// <param name="key">The key under this menu's node, e.g. <c>GenerateAliasFile</c>.</param>
	/// <param name="value">The value to store.</param>
	/// <remarks>
	/// Every <see cref="WriteToConfig"/> implementation goes through this rather than touching
	/// <see cref="UserConfigFile"/> directly, which is what lets the same method serve twice:
	/// normally it persists, and while a snapshot is being taken it writes into a buffer
	/// instead. That is how the tab can tell whether it currently differs from what it saved
	/// without a second, hand-maintained list of its own fields to drift out of step.
	/// </remarks>
	protected void Set(string key, string value)
	{
		if (_captureBuffer is not null)
		{
			_captureBuffer[key] = value;
			return;
		}

		UserConfigFile.TrySetValue($"{NodePath}.{key}", value);
	}

	/// <summary>Reads one of this menu's saved values, relative to <see cref="NodePath"/>.</summary>
	/// <param name="key">The key under this menu's node.</param>
	/// <returns>The saved value, or <see langword="null"/> when it has none.</returns>
	protected string? Get(string key) => UserConfigFile.GetValue($"{NodePath}.{key}");

	/// <summary>Reads one of this menu's saved yes/no values.</summary>
	/// <param name="key">The key under this menu's node.</param>
	/// <param name="defaultValue">What to use when nothing is saved.</param>
	/// <returns><see langword="true"/> for <c>Y</c> or <c>true</c> (any case), <see langword="false"/> for anything else.</returns>
	protected bool GetBool(string key, bool defaultValue)
	{
		string? value = Get(key)?.Trim();

		return string.IsNullOrEmpty(value)
			? defaultValue
			: value.Equals("Y", StringComparison.OrdinalIgnoreCase) || value.Equals("true", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>The <c>Y</c> / <c>N</c> spelling every yes/no setting is saved and sent in.</summary>
	/// <param name="value">The value.</param>
	/// <returns><c>Y</c> or <c>N</c>.</returns>
	protected static string YesNo(bool value) => value ? "Y" : "N";

	/// <summary>Splits a saved comma-separated list, ignoring blanks and case.</summary>
	/// <param name="saved">The saved value, e.g. <c>ZOB, ZNY</c>.</param>
	/// <returns>The entries.</returns>
	protected static HashSet<string> ParseList(string? saved) =>
		new((saved ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
			StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Called after <see cref="RevertChanges"/> or <see cref="UndoLastSave"/> has reloaded this
	/// tab from the config. The base does nothing.
	/// </summary>
	/// <remarks>
	/// <see cref="LoadFromConfig"/> restores values with its change events suppressed, which is
	/// right while a tab is being built but leaves anything driven by those values - the open
	/// tab rail, the loaded cycle - still showing the discarded state. A tab whose settings
	/// reach outside itself re-announces them here.
	/// </remarks>
	protected virtual void OnReloadedFromConfig()
	{
	}

	/// <summary>Loads this menu's fields from the in-memory <c>UserConfig</c> dictionary.</summary>
	protected abstract void LoadFromConfig();

	/// <summary>Pushes this menu's fields into the in-memory <c>UserConfig</c> dictionary (via <see cref="Set"/>).</summary>
	protected abstract void WriteToConfig();

	/// <summary>
	/// Runs <see cref="WriteToConfig"/> against a buffer instead of the config file, giving the
	/// tab's current values as a plain dictionary.
	/// </summary>
	/// <returns>Every value this tab would save right now.</returns>
	private IReadOnlyDictionary<string, string> CaptureCurrentValues()
	{
		Dictionary<string, string> buffer = new(StringComparer.Ordinal);
		_captureBuffer = buffer;

		try
		{
			WriteToConfig();
		}
		finally
		{
			_captureBuffer = null;
		}

		return buffer;
	}

	/// <summary>Whether the tab's current values are identical to the last saved snapshot.</summary>
	/// <returns><see langword="true"/> when nothing differs.</returns>
	private bool MatchesSavedState() =>
		_savedState is { } saved && saved.Matches(CaptureCurrentValues());
}
