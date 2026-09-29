using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.Views;

using FeBuddy.Core.Application.Conversions.EramToGeojson;
using FeBuddy.Core.Application.Conversions.EramToGeojson.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Eram;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>ERAM to GeoJSON</b> tab on the File Conversions screen: converts the
/// <c>Geomaps.xml</c> of an ERAM adaptation export into CRC-ready GeoJSON in
/// <c>ERAM_TO_GEOJSON</c>, in the original ERAM_2_GEOJSON tool's three layouts - By Filters, By
/// Attributes and Raw - with its names, plus its <c>ConsoleCommandControl.txt</c> rundown of the
/// export's map menus when <c>ConsoleCommandControl.xml</c> is beside the Geomaps file.
/// </summary>
/// <remarks>
/// <para>
/// The source, save contract and run description come from <see cref="FileConversionTabViewModel"/>.
/// This tab adds the output layout, where the CRC defaults come from (the CRC ERAM Defaults card
/// is in use only when the tab's defaults are one of the sources) and the <c>feb.*</c> properties.
/// </para>
/// <para>
/// One Geomaps file per run (<see cref="OneSourceFileOnly"/>). A source folder may be the whole
/// unzipped export: only its Geomaps file is counted and converted. Each run empties
/// <c>ERAM_TO_GEOJSON</c> first, so the tab asks before a run that would delete anything
/// (<see cref="ConfirmRun"/>).
/// </para>
/// </remarks>
public sealed class EramToGeojsonViewModel : FileConversionTabViewModel, IFebPropertySettings
{
	private const string Node = "Services.FileConversions.EramToGeojson";

	/// <summary>The layouts before these three, as the tab saved them, and the one each is now read as.</summary>
	private static readonly IReadOnlyDictionary<string, EramOutputLayout> RetiredLayouts =
		new Dictionary<string, EramOutputLayout>(StringComparer.OrdinalIgnoreCase)
		{
			["ByFilter"] = EramOutputLayout.ByFilters,
			["ByObject"] = EramOutputLayout.ByAttributes,
		};

	private EramOutputLayout _outputLayout = EramOutputLayout.ByAttributes;
	private EramDefaultsSource _defaultsSource = EramDefaultsSource.Xml;
	private bool _includeFebCustomProperties;

	/// <summary>Builds the tab and restores its saved settings.</summary>
	public EramToGeojsonViewModel()
		: base(EramToGeojsonSettingsParser.CrcClassName, writesSymbols: true, writesText: true)
	{
		FebProperties = FebPropertyToggle.ListFor(EramFebPropertyOptions.All, MarkDirty);
		LoadFromConfig();
	}

	/// <inheritdoc />
	public override string NodePath => Node;

	/// <inheritdoc />
	public override string Title => "ERAM to GeoJSON";

	/// <inheritdoc />
	public override string RunLabel => "Convert GeoMaps";

	/// <inheritdoc />
	public override string FileTypeLabel => "Geomaps";

	/// <inheritdoc />
	/// <remarks>The maps' folders and files are named after the maps alone, so two files' maps could land on each other.</remarks>
	public override bool OneSourceFileOnly => true;

	/// <inheritdoc />
	protected override IReadOnlyList<string> Extensions => EramToGeojsonService.Extensions;

	/// <inheritdoc />
	protected override string FileDescription => "ERAM Geomaps";

	/// <inheritdoc />
	/// <remarks>Only a Geomaps file: the rest of an adaptation export is XML too.</remarks>
	protected override bool IsSourceFile(string path) => EramGeoMapReader.IsGeoMapsFile(path);

	// ================= output layout =================

	/// <summary>A folder per set of filters, holding its Lines, Symbols and Text files.</summary>
	public bool LayoutByFilters
	{
		get => _outputLayout == EramOutputLayout.ByFilters;
		set { if (value) SetOutputLayout(EramOutputLayout.ByFilters); }
	}

	/// <summary>A file per shared look, named after it.</summary>
	public bool LayoutByAttributes
	{
		get => _outputLayout == EramOutputLayout.ByAttributes;
		set { if (value) SetOutputLayout(EramOutputLayout.ByAttributes); }
	}

	/// <summary>One file per map, every Feature carrying its own properties.</summary>
	public bool LayoutRaw
	{
		get => _outputLayout == EramOutputLayout.Raw;
		set { if (value) SetOutputLayout(EramOutputLayout.Raw); }
	}

	// ================= CRC defaults source =================

	/// <summary>Whether as much as possible is carried over from the XML.</summary>
	public bool DefaultsFromXml
	{
		get => _defaultsSource == EramDefaultsSource.Xml;
		set { if (value) SetDefaultsSource(EramDefaultsSource.Xml); }
	}

	/// <summary>Whether the XML's defaults are used, with the card filling in what they leave out.</summary>
	public bool DefaultsFromXmlThenCard
	{
		get => _defaultsSource == EramDefaultsSource.XmlThenCard;
		set { if (value) SetDefaultsSource(EramDefaultsSource.XmlThenCard); }
	}

	/// <summary>Whether only the card's defaults are used.</summary>
	public bool DefaultsFromCard
	{
		get => _defaultsSource == EramDefaultsSource.Card;
		set { if (value) SetDefaultsSource(EramDefaultsSource.Card); }
	}

