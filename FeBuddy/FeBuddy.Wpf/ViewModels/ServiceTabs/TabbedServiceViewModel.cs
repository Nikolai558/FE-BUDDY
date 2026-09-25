using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Views;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// Base for a first-tier service screen built as tabs: an optional permanent <b>General</b> tab,
/// one tab per sub-service, an optional <b>Preview Settings</b> tab, and - after a run - a
/// <b>Review</b> tab at the very end.
/// </summary>
/// <remarks>
/// <para>
/// The model is deliberately generic. AIRAC Service uses every part of it: a General tab to pick
/// sub-services, and one Preview Settings tab that runs them all together. File Conversions has
/// neither - every conversion is always on the rail and runs on its own from its own tab. Data
/// Viewers and File Health Services are expected to take one of those two shapes, and AIRAC
/// Service alone is expected to reach roughly twenty sub-services - so tabs are data, created
/// and destroyed as the screen needs them, never hand-placed in XAML.
/// </para>
/// <para>
/// Navigation goes through <see cref="NextCommand"/> / <see cref="PreviousCommand"/> /
/// <see cref="GoToPreviewCommand"/>, each of which offers to save a dirty tab before leaving it.
/// Cancelling that prompt keeps the user where they are rather than silently discarding edits.
/// </para>
/// </remarks>
public abstract class TabbedServiceViewModel : ObservableObject
{
	private ServiceTabViewModel? _selectedTab;
	private bool _isRunning;

	/// <summary>Wires the shared tab-bar commands.</summary>
	protected TabbedServiceViewModel()
	{
		SaveCommand = new RelayCommand(
			() => SelectedTab?.Save(),
			() => SelectedTab?.IsDirty == true);

		UndoLastSaveCommand = new RelayCommand(
			() => (SelectedTab as SubServiceSettingsViewModel)?.UndoLastSave(),
			() => SelectedTab is SubServiceSettingsViewModel { CanUndo: true });

		RevertChangesCommand = new RelayCommand(
			() => (SelectedTab as SubServiceSettingsViewModel)?.RevertChanges(),
			() => SelectedTab is SubServiceSettingsViewModel { IsDirty: true });

		NextCommand = new RelayCommand(() => Step(1), () => CanStep(1));
		PreviousCommand = new RelayCommand(() => Step(-1), () => CanStep(-1));

		GoToPreviewCommand = new RelayCommand(
			GoToPreview,
			() => PreviewTab is { } preview && Tabs.Contains(preview) && !ReferenceEquals(SelectedTab, preview));
	}

	/// <summary>The screen's heading, e.g. <c>AIRAC Services</c>.</summary>
	public abstract string ScreenTitle { get; }

	/// <summary>
	/// Whether the screen has a Preview Settings tab at all. The action bar shows its
	/// <b>Preview settings</b> button only when it does.
	/// </summary>
	public bool HasPreviewTab => PreviewTab is not null;

