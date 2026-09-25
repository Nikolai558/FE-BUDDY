using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

using FeBuddy.Core.Application.Conversions.DatToGeojson;
using FeBuddy.Core.Application.Conversions.DatToGeojson.Models;
using FeBuddy.Core.Application.Models;

using Microsoft.Win32;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>DAT to GeoJSON</b> tab on the File Conversions screen: converts FAA <c>.dat</c> RADAR
/// Video Maps into CRC-ready GeoJSON, one file per map, optionally cropped to a distance from
/// each map's point of tangency.
/// </summary>
/// <remarks>
/// <para>
/// The source is either a folder - every <c>.dat</c> in it - or files picked one or more at a
/// time. The folder is saved with the tab's settings; picked files are not, as they are rarely
/// the same twice. Which of the two is in use is saved.
/// </para>
/// <para>
/// A video map is lines only, so the CRC ERAM Defaults card shows just its Lines panel.
/// </para>
/// </remarks>
public sealed class DatToGeojsonViewModel : ConversionTabViewModel, ICrcDefaultsSettings
{
	private const string Node = "Services.FileConversions.DatToGeojson";

	private DatSourceType _sourceType = DatSourceType.Folder;
	private string _sourceFolder = string.Empty;
	private string _croppingDistance = string.Empty;
	private bool _includeCrcLineDefaults = true;
	private int? _folderDatCount;

	/// <summary>Builds the tab and restores its saved settings.</summary>
	public DatToGeojsonViewModel()
	{
		LineDefaults = [new(DatToGeojsonSettingsParser.CrcClassName, EramFieldKind.Line, MarkDirty)];

		SourceFiles.CollectionChanged += (_, _) =>
		{
			OnPropertyChanged(nameof(HasSourceFiles));
			RaiseRunBlockerChanged();
		};

		BrowseFolderCommand = new RelayCommand(BrowseFolder);
		BrowseFilesCommand = new RelayCommand(BrowseFiles);
		ClearFilesCommand = new RelayCommand(SourceFiles.Clear, () => SourceFiles.Count > 0);
		RemoveFileCommand = new RelayCommand<SourceFileItem>(file =>
		{
			if (file is not null)
			{
				SourceFiles.Remove(file);
			}
		});

		LoadFromConfig();
	}

	/// <inheritdoc />
	public override string NodePath => Node;

	/// <inheritdoc />
	public override string Title => "DAT to GeoJSON";

	/// <inheritdoc />
	public override string RunLabel => "Convert DAT files";

	// ================= source =================

	/// <summary>Whether every <c>.dat</c> in <see cref="SourceFolder"/> is converted.</summary>
	public bool SourceIsFolder
	{
		get => _sourceType == DatSourceType.Folder;
		set { if (value) SetSourceType(DatSourceType.Folder); }
	}

	/// <summary>Whether the files in <see cref="SourceFiles"/> are converted.</summary>
	public bool SourceIsFiles
	{
		get => _sourceType == DatSourceType.Files;
		set { if (value) SetSourceType(DatSourceType.Files); }
	}

	/// <summary>The folder whose <c>.dat</c> files are converted. Saved.</summary>
	public string SourceFolder
	{
		get => _sourceFolder;
		set
		{
			if (SetProperty(ref _sourceFolder, value))
			{
				MarkDirty();
				RefreshFolder();
			}
		}
	}

	/// <summary>What is in <see cref="SourceFolder"/>, e.g. <c>3 .dat files in this folder.</c></summary>
	/// <remarks>Counted when the folder is chosen, not on every read; picking it again re-counts.</remarks>
	public string FolderSummary => _folderDatCount switch
	{
		null when string.IsNullOrWhiteSpace(SourceFolder) => "No folder selected.",
		null => "This folder does not exist.",
		0 => "There are no .dat files in this folder.",
		1 => "1 .dat file in this folder.",
		{ } count => $"{count:N0} .dat files in this folder.",
	};

	/// <summary>The picked <c>.dat</c> files. Not saved.</summary>
	public ObservableCollection<SourceFileItem> SourceFiles { get; } = [];

	/// <summary>Whether any file has been picked.</summary>
	public bool HasSourceFiles => SourceFiles.Count > 0;

	/// <summary>Picks <see cref="SourceFolder"/> with a folder dialog.</summary>
	public ICommand BrowseFolderCommand { get; }

	/// <summary>Adds one or more <c>.dat</c> files to <see cref="SourceFiles"/> with a file dialog.</summary>
	public ICommand BrowseFilesCommand { get; }

	/// <summary>Empties <see cref="SourceFiles"/>.</summary>
	public ICommand ClearFilesCommand { get; }

	/// <summary>Takes one file off <see cref="SourceFiles"/>; the command parameter is the <see cref="SourceFileItem"/>.</summary>
	public ICommand RemoveFileCommand { get; }

	// ================= cropping =================

