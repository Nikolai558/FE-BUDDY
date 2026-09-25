using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

using FeBuddy.Core.Application.Conversions;
using FeBuddy.Core.Application.Conversions.Models;
using FeBuddy.Core.Application.Models;

using Microsoft.Win32;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// Base for a conversion tab that turns source files into GeoJSON (DAT to GeoJSON, SCT2 to
/// GeoJSON): everything those tabs share - the source (a saved folder, or files picked one or
/// several at a time and not saved), the CRC ERAM defaults for the kinds the conversion writes,
/// the save contract, the settings block's common keys and the run's description.
/// </summary>
/// <remarks>
/// <para>
/// A derived tab passes its CRC class and whether it writes text to the constructor, calls
/// <see cref="SubServiceSettingsViewModel.LoadFromConfig"/> at the end of its own constructor,
/// and adds only what is its own through <see cref="LoadOwnSettings"/>,
/// <see cref="SaveOwnSettings"/>, <see cref="ValidateOwnSettings"/>, <see cref="AddOwnSettings"/>
/// and <see cref="DescribeDetail"/>.
/// </para>
/// <para>
/// The folder's matching files are counted when the folder is chosen, not in a getter: WPF reads
/// <see cref="RunBlocker"/> on every command re-query, and that must not touch the disk.
/// </para>
/// </remarks>
public abstract class FileConversionTabViewModel : ConversionTabViewModel, ISourceFileSettings, ICrcDefaultsSettings
{
	private const string CrcDefaultsIncompleteMessage =
		"Some CRC ERAM default values are empty or invalid. Fix the marked boxes, or untick Include on that panel.";

	private ConversionSourceType _sourceType = ConversionSourceType.Folder;
	private string _sourceFolder = string.Empty;
	private bool _includeCrcLineDefaults = true;
	private bool _includeCrcSymbolDefaults = true;
	private bool _includeCrcTextDefaults = true;
	private int? _folderFileCount;

	/// <summary>Builds the CRC defaults rows and wires the source commands.</summary>
	/// <param name="crcClassName">The CRC class the library reads this conversion's defaults under, e.g. <c>VideoMap</c>.</param>
	/// <param name="writesSymbols">Whether the conversion writes symbols, and so has Symbol defaults.</param>
	/// <param name="writesText">Whether the conversion writes labels, and so has Text defaults.</param>
	protected FileConversionTabViewModel(string crcClassName, bool writesSymbols, bool writesText)
	{
		LineDefaults = [new(crcClassName, EramFieldKind.Line, MarkDirty)];
		SymbolDefaults = writesSymbols ? [new(crcClassName, EramFieldKind.Symbol, MarkDirty)] : [];
		TextDefaults = writesText ? [new(crcClassName, EramFieldKind.Text, MarkDirty)] : [];

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
	}

	// ================= for the derived tab =================

	/// <inheritdoc />
	public abstract string FileTypeLabel { get; }

	/// <summary>The extensions a folder's files must have, from the library's service, e.g. <c>.dat</c>.</summary>
	protected abstract IReadOnlyList<string> Extensions { get; }

	/// <summary>What the file dialog calls the files, e.g. <c>FAA video maps</c>.</summary>
	protected abstract string FileDescription { get; }

	// ================= source =================

	/// <inheritdoc />
	public bool SourceIsFolder
	{
		get => _sourceType == ConversionSourceType.Folder;
		set { if (value) SetSourceType(ConversionSourceType.Folder); }
	}

	/// <inheritdoc />
	public bool SourceIsFiles
	{
		get => _sourceType == ConversionSourceType.Files;
		set { if (value) SetSourceType(ConversionSourceType.Files); }
	}

	/// <inheritdoc />
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

	/// <inheritdoc />
	/// <remarks>Counted when the folder is chosen, not on every read; picking it again re-counts.</remarks>
	public string FolderSummary => _folderFileCount switch
	{
		null when string.IsNullOrWhiteSpace(SourceFolder) => "No folder selected.",
		null => "This folder does not exist.",
		0 => $"There are no {FileTypeLabel} files in this folder.",
		1 => $"1 {FileTypeLabel} file in this folder.",
		{ } count => $"{count:N0} {FileTypeLabel} files in this folder.",
	};

