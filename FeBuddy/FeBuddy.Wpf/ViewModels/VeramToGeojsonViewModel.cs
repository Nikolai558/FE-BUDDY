using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Conversions.VeramToGeojson;
using FeBuddy.Core.Application.Conversions.VeramToGeojson.Models;
using FeBuddy.Core.Application.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>vERAM to GeoJSON</b> tab on the File Conversions screen: converts vERAM GeoMaps XML
/// files into CRC-ready GeoJSON, a folder per GeoMap, laid out by GeoMapObject description or by
/// filter index and similar attributes.
/// </summary>
/// <remarks>
/// The source, save contract and run description come from <see cref="FileConversionTabViewModel"/>.
/// This tab adds the output layout and where the CRC defaults come from; the CRC ERAM Defaults
/// card (Lines, Symbols and Text) is in use only when the tab's defaults are one of the sources.
/// </remarks>
public sealed class VeramToGeojsonViewModel : FileConversionTabViewModel
{
	private const string Node = "Services.FileConversions.VeramToGeojson";

	private VeramOutputLayout _outputLayout = VeramOutputLayout.ByObject;
	private VeramDefaultsSource _defaultsSource = VeramDefaultsSource.Xml;

	/// <summary>Builds the tab and restores its saved settings.</summary>
	public VeramToGeojsonViewModel()
		: base(VeramToGeojsonSettingsParser.CrcClassName, writesSymbols: true, writesText: true)
	{
		LoadFromConfig();
	}

	/// <inheritdoc />
	public override string NodePath => Node;

	/// <inheritdoc />
	public override string Title => "vERAM to GeoJSON";

	/// <inheritdoc />
	public override string RunLabel => "Convert GeoMaps";

	/// <inheritdoc />
	public override string FileTypeLabel => ".xml";

	/// <inheritdoc />
	protected override IReadOnlyList<string> Extensions => VeramToGeojsonService.Extensions;

	/// <inheritdoc />
	protected override string FileDescription => "vERAM GeoMaps";

	// ================= output layout =================

	/// <summary>Whether each GeoMapObject gets its own file, named after its description.</summary>
	public bool LayoutByObject
	{
		get => _outputLayout == VeramOutputLayout.ByObject;
		set { if (value) SetOutputLayout(VeramOutputLayout.ByObject); }
	}

	/// <summary>Whether files are grouped by filter index and similar attributes.</summary>
	public bool LayoutByFilter
	{
		get => _outputLayout == VeramOutputLayout.ByFilter;
		set { if (value) SetOutputLayout(VeramOutputLayout.ByFilter); }
	}

	// ================= CRC defaults source =================

	/// <summary>Whether as much as possible is carried over from the XML.</summary>
	public bool DefaultsFromXml
	{
		get => _defaultsSource == VeramDefaultsSource.Xml;
		set { if (value) SetDefaultsSource(VeramDefaultsSource.Xml); }
	}

	/// <summary>Whether the XML's defaults are used, with the card filling in where an object has none.</summary>
	public bool DefaultsFromXmlThenCard
	{
		get => _defaultsSource == VeramDefaultsSource.XmlThenCard;
		set { if (value) SetDefaultsSource(VeramDefaultsSource.XmlThenCard); }
	}

	/// <summary>Whether only the card's defaults are used.</summary>
	public bool DefaultsFromCard
	{
		get => _defaultsSource == VeramDefaultsSource.Card;
		set { if (value) SetDefaultsSource(VeramDefaultsSource.Card); }
	}

	/// <inheritdoc />
	/// <remarks>Not when everything is carried over from the XML.</remarks>
	public override bool UsesCrcDefaults => _defaultsSource != VeramDefaultsSource.Xml;

	// ================= run =================

	/// <inheritdoc />
	public override ServiceResult Execute(IReadOnlyDictionary<string, string> settings, Action<string, string, bool> reportStep) =>
		VeramToGeojsonService.Run(settings, StepProgress(reportStep));

	/// <inheritdoc />
	protected override void LoadOwnSettings()
	{
		_outputLayout = Parse(Get("OutputLayout"), VeramOutputLayout.ByObject);
		_defaultsSource = Parse(Get("DefaultsSource"), VeramDefaultsSource.Xml);
		RaiseChoices();
	}

	/// <inheritdoc />
	protected override void SaveOwnSettings()
	{
		Set("OutputLayout", _outputLayout.ToString());
		Set("DefaultsSource", _defaultsSource.ToString());
	}

	/// <inheritdoc />
	protected override void AddOwnSettings(Dictionary<string, string> settings)
	{
		settings["OutputLayout"] = _outputLayout.ToString();
		settings["DefaultsSource"] = _defaultsSource.ToString();
	}

	// ================= helpers =================

	/// <summary>A saved choice by name; anything else (blank, a typo) falls back to the default.</summary>
	private static T Parse<T>(string? saved, T fallback) where T : struct, Enum =>
		Enum.GetValues<T>().FirstOrDefault(value => value.ToString().Equals(saved?.Trim(), StringComparison.OrdinalIgnoreCase), fallback);

	private void SetOutputLayout(VeramOutputLayout layout)
	{
		if (_outputLayout != layout)
		{
			_outputLayout = layout;
			RaiseChoices();
			MarkDirty();
		}
	}

	private void SetDefaultsSource(VeramDefaultsSource source)
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
			nameof(LayoutByObject), nameof(LayoutByFilter),
			nameof(DefaultsFromXml), nameof(DefaultsFromXmlThenCard), nameof(DefaultsFromCard), nameof(UsesCrcDefaults),
		})
		{
			OnPropertyChanged(name);
		}
	}
}
