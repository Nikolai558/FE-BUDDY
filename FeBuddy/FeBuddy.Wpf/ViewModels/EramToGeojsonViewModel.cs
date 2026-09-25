using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Conversions.EramToGeojson;
using FeBuddy.Core.Application.Conversions.EramToGeojson.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Eram;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>ERAM to GeoJSON</b> tab on the File Conversions screen: converts the
/// <c>Geomaps.xml</c> of an ERAM adaptation export into CRC-ready GeoJSON, a folder per GeoMap,
/// laid out by object type and map group or by filter index and similar attributes.
/// </summary>
/// <remarks>
/// The source, save contract and run description come from <see cref="FileConversionTabViewModel"/>.
/// This tab adds the output layout and where the CRC defaults come from; the CRC ERAM Defaults
/// card (Lines, Symbols and Text) is in use only when the tab's defaults are one of the sources.
/// A source folder may be the whole unzipped export: only its Geomaps file is counted and converted.
/// </remarks>
public sealed class EramToGeojsonViewModel : FileConversionTabViewModel
{
	private const string Node = "Services.FileConversions.EramToGeojson";

	private EramOutputLayout _outputLayout = EramOutputLayout.ByObject;
	private EramDefaultsSource _defaultsSource = EramDefaultsSource.Xml;

	/// <summary>Builds the tab and restores its saved settings.</summary>
	public EramToGeojsonViewModel()
		: base(EramToGeojsonSettingsParser.CrcClassName, writesSymbols: true, writesText: true)
	{
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
	protected override IReadOnlyList<string> Extensions => EramToGeojsonService.Extensions;

	/// <inheritdoc />
	protected override string FileDescription => "ERAM Geomaps";

	/// <inheritdoc />
	/// <remarks>Only a Geomaps file: the rest of an adaptation export is XML too.</remarks>
	protected override bool IsSourceFile(string path) => EramGeoMapReader.IsGeoMapsFile(path);

	// ================= output layout =================

	/// <summary>Whether each object gets its own file, named <c>&lt;MapObjectType&gt;_&lt;MapGroupId&gt;</c>.</summary>
	public bool LayoutByObject
	{
		get => _outputLayout == EramOutputLayout.ByObject;
		set { if (value) SetOutputLayout(EramOutputLayout.ByObject); }
	}

	/// <summary>Whether files are grouped by filter index and similar attributes.</summary>
	public bool LayoutByFilter
	{
		get => _outputLayout == EramOutputLayout.ByFilter;
		set { if (value) SetOutputLayout(EramOutputLayout.ByFilter); }
	}

	// ================= CRC defaults source =================

	/// <summary>Whether as much as possible is carried over from the XML.</summary>
	public bool DefaultsFromXml
	{
		get => _defaultsSource == EramDefaultsSource.Xml;
		set { if (value) SetDefaultsSource(EramDefaultsSource.Xml); }
	}

	/// <summary>Whether the XML's defaults are used, with the card filling in where an object has none.</summary>
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

	// ================= run =================

	/// <inheritdoc />
	public override ServiceResult Execute(IReadOnlyDictionary<string, string> settings, Action<string, string, bool> reportStep) =>
		EramToGeojsonService.Run(settings, StepProgress(reportStep));

	/// <inheritdoc />
	protected override void LoadOwnSettings()
	{
		_outputLayout = Parse(Get("OutputLayout"), EramOutputLayout.ByObject);
		_defaultsSource = Parse(Get("DefaultsSource"), EramDefaultsSource.Xml);
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
			nameof(LayoutByObject), nameof(LayoutByFilter),
			nameof(DefaultsFromXml), nameof(DefaultsFromXmlThenCard), nameof(DefaultsFromCard), nameof(UsesCrcDefaults),
			nameof(HasCrcDefaultsInUse),
		})
		{
			OnPropertyChanged(name);
		}
	}
}