	/// <inheritdoc />
	public ObservableCollection<SourceFileItem> SourceFiles { get; } = [];

	/// <summary>Whether any file has been picked.</summary>
	public bool HasSourceFiles => SourceFiles.Count > 0;

	/// <inheritdoc />
	public ICommand BrowseFolderCommand { get; }

	/// <inheritdoc />
	public ICommand BrowseFilesCommand { get; }

	/// <inheritdoc />
	public ICommand ClearFilesCommand { get; }

	/// <inheritdoc />
	public ICommand RemoveFileCommand { get; }

	// ================= CRC ERAM defaults =================

	/// <inheritdoc />
	public bool IncludeCrcLineDefaults
	{
		get => _includeCrcLineDefaults;
		set { if (SetProperty(ref _includeCrcLineDefaults, value)) MarkDirty(); }
	}

	/// <inheritdoc />
	public bool IncludeCrcSymbolDefaults
	{
		get => _includeCrcSymbolDefaults;
		set { if (SetProperty(ref _includeCrcSymbolDefaults, value)) MarkDirty(); }
	}

	/// <inheritdoc />
	public bool IncludeCrcTextDefaults
	{
		get => _includeCrcTextDefaults;
		set { if (SetProperty(ref _includeCrcTextDefaults, value)) MarkDirty(); }
	}

	/// <inheritdoc />
	/// <remarks>Every conversion writes lines; always <see langword="true"/>, which keeps the Lines panel on the card.</remarks>
	public bool EmitLines { get => true; set { } }

	/// <inheritdoc />
	/// <remarks>Whether the conversion writes symbols; fixed by the tab, and decides whether the Symbols panel shows.</remarks>
	public bool EmitSymbols { get => SymbolDefaults.Count > 0; set { } }

	/// <inheritdoc />
	/// <remarks>Whether the conversion writes labels; fixed by the tab, and decides whether the Text panel shows.</remarks>
	public bool EmitText { get => TextDefaults.Count > 0; set { } }

	/// <inheritdoc />
	public ObservableCollection<EramClassDefault> LineDefaults { get; }

	/// <inheritdoc />
	public ObservableCollection<EramClassDefault> SymbolDefaults { get; }

	/// <inheritdoc />
	public ObservableCollection<EramClassDefault> TextDefaults { get; }

	/// <summary>
	/// Whether the CRC ERAM Defaults card is in use. Always, unless a tab takes its defaults from
	/// somewhere else - vERAM can carry over the source file's own - in which case the card is
	/// hidden, nothing on it is required, and nothing on it is sent.
	/// </summary>
	public virtual bool UsesCrcDefaults => true;

	// ================= run =================

	/// <inheritdoc />
	public override string? RunBlocker => _sourceType switch
	{
		ConversionSourceType.Folder when string.IsNullOrWhiteSpace(SourceFolder) => "Pick the folder to convert.",
		ConversionSourceType.Folder when _folderFileCount is null => "The source folder does not exist.",
		ConversionSourceType.Folder when _folderFileCount == 0 => $"The source folder has no {FileTypeLabel} files in it.",
		ConversionSourceType.Files when SourceFiles.Count == 0 => $"Pick at least one {FileTypeLabel} file to convert.",
		_ => null,
	};

	/// <inheritdoc />
	public sealed override IReadOnlyDictionary<string, string> BuildSettingsBlock(string outputDirectory, bool addFeBuddyOutputFolder)
	{
		Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputDirectory"] = outputDirectory,
			["AddFeBuddyOutputFolder"] = YesNo(addFeBuddyOutputFolder),
			["CoordinatePrecision"] = OutputPreferences.CoordinatePrecision.ToString(CultureInfo.InvariantCulture),
			["IncludeCrcLineDefaults"] = YesNo(IncludeCrcLineDefaults),
		};