	/// <summary><see langword="true"/> while a run is in progress.</summary>
	public bool IsRunning
	{
		get => _isRunning;
		protected set
		{
			if (SetProperty(ref _isRunning, value))
			{
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	/// <summary>The open tabs, in rail order: General, the selected sub-services, Preview Settings, then Review after a run.</summary>
	public ObservableCollection<ServiceTabViewModel> Tabs { get; } = [];

	/// <summary>Saves the selected tab (validates first).</summary>
	public ICommand SaveCommand { get; }

	/// <summary>Reverts the selected tab to its previous save, while a snapshot exists.</summary>
	public ICommand UndoLastSaveCommand { get; }

	/// <summary>Discards the selected tab's unsaved edits, returning it to its last saved state.</summary>
	public ICommand RevertChangesCommand { get; }

	/// <summary>Offers to save, then moves to the next tab.</summary>
	public ICommand NextCommand { get; }

	/// <summary>Offers to save, then moves to the previous tab.</summary>
	public ICommand PreviousCommand { get; }

	/// <summary>Offers to save, then jumps to the settings-preview tab.</summary>
	public ICommand GoToPreviewCommand { get; }

	/// <summary>The tab on screen.</summary>
	public ServiceTabViewModel? SelectedTab
	{
		get => _selectedTab;
		set
		{
			if (SetProperty(ref _selectedTab, value))
			{
				if (value is not null && PreviewTab is { } preview && ReferenceEquals(value, preview))
				{
					preview.Refresh();
				}

				OnPropertyChanged(nameof(SelectedTabTitle));
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	/// <summary>The selected tab's title, for the content header.</summary>
	public string SelectedTabTitle => SelectedTab?.Title ?? string.Empty;

	/// <summary>
	/// The permanent first tab - the service's own settings and the sub-service picker - or
	/// <see langword="null"/> for a screen whose sub-services are always on the rail.
	/// </summary>
	protected virtual ServiceTabViewModel? GeneralTab => null;

	/// <summary>
	/// The settings-preview tab that runs every sub-service together, present once at least one
	/// is open; or <see langword="null"/> for a screen whose sub-services each run from their own tab.
	/// </summary>
	protected virtual ServicePreviewTabViewModel? PreviewTab => null;

	/// <summary>
	/// A tab that belongs at the very end and only exists after something has happened - the
	/// run review. <see langword="null"/> until then, and never shown before it has content.
	/// </summary>
	protected virtual ServiceTabViewModel? PostRunTab => null;

	/// <summary>
	/// Reconciles <see cref="Tabs"/> with the sub-service tabs that should currently be open.
	/// Existing tab instances are kept (so their state and their place in the rail survive), the
	/// Preview Settings and Review tabs are added or removed to match, and the selection is moved
	/// only if the tab it pointed at is gone.
	/// </summary>
	/// <param name="subServiceTabs">The tabs for the open sub-services, in display order.</param>
	protected void RebuildTabs(IEnumerable<ServiceTabViewModel> subServiceTabs)
	{
		List<ServiceTabViewModel> desired = [.. subServiceTabs];

		if (desired.Count > 0 && PreviewTab is { } preview)
		{
			desired.Add(preview);
		}

		if (GeneralTab is { } general)
		{
			desired.Insert(0, general);
		}

		if (PostRunTab is { } postRun)
		{
			desired.Add(postRun);
		}

		// Remove first, then insert, so index maths below is never done against a stale list.
		foreach (ServiceTabViewModel gone in Tabs.Except(desired).ToList())
		{
			Tabs.Remove(gone);
		}

		for (int i = 0; i < desired.Count; i++)
		{
			if (i >= Tabs.Count)
			{
				Tabs.Add(desired[i]);
			}
			else if (!ReferenceEquals(Tabs[i], desired[i]))
			{
				Tabs.Insert(i, desired[i]);
			}
		}

		if (SelectedTab is null || !Tabs.Contains(SelectedTab))
		{
			SelectedTab = Tabs.FirstOrDefault();
		}

		CommandManager.InvalidateRequerySuggested();
	}

	/// <summary>
	/// Offers to save <paramref name="tab"/> when it is dirty.
	/// </summary>
	/// <param name="tab">The tab being left.</param>
	/// <returns><see langword="true"/> when it is safe to leave the tab.</returns>
	protected static bool ConfirmLeave(ServiceTabViewModel? tab)
	{
		if (tab is null || !tab.IsDirty)
		{
			return true;
		}

		bool save = ConfirmWindow.Show(
			Application.Current?.MainWindow,
			"Unsaved changes",
			$"{tab.Title} has unsaved changes. The newly input data will be saved before moving on.",
			confirmText: "Save & Continue");

		// Cancel keeps the user on the tab with their edits intact; a failed save does the same,
		// having already explained why (the tab shows the message and the bad field is marked).
		return save && tab.Save();
	}

	private bool CanStep(int direction)
	{
		int index = SelectedTab is null ? -1 : Tabs.IndexOf(SelectedTab);
		int target = index + direction;
		return index >= 0 && target >= 0 && target < Tabs.Count;
	}

	private void Step(int direction)
	{
		if (!CanStep(direction) || !ConfirmLeave(SelectedTab))
		{
			return;
		}

		SelectedTab = Tabs[Tabs.IndexOf(SelectedTab!) + direction];
	}

	private void GoToPreview()
	{
		if (PreviewTab is not { } preview || !Tabs.Contains(preview) || !ConfirmLeave(SelectedTab))
		{
			return;
		}

		SelectedTab = preview;
	}

	/// <summary>Brings the run-review tab into the rail and selects it.</summary>
	protected void ShowPostRunTab()
	{
		if (PostRunTab is not { } postRun)
		{
			return;
		}

		if (!Tabs.Contains(postRun))
		{
			Tabs.Add(postRun);
		}

		SelectedTab = postRun;
	}
}
