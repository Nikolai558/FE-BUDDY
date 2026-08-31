using System.Collections.ObjectModel;
using System.Windows.Input;
using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.ViewModels.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The alias / reference text files the AIRAC build produces (v2.x <c>ALIAS/</c>
/// folder), plus a cross-file duplicate-command check. All sample data.
/// </summary>
public sealed class AliasReferenceViewModel : ObservableObject
{
    private bool _checkedForDupes;

    public AliasReferenceViewModel()
    {
        Artifacts =
        [
            new AliasArtifact("AWY_ALIAS.txt",
                ".<airwayId>F expands to every waypoint on the airway", "412 commands",
                ".j80f .ff SFO OAK LIN OAL FMG ... EMPTY"),
            new AliasArtifact("STAR_DP_Fixes_Alias.txt",
                "Fix aliases for every STAR and DP fix", "1,180 commands",
                ".acton .ff ACTON  /  .wwshr .ff WWSHR"),
            new AliasArtifact("ISR_APT.txt",
                ".ECHO in-scope reference for airports (multi-line with \\n)", "312 commands",
                ".kcle .echo CLE  Cleveland-Hopkins Intl\\n Twr 124.5  Gnd 121.9"),
            new AliasArtifact("ISR_NAVAID.txt",
                ".ECHO in-scope reference for NAVAIDs", "88 commands",
                ".dje .echo DJB  115.0  Dryer VORTAC"),
            new AliasArtifact("FAA_CHART_RECALL.txt",
                "Chart-recall aliases from the d-TPP metafile", "540 commands",
                ".kclerc .echo CLE  ILS OR LOC RWY 6L  (10-1)"),
            new AliasArtifact("TELEPHONY.txt",
                "ARTCC callsign / telephony lookups", "790 commands",
                ".zob .echo Cleveland Center"),
        ];

        CheckDuplicatesCommand = new RelayCommand(CheckDuplicates);
    }

    public ObservableCollection<AliasArtifact> Artifacts { get; }

    public ObservableCollection<DupCommand> Duplicates { get; } = [];

    public bool CheckedForDupes
    {
        get => _checkedForDupes;
        private set
        {
            if (SetProperty(ref _checkedForDupes, value))
            {
                OnPropertyChanged(nameof(DupSummary));
            }
        }
    }

    public string DupSummary => !CheckedForDupes
        ? "Scans every generated alias file plus any custom files for commands defined more than once."
        : Duplicates.Count == 0
            ? "No duplicate commands found."
            : $"{Duplicates.Count} duplicate command{(Duplicates.Count == 1 ? "" : "s")} found.";

    public ICommand CheckDuplicatesCommand { get; }

    private void CheckDuplicates()
    {
        Duplicates.Clear();
        Duplicates.Add(new DupCommand(".bosf", "AWY_ALIAS.txt, custom_aliases.txt"));
        Duplicates.Add(new DupCommand(".jfkf", "AWY_ALIAS.txt, STAR_DP_Fixes_Alias.txt"));
        Duplicates.Add(new DupCommand(".klerc", "FAA_CHART_RECALL.txt, custom_aliases.txt"));
        CheckedForDupes = true;
        Toast.Warn("Duplicate commands", $"{Duplicates.Count} found across 3 files.");
    }
}