		if (_sourceType == ConversionSourceType.Folder)
		{
			settings["SourceFolder"] = SourceFolder.Trim();
		}
		else
		{
			settings["SourceFiles"] = string.Join(ConversionSettingsReader.SourceFileSeparator, SourceFiles.Select(f => f.Path));
		}

		if (EmitSymbols)
		{
			settings["IncludeCrcSymbolDefaults"] = YesNo(IncludeCrcSymbolDefaults);
		}

		if (EmitText)
		{
			settings["IncludeCrcTextDefaults"] = YesNo(IncludeCrcTextDefaults);
		}

		// Only the rows whose Include box is ticked: the parser requires exactly those.
		foreach (EramClassDefault row in CrcRows().Where(IsCrcRowNeeded))
		{
			CrcDefaultsRowIo.AddToSettingsBlock(row, settings);
		}

		AddOwnSettings(settings);
		return settings;
	}

	/// <inheritdoc />
	public sealed override ConversionRunOutcome DescribeRun(ServiceResult result)
	{
		ConversionServiceResult conversion = (ConversionServiceResult)result;

		string summary = $"{conversion.SourceFileCount:N0} file(s) read, {conversion.GeojsonFilesWritten.Count:N0} GeoJSON file(s) written";

		if (DescribeDetail(conversion) is { } detail)
		{
			summary += $", {detail}";
		}

		if (conversion.FailedCount > 0)
		{
			summary += $", {conversion.FailedCount:N0} failed";
		}

		return new ConversionRunOutcome(
			summary,
			new SubServiceRunResult(Title, summary, conversion.Messages),
			conversion.GeojsonFilesWritten,
			Directory.Exists(conversion.OutputDirectory) ? conversion.OutputDirectory : null);
	}

	/// <summary>
	/// Passes the library's progress straight on, on the thread it arrives on, for a derived
	/// tab's <see cref="ConversionTabViewModel.Execute"/>.
	/// </summary>
	/// <param name="reportStep">The screen's step reporter.</param>
	/// <returns>The progress sink to hand the library.</returns>
	/// <remarks>
	/// <see cref="Progress{T}"/> would post each report to the thread pool, which can reorder
	/// them; the screen's callback already marshals to the UI thread in order.
	/// </remarks>
	protected static IProgress<ConversionProgress> StepProgress(Action<string, string, bool> reportStep) =>
		new StepReporter(reportStep);

	// ================= save contract =================

	/// <inheritdoc />
	protected sealed override void LoadFromConfig()
	{
		_sourceType = string.Equals(Get("SourceType")?.Trim(), nameof(ConversionSourceType.Files), StringComparison.OrdinalIgnoreCase)
			? ConversionSourceType.Files
			: ConversionSourceType.Folder;
		_sourceFolder = Get("SourceFolder") ?? string.Empty;
		_includeCrcLineDefaults = GetBool("IncludeCrcLineDefaults", true);
		_includeCrcSymbolDefaults = GetBool("IncludeCrcSymbolDefaults", true);
		_includeCrcTextDefaults = GetBool("IncludeCrcTextDefaults", true);

		foreach (EramClassDefault row in CrcRows())
		{
			CrcDefaultsRowIo.Load(row, CrcConfigPrefix(row), Get);
		}

		LoadOwnSettings();

		foreach (string name in new[]
		{
			nameof(SourceIsFolder), nameof(SourceIsFiles), nameof(SourceFolder),
			nameof(IncludeCrcLineDefaults), nameof(IncludeCrcSymbolDefaults), nameof(IncludeCrcTextDefaults),
		})
		{
			OnPropertyChanged(name);
		}

		RefreshFolder();
		ClearDirty();
	}

	/// <inheritdoc />
	protected sealed override void WriteToConfig()
	{
		Set("SourceType", _sourceType.ToString());
		Set("SourceFolder", SourceFolder);
		Set("IncludeCrcLineDefaults", YesNo(IncludeCrcLineDefaults));

		if (EmitSymbols)
		{
			Set("IncludeCrcSymbolDefaults", YesNo(IncludeCrcSymbolDefaults));
		}

		if (EmitText)
		{
			Set("IncludeCrcTextDefaults", YesNo(IncludeCrcTextDefaults));
		}

		foreach (EramClassDefault row in CrcRows())
		{
			CrcDefaultsRowIo.Save(row, CrcConfigPrefix(row), Set);
		}

		SaveOwnSettings();
	}

	/// <inheritdoc />
	protected sealed override void Validate(ServiceValidation validation)
	{
		ValidateOwnSettings(validation);

		foreach (EramClassDefault row in CrcRows())
		{
			row.IsRequired = IsCrcRowNeeded(row);
		}

		if (CrcRows().Any(row => row.IsRequired && row.HasMissingValues))
		{
			validation.Add(CrcDefaultsIncompleteMessage);
		}
	}

	/// <summary>Restores the tab's own settings from config, without marking it dirty. The base has none.</summary>
	protected virtual void LoadOwnSettings()
	{
	}

	/// <summary>Writes the tab's own settings with <c>Set</c>. The base has none.</summary>
	protected virtual void SaveOwnSettings()
	{
	}

	/// <summary>Checks the tab's own settings. The base has none.</summary>
	/// <param name="validation">The collector to add failures to.</param>
	protected virtual void ValidateOwnSettings(ServiceValidation validation)
	{
	}

	/// <summary>Adds the tab's own keys to the settings block. The base adds none.</summary>
	/// <param name="settings">The settings block being built.</param>
	protected virtual void AddOwnSettings(Dictionary<string, string> settings)
	{
	}

	/// <summary>
	/// One more thing the run summary should say, e.g. how many lines were written, or
	/// <see langword="null"/> for nothing.
	/// </summary>
	/// <param name="result">The run's result.</param>
	/// <returns>The detail, or <see langword="null"/>.</returns>
	/// <remarks>The base gives the feature count for a conversion that writes several files per source.</remarks>
	protected virtual string? DescribeDetail(ConversionServiceResult result) =>
		result is SourceFilesConversionResult files ? $"{files.FeaturesWritten:N0} feature(s)" : null;

	// ================= private =================

	private IEnumerable<EramClassDefault> CrcRows() => LineDefaults.Concat(SymbolDefaults).Concat(TextDefaults);

	private bool IsCrcRowNeeded(EramClassDefault row) => UsesCrcDefaults && row.Kind switch
	{
		EramFieldKind.Line => IncludeCrcLineDefaults,
		EramFieldKind.Symbol => IncludeCrcSymbolDefaults,
		_ => IncludeCrcTextDefaults,
	};

	private static string CrcConfigPrefix(EramClassDefault row) => $"CrcEramPropertyDefaults.{row.ClassName}_{row.Kind}";

	private void SetSourceType(ConversionSourceType type)
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

	/// <summary>Counts the matching files in <see cref="SourceFolder"/> and refreshes what depends on the count.</summary>
	private void RefreshFolder()
	{
		string folder = SourceFolder.Trim();

		try
		{
			_folderFileCount = folder.Length > 0 && Directory.Exists(folder)
				? Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly)
					.Count(path => Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
				: null;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			_folderFileCount = null;
		}

		OnPropertyChanged(nameof(FolderSummary));
		RaiseRunBlockerChanged();
	}

	private void BrowseFolder()
	{
		OpenFolderDialog dialog = new()
		{
			Title = $"Select the folder of {FileTypeLabel} files to convert",
			InitialDirectory = Directory.Exists(SourceFolder) ? SourceFolder : null,
		};

		if (dialog.ShowDialog() == true)
		{
			SourceFolder = dialog.FolderName;
		}
	}

	private void BrowseFiles()
	{
		string patterns = string.Join(';', Extensions.Select(extension => $"*{extension}"));

		OpenFileDialog dialog = new()
		{
			Title = $"Select the {FileTypeLabel} files to convert",
			Filter = $"{FileDescription} ({patterns})|{patterns}|All files (*.*)|*.*",
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

	private sealed class StepReporter(Action<string, string, bool> reportStep) : IProgress<ConversionProgress>
	{
		public void Report(ConversionProgress value) => reportStep(value.FileName, value.Message, value.IsComplete);
	}
}