	/// <inheritdoc />
	/// <remarks>Not when everything is carried over from the XML.</remarks>
	public override bool UsesCrcDefaults => _defaultsSource != EramDefaultsSource.Xml;

	// ================= FE-Buddy properties =================

	/// <inheritdoc />
	public bool IncludeFebCustomProperties
	{
		get => _includeFebCustomProperties;
		set { if (SetProperty(ref _includeFebCustomProperties, value)) MarkDirty(); }
	}

	/// <inheritdoc />
	public ObservableCollection<FebPropertyToggle> FebProperties { get; }

	// ================= run =================

	/// <inheritdoc />
	/// <remarks>
	/// Each run empties <c>ERAM_TO_GEOJSON</c>, as the original tool emptied its output folder;
	/// with anything in it, the user says so first.
	/// </remarks>
	public override bool ConfirmRun(IReadOnlyDictionary<string, string> settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		string folder = EramGeojsonWriter.OutputDirectory(
			settings["OutputDirectory"], string.Equals(settings["AddFeBuddyOutputFolder"], "Y", StringComparison.OrdinalIgnoreCase));

		if (!Directory.Exists(folder) || !Directory.EnumerateFileSystemEntries(folder).Any())
		{
			return true;
		}

		return ConfirmWindow.Show(
			Application.Current?.MainWindow,
			$"Empty {EramGeojsonWriter.RootFolder} first?",
			$"`{folder}` has files from an earlier run. Everything in it is deleted before this run, so it holds only " +
			"this run's files. Move anything you want to keep out of it first.",
			confirmText: "Delete and convert");
	}

	/// <inheritdoc />
	public override ServiceResult Execute(IReadOnlyDictionary<string, string> settings, Action<string, string, bool> reportStep) =>
		EramToGeojsonService.Run(settings, StepProgress(reportStep));

	/// <inheritdoc />
	protected override void LoadOwnSettings()
	{
		string? layout = Get("OutputLayout")?.Trim();
		_outputLayout = layout is not null && RetiredLayouts.TryGetValue(layout, out EramOutputLayout nearest)
			? nearest
			: Parse(layout, EramOutputLayout.ByAttributes);
		_defaultsSource = Parse(Get("DefaultsSource"), EramDefaultsSource.Xml);
		_includeFebCustomProperties = GetBool("IncludeFebCustomProperties", false);

		HashSet<string> selected = ParseList(Get("FebProperties"));
		foreach (FebPropertyToggle toggle in FebProperties)
		{
			toggle.IsSelected = selected.Contains(toggle.Name);
		}

		OnPropertyChanged(nameof(IncludeFebCustomProperties));
		RaiseChoices();
	}

	/// <inheritdoc />
	protected override void SaveOwnSettings()
	{
		Set("OutputLayout", _outputLayout.ToString());
		Set("DefaultsSource", _defaultsSource.ToString());
		Set("IncludeFebCustomProperties", YesNo(IncludeFebCustomProperties));
		Set("FebProperties", SelectedFebPropertyNames());
	}

	/// <inheritdoc />
	protected override void ValidateOwnSettings(ServiceValidation validation)
	{
		if (IncludeFebCustomProperties && FebProperties.All(p => !p.IsSelected))
		{
			validation.Add("FE-Buddy properties are on but none are selected. Pick at least one, or switch them off.");
		}
	}

	/// <inheritdoc />
	protected override void AddOwnSettings(Dictionary<string, string> settings)
	{
		settings["OutputLayout"] = _outputLayout.ToString();
		settings["DefaultsSource"] = _defaultsSource.ToString();
		settings["IncludeFebCustomProperties"] = YesNo(IncludeFebCustomProperties);
		settings["FebProperties"] = SelectedFebPropertyNames();
	}

	// ================= helpers =================

	/// <summary>A saved choice by name; anything else (blank, a typo) falls back to the default.</summary>
	private static T Parse<T>(string? saved, T fallback) where T : struct, Enum =>
		Enum.GetValues<T>().FirstOrDefault(value => value.ToString().Equals(saved?.Trim(), StringComparison.OrdinalIgnoreCase), fallback);

	private string SelectedFebPropertyNames() =>
		string.Join(',', FebProperties.Where(p => p.IsSelected).Select(p => p.Name));

	private void SetOutputLayout(EramOutputLayout layout)
	{
		if (_outputLayout != layout)
		{
			_outputLayout = layout;
			RaiseChoices();
			MarkDirty();
		}
	}

	private void SetDefaultsSource(EramDefaultsSource source)
	{
		if (_defaultsSource != source)
		{
			_defaultsSource = source;
			RaiseChoices();

			// Re-validates too: the card's values are required only while it is in use.
			MarkDirty();
		}
	}

	private void RaiseChoices()
	{
		foreach (string name in new[]
		{
			nameof(LayoutByFilters), nameof(LayoutByAttributes), nameof(LayoutRaw),
			nameof(DefaultsFromXml), nameof(DefaultsFromXmlThenCard), nameof(DefaultsFromCard), nameof(UsesCrcDefaults),
			nameof(HasCrcDefaultsInUse),
		})
		{
			OnPropertyChanged(name);
		}
	}
}
