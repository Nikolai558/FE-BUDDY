using System.Collections.ObjectModel;
using System.Windows.Input;
using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.ViewModels.Models;
using Microsoft.Win32;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Import external vector data into CRC GeoJSON: FAA <c>.DAT</c> RADAR video maps,
/// Google Earth <c>.KML/.KMZ</c>, and <c>.SCT2</c> sector files. vSTARS/vERAM and
/// AutoCAD (DXF) conversions were retired in 3.0 - CRC is the only client.
/// Nothing is written; "Convert" just reports.
/// </summary>
public sealed class ConversionsViewModel : ObservableObject
{
    private ConvKind _kind = ConvKind.Dat;
    private bool _potFromFile = true;
    private string _potLat = string.Empty;
    private string _potLon = string.Empty;
    private string _clipDistanceNm = string.Empty;
    private bool _singleLine = true;
    private string? _resultMessage;

    public ConversionsViewModel()
    {
        Files =
        [
            new ConvFile("ZOB_ASR9_CLE.dat", "266 KB"),
            new ConvFile("ZOB_ASR11_TOL.dat", "204 KB"),
        ];

        AddFilesCommand = new RelayCommand(AddFiles);
        RemoveFileCommand = new RelayCommand<ConvFile>(f => { if (f is not null) Files.Remove(f); });
        ClearFilesCommand = new RelayCommand(Files.Clear);
        ConvertCommand = new RelayCommand(Convert, () => Files.Count > 0);
        ResetCommand = new RelayCommand(Reset);
    }

    public ConvKind Kind
    {
        get => _kind;
        set
        {
            if (SetProperty(ref _kind, value))
            {
                OnPropertyChanged(nameof(KindHint));
                OnPropertyChanged(nameof(IsDat));
            }
        }
    }

    public bool IsDat => Kind == ConvKind.Dat;

    public string KindHint => Kind switch
    {
        ConvKind.Dat => "FAA RADAR video maps. Point of tangency is read from each file unless you override it; an optional clip distance trims the map to a radius from centre.",
        ConvKind.Kml => "Google Earth files from the FAA. Placemarks and paths become Point / LineString features.",
        ConvKind.Sct2 => "Legacy sector files - kept so neighbouring non-US facilities can hand off data. Section titles become file names.",
        _ => string.Empty,
    };

    public ObservableCollection<ConvFile> Files { get; }

    public bool PotFromFile
    {
        get => _potFromFile;
        set => SetProperty(ref _potFromFile, value);
    }

    public string PotLat { get => _potLat; set => SetProperty(ref _potLat, value); }

    public string PotLon { get => _potLon; set => SetProperty(ref _potLon, value); }

    public string ClipDistanceNm { get => _clipDistanceNm; set => SetProperty(ref _clipDistanceNm, value); }

    public bool SingleLine
    {
        get => _singleLine;
        set => SetProperty(ref _singleLine, value);
    }

    public string? ResultMessage
    {
        get => _resultMessage;
        private set => SetProperty(ref _resultMessage, value);
    }

    public ICommand AddFilesCommand { get; }

    public ICommand RemoveFileCommand { get; }

    public ICommand ClearFilesCommand { get; }

    public ICommand ConvertCommand { get; }

    public ICommand ResetCommand { get; }

    private void AddFiles()
    {
        var filter = Kind switch
        {
            ConvKind.Dat => "FAA RADAR video map (*.dat)|*.dat|All files (*.*)|*.*",
            ConvKind.Kml => "Google Earth (*.kml;*.kmz)|*.kml;*.kmz|All files (*.*)|*.*",
            _ => "Sector file (*.sct2;*.sct)|*.sct2;*.sct|All files (*.*)|*.*",
        };

        var dialog = new OpenFileDialog { Title = "Add source files", Filter = filter, Multiselect = true };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        foreach (var path in dialog.FileNames)
        {
            Files.Add(new ConvFile(System.IO.Path.GetFileName(path), "—"));
        }
    }

    private void Convert()
    {
        var n = Files.Count;
        ResultMessage = $"{n} file{(n == 1 ? "" : "s")} → {n} GeoJSON in …\\FE-Buddy\\Output\\Converted.";
        Toast.Success("Conversion complete", ResultMessage);
    }

    private void Reset()
    {
        Files.Clear();
        Kind = ConvKind.Dat;
        PotFromFile = true;
        PotLat = PotLon = ClipDistanceNm = string.Empty;
        SingleLine = true;
        ResultMessage = null;
    }
}