	/// <summary>
	/// Keep only what lies within this many nautical miles of each map's point of tangency, as
	/// typed; blank keeps every line. Saved.
	/// </summary>
	public string CroppingDistance
	{
		get => _croppingDistance;
		set { if (SetProperty(ref _croppingDistance, value)) MarkDirty(); }
	}

	// ================= CRC ERAM defaults =================

	/// <inheritdoc />
	public bool IncludeCrcLineDefaults
	{
		get => _includeCrcLineDefaults;
		set { if (SetProperty(ref _includeCrcLineDefaults, value)) MarkDirty(); }
	}

	/// <inheritdoc />
	/// <remarks>A video map has no symbols; always <see langword="false"/>.</remarks>
	public bool IncludeCrcSymbolDefaults { get => false; set { } }

	/// <inheritdoc />
	/// <remarks>A video map has no text; always <see langword="false"/>.</remarks>
	public bool IncludeCrcTextDefaults { get => false; set { } }

	/// <inheritdoc />
	/// <remarks>Every converted file is lines; always <see langword="true"/>, which keeps the Lines panel on the card.</remarks>
	public bool EmitLines { get => true; set { } }

	/// <inheritdoc />
	/// <remarks>Always <see langword="false"/>, which keeps the Symbols panel off the card.</remarks>
	public bool EmitSymbols { get => false; set { } }

	/// <inheritdoc />
	/// <remarks>Always <see langword="false"/>, which keeps the Text panel off the card.</remarks>
	public bool EmitText { get => false; set { } }

	/// <inheritdoc />
	public ObservableCollection<EramClassDefault> LineDefaults { get; }

	/// <inheritdoc />
	public ObservableCollection<EramClassDefault> SymbolDefaults { get; } = [];

	/// <inheritdoc />
	public ObservableCollection<EramClassDefault> TextDefaults { get; } = [];

	// ================= run =================

	/// <inheritdoc />
	public override string? RunBlocker => _sourceType switch
	{
		DatSourceType.Folder when string.IsNullOrWhiteSpace(SourceFolder) => "Pick the folder to convert.",
		DatSourceType.Folder when _folderDatCount is null => "The source folder does not exist.",
		DatSourceType.Folder when _folderDatCount == 0 => "The source folder has no .dat files in it.",
		DatSourceType.Files when SourceFiles.Count == 0 => "Pick at least one .dat file to convert.",
		_ => null,
	};

	/// <inheritdoc />
	public override IReadOnlyDictionary<string, string> BuildSettingsBlock(string outputDirectory, bool addFeBuddyOutputFolder)
	{
		Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputDirectory"] = outputDirectory,
			["AddFeBuddyOutputFolder"] = YesNo(addFeBuddyOutputFolder),
			["CoordinatePrecision"] = OutputPreferences.CoordinatePrecision.ToString(CultureInfo.InvariantCulture),
			["CroppingDistance"] = CroppingDistance.Trim(),
			["IncludeCrcLineDefaults"] = YesNo(IncludeCrcLineDefaults),
		};

		if (_sourceType == DatSourceType.Folder)
		{
			settings["SourceFolder"] = SourceFolder.Trim();
		}
		else
		{
			settings["SourceFiles"] = string.Join(DatToGeojsonSettingsParser.SourceFileSeparator, SourceFiles.Select(f => f.Path));
		}

		if (IncludeCrcLineDefaults)
		{
			foreach (EramClassDefault row in LineDefaults)
			{
				CrcDefaultsRowIo.AddToSettingsBlock(row, settings);
			}
		}

