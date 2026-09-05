using System.Collections.ObjectModel;
using System.Windows.Input;
using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.ViewModels.Models;
using Microsoft.Win32;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Validate a CRC GeoJSON against the ERAM / STARS property schema, or run a
/// clean-up pass (the logic salvaged from v2.x's GeoJson converter). All findings
/// and sizes are sample data.
/// </summary>
public sealed class GeoJsonToolsViewModel : ObservableObject
{
    private GjMode _mode = GjMode.Validate;
    private bool _targetEram = true;
    private string _fileName = "ZOB_ERAM_lines.geojson";

    public GeoJsonToolsViewModel()
    {
        Findings = SampleFindings();

        CleanupOps =
        [
            new CleanupOp("Single-line output", "Condense to one line to save space in CRC."),
            new CleanupOp("Combine adjacent LineStrings", "Merge segments into MultiLineStrings (v2.x #152)."),
            new CleanupOp("Round coordinates to 6 dp", "~1 m precision; trims file size."),
            new CleanupOp("Strip unused properties", "Remove keys CRC never reads."),
            new CleanupOp("Normalise BCG / filter names", "Fix spelling; drop blanks (v2.x #155)."),
            new CleanupOp("Remove empty geometries", "Drop features with no coordinates.", enabled: false),
        ];

        ChooseFileCommand = new RelayCommand(ChooseFile);
        ValidateCommand = new RelayCommand(() =>
            Toast.Info("Validated", $"{FindingsSummary} against {SchemaLabel}."));
        CleanupCommand = new RelayCommand(() =>
            Toast.Success("Cleaned up", $"{FileName}: {SizeBeforeAfter}."));
    }

    public GjMode Mode
    {
        get => _mode;
        set
        {
            if (SetProperty(ref _mode, value))
            {
                OnPropertyChanged(nameof(IsValidate));
            }
        }
    }

    public bool IsValidate => Mode == GjMode.Validate;

    public bool TargetEram
    {
        get => _targetEram;
        set
        {
            if (SetProperty(ref _targetEram, value))
            {
                OnPropertyChanged(nameof(SchemaLabel));
            }
        }
    }

    public string SchemaLabel => TargetEram ? "CRC ERAM schema" : "CRC STARS schema";

    public string FileName
    {
        get => _fileName;
        private set => SetProperty(ref _fileName, value);
    }

    public ObservableCollection<Finding> Findings { get; }

    public ObservableCollection<CleanupOp> CleanupOps { get; }

    public string FindingsSummary
    {
        get
        {
            var e = Findings.Count(f => f.Severity == FindingSeverity.Error);
            var w = Findings.Count(f => f.Severity == FindingSeverity.Warn);
            var i = Findings.Count(f => f.Severity == FindingSeverity.Info);
            return $"{e} error{Plural(e)} · {w} warning{Plural(w)} · {i} note{Plural(i)}";
        }
    }

    public string SizeBeforeAfter => "412 KB → 264 KB  (−36%)";

    public ICommand ChooseFileCommand { get; }

    public ICommand ValidateCommand { get; }

    public ICommand CleanupCommand { get; }

    private void ChooseFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open GeoJSON",
            Filter = "GeoJSON (*.geojson;*.json)|*.geojson;*.json|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog() == true)
        {
            FileName = System.IO.Path.GetFileName(dialog.FileName);
            Toast.Info("Loaded", $"{FileName} — sample report shown.");
        }
    }

    private static string Plural(int n) => n == 1 ? "" : "s";

    private static ObservableCollection<Finding> SampleFindings() =>
    [
        new(FindingSeverity.Error, "Line defaults present but no filter — object will not display in CRC", "AWY-HIGH_lines · defaults"),
        new(FindingSeverity.Error, "Self-intersecting polygon", "feature 7"),
        new(FindingSeverity.Warn, "Style \"SolidThin\" corrected to \"Solid\" (unknown value)", "feature 41"),
        new(FindingSeverity.Warn, "Coordinate precision > 6 decimals — inflates file size", "features 118–142"),
        new(FindingSeverity.Warn, "Empty geometry", "feature 203"),
        new(FindingSeverity.Info, "18 LineString features could be merged into 3 MultiLineStrings", "whole file"),
        new(FindingSeverity.Info, "BCG group 5 is not referenced by any feature", "defaults"),
    ];
}
