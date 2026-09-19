using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

using FeBuddy.Wpf.Views;

namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// Base for a first-tier service screen built as tabs: a permanent <b>General</b> tab, one tab
/// per sub-service the user selected there, and a <b>Review</b> tab at the end once at least one
/// sub-service is selected.
/// </summary>
/// <remarks>
/// <para>
/// The model is deliberately generic. AIRAC Service is the first screen built on it, but File
/// Conversion Services, Data Viewers and File Health Services are expected to use the same shape,
/// and AIRAC Service alone is expected to reach roughly twenty sub-services - so tabs are data,
/// created and destroyed as the user selects sub-services, never hand-placed in XAML.
/// </para>
/// <para>
/// Navigation goes through <see cref="NextCommand"/> / <see cref="PreviousCommand"/> /
/// <see cref="GoToReviewCommand"/>, each of which offers to save a dirty tab before leaving it.
/// Cancelling that prompt keeps the user where they are rather than silently discarding edits.
/// </para>
/// </remarks>
public abstract class TabbedServiceViewModel : ObservableObject
{
    private ServiceTabViewModel? _selectedTab;

    /// <summary>Wires the shared tab-bar commands.</summary>
    protected TabbedServiceViewModel()
    {
        SaveCommand = new RelayCommand(
            () => SelectedTab?.Save(),
            () => SelectedTab?.IsDirty == true);

        UndoLastSaveCommand = new RelayCommand(
            () => (SelectedTab as SubServiceSettingsViewModel)?.UndoLastSave(),
            () => (SelectedTab as SubServiceSettingsViewModel)?.CanUndo == true);

        NextCommand = new RelayCommand(() => Step(1), () => CanStep(1));
        PreviousCommand = new RelayCommand(() => Step(-1), () => CanStep(-1));

        GoToReviewCommand = new RelayCommand(
            GoToReview,
            () => Tabs.Contains(ReviewTab) && !ReferenceEquals(SelectedTab, ReviewTab));
    }

    /// <summary>The open tabs, in rail order: General, the selected sub-services, then Review.</summary>
    public ObservableCollection<ServiceTabViewModel> Tabs { get; } = new();

    /// <summary>Saves the selected tab (validates first).</summary>
    public ICommand SaveCommand { get; }

    /// <summary>Reverts the selected tab to its previous save, while a snapshot exists.</summary>
    public ICommand UndoLastSaveCommand { get; }

    /// <summary>Offers to save, then moves to the next tab.</summary>
    public ICommand NextCommand { get; }

    /// <summary>Offers to save, then moves to the previous tab.</summary>
    public ICommand PreviousCommand { get; }

    /// <summary>Offers to save, then jumps to the Review tab.</summary>
    public ICommand GoToReviewCommand { get; }

    /// <summary>The tab on screen.</summary>
    public ServiceTabViewModel? SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (SetProperty(ref _selectedTab, value))
            {
                if (value is not null && ReferenceEquals(value, ReviewTab))
                {
                    ReviewTab.Refresh();
                }

                OnPropertyChanged(nameof(SelectedTabTitle));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>The selected tab's title, for the content header.</summary>
    public string SelectedTabTitle => SelectedTab?.Title ?? string.Empty;

    /// <summary>The permanent first tab: the service's own cycle-wide settings and the sub-service picker.</summary>
    protected abstract ServiceTabViewModel GeneralTab { get; }

    /// <summary>The permanent last tab, present once at least one sub-service is selected.</summary>
    protected abstract ServiceReviewTabViewModel ReviewTab { get; }

    /// <summary>
    /// Reconciles <see cref="Tabs"/> with the sub-service tabs that should currently be open.
    /// Existing tab instances are kept (so their state and their place in the rail survive), the
    /// Review tab is added or removed to match, and the selection is moved only if the tab it
    /// pointed at is gone.
    /// </summary>
    /// <param name="subServiceTabs">The tabs for the selected sub-services, in display order.</param>
    protected void RebuildTabs(IEnumerable<ServiceTabViewModel> subServiceTabs)
    {
        List<ServiceTabViewModel> desired = new() { GeneralTab };
        desired.AddRange(subServiceTabs);

        if (desired.Count > 1)
        {
            desired.Add(ReviewTab);
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

    private void GoToReview()
    {
        if (!Tabs.Contains(ReviewTab) || !ConfirmLeave(SelectedTab))
        {
            return;
        }

        SelectedTab = ReviewTab;
    }
}