		return settings;
	}

	/// <inheritdoc />
	public override ServiceResult Execute(IReadOnlyDictionary<string, string> settings, Action<string, string, bool> reportStep) =>
		DatToGeojsonService.Run(settings, new StepReporter(reportStep));

	/// <inheritdoc />
	public override ConversionRunOutcome DescribeRun(ServiceResult result)
	{
		DatToGeojsonServiceResult dat = (DatToGeojsonServiceResult)result;

		int converted = dat.GeojsonFilesWritten.Count;
		int lines = dat.Files.Sum(f => f.LinesWritten);

		string summary = $"{converted:N0} of {dat.Files.Count:N0} file(s) converted, {lines:N0} line(s) written";

		if (dat.FailedCount > 0)
		{
			summary += $", {dat.FailedCount:N0} failed";
		}

		return new ConversionRunOutcome(
			summary,
			new SubServiceRunResult(Title, summary, dat.Messages),
			dat.GeojsonFilesWritten,
			Directory.Exists(dat.OutputDirectory) ? dat.OutputDirectory : null);
	}

	// ================= save contract =================

	/// <inheritdoc />
	protected override void LoadFromConfig()
	{
		_sourceType = string.Equals(Get("SourceType")?.Trim(), nameof(DatSourceType.Files), StringComparison.OrdinalIgnoreCase)
			? DatSourceType.Files
			: DatSourceType.Folder;
		_sourceFolder = Get("SourceFolder") ?? string.Empty;
		_croppingDistance = Get("CroppingDistance") ?? string.Empty;
		_includeCrcLineDefaults = GetBool("IncludeCrcLineDefaults", true);

		foreach (EramClassDefault row in LineDefaults)
		{
			CrcDefaultsRowIo.Load(row, CrcConfigPrefix(row), Get);
		}

		foreach (string name in new[]
		{
			nameof(SourceIsFolder), nameof(SourceIsFiles), nameof(SourceFolder),
			nameof(CroppingDistance), nameof(IncludeCrcLineDefaults),
		})
		{
			OnPropertyChanged(name);
		}

		RefreshFolder();
		ClearDirty();
	}

	/// <inheritdoc />
	protected override void WriteToConfig()
	{
		Set("SourceType", _sourceType.ToString());
		Set("SourceFolder", SourceFolder);
		Set("CroppingDistance", CroppingDistance);
		Set("IncludeCrcLineDefaults", YesNo(IncludeCrcLineDefaults));

		foreach (EramClassDefault row in LineDefaults)
		{
			CrcDefaultsRowIo.Save(row, CrcConfigPrefix(row), Set);
		}
	}

	/// <inheritdoc />
	protected override void Validate(ServiceValidation validation)
	{
		string distance = CroppingDistance.Trim();

		if (distance.Length > 0
			&& (!double.TryParse(distance, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double nm)
				|| nm <= 0
				|| nm > DatToGeojsonSettingsParser.MaxCroppingDistanceNm))
		{
			validation.AddField(nameof(CroppingDistance),
				$"Enter a distance in NM greater than 0 and at most {DatToGeojsonSettingsParser.MaxCroppingDistanceNm:0}, or leave it blank to keep every line.");
		}

		foreach (EramClassDefault row in LineDefaults)
		{
			row.IsRequired = IncludeCrcLineDefaults;
		}

		if (IncludeCrcLineDefaults && LineDefaults.Any(row => row.HasMissingValues))
		{
			validation.Add("Some CRC ERAM default values are empty or invalid. Fix the marked boxes, or untick Include on that panel.");
		}
	}

	// ================= helpers =================

	private static string CrcConfigPrefix(EramClassDefault row) => $"CrcEramPropertyDefaults.{row.ClassName}_{row.Kind}";

	private void SetSourceType(DatSourceType type)
	{
		if (_sourceType == type)
		{
			return;
		}

		_sourceType = type;
		OnPropertyChanged(nameof(SourceIsFolder));
		OnPropertyChanged(nameof(SourceIsFiles));
		MarkDirty();

		// Switching back to the folder re-counts it, in case its files changed meanwhile.
		RefreshFolder();
	}

	/// <summary>
	/// Counts the <c>.dat</c> files in <see cref="SourceFolder"/> and refreshes what depends on
	/// the count. Done here rather than in the getters, which WPF reads on every command re-query.
	/// </summary>
	private void RefreshFolder()
	{
		string folder = SourceFolder.Trim();

		try
		{
			_folderDatCount = folder.Length > 0 && Directory.Exists(folder)
				? Directory.EnumerateFiles(folder, "*.dat", SearchOption.TopDirectoryOnly).Count()
				: null;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			_folderDatCount = null;
		}

		OnPropertyChanged(nameof(FolderSummary));
		RaiseRunBlockerChanged();
	}

	private void BrowseFolder()
	{
		OpenFolderDialog dialog = new()
		{
			Title = "Select the folder of .dat files to convert",
			InitialDirectory = Directory.Exists(SourceFolder) ? SourceFolder : null,
		};

		if (dialog.ShowDialog() == true)
		{
			SourceFolder = dialog.FolderName;
		}
	}

	private void BrowseFiles()
	{
		OpenFileDialog dialog = new()
		{
			Title = "Select the .dat files to convert",
			Filter = "FAA video maps (*.dat)|*.dat|All files (*.*)|*.*",
			Multiselect = true,
		};

		if (dialog.ShowDialog() != true)
		{
			return;
		}

		HashSet<string> already = new(SourceFiles.Select(f => f.Path), StringComparer.OrdinalIgnoreCase);

		foreach (string path in dialog.FileNames.Where(already.Add))
		{
			SourceFiles.Add(new SourceFileItem(path));
		}
	}

	/// <summary>Passes the library's progress straight on, on the thread it arrives on.</summary>
	/// <remarks>
	/// <see cref="Progress{T}"/> would post each report to the thread pool, which can reorder
	/// them; the screen's callback already marshals to the UI thread in order.
	/// </remarks>
	private sealed class StepReporter(Action<string, string, bool> reportStep) : IProgress<DatToGeojsonProgress>
	{
		public void Report(DatToGeojsonProgress value) => reportStep(value.FileName, value.Message, value.IsComplete);
	}
}
